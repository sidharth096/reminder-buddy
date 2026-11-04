using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ReminderBuddy
{
    internal static class Dialogs
    {
        public static Task<bool> Show(Window owner, string message, string title, bool confirm = false)
        {
            var window = new Window { Title = title, Width = 440, SizeToContent = SizeToContent.Height,
                CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var panel = new StackPanel { Margin = new Thickness(24), Spacing = 18 };
            panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
            if (confirm)
            {
                var cancel = new Button { Content = "Cancel", IsCancel = true };
                cancel.Click += delegate { window.Close(false); };
                buttons.Children.Add(cancel);
            }
            var ok = new Button { Content = confirm ? "Continue" : "OK", IsDefault = true };
            ok.Click += delegate { window.Close(true); };
            buttons.Children.Add(ok);
            panel.Children.Add(buttons);
            window.Content = panel;
            return window.ShowDialog<bool>(owner);
        }
    }
}
