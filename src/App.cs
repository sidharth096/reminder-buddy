using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace ReminderBuddy
{
    public class BuddyApp : Application
    {
        public Preferences Data;
        public Store Storage;
        public SettingsWindow Settings;
        public bool Exiting;
        public bool SmokeTest;
        private TrayIcon tray;
        private IClassicDesktopStyleApplicationLifetime desktop;
        private DispatcherTimer timer;
        private readonly Queue<Reminder> queue = new Queue<Reminder>();
        private OverlayWindow overlay;
        private DateTime lastTick = DateTime.Now;
        public static readonly string DataDirectory = OperatingSystem.IsMacOS()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "ReminderBuddy")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReminderBuddy");

        public override void Initialize()
        {
            Name = "Reminder Buddy";
            RequestedThemeVariant = ThemeVariant.Light;
            Styles.Add(new FluentTheme());
            var appMenu = new NativeMenu();
            AddMenu(appMenu, "Open Reminder Buddy", () => { if (Settings != null) OpenSettings(); });
            NativeMenu.SetMenu(this, appMenu);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var args = desktop.Args ?? Array.Empty<string>();
            SmokeTest = args.Contains("--smoke-test");
            Dispatcher.UIThread.UnhandledException += async delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
            {
                Log(e.Exception);
                e.Handled = true;
                if (Settings != null)
                {
                    OpenSettings();
                    await Dialogs.Show(Settings, e.Exception.Message, "Reminder Buddy");
                }
            };
            try { Start(args.Contains("--background")); }
            catch (Exception ex) { ShowStartupError(ex); }
            base.OnFrameworkInitializationCompleted();
        }

        private async void ShowStartupError(Exception ex)
        {
            Log(ex);
            var owner = new Window { Title = "Reminder Buddy", Width = 440, Height = 160 };
            desktop.MainWindow = owner;
            owner.Show();
            await Dialogs.Show(owner, ex.Message, "Reminder Buddy could not start");
            desktop.Shutdown(1);
        }

        public static WindowIcon LoadIcon() { return new WindowIcon(AssetLoader.Open(new Uri("avares://ReminderBuddy/Assets/ReminderBuddy.ico"))); }

        private void Start(bool background)
        {
            if (!SmokeTest)
            {
                string legacyDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskbarBuddy");
                if (!File.Exists(Path.Combine(DataDirectory, "settings.json")) && File.Exists(Path.Combine(legacyDirectory, "settings.json")))
                {
                    Directory.CreateDirectory(DataDirectory);
                    File.Copy(Path.Combine(legacyDirectory, "settings.json"), Path.Combine(DataDirectory, "settings.json"));
                    if (File.Exists(Path.Combine(legacyDirectory, "settings.json.bak")))
                        File.Copy(Path.Combine(legacyDirectory, "settings.json.bak"), Path.Combine(DataDirectory, "settings.json.bak"), true);
                }
            }
            Storage = new Store(Path.Combine(DataDirectory, SmokeTest ? "smoke-settings.json" : "settings.json"));
            Data = SmokeTest ? new Preferences() : Storage.Load();
            Settings = new SettingsWindow(this);
            if (!background || SmokeTest) desktop.MainWindow = Settings;
            tray = new TrayIcon { Icon = LoadIcon(), ToolTipText = "Reminder Buddy", IsVisible = true };
            var menu = new NativeMenu();
            AddMenu(menu, "Open Reminder Buddy", OpenSettings);
            menu.Items.Add(new NativeMenuItemSeparator());
            AddMenu(menu, "Pause for 30 minutes", () => Pause(DateTime.Now.AddMinutes(30)));
            AddMenu(menu, "Pause for 1 hour", () => Pause(DateTime.Now.AddHours(1)));
            AddMenu(menu, "Pause until tomorrow", () => Pause(DateTime.Today.AddDays(1)));
            AddMenu(menu, "Resume reminders", () => Pause(DateTime.MinValue));
            menu.Items.Add(new NativeMenuItemSeparator());
            AddMenu(menu, "Exit", Quit);
            tray.Menu = menu;
            TrayIcon.SetIcons(this, new TrayIcons { tray });
            tray.Clicked += delegate { OpenSettings(); };
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Tick;
            timer.Start();
            if (!background || SmokeTest) OpenSettings();
            if (SmokeTest)
            {
                Preview(new Reminder { Name = "Drink water", Message = "Time for a water break. You've got this!", Persistent = true });
                var exit = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
                exit.Tick += delegate { exit.Stop(); Quit(); };
                exit.Start();
            }
        }

        private static void AddMenu(NativeMenu menu, string title, Action action)
        {
            var item = new NativeMenuItem(title);
            item.Click += delegate { action(); };
            menu.Items.Add(item);
        }

        public void OpenSettings() { desktop.MainWindow = Settings; Settings.Show(); Settings.WindowState = WindowState.Normal; Settings.Activate(); Settings.RefreshStatus(); }
        public void Save() { if (!SmokeTest) Storage.Save(Data); }
        public void Pause(DateTime until)
        {
            Data.PauseUntil = until;
            queue.Clear();
            if (overlay != null) overlay.Close();
            Save();
            Settings.RefreshStatus();
        }

        private void Tick(object sender, EventArgs args)
        {
            DateTime now = DateTime.Now;
            bool jumped = now - lastTick > TimeSpan.FromMinutes(2) || now < lastTick;
            lastTick = now;
            bool dirty = false;
            var candidates = new List<Reminder>();
            foreach (var reminder in Data.Reminders)
            {
                var due = Schedule.Due(reminder, now);
                if (due.HasValue)
                {
                    reminder.LastDelivered = due.Value;
                    dirty = true;
                    if (now >= Data.PauseUntil) candidates.Add(reminder.Copy(false));
                }
            }
            foreach (var snooze in Data.Snoozes.Where(s => s.Due <= now).ToList())
            {
                Data.Snoozes.Remove(snooze);
                dirty = true;
                var reminder = Data.Reminders.FirstOrDefault(r => r.Id == snooze.ReminderId && r.Enabled);
                if (reminder != null && now >= Data.PauseUntil && now - snooze.Due < TimeSpan.FromHours(12)) candidates.Add(reminder.Copy(false));
            }
            if (dirty) Save();
            if (jumped) queue.Clear();
            if (jumped && candidates.Count > 1) candidates = candidates.Take(1).ToList();
            foreach (var reminder in candidates)
                if (!queue.Any(r => r.Id == reminder.Id) && (overlay == null || overlay.ReminderId != reminder.Id)) queue.Enqueue(reminder);
            if (overlay == null && queue.Count > 0 && now >= Data.PauseUntil && (!Data.DeferFullScreen || !Platform.IsFullScreen(Settings))) ShowReminder(queue.Dequeue(), false);
            if (Settings.IsVisible) Settings.RefreshStatus();
        }

        public void Preview(Reminder reminder)
        {
            if (overlay != null) overlay.Close();
            ShowReminder(reminder.Copy(false), true);
        }

        private void ShowReminder(Reminder reminder, bool preview)
        {
            var window = new OverlayWindow(reminder, preview, delegate(int minutes)
            {
                if (preview) return;
                Data.Snoozes.RemoveAll(s => s.ReminderId == reminder.Id);
                Data.Snoozes.Add(new SnoozedReminder { ReminderId = reminder.Id, Due = DateTime.Now.AddMinutes(minutes) });
                Save();
            });
            overlay = window;
            window.Closed += delegate { if (overlay == window) overlay = null; };
            window.Show();
            Platform.PlaySound(reminder.Sound);
        }

        public void RemovePending(string id)
        {
            var keep = queue.Where(r => r.Id != id).ToList();
            queue.Clear();
            foreach (var r in keep) queue.Enqueue(r);
            Data.Snoozes.RemoveAll(s => s.ReminderId == id);
            if (overlay != null && overlay.ReminderId == id) overlay.Close();
        }

        public void Quit()
        {
            Exiting = true;
            if (timer != null) timer.Stop();
            if (overlay != null) overlay.Close();
            if (tray != null) { tray.IsVisible = false; tray.Dispose(); }
            desktop.Shutdown();
        }

        public static void Log(Exception error)
        {
            try { Directory.CreateDirectory(DataDirectory); File.AppendAllText(Path.Combine(DataDirectory, "error.log"), DateTime.Now + " " + error + Environment.NewLine); } catch { }
        }

    }
}
