using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using FormsResult = System.Windows.Forms.DialogResult;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MaghaleNegar.CustomControls
{
    internal enum MessageDialogIcon { Information, Error, Warning, Question }

    internal enum MessageDialogButtons { Ok, OkCancel, RetryCancel, YesNo, YesNoCancel, AbortRetryIgnore }

    public partial class MessageDialogWindow : Window
    {
        private struct ButtonInfo
        {
            public string Text;
            public FormsResult Result;

            public ButtonInfo(string text, FormsResult result)
            {
                Text = text;
                Result = result;
            }
        }

        private readonly List<ButtonInfo> buttonInfos = new List<ButtonInfo>();
        private FormsResult escapeResult = FormsResult.None;

        internal FormsResult Result { get; private set; } = FormsResult.None;

        internal MessageDialogWindow(string message, MessageDialogButtons buttons, MessageDialogIcon icon, int defaultButtonIndex)
        {
            InitializeComponent();

            lblMessage.Text = message ?? string.Empty;
            SetIcon(icon);
            SetButtons(buttons, defaultButtonIndex);

            Loaded += MessageDialogWindow_Loaded;
            PreviewKeyDown += MessageDialogWindow_PreviewKeyDown;
            MouseLeftButtonDown += MessageDialogWindow_MouseLeftButtonDown;
        }

        private void SetIcon(MessageDialogIcon icon)
        {
            switch (icon)
            {
                case MessageDialogIcon.Error:
                    iconMessage.Kind = PackIconKind.Error;
                    iconMessage.Foreground = (Brush)FindResource("ExitBackgroundButton");
                    break;
                case MessageDialogIcon.Warning:
                    iconMessage.Kind = PackIconKind.Alert;
                    iconMessage.Foreground = Brushes.Orange;
                    break;
                case MessageDialogIcon.Question:
                    iconMessage.Kind = PackIconKind.HelpCircle;
                    iconMessage.Foreground = (Brush)FindResource("PrimaryBackgroundColor");
                    break;
                default:
                    iconMessage.Kind = PackIconKind.Information;
                    iconMessage.Foreground = (Brush)FindResource("PrimaryBackgroundColor");
                    break;
            }
        }

        private void SetButtons(MessageDialogButtons buttons, int defaultButtonIndex)
        {
            switch (buttons)
            {
                case MessageDialogButtons.OkCancel:
                    buttonInfos.Add(new ButtonInfo("تأیید", FormsResult.OK));
                    buttonInfos.Add(new ButtonInfo("لغو", FormsResult.Cancel));
                    escapeResult = FormsResult.Cancel;
                    break;
                case MessageDialogButtons.RetryCancel:
                    buttonInfos.Add(new ButtonInfo("تلاش مجدد", FormsResult.Retry));
                    buttonInfos.Add(new ButtonInfo("لغو", FormsResult.Cancel));
                    escapeResult = FormsResult.Cancel;
                    break;
                case MessageDialogButtons.YesNo:
                    buttonInfos.Add(new ButtonInfo("بله", FormsResult.Yes));
                    buttonInfos.Add(new ButtonInfo("خیر", FormsResult.No));
                    break;
                case MessageDialogButtons.YesNoCancel:
                    buttonInfos.Add(new ButtonInfo("بله", FormsResult.Yes));
                    buttonInfos.Add(new ButtonInfo("خیر", FormsResult.No));
                    buttonInfos.Add(new ButtonInfo("لغو", FormsResult.Cancel));
                    escapeResult = FormsResult.Cancel;
                    break;
                case MessageDialogButtons.AbortRetryIgnore:
                    buttonInfos.Add(new ButtonInfo("توقف", FormsResult.Abort));
                    buttonInfos.Add(new ButtonInfo("تلاش مجدد", FormsResult.Retry));
                    buttonInfos.Add(new ButtonInfo("نادیده گرفتن", FormsResult.Ignore));
                    break;
                default:
                    buttonInfos.Add(new ButtonInfo("تأیید", FormsResult.OK));
                    escapeResult = FormsResult.OK;
                    break;
            }

            System.Windows.Controls.Button[] controls = { btn1, btn2, btn3 };

            if (defaultButtonIndex < 0 || defaultButtonIndex >= buttonInfos.Count)
                defaultButtonIndex = 0;

            for (int i = 0; i < controls.Length; i++)
            {
                if (i >= buttonInfos.Count)
                {
                    controls[i].Visibility = Visibility.Collapsed;
                    continue;
                }

                ButtonInfo info = buttonInfos[i];
                controls[i].Content = info.Text;
                controls[i].IsDefault = i == defaultButtonIndex;
                controls[i].Click += (s, e) => Close(info.Result);
            }
        }

        private void MessageDialogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button[] controls = { btn1, btn2, btn3 };
            foreach (var button in controls)
            {
                if (button.IsDefault && button.Visibility == Visibility.Visible)
                {
                    button.Focus();
                    break;
                }
            }

            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
        }

        private void MessageDialogWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape && escapeResult != FormsResult.None)
            {
                e.Handled = true;
                Close(escapeResult);
            }
        }

        private void MessageDialogWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.TextBlock || e.OriginalSource is System.Windows.Controls.Primitives.ButtonBase)
                return;

            try { DragMove(); } catch { }
        }

        private void Close(FormsResult result)
        {
            Result = result;
            Close();
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left, Top, Right, Bottom;
        }

        internal void CenterOverWindow(IntPtr ownerHandle)
        {
            if (ownerHandle == IntPtr.Zero || !GetWindowRect(ownerHandle, out NativeRect rect))
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            double dpiX = 1, dpiY = 1;
            using (var source = new HwndSource(new HwndSourceParameters()))
            {
                if (source.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformToDevice.M11;
                    dpiY = source.CompositionTarget.TransformToDevice.M22;
                }
            }

            double centerX = (rect.Left + rect.Right) / 2.0 / dpiX;
            double centerY = (rect.Top + rect.Bottom) / 2.0 / dpiY;

            WindowStartupLocation = WindowStartupLocation.Manual;
            SourceInitialized += (s, e) =>
            {
                Left = centerX - ActualWidth / 2;
                Top = centerY - ActualHeight / 2;
            };
            ContentRendered += (s, e) =>
            {
                Left = centerX - ActualWidth / 2;
                Top = centerY - ActualHeight / 2;
            };
        }
    }
}