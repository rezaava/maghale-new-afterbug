using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;

namespace MaghaleNegar.Forms.JournalSettings
{


    public partial class JournalSettingsControl : UserControl, Interfaces.IStatusFormRequest
    {
        private const string TagJournalName = "JournalName";
        private const string TagIssue = "JournalIssue";
        private const string TagSeason = "JournalSeason";
        private const string TagYear = "JournalYear";
        private const string TagReceived = "JournalReceivedDate";
        private const string TagAccepted = "JournalAcceptedDate";

        private const string DefaultText = "…";
        private const string DefaultDate = "//";
        private const string TagVolume = "JournalVolume";
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

        public JournalSettingsControl(Document doc)
        {
            InitializeComponent();

            this.doc = doc;

            btnCloseApp.Click += BtnCloseApp_Click;
            btnConfirm.Click += BtnConfirm_Click;

            // ====== ✅ پر کردن اسم نشریه از سند ======
            LoadJournalName();
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
            txtJournalTitle.Text = ReadField(TagJournalName);
            txtVolume.Text = ReadField(TagVolume);
            txtIssue.Text = ReadField(TagIssue);
            txtYear.Text = ReadField(TagYear);
            txtReceivedDate.Text = ReadField(TagReceived);
            txtAcceptedDate.Text = ReadField(TagAccepted);

            string season = ReadField(TagSeason);
            foreach (ComboBoxItem item in cmbSeason.Items)
            {
                if ((item.Content as string) == season)
                {
                    cmbSeason.SelectedItem = item;
                    break;
                }
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string season = (cmbSeason.SelectedItem as ComboBoxItem)?.Content as string ?? "";

            bool allEmpty = string.IsNullOrWhiteSpace(txtVolume.Text)
                         && string.IsNullOrWhiteSpace(txtIssue.Text)
                         && string.IsNullOrWhiteSpace(txtYear.Text)
                         && string.IsNullOrWhiteSpace(txtReceivedDate.Text)
                         && string.IsNullOrWhiteSpace(txtAcceptedDate.Text)
                         && string.IsNullOrWhiteSpace(txtJournalTitle.Text);
            if (allEmpty)
            {
                lblStatus.Text = "لطفاً اطلاعات را وارد کنید";
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(txtJournalTitle.Text))
                    WriteField(TagJournalName, txtJournalTitle.Text, txtJournalTitle.Text);

                WriteField(TagVolume, txtVolume.Text, DefaultText);
                WriteField(TagIssue, txtIssue.Text, DefaultText);
                WriteField(TagSeason, season, DefaultText);
                WriteField(TagYear, txtYear.Text, DefaultText);
                WriteField(TagReceived, txtReceivedDate.Text, DefaultDate);
                WriteField(TagAccepted, txtAcceptedDate.Text, DefaultDate);
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