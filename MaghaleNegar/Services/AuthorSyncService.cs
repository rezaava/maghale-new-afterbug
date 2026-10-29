using MaghaleNegar.Constants;
using MaghaleNegar.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MaghaleNegar.Services
{
    /// <summary>
    /// دریافت اطلاعات نویسندگان هنگام باز شدن (newDocInfo)
    /// و ارسال آن‌ها هنگام بسته شدن (saveAuthorJson).
    /// هیچ‌کدام از متدها استثنا بیرون نمی‌دهند.
    /// </summary>
    internal static class AuthorSyncService
    {
        // ---------- دریافت از سرور (غیرهمگام، موقع Startup و بعد از Login) ----------
        public static async Task DownloadAsync()
        {
            try
            {
                string token = Properties.Settings.Default.UserToken;
                string mobile = Properties.Settings.Default.Mobile;
                Log("DOWNLOAD start. tokenEmpty=" + string.IsNullOrWhiteSpace(token) + " mobile='" + mobile + "'");
                if (string.IsNullOrWhiteSpace(token)) { Log("DOWNLOAD skipped: token is empty"); return; }

                using (var client = CreateClient(token, TimeSpan.FromSeconds(15)))
                {
                    var resp = await client.GetAsync("newDocInfo").ConfigureAwait(false);

                    // اگر در routes/api.php این مسیر post تعریف شده باشد، یک بار با POST تلاش می‌کنیم
                    if (resp.StatusCode == HttpStatusCode.MethodNotAllowed)
                        resp = await client.PostAsync("newDocInfo",
                            new StringContent("", Encoding.UTF8, "application/json")).ConfigureAwait(false);

                    string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Log("DOWNLOAD response " + (int)resp.StatusCode + ": " + Cut(body));
                    if (!resp.IsSuccessStatusCode) return;

                    List<AuthorRecord> list = ParseServerResponse(body);
                    if (list == null) { Log("DOWNLOAD: nothing to apply"); return; }

                    new LocalAuthorStore(mobile).MergeFromServer(list);
                    Log("DOWNLOAD merged " + list.Count + " records");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AuthorSync download failed: " + ex.Message);
                Log("DOWNLOAD exception: " + ex);
            }
        }

        // ---------- ارسال به سرور (همگام، موقع Shutdown) ----------
        // عمداً همگام است: در Shutdown نمی‌شود منتظر async void ماند.
        public static void UploadBlocking()
        {
            try
            {
                string token = Properties.Settings.Default.UserToken;
                string mobile = Properties.Settings.Default.Mobile;
                Log("UPLOAD start. tokenEmpty=" + string.IsNullOrWhiteSpace(token) + " mobile='" + mobile + "'");
                if (string.IsNullOrWhiteSpace(token)) { Log("UPLOAD skipped: token is empty"); return; }

                var store = new LocalAuthorStore(mobile);
                IReadOnlyList<AuthorRecord> all = store.GetAll();
                Log("UPLOAD records=" + all.Count + " unsynced=" + all.Count(a => !a.IsSynced));

                // لیست خالی نفرست (سرور کل ستون را بازنویسی می‌کند) و اگر تغییری نیست هم نفرست
                if (all.Count == 0) { Log("UPLOAD skipped: no records"); return; }
                if (!all.Any(a => !a.IsSynced)) { Log("UPLOAD skipped: nothing unsynced"); return; }

                // سرور فیلد author_json را به صورت «رشته» می‌خواهد -> دو بار سریال‌سازی
                string inner = JsonConvert.SerializeObject(all);
                string body = JsonConvert.SerializeObject(new { author_json = inner });

                using (var client = CreateClient(token, TimeSpan.FromSeconds(5)))
                using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
                {
                    // Task.Run برای جلوگیری از deadlock روی thread اصلی
                    HttpResponseMessage resp = Task.Run(() => client.PostAsync("saveAuthorJson", content))
                                                   .GetAwaiter().GetResult();
                    string respBody = Task.Run(() => resp.Content.ReadAsStringAsync())
                                          .GetAwaiter().GetResult();
                    Log("UPLOAD response " + (int)resp.StatusCode + ": " + Cut(respBody));
                    if (!resp.IsSuccessStatusCode) return;

                    if (IsStatusOk(respBody))
                    {
                        store.MarkAllSynced();
                        Log("UPLOAD ok, marked synced");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AuthorSync upload failed: " + ex.Message);
                Log("UPLOAD exception: " + ex);
            }
        }

        // ---------- ابزارها ----------
        // لاگ در %LocalAppData%\MaghaleNegar\authorsync.log (بعد از رفع مشکل می‌توانی حذفش کنی)
        private static void Log(string message)
        {
            try
            {
                string dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MaghaleNegar");
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "authorsync.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch { }
        }

        private static string Cut(string s)
        {
            if (s == null) return "";
            return s.Length > 300 ? s.Substring(0, 300) + "..." : s;
        }

        private static HttpClient CreateClient(string token, TimeSpan timeout)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(StringConstant.PrimaryServerApiBaseAddress), // .../api/
                Timeout = timeout
            };
            // بدون Accept: application/json لاراول در خطا به جای JSON ریدایرکت می‌کند
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static bool IsStatusOk(string body)
        {
            try { return JObject.Parse(body)["status"]?.ToString() == "1"; }
            catch { return false; }
        }

        // خروجی null یعنی «چیزی برای اعمال نیست»
        private static List<AuthorRecord> ParseServerResponse(string body)
        {
            var root = JObject.Parse(body);
            if (root["status"]?.ToString() != "1") return null;

            JToken tok = root["author_json"];
            if (tok == null || tok.Type == JTokenType.Null) return null;

            // معمولاً رشته است؛ اگر سرور آرایه فرستاد هم کار می‌کند
            string json = tok.Type == JTokenType.String ? (string)tok : tok.ToString(Formatting.None);
            if (string.IsNullOrWhiteSpace(json)) return null;

            return JsonConvert.DeserializeObject<List<AuthorRecord>>(json);
        }
    }
}