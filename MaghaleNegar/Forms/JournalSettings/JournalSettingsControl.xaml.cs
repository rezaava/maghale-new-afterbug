using MaghaleNegar.Constants;
using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaghaleNegar.Forms.JournalSettings
{
    public partial class JournalSettingsControl : UserControl, Interfaces.IStatusFormRequest
    {
        private readonly Dictionary<string, string> _original = new Dictionary<string, string>();
        private const string TagJournalName = "JournalName";
        private const string TagIssue = "JournalIssue";
        private const string TagSeason = "JournalSeason";
        private const string TagYear = "JournalYear";
        private const string TagReceived = "JournalReceivedDate";
        private const string TagAccepted = "JournalAcceptedDate";

        private const string DefaultText = "…";
        private const string DefaultDate = "//";
        private const string TagVolume = "JournalVolume";
        private const string VarJournalName = "JournalName";
        //private const string TagIssue = "JournalIssue";
        //private const string TagSeason = "JournalSeason";
        //private const string TagYear = "JournalYear";
        //private const string TagReceived = "JournalReceivedDate";
        //private const string TagAccepted = "JournalAcceptedDate";
        Document doc;
        public Action CloseFormRequest { get; set; }
        public Action NormalStateFormRequest { get; set; }
        public Action MinimizeStateFormRequest { get; set; }
        public Action MaximizeStateFormRequest { get; set; }
        public Action AlwaysOnTopEnableRequest { get; set; }
        public Action AlwaysOnTopDisableRequest { get; set; }

        private string LoadValue(string tag)
        {
            string value = ReadField(tag);
            _original[tag] = value;
            return value;
        }
        public JournalSettingsControl(Document doc)
        {
            InitializeComponent();

            this.doc = doc;

            btnCloseApp.Click += BtnCloseApp_Click;
            btnConfirm.Click += BtnConfirm_Click;

            // ====== ✅ پر کردن اسم نشریه از سند ======
            LoadFields();

        }



        private void LoadJournalName()
        {
            try
            {
                string journalName = GetContentControlText(doc, "JournalName");
                txtJournalTitle.Text = journalName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در خواندن نام نشریه: {ex.Message}");
            }
        }

        private string GetContentControlText(Document doc, string tag)
        {
            try
            {
                if (doc == null) return "";

                foreach (Microsoft.Office.Interop.Word.ContentControl cc in doc.ContentControls)
                {
                    if (cc.Tag == tag)
                    {
                        return cc.Range.Text;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در GetContentControlText برای {tag}: {ex.Message}");
            }
            return "";
        }

        private List<Microsoft.Office.Interop.Word.ContentControl> FindControls(string tag)   // ← اینجا
        {
            var result = new List<Microsoft.Office.Interop.Word.ContentControl>();
            foreach (Microsoft.Office.Interop.Word.ContentControl cc in doc.SelectContentControlsByTag(tag))
                result.Add(cc);
            return result;
        }

        private bool WriteField(string tag, string value, string defaultText)
        {
            List<Microsoft.Office.Interop.Word.ContentControl> controls = FindControls(tag);
            if (controls.Count == 0) return false;

            string text = string.IsNullOrWhiteSpace(value) ? defaultText : value.Trim();
            foreach (Microsoft.Office.Interop.Word.ContentControl cc in controls)
            {
                bool wasLocked = false;
                try
                {
                    wasLocked = cc.LockContents;
                    cc.LockContents = false;
                    cc.Range.Text = text;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"خطا در نوشتن {tag}: {ex.Message}");
                }
                finally
                {
                    try { cc.LockContents = wasLocked; } catch (Exception) { }
                }
            }
            return true;
        }

        private string ReadField(string tag)
        {
            List<Microsoft.Office.Interop.Word.ContentControl> controls = FindControls(tag);
            if (controls.Count == 0) return "";

            string text = "";
            try { text = controls[0].Range.Text; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"خطا در خواندن {tag}: {ex.Message}"); }

            text = (text ?? "").Trim();
            if (text == DefaultText || text == DefaultDate || text.Contains("XX")) return "";
            return text;
        }

        private void LoadFields()
        {
            string journalName = ReadField(TagJournalName);
            if (string.IsNullOrEmpty(journalName))
            {
                journalName = DedicatedFunctions.getStaticVariableValue(doc, VarJournalName);
                if (journalName == SettingValues.NotExist) journalName = "";
            }
            txtJournalTitle.Text = journalName;
            txtJournalTitle.IsReadOnly = true;
            txtVolume.Text = LoadValue(TagVolume);
            txtIssue.Text = LoadValue(TagIssue);
            txtYear.Text = LoadValue(TagYear);
            txtReceivedDate.Text = LoadValue(TagReceived);
            txtAcceptedDate.Text = LoadValue(TagAccepted);

            string season = LoadValue(TagSeason);
            foreach (ComboBoxItem item in cmbSeason.Items)
            {
                if ((item.Content as string) == season)
                {
                    cmbSeason.SelectedItem = item;
                    break;
                }
            }
            if (string.IsNullOrEmpty(season)) cmbSeason.SelectedIndex = -1;
        }

        private void SaveField(string tag, string value, string defaultText)
        {
            string current = (value ?? "").Trim();
            string original;
            _original.TryGetValue(tag, out original);

            if (current == (original ?? "")) return;   // تغییری نکرده؛ به سند دست نزن

            WriteField(tag, current, defaultText);
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string season = (cmbSeason.SelectedItem as ComboBoxItem)?.Content as string ?? "";

            bool allEmpty = string.IsNullOrWhiteSpace(txtVolume.Text)
                         && string.IsNullOrWhiteSpace(txtIssue.Text)
                         && string.IsNullOrWhiteSpace(txtYear.Text)
                         && string.IsNullOrWhiteSpace(txtReceivedDate.Text)
                         && string.IsNullOrWhiteSpace(txtAcceptedDate.Text);
            if (allEmpty)
            {
                lblStatus.Text = "لطفاً اطلاعات را وارد کنید";
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(txtJournalTitle.Text))
                SaveField(TagVolume, txtVolume.Text, DefaultText);
                SaveField(TagIssue, txtIssue.Text, DefaultText);
                SaveField(TagSeason, season, DefaultText);
                SaveField(TagYear, txtYear.Text, DefaultText);
                SaveField(TagReceived, txtReceivedDate.Text, DefaultDate);
                SaveField(TagAccepted, txtAcceptedDate.Text, DefaultDate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در ثبت مشخصات نشریه: {ex.Message}");
                lblStatus.Text = "ثبت اطلاعات با خطا مواجه شد";
                return;
            }

            CloseFormRequest?.Invoke();
        }

        private void BtnCloseApp_Click(object sender, RoutedEventArgs e)
        {
            CloseFormRequest?.Invoke();
        }

        public static T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                T childType = child as T;
                if (childType == null)
                {
                    foundChild = FindChild<T>(child, childName);
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }
            return foundChild;
        }
    }
}