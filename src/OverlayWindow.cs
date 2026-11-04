using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Input;

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
            SystemDecorations = SystemDecorations.None; CanResize = false;
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent }; Background = Brushes.Transparent;
            Topmost = true; ShowInTaskbar = false; ShowActivated = false;
            Title = "Reminder Buddy reminder";
            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
            root.ColumnDefinitions.Add(new ColumnDefinition());
            Content = root;
            character = new Walker { CharacterId = CharacterCatalog.Find(data.CharacterId).Id, VerticalAlignment = VerticalAlignment.Bottom, Width = 108, Height = 205, IsHitTestVisible = false };
            var characterHost = new Grid { Width = 112, Height = 215, VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false };
            characterHost.Children.Add(character);
            root.Children.Add(characterHost);
            var bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(19, 43, 51)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(93, 203, 177)), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16), Padding = new Thickness(15, 12, 15, 11),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 10)
            };
            Grid.SetColumn(bubble, 1); root.Children.Add(bubble);
            var body = new StackPanel(); bubble.Child = body;
            body.Children.Add(new TextBlock { Text = (preview ? "PREVIEW  /  " : "") + data.Name, FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap });
            body.Children.Add(new TextBlock { Text = data.Message, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(221, 237, 231)), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 10) });
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
            var snoozeButton = AddButton(controls, "Snooze", delegate { snoozeMenu.Open(); });
            snoozeMenu.PlacementTarget = snoozeButton; snoozeButton.ContextMenu = snoozeMenu;
            AddButton(controls, "Close", delegate { Close(); });
            body.Children.Add(controls);
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            timer.Tick += Animate;
            Opened += delegate { Platform.ConfigureOverlay(this); UpdatePosition(0); timer.Start(); };
            Closed += delegate { timer.Stop(); clock.Stop(); };
        }

        private void Animate(object sender, EventArgs args)
        {
            double elapsed = clock.Elapsed.TotalSeconds;
            if (!reminder.Persistent && elapsed >= reminder.DisplaySeconds) { Close(); return; }
            bool walking = !reminder.Persistent || elapsed < 2.2;
            character.Phase = walking ? elapsed * 9 : 0;
            character.Walking = walking;
            character.InvalidateVisual();
            UpdatePosition(elapsed);
        }

        private void UpdatePosition(double elapsed)
        {
            var screen = Screens.Primary;
            if (screen == null) return;
            var area = screen.WorkingArea;
            double scaling = screen.Scaling;
            Width = Math.Min(470, area.Width / scaling);
            double available = Math.Max(0, area.Width - Width * scaling - 16 * scaling);
            double progress = reminder.Persistent ? Math.Min(1, elapsed / 2.2) * 0.18 : Math.Min(1, elapsed / reminder.DisplaySeconds);
            Position = new PixelPoint((int)(area.X + 8 * scaling + available * progress), (int)Math.Max(area.Y, area.Bottom - Height * scaling));
        }

        private static Button AddButton(Panel panel, string title, Action action)
        {
            var b = new Button { Content = title, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 7, 0), FontSize = 12, Cursor = new Cursor(StandardCursorType.Hand), Focusable = false };
            b.Click += delegate { action(); }; panel.Children.Add(b); return b;
        }
    }

    // Original vector character: no external images, animation files, or network access.
    public class Walker : Control
    {
        public string CharacterId = "buddy";
        public double Phase;
        public bool Walking = true;
        public override void Render(DrawingContext dc)
        {
            double scale = Math.Min(Bounds.Width / 108.0, Bounds.Height / 180.0);
            using var offset = dc.PushTransform(Matrix.CreateTranslation((Bounds.Width - 108 * scale) / 2, Bounds.Height - 180 * scale));
            using var size = dc.PushTransform(Matrix.CreateScale(scale, scale));
            double swing = Walking ? Math.Sin(Phase) : 0;
            double bob = Walking ? Math.Abs(Math.Cos(Phase)) * 3 : 0;
            DrawShadow(dc);
            using var bounce = dc.PushTransform(Matrix.CreateTranslation(0, -bob));
            if (CharacterId == "abu") DrawAbu(dc, swing);
            else if (CharacterId == "shibu") DrawShibu(dc, swing);
            else if (CharacterId == "tom") DrawTom(dc, swing);
            else DrawOriginal(dc, swing);
        }

        private static void DrawShadow(DrawingContext dc)
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(40, 20, 50, 40)), null, new Point(55, 171), 39, 5);
        }

        private static void DrawOriginal(DrawingContext dc, double swing)
        {
            var ink = new SolidColorBrush(Color.FromRgb(24, 48, 59));
            var mint = new SolidColorBrush(Color.FromRgb(81, 210, 169));
            var skin = new SolidColorBrush(Color.FromRgb(237, 181, 133));
            DrawLegs(dc, ink, Brushes.WhiteSmoke, swing, 104);
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
        }

        private static void DrawAbu(DrawingContext dc, double swing)
        {
            var ink = new SolidColorBrush(Color.FromRgb(29, 27, 26));
            var skin = new SolidColorBrush(Color.FromRgb(207, 139, 95));
            var red = new SolidColorBrush(Color.FromRgb(204, 48, 43));
            DrawLegs(dc, skin, new SolidColorBrush(Color.FromRgb(69, 39, 25)), swing, 112);
            Limb(dc, skin, 10, 39, 73, 29 - swing * 12, 94, 34 - swing * 20, 109);
            Limb(dc, skin, 10, 70, 73, 78 + swing * 12, 94, 75 + swing * 20, 109);
            dc.DrawRoundedRectangle(ink, null, new Rect(32, 61, 45, 57), 11, 11);
            // A red wrap gives this original caricature a Kerala-inspired silhouette.
            dc.DrawGeometry(red, null, Polygon(new Point(34, 107), new Point(76, 107), new Point(71, 135), new Point(55, 129), new Point(38, 135)));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(245, 173, 62)), 2), new Point(35, 111), new Point(75, 111));
            dc.DrawRoundedRectangle(skin, null, new Rect(48, 50, 14, 17), 5, 5);
            dc.DrawEllipse(skin, null, new Point(55, 35), 27, 28);
            dc.DrawEllipse(ink, null, new Point(54, 17), 26, 13);
            dc.DrawEllipse(ink, null, new Point(35, 30), 8, 16);
            dc.DrawEllipse(ink, null, new Point(75, 30), 8, 16);
            Eye(dc, 45, 34); Eye(dc, 64, 34);
            dc.DrawGeometry(ink, null, Mustache());
            Line(dc, new SolidColorBrush(Color.FromRgb(111, 50, 36)), 2, 50, 50, 60, 50);
        }

        private static void DrawShibu(DrawingContext dc, double swing)
        {
            var skin = new SolidColorBrush(Color.FromRgb(204, 137, 92));
            var hair = new SolidColorBrush(Color.FromRgb(35, 30, 24));
            var red = new SolidColorBrush(Color.FromRgb(189, 25, 50));
            var green = new SolidColorBrush(Color.FromRgb(18, 99, 71));
            DrawLegs(dc, skin, green, swing, 108);
            Limb(dc, skin, 10, 38, 69, 27 - swing * 12, 88, 34 - swing * 21, 104);
            Limb(dc, skin, 10, 72, 69, 82 + swing * 12, 88, 76 + swing * 21, 104);
            dc.DrawRoundedRectangle(red, null, new Rect(31, 57, 49, 58), 8, 8);
            dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(151, 18, 42)), null, Polygon(new Point(34, 106), new Point(77, 106), new Point(72, 132), new Point(57, 132), new Point(54, 119), new Point(50, 132), new Point(37, 132)));
            dc.DrawGeometry(green, null, Polygon(new Point(43, 57), new Point(55, 66), new Point(67, 57)));
            dc.DrawRoundedRectangle(skin, null, new Rect(49, 48, 13, 16), 4, 4);
            dc.DrawEllipse(skin, null, new Point(55, 33), 23, 27);
            dc.DrawGeometry(hair, null, SpikyHair());
            Eye(dc, 47, 33); Eye(dc, 63, 33);
            Line(dc, new SolidColorBrush(Color.FromRgb(126, 55, 44)), 2, 51, 48, 61, 47);
            // Abstract crest: original mark, no team logo.
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(242, 190, 61)), null, new Point(68, 77), 4, 5);
        }

        private static void DrawTom(DrawingContext dc, double swing)
        {
            var skin = new SolidColorBrush(Color.FromRgb(221, 158, 113));
            var hair = new SolidColorBrush(Color.FromRgb(82, 48, 31));
            var sky = new SolidColorBrush(Color.FromRgb(101, 190, 224));
            var dark = new SolidColorBrush(Color.FromRgb(34, 38, 47));
            DrawLegs(dc, skin, Brushes.WhiteSmoke, swing, 108);
            Limb(dc, skin, 10, 38, 69, 27 - swing * 12, 88, 34 - swing * 21, 104);
            Limb(dc, skin, 10, 72, 69, 82 + swing * 12, 88, 76 + swing * 21, 104);
            dc.DrawRoundedRectangle(Brushes.WhiteSmoke, null, new Rect(31, 57, 49, 58), 8, 8);
            dc.DrawRoundedRectangle(sky, null, new Rect(39, 59, 9, 52), 2, 2);
            dc.DrawRoundedRectangle(sky, null, new Rect(62, 59, 9, 52), 2, 2);
            dc.DrawGeometry(dark, null, Polygon(new Point(33, 106), new Point(78, 106), new Point(72, 133), new Point(57, 133), new Point(54, 119), new Point(50, 133), new Point(38, 133)));
            dc.DrawRoundedRectangle(skin, null, new Rect(49, 49, 13, 16), 4, 4);
            dc.DrawEllipse(skin, null, new Point(55, 34), 23, 27);
            dc.DrawGeometry(hair, null, TousledHair());
            Eye(dc, 47, 33); Eye(dc, 63, 33);
            dc.DrawGeometry(hair, null, Beard());
            Line(dc, new SolidColorBrush(Color.FromRgb(121, 55, 43)), 2, 51, 48, 60, 48);
        }

        private static void DrawLegs(DrawingContext dc, IBrush leg, IBrush sock, double swing, double hipY)
        {
            Limb(dc, leg, 14, 51, hipY, 49 - swing * 14, 136, 50 + swing * 23, 162);
            Limb(dc, leg, 14, 59, hipY, 62 + swing * 14, 136, 63 - swing * 23, 162);
            Line(dc, sock, 8, 47 + swing * 23, 165, 59 + swing * 23, 165);
            Line(dc, sock, 8, 60 - swing * 23, 165, 73 - swing * 23, 165);
        }

        private static void Eye(DrawingContext dc, double x, double y)
        {
            dc.DrawEllipse(Brushes.WhiteSmoke, null, new Point(x, y), 4, 3);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(30, 27, 25)), null, new Point(x + 1, y), 1.5, 1.8);
        }

        private static Geometry Polygon(params Point[] points)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(points[0], true);
                foreach (var point in new System.Collections.Generic.List<Point>(points).GetRange(1, points.Length - 1)) context.LineTo(point);
                context.EndFigure(true);
            }
            return geometry;
        }

        private static Geometry Mustache()
        {
            var geometry = new StreamGeometry();
            using (var c = geometry.Open())
            {
                c.BeginFigure(new Point(55, 43), true);
                c.CubicBezierTo(new Point(48, 38), new Point(41, 43), new Point(38, 47));
                c.CubicBezierTo(new Point(45, 49), new Point(51, 48), new Point(55, 44));
                c.CubicBezierTo(new Point(61, 48), new Point(68, 49), new Point(74, 46));
                c.CubicBezierTo(new Point(68, 41), new Point(61, 39), new Point(55, 43));
                c.EndFigure(true);
            }
            return geometry;
        }

        private static Geometry SpikyHair()
        {
            return Polygon(new Point(34, 29), new Point(39, 15), new Point(43, 21), new Point(47, 7), new Point(51, 18), new Point(58, 5), new Point(60, 18), new Point(69, 9), new Point(68, 21), new Point(77, 17), new Point(75, 31), new Point(68, 22), new Point(43, 23));
        }

        private static Geometry TousledHair()
        {
            return Polygon(new Point(33, 29), new Point(38, 16), new Point(44, 19), new Point(49, 9), new Point(54, 17), new Point(61, 8), new Point(64, 18), new Point(73, 15), new Point(76, 29), new Point(68, 22), new Point(43, 23));
        }

        private static Geometry Beard()
        {
            var geometry = new StreamGeometry();
            using (var c = geometry.Open())
            {
                c.BeginFigure(new Point(36, 35), true);
                c.CubicBezierTo(new Point(37, 51), new Point(45, 60), new Point(55, 62));
                c.CubicBezierTo(new Point(66, 59), new Point(74, 50), new Point(75, 35));
                c.LineTo(new Point(70, 48));
                c.CubicBezierTo(new Point(61, 55), new Point(49, 55), new Point(40, 48));
                c.EndFigure(true);
            }
            return geometry;
        }
        private static void Line(DrawingContext dc, IBrush color, double size, double x1, double y1, double x2, double y2)
        {
            var pen = new Pen(color, size, lineCap: PenLineCap.Round);
            dc.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
        }
        private static void Limb(DrawingContext dc, IBrush color, double size, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            Line(dc, color, size, x1, y1, x2, y2); Line(dc, color, size, x2, y2, x3, y3);
        }
    }
}
