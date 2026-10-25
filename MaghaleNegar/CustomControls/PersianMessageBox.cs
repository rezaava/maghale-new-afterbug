using MaghaleNegar.CustomControls;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using WinForms = System.Windows.Forms;

namespace MaghaleNegar
{
    internal static class PersianMessageBox
    {
        // ---------- WinForms style ----------
        public static WinForms.DialogResult Show(string text)
            => Show(text, "", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.None);

        public static WinForms.DialogResult Show(string text, string caption)
            => Show(text, caption, WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.None);

        public static WinForms.DialogResult Show(string text, string caption, WinForms.MessageBoxButtons buttons)
            => Show(text, caption, buttons, WinForms.MessageBoxIcon.None);

        public static WinForms.DialogResult Show(string text, string caption, WinForms.MessageBoxButtons buttons, WinForms.MessageBoxIcon icon)
            => Show(text, caption, buttons, icon, WinForms.MessageBoxDefaultButton.Button1);

        public static WinForms.DialogResult Show(string text, string caption, WinForms.MessageBoxButtons buttons, WinForms.MessageBoxIcon icon, WinForms.MessageBoxDefaultButton defaultButton)
        {
            MessageDialogButtons dialogButtons;
            switch (buttons)
            {
                case WinForms.MessageBoxButtons.OKCancel: dialogButtons = MessageDialogButtons.OkCancel; break;
                case WinForms.MessageBoxButtons.RetryCancel: dialogButtons = MessageDialogButtons.RetryCancel; break;
                case WinForms.MessageBoxButtons.YesNo: dialogButtons = MessageDialogButtons.YesNo; break;
                case WinForms.MessageBoxButtons.YesNoCancel: dialogButtons = MessageDialogButtons.YesNoCancel; break;
                case WinForms.MessageBoxButtons.AbortRetryIgnore: dialogButtons = MessageDialogButtons.AbortRetryIgnore; break;
                default: dialogButtons = MessageDialogButtons.Ok; break;
            }

            MessageDialogIcon dialogIcon;
            switch (icon)
            {
                case WinForms.MessageBoxIcon.Error: dialogIcon = MessageDialogIcon.Error; break;
                case WinForms.MessageBoxIcon.Warning: dialogIcon = MessageDialogIcon.Warning; break;
                case WinForms.MessageBoxIcon.Question: dialogIcon = MessageDialogIcon.Question; break;
                case WinForms.MessageBoxIcon.Information: dialogIcon = MessageDialogIcon.Information; break;
                default: dialogIcon = GuessIcon(text); break;
            }

            int defaultIndex = 0;
            if (defaultButton == WinForms.MessageBoxDefaultButton.Button2) defaultIndex = 1;
            else if (defaultButton == WinForms.MessageBoxDefaultButton.Button3) defaultIndex = 2;

            return ShowCore(text, dialogButtons, dialogIcon, defaultIndex);
        }

        // ---------- WPF style ----------
        public static MessageBoxResult Show(string text, string caption, MessageBoxButton buttons)
            => Show(text, caption, buttons, MessageBoxImage.None, MessageBoxResult.None);

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
            => Show(text, caption, buttons, icon, MessageBoxResult.None);

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            MessageDialogButtons dialogButtons;
            switch (buttons)
            {
                case MessageBoxButton.OKCancel: dialogButtons = MessageDialogButtons.OkCancel; break;
                case MessageBoxButton.YesNo: dialogButtons = MessageDialogButtons.YesNo; break;
                case MessageBoxButton.YesNoCancel: dialogButtons = MessageDialogButtons.YesNoCancel; break;
                default: dialogButtons = MessageDialogButtons.Ok; break;
            }

            MessageDialogIcon dialogIcon;
            switch (icon)
            {
                case MessageBoxImage.Error: dialogIcon = MessageDialogIcon.Error; break;
                case MessageBoxImage.Warning: dialogIcon = MessageDialogIcon.Warning; break;
                case MessageBoxImage.Question: dialogIcon = MessageDialogIcon.Question; break;
                case MessageBoxImage.Information: dialogIcon = MessageDialogIcon.Information; break;
                default: dialogIcon = GuessIcon(text); break;
            }

            int defaultIndex = 0;
            if (defaultResult == MessageBoxResult.No || defaultResult == MessageBoxResult.Cancel)
                defaultIndex = 1;
            if (dialogButtons == MessageDialogButtons.YesNoCancel && defaultResult == MessageBoxResult.Cancel)
                defaultIndex = 2;

            WinForms.DialogResult result = ShowCore(text, dialogButtons, dialogIcon, defaultIndex);
            switch (result)
            {
                case WinForms.DialogResult.OK: return MessageBoxResult.OK;
                case WinForms.DialogResult.Yes: return MessageBoxResult.Yes;
                case WinForms.DialogResult.No: return MessageBoxResult.No;
                case WinForms.DialogResult.Cancel: return MessageBoxResult.Cancel;
                default: return MessageBoxResult.None;
            }
        }

        // ---------- Core ----------
        private static MessageDialogIcon GuessIcon(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (text.Contains("خطا") || text.Contains("❌")) return MessageDialogIcon.Error;
                if (text.Contains("⚠")) return MessageDialogIcon.Warning;
            }
            return MessageDialogIcon.Information;
        }

        private static WinForms.DialogResult ShowCore(string text, MessageDialogButtons buttons, MessageDialogIcon icon, int defaultButtonIndex)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                return ShowOnCurrentThread(text, buttons, icon, defaultButtonIndex, true);

            WinForms.DialogResult result = WinForms.DialogResult.None;
            var thread = new Thread(() =>
            {
                try { result = ShowOnCurrentThread(text, buttons, icon, defaultButtonIndex, false); }
                catch (Exception ex) { Debug.WriteLine("PersianMessageBox: " + ex); }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            thread.Join();
            return result;
        }

        private static WinForms.DialogResult ShowOnCurrentThread(string text, MessageDialogButtons buttons, MessageDialogIcon icon, int defaultButtonIndex, bool useOwner)
        {
            var window = new MessageDialogWindow(text, buttons, icon, defaultButtonIndex);

            IntPtr ownerHandle = IntPtr.Zero;
            try { ownerHandle = Process.GetCurrentProcess().MainWindowHandle; } catch { }

            if (useOwner && ownerHandle != IntPtr.Zero)
            {
                new WindowInteropHelper(window).Owner = ownerHandle;
                window.CenterOverWindow(ownerHandle);
            }
            else
            {
                window.Topmost = true;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            window.ShowDialog();
            return window.Result;
        }
    }
}