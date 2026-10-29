using MaghaleNegar.Interfaces;
using MaghaleNegar.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MaghaleNegar.Services
{
    public sealed class LocalAuthorStore : IAuthorStore
    {
        private static readonly object _lock = new object();
        private readonly string _filePath;

        public LocalAuthorStore(string userKey)
        {
            if (string.IsNullOrWhiteSpace(userKey))
                userKey = "default";

            foreach (char c in Path.GetInvalidFileNameChars())
                userKey = userKey.Replace(c, '_');

            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MaghaleNegar", "Authors");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "authors_" + userKey + ".json");
        }

        public IReadOnlyList<AuthorRecord> GetAll()
        {
            lock (_lock) { return Load(); }
        }

        public AuthorRecord FindByName(string authorName)
        {
            string key = Normalize(authorName);
            if (key.Length == 0) return null;
            lock (_lock)
            {
                return Load().FirstOrDefault(a => Normalize(a.AuthorName) == key);
            }
        }

        public void Save(AuthorRecord author)
        {
            if (author == null) throw new ArgumentNullException(nameof(author));
            string key = Normalize(author.AuthorName);
            if (key.Length == 0) return;

            lock (_lock)
            {
                var all = Load();
                var existing = all.FirstOrDefault(a => Normalize(a.AuthorName) == key);

                if (existing != null)
                {
                    author.AuthorId = existing.AuthorId;
                    author.CreatedAt = existing.CreatedAt;
                    all[all.IndexOf(existing)] = author;
                }
                else
                {
                    all.Add(author);
                }

                author.UpdatedAt = DateTime.UtcNow;
                author.IsSynced = false;
                Write(all);
            }
        }

        // ===== همگام‌سازی با سرور =====

        // آیا تغییری هست که هنوز به سرور نرفته؟
        public bool HasUnsynced()
        {
            lock (_lock) { return Load().Any(a => !a.IsSynced); }
        }

        // بعد از ارسال موفق به سرور صدا زده می‌شود
        public void MarkAllSynced()
        {
            lock (_lock)
            {
                var all = Load();
                if (all.Count == 0) return;
                foreach (var a in all) a.IsSynced = true;
                Write(all);
            }
        }

        // نتیجه‌ی دریافت از سرور را در همان فایل محلی ذخیره می‌کند.
        // - اگر تغییر ارسال‌نشده نداریم: سرور مرجع است و فایل جایگزین می‌شود.
        // - اگر تغییر ارسال‌نشده داریم: ادغام؛ نسخه‌ی محلیِ ارسال‌نشده هیچ‌وقت پاک نمی‌شود.
        public void MergeFromServer(IList<AuthorRecord> server)
        {
            if (server == null) return;
            var valid = server.Where(s => s != null && Normalize(s.AuthorName).Length > 0).ToList();
            if (valid.Count == 0) return; // پاسخ خالی هرگز فایل محلی را پاک نکند

            lock (_lock)
            {
                var local = Load();

                if (!local.Any(a => !a.IsSynced))
                {
                    foreach (var s in valid) s.IsSynced = true;
                    Write(valid);
                    return;
                }

                foreach (var s in valid)
                {
                    s.IsSynced = true;
                    string key = Normalize(s.AuthorName);
                    var mine = local.FirstOrDefault(a => Normalize(a.AuthorName) == key);

                    if (mine == null)
                        local.Add(s);
                    else if (mine.IsSynced && s.UpdatedAt > mine.UpdatedAt)
                        local[local.IndexOf(mine)] = s;
                }
                Write(local);
            }
        }

        public bool Delete(string authorId)
        {
            lock (_lock)
            {
                var all = Load();
                int removed = all.RemoveAll(a => a.AuthorId == authorId);
                if (removed > 0) Write(all);
                return removed > 0;
            }
        }

        private List<AuthorRecord> Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return new List<AuthorRecord>();
                string json = File.ReadAllText(_filePath, Encoding.UTF8);
                return JsonConvert.DeserializeObject<List<AuthorRecord>>(json)
                       ?? new List<AuthorRecord>();
            }
            catch
            {
                try { File.Copy(_filePath, _filePath + ".corrupt", true); } catch { }
                return new List<AuthorRecord>();
            }
        }

        private void Write(List<AuthorRecord> all)
        {
            string json = JsonConvert.SerializeObject(all, Formatting.Indented);
            string tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(false));

            if (File.Exists(_filePath))
                File.Replace(tmp, _filePath, null);
            else
                File.Move(tmp, _filePath);
        }

        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return s.Replace('ي', 'ی').Replace('ك', 'ک').Replace("\u200c", " ")
                    .Trim().Replace("  ", " ");
        }
    }
}