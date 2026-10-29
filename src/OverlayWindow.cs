using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReminderBuddy
{
    public class OverlayWindow : Window
    {
        public readonly string ReminderId;
        private readonly DispatcherTimer timer;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Walker character;
        private readonly Reminder reminder;

        public OverlayWindow(Reminder data, bool preview, Action<int> snooze)
        {
            reminder = data; ReminderId = data.Id;
            Width = 470; Height = 270;
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true; Background = Brushes.Transparent;
            Topmost = true; ShowInTaskbar = false; ShowActivated = false;
            Title = "Reminder Buddy reminder";
            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
            root.ColumnDefinitions.Add(new ColumnDefinition());
            Content = root;
            character = new Walker { VerticalAlignment = VerticalAlignment.Bottom, Width = 108, Height = 165, IsHitTestVisible = false };
            root.Children.Add(character);
            var bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(19, 43, 51)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(93, 203, 177)), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16), Padding = new Thickness(15, 12, 15, 11),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 10)
            };
            Grid.SetColumn(bubble, 1); root.Children.Add(bubble);
            var body = new StackPanel(); bubble.Child = body;
            body.Children.Add(new TextBlock { Text = (preview ? "PREVIEW  /  " : "") + data.Name, FontFamily = new FontFamily("Segoe UI"), FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap });
            body.Children.Add(new TextBlock { Text = data.Message, FontFamily = new FontFamily("Segoe UI"), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(221, 237, 231)), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 10) });
            var controls = new StackPanel { Orientation = Orientation.Horizontal };
            AddButton(controls, "OK", delegate { Close(); });
            var snoozeMenu = new ContextMenu();
            foreach (int minutes in new[] { 5, 10, 15 })
            {
                int captured = minutes;
                var item = new MenuItem { Header = minutes + " minutes" };
                item.Click += delegate { snooze(captured); Close(); };
                snoozeMenu.Items.Add(item);
            }
            var snoozeButton = AddButton(controls, "Snooze", delegate { snoozeMenu.IsOpen = true; });
            snoozeMenu.PlacementTarget = snoozeButton; snoozeButton.ContextMenu = snoozeMenu;
            AddButton(controls, "Close", delegate { Close(); });
            body.Children.Add(controls);
            SourceInitialized += delegate
            {
                var handle = new WindowInteropHelper(this).Handle;
                int style = GetWindowLong(handle, -20);
                SetWindowLong(handle, -20, style | 0x08000000 | 0x00000080); // No activation; tool window.
                HwndSource.FromHwnd(handle).AddHook(WindowMessage);
            };
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            timer.Tick += Animate;
            Loaded += delegate { Position(0); timer.Start(); };
            Closed += delegate { timer.Stop(); clock.Stop(); };
        }

        private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == 0x0021) { handled = true; return new IntPtr(3); } // MA_NOACTIVATE
            return IntPtr.Zero;
        }

        private void Animate(object sender, EventArgs args)
        {
            double elapsed = clock.Elapsed.TotalSeconds;
            if (!reminder.Persistent && elapsed >= reminder.DisplaySeconds) { Close(); return; }
            bool walking = !reminder.Persistent || elapsed < 2.2;
            character.Phase = walking ? elapsed * 9 : 0;
            character.Walking = walking;
            character.InvalidateVisual();
            Position(elapsed);
        }

        private void Position(double elapsed)
        {
            Rect area = SystemParameters.WorkArea;
            Width = Math.Min(470, area.Width);
            double available = Math.Max(0, area.Width - Width - 16);
            double progress = reminder.Persistent ? Math.Min(1, elapsed / 2.2) * 0.18 : Math.Min(1, elapsed / reminder.DisplaySeconds);
            Left = area.Left + 8 + available * progress;
            Top = Math.Max(area.Top, area.Bottom - Height);
        }

        private static Button AddButton(Panel panel, string title, Action action)
        {
            var b = new Button { Content = title, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 7, 0), FontSize = 12, Cursor = System.Windows.Input.Cursors.Hand, Focusable = false };
            b.Click += delegate { action(); }; panel.Children.Add(b); return b;
        }
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr h, int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr h, int index, int value);
    }

    // Original vector character: no external images, animation files, or network access.
    public class Walker : FrameworkElement
    {
        public double Phase;
        public bool Walking = true;
        protected override void OnRender(DrawingContext dc)
        {
            double swing = Walking ? Math.Sin(Phase) : 0;
            double bob = Walking ? Math.Abs(Math.Cos(Phase)) * 3 : 0;
            var ink = new SolidColorBrush(Color.FromRgb(24, 48, 59));
            var mint = new SolidColorBrush(Color.FromRgb(81, 210, 169));
            var skin = new SolidColorBrush(Color.FromRgb(237, 181, 133));
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(40, 20, 50, 40)), null, new Point(55, 159), 39, 5);
            dc.PushTransform(new TranslateTransform(0, -bob));
            Limb(dc, ink, 15, 52, 104, 50 - swing * 15, 128, 51 + swing * 24, 150);
            Limb(dc, ink, 15, 59, 104, 61 + swing * 15, 128, 62 - swing * 24, 150);
            Line(dc, Brushes.WhiteSmoke, 8, 48 + swing * 24, 153, 60 + swing * 24, 153);
            Line(dc, Brushes.WhiteSmoke, 8, 59 - swing * 24, 153, 72 - swing * 24, 153);
            Limb(dc, skin, 10, 42, 67, 29 - swing * 13, 87, 37 - swing * 23, 101);
            dc.DrawRoundedRectangle(mint, null, new Rect(35, 59, 39, 54), 12, 12);
            dc.DrawRoundedRectangle(Brushes.WhiteSmoke, null, new Rect(50, 65, 8, 41), 2, 2);
            Limb(dc, skin, 10, 69, 68, 79 + swing * 10, 86, 82 + swing * 18, 71);
            dc.DrawRoundedRectangle(skin, null, new Rect(49, 47, 14, 18), 5, 5);
            dc.DrawEllipse(skin, null, new Point(55, 34), 21, 25);
            dc.DrawEllipse(ink, null, new Point(52, 18), 20, 11);
            dc.DrawRoundedRectangle(ink, null, new Rect(34, 19, 9, 20), 4, 4);
            dc.DrawEllipse(ink, null, new Point(65, 32), 2, 2.5);
            Line(dc, ink, 2, 62, 45, 68, 43);
            // Tiny water bottle in the forward hand.
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(101, 188, 240)), new Pen(Brushes.WhiteSmoke, 1), new Rect(77 + swing * 18, 60, 12, 22), 3, 3);
            dc.DrawRectangle(Brushes.WhiteSmoke, null, new Rect(80 + swing * 18, 57, 6, 4));
            dc.Pop();
        }
        private static void Line(DrawingContext dc, Brush color, double size, double x1, double y1, double x2, double y2)
        {
            var pen = new Pen(color, size) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            dc.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
        }
        private static void Limb(DrawingContext dc, Brush color, double size, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            Line(dc, color, size, x1, y1, x2, y2); Line(dc, color, size, x2, y2, x3, y3);
        }
    }
}
