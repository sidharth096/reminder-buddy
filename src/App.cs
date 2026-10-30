using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace ReminderBuddy
{
    public class BuddyApp : Application
    {
        public Preferences Data;
        public Store Storage;
        public SettingsWindow Settings;
        public bool Exiting;
        public bool SmokeTest;
        private Forms.NotifyIcon tray;
        private DispatcherTimer timer;
        private readonly Queue<Reminder> queue = new Queue<Reminder>();
        private OverlayWindow overlay;
        private DateTime lastTick = DateTime.Now;
        public static readonly string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReminderBuddy");

        [STAThread]
        public static void Main(string[] args)
        {
            bool smoke = args.Contains("--smoke-test");
            using (var mutex = new Mutex(false, "Local\\ReminderBuddy-v1" + (smoke ? "-test" : "")))
            {
                if (!mutex.WaitOne(0, false))
                {
                    foreach (var process in Process.GetProcessesByName("ReminderBuddy"))
                        if (process.Id != Process.GetCurrentProcess().Id && process.MainWindowHandle != IntPtr.Zero) { ShowWindow(process.MainWindowHandle, 9); SetForegroundWindow(process.MainWindowHandle); }
                    return;
                }
                try
                {
                    var app = new BuddyApp { ShutdownMode = ShutdownMode.OnExplicitShutdown, SmokeTest = smoke };
                    app.Start(args.Contains("--background"));
                    app.Run();
                }
                catch (Exception ex) { Log(ex); MessageBox.Show(ex.Message, "Reminder Buddy could not start", MessageBoxButton.OK, MessageBoxImage.Error); }
                finally { mutex.ReleaseMutex(); }
            }
        }

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
            DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e) { Log(e.Exception); MessageBox.Show(e.Exception.Message, "Reminder Buddy", MessageBoxButton.OK, MessageBoxImage.Error); e.Handled = true; };
            Settings = new SettingsWindow(this);
            MainWindow = Settings;
            tray = new Forms.NotifyIcon { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName), Text = "Reminder Buddy", Visible = true };
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open Reminder Buddy", null, delegate { OpenSettings(); });
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Pause for 30 minutes", null, delegate { Pause(DateTime.Now.AddMinutes(30)); });
            menu.Items.Add("Pause for 1 hour", null, delegate { Pause(DateTime.Now.AddHours(1)); });
            menu.Items.Add("Pause until tomorrow", null, delegate { Pause(DateTime.Today.AddDays(1)); });
            menu.Items.Add("Resume reminders", null, delegate { Pause(DateTime.MinValue); });
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { Quit(); });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { OpenSettings(); };
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Tick;
            timer.Start();
            if (!background || SmokeTest) OpenSettings();
            if (SmokeTest)
            {
                Preview(new Reminder { Name = "Drink water", Message = "Time for a water break. You've got this!", Persistent = false, DisplaySeconds = 5 });
                var exit = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
                exit.Tick += delegate { exit.Stop(); Quit(); };
                exit.Start();
            }
        }

        public void OpenSettings() { Settings.Show(); Settings.WindowState = WindowState.Normal; Settings.Activate(); Settings.RefreshStatus(); }
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
            if (overlay == null && queue.Count > 0 && now >= Data.PauseUntil && (!Data.DeferFullScreen || !IsFullScreen())) ShowReminder(queue.Dequeue(), false);
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
            if (reminder.Sound == "Chime") System.Media.SystemSounds.Asterisk.Play();
            if (reminder.Sound == "Bell") System.Media.SystemSounds.Exclamation.Play();
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
            if (tray != null) { tray.Visible = false; tray.Dispose(); }
            Shutdown();
        }

        public static bool StartsWithWindows()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) return key != null && key.GetValue("ReminderBuddy") != null;
        }
        public static void SetStartup(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (enabled) key.SetValue("ReminderBuddy", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\" --background"); else key.DeleteValue("ReminderBuddy", false);
                key.DeleteValue("TaskbarBuddy", false);
            }
        }
        public static void Log(Exception error)
        {
            try { Directory.CreateDirectory(DataDirectory); File.AppendAllText(Path.Combine(DataDirectory, "error.log"), DateTime.Now + " " + error + Environment.NewLine); } catch { }
        }

        private static bool IsFullScreen()
        {
            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero || handle == GetShellWindow() || handle == GetDesktopWindow()) return false;
            var className = new System.Text.StringBuilder(256);
            GetClassName(handle, className, className.Capacity);
            if (className.ToString() == "WorkerW" || className.ToString() == "Progman") return false;
            RectNative rect;
            if (!GetWindowRect(handle, out rect)) return false;
            var screen = Forms.Screen.FromHandle(handle).Bounds;
            return rect.Left <= screen.Left && rect.Top <= screen.Top && rect.Right >= screen.Right && rect.Bottom >= screen.Bottom;
        }
        [StructLayout(LayoutKind.Sequential)] private struct RectNative { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out RectNative rect);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr h, System.Text.StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr h, int command);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr h);
    }
}
