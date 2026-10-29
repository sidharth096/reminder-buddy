using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReminderBuddy
{
    public class SettingsWindow : Window
    {
        private readonly BuddyApp app;
        private readonly ListBox list = new ListBox();
        private readonly TextBox name = new TextBox(), message = new TextBox(), time = new TextBox(), interval = new TextBox(), start = new TextBox(), end = new TextBox(), duration = new TextBox();
        private readonly CheckBox enabled = new CheckBox { Content = "Reminder enabled" };
        private readonly CheckBox[] days = new CheckBox[7];
        private readonly ComboBox schedule = new ComboBox(), display = new ComboBox(), sound = new ComboBox();
        private readonly TextBlock status = new TextBlock(), error = new TextBlock();
        private readonly StackPanel fixedFields = new StackPanel(), intervalFields = new StackPanel(), durationFields = new StackPanel();
        private string editingId;
        private bool loading;
        private bool dirty;

        public SettingsWindow(BuddyApp owner)
        {
            app = owner;
            Title = "Reminder Buddy";
            Width = 1060; Height = 790; MinWidth = 850; MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brush("#F3F5F1"); Foreground = Brush("#19352E");
            FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
            var buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(13, 8, 13, 8)));
            buttonStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 6)));
            buttonStyle.Setters.Add(new Setter(Control.CursorProperty, System.Windows.Input.Cursors.Hand));
            Resources.Add(typeof(Button), buttonStyle);
            var boxStyle = new Style(typeof(TextBox));
            boxStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(9, 7, 9, 7)));
            boxStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#A7BDB2")));
            Resources.Add(typeof(TextBox), boxStyle);
            var comboStyle = new Style(typeof(ComboBox));
            comboStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(7)));
            Resources.Add(typeof(ComboBox), comboStyle);

            var root = new DockPanel { Margin = new Thickness(24) };
            Content = root;
            var header = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
            header.Children.Add(new TextBlock { Text = "REMINDER BUDDY", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brush("#427663") });
            header.Children.Add(new TextBlock { Text = "Small nudges. Better days.", FontFamily = new FontFamily("Georgia"), FontSize = 30, Margin = new Thickness(0, 5, 0, 5) });
            status.FontSize = 12; header.Children.Add(status);
            DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
            var footer = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 0) };
            var startup = new CheckBox { Content = "Start with Windows", IsChecked = BuddyApp.StartsWithWindows(), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 22, 0) };
            startup.Click += delegate { try { BuddyApp.SetStartup(startup.IsChecked == true); } catch (Exception ex) { startup.IsChecked = BuddyApp.StartsWithWindows(); MessageBox.Show(ex.Message, "Startup setting"); } };
            footer.Children.Add(startup);
            var full = new CheckBox { Content = "Wait during full-screen apps", IsChecked = app.Data.DeferFullScreen, VerticalAlignment = VerticalAlignment.Center };
            full.Click += delegate { app.Data.DeferFullScreen = full.IsChecked == true; app.Save(); };
            footer.Children.Add(full);
            DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
            var columns = new Grid();
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            columns.ColumnDefinitions.Add(new ColumnDefinition());
            root.Children.Add(columns);

            var sidebar = new DockPanel(); columns.Children.Add(sidebar);
            var actions = new WrapPanel();
            actions.Children.Add(Button("+ New reminder", delegate { if (CanDiscard()) { list.SelectedIndex = -1; LoadEditor(new Reminder(), null); } }));
            actions.Children.Add(Button("Duplicate", Duplicate));
            actions.Children.Add(Button("Delete", Delete));
            DockPanel.SetDock(actions, Dock.Bottom); sidebar.Children.Add(actions);
            var hint = new TextBlock { Text = "YOUR REMINDERS", FontSize = 11, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) };
            DockPanel.SetDock(hint, Dock.Top); sidebar.Children.Add(hint);
            list.Background = Brushes.White; list.BorderBrush = Brush("#D6DFD8");
            list.SelectionChanged += delegate
            {
                if (loading) return;
                var item = list.SelectedItem as ListBoxItem;
                if (item == null) return;
                if (!CanDiscard()) { RefreshList(editingId); return; }
                var reminder = (Reminder)item.Tag;
                LoadEditor(reminder, reminder.Id);
            };
            sidebar.Children.Add(list);

            var card = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(12), Padding = new Thickness(22), BorderBrush = Brush("#D6DFD8"), BorderThickness = new Thickness(1) };
            Grid.SetColumn(card, 2); columns.Children.Add(card);
            var editor = new StackPanel();
            card.Child = new ScrollViewer { Content = editor, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(0, 0, 10, 0) };
            editor.Children.Add(new TextBlock { Text = "Make room for a good habit", FontSize = 21, FontFamily = new FontFamily("Georgia"), Margin = new Thickness(0, 0, 0, 15) });
            editor.Children.Add(enabled);
            AddField(editor, "Reminder name", name); name.MaxLength = 80;
            message.AcceptsReturn = true; message.TextWrapping = TextWrapping.Wrap; message.Height = 72; message.MaxLength = 220;
            AddField(editor, "Message beside your character", message);
            Label(editor, "Repeat on");
            var dayRow = new WrapPanel();
            for (int i = 0; i < 7; i++)
            {
                days[i] = new CheckBox { Content = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[i], Margin = new Thickness(0, 4, 13, 4) };
                dayRow.Children.Add(days[i]);
            }
            editor.Children.Add(dayRow);
            var shortcuts = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            shortcuts.Children.Add(Button("Every day", delegate { SetDays(127); }));
            shortcuts.Children.Add(Button("Weekdays", delegate { SetDays(62); }));
            editor.Children.Add(shortcuts);
            schedule.Items.Add("At a specific time"); schedule.Items.Add("Repeat at an interval");
            AddField(editor, "Schedule", schedule);
            AddField(fixedFields, "Time (24-hour HH:mm)", time); editor.Children.Add(fixedFields);
            AddField(intervalFields, "Every how many minutes? (1-1440)", interval);
            var hours = new Grid(); hours.ColumnDefinitions.Add(new ColumnDefinition()); hours.ColumnDefinitions.Add(new ColumnDefinition());
            var left = new StackPanel { Margin = new Thickness(0, 0, 10, 0) }; AddField(left, "From (HH:mm)", start); hours.Children.Add(left);
            var right = new StackPanel(); AddField(right, "Until (HH:mm)", end); Grid.SetColumn(right, 1); hours.Children.Add(right);
            intervalFields.Children.Add(hours);
            intervalFields.Children.Add(new TextBlock { Text = "Starts at the From time. End time is excluded. Overnight ranges are supported.", FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = Brush("#577367"), Margin = new Thickness(0, 5, 0, 0) });
            editor.Children.Add(intervalFields);
            display.Items.Add("Stay until I click OK or Close"); display.Items.Add("Show, then automatically hide");
            AddField(editor, "On-screen behavior", display);
            AddField(durationFields, "Show for how many seconds? (5-300)", duration); editor.Children.Add(durationFields);
            sound.Items.Add("None"); sound.Items.Add("Chime"); sound.Items.Add("Bell");
            AddField(editor, "Notification sound", sound);
            error.Foreground = Brush("#AD3030"); error.TextWrapping = TextWrapping.Wrap; error.Margin = new Thickness(0, 12, 0, 8); editor.Children.Add(error);
            var buttons = new WrapPanel();
            var save = Button("Save reminder", SaveReminder); save.Background = Brush("#165F49"); save.Foreground = Brushes.White;
            buttons.Children.Add(save);
            buttons.Children.Add(Button("Preview animation", delegate { Reminder r; if (ReadEditor(out r)) app.Preview(r); }));
            editor.Children.Add(buttons);
            schedule.SelectionChanged += delegate { fixedFields.Visibility = schedule.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed; intervalFields.Visibility = schedule.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed; MarkDirty(); };
            display.SelectionChanged += delegate { durationFields.Visibility = display.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed; MarkDirty(); };
            sound.SelectionChanged += delegate { MarkDirty(); };
            foreach (var box in new[] { name, message, time, interval, start, end, duration }) box.TextChanged += delegate { MarkDirty(); };
            foreach (var check in days.Concat(new[] { enabled })) { check.Checked += delegate { MarkDirty(); }; check.Unchecked += delegate { MarkDirty(); }; }
            Closing += delegate(object sender, System.ComponentModel.CancelEventArgs args)
            {
                if (!app.Exiting) { args.Cancel = true; if (CanDiscard()) { dirty = false; Hide(); } }
            };
            RefreshList(null);
            LoadEditor(new Reminder { Name = "Drink water", Message = "Time to hydrate. Drink a glass of water!", IsInterval = true }, null);
        }

        private void MarkDirty() { if (!loading) { dirty = true; error.Text = "Unsaved changes"; } }
        private bool CanDiscard() { return !dirty || MessageBox.Show(this, "Discard your unsaved reminder changes?", "Unsaved reminder", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes; }
        private static Brush Brush(string hex) { return (Brush)new BrushConverter().ConvertFromString(hex); }
        private static Button Button(string title, Action action) { var b = new Button { Content = title }; b.Click += delegate { action(); }; return b; }
        private static void Label(Panel panel, string text) { panel.Children.Add(new TextBlock { Text = text, Margin = new Thickness(0, 12, 0, 5), FontWeight = FontWeights.SemiBold }); }
        private static void AddField(Panel panel, string text, UIElement field) { Label(panel, text); panel.Children.Add(field); }
        private void SetDays(int mask) { for (int i = 0; i < 7; i++) days[i].IsChecked = (mask & (1 << i)) != 0; }

        private void LoadEditor(Reminder r, string id)
        {
            loading = true; editingId = id;
            name.Text = r.Name; message.Text = r.Message; enabled.IsChecked = r.Enabled; SetDays(r.Days);
            schedule.SelectedIndex = r.IsInterval ? 1 : 0;
            time.Text = Reminder.FormatTime(r.TimeMinutes); start.Text = Reminder.FormatTime(r.StartMinutes); end.Text = Reminder.FormatTime(r.EndMinutes);
            interval.Text = r.IntervalMinutes.ToString(); duration.Text = r.DisplaySeconds.ToString(); display.SelectedIndex = r.Persistent ? 0 : 1;
            sound.SelectedItem = r.Sound;
            loading = false; dirty = false; error.Text = "";
        }
        private bool ReadEditor(out Reminder r)
        {
            r = new Reminder { Name = name.Text.Trim(), Message = message.Text.Trim(), Enabled = enabled.IsChecked == true, Days = 0, IsInterval = schedule.SelectedIndex == 1, Persistent = display.SelectedIndex == 0, Sound = sound.SelectedItem as string ?? "None" };
            for (int i = 0; i < 7; i++) if (days[i].IsChecked == true) r.Days |= 1 << i;
            int value;
            if (!TryTime(time.Text, out r.TimeMinutes)) { error.Text = "Enter a valid time, for example 17:30."; return false; }
            if (!TryTime(start.Text, out r.StartMinutes) || !TryTime(end.Text, out r.EndMinutes)) { error.Text = "Enter active hours as HH:mm, for example 09:00 and 18:00."; return false; }
            if (!int.TryParse(interval.Text, out value)) { error.Text = "Enter a whole number of minutes."; return false; } r.IntervalMinutes = value;
            if (!int.TryParse(duration.Text, out value)) { error.Text = "Enter a whole number of seconds."; return false; } r.DisplaySeconds = value;
            error.Text = r.Validate() ?? "";
            return error.Text == "";
        }
        private static bool TryTime(string text, out int minutes)
        {
            DateTime parsed; bool ok = DateTime.TryParseExact(text.Trim(), new[] { "HH:mm", "H:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
            minutes = ok ? parsed.Hour * 60 + parsed.Minute : 0; return ok;
        }

        private void SaveReminder()
        {
            Reminder r; if (!ReadEditor(out r)) return;
            var existing = app.Data.Reminders.FirstOrDefault(x => x.Id == editingId);
            // Editing starts a fresh schedule from now, avoiding an immediate stale alarm.
            if (existing != null) r.Id = existing.Id;
            r.LastDelivered = DateTime.Now;
            var before = app.Data.Reminders.ToList();
            if (existing != null) app.Data.Reminders[app.Data.Reminders.IndexOf(existing)] = r; else app.Data.Reminders.Add(r);
            try { app.Save(); }
            catch { app.Data.Reminders = before; throw; }
            app.RemovePending(r.Id); app.Save();
            RefreshList(r.Id); LoadEditor(r, r.Id); error.Text = "Saved. Your buddy will arrive on schedule.";
        }

        private void RefreshList(string selected)
        {
            loading = true; list.Items.Clear();
            foreach (var r in app.Data.Reminders)
            {
                var content = new StackPanel { Margin = new Thickness(9, 10, 9, 10), Width = 238 };
                content.Children.Add(new TextBlock { Text = (r.Enabled ? "" : "[Off] ") + r.Name, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
                content.Children.Add(new TextBlock { Text = r.Summary, FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = Brush("#577367"), Margin = new Thickness(0, 6, 0, 0) });
                var item = new ListBoxItem { Tag = r, Content = content }; list.Items.Add(item); if (r.Id == selected) list.SelectedItem = item;
            }
            loading = false;
        }
        private void Duplicate()
        {
            var existing = app.Data.Reminders.FirstOrDefault(r => r.Id == editingId);
            if (existing == null) { error.Text = "Select a saved reminder to duplicate."; return; }
            if (!CanDiscard()) return;
            var copy = existing.Copy(true); copy.Name = existing.Name.Substring(0, Math.Min(existing.Name.Length, 73)) + " (copy)";
            list.SelectedIndex = -1; LoadEditor(copy, null); dirty = true; error.Text = "Copy ready. Save to add it.";
        }
        private void Delete()
        {
            var existing = app.Data.Reminders.FirstOrDefault(r => r.Id == editingId);
            if (existing == null) return;
            if (MessageBox.Show(this, "Delete \"" + existing.Name + "\"?", "Delete reminder", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            int index = app.Data.Reminders.IndexOf(existing); app.Data.Reminders.Remove(existing);
            try { app.Save(); } catch { app.Data.Reminders.Insert(index, existing); throw; }
            app.RemovePending(existing.Id); app.Save(); RefreshList(null); LoadEditor(new Reminder(), null);
        }
        public void RefreshStatus()
        {
            status.Text = (app.Data.PauseUntil > DateTime.Now ? "Paused until " + app.Data.PauseUntil.ToString("ddd HH:mm") : "Running in your tray") + "  |  " + app.Data.Reminders.Count(r => r.Enabled) + " active reminders  |  Times use your PC's local clock";
        }
    }
}
