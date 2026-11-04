using Avalonia;
using Avalonia.Media;

namespace ReminderBuddy
{
    internal static class DrawingExtensions
    {
        public static void DrawRoundedRectangle(this DrawingContext dc, IBrush brush, IPen pen, Rect rect, double x, double y)
        {
            dc.DrawRectangle(brush, pen, rect, x, y);
        }
    }
}
