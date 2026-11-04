using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Avalonia.Controls;
using Microsoft.Win32;

namespace ReminderBuddy
{
    internal static class Platform
    {
        private const string StartupKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private static string LoginFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", "com.reminderbuddy.app.plist");
        public static bool SupportsFullScreenDetection => OperatingSystem.IsWindows();

        public static bool StartsAtLogin()
        {
            if (OperatingSystem.IsMacOS()) return File.Exists(LoginFile);
            if (!OperatingSystem.IsWindows()) return false;
            using var key = Registry.CurrentUser.OpenSubKey(StartupKey);
            return key?.GetValue("ReminderBuddy") != null;
        }

        public static void SetStartup(bool enabled)
        {
            string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate the application.");
            if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Enable login startup from the installed app, not dotnet run.");
            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.CurrentUser.CreateSubKey(StartupKey);
                if (enabled) key.SetValue("ReminderBuddy", "\"" + executable + "\" --background");
                else key.DeleteValue("ReminderBuddy", false);
                key.DeleteValue("TaskbarBuddy", false);
            }
            else if (OperatingSystem.IsMacOS())
            {
                if (!enabled) { if (File.Exists(LoginFile)) File.Delete(LoginFile); return; }
                if (!executable.Contains(".app/Contents/MacOS/", StringComparison.Ordinal))
                    throw new InvalidOperationException("Move Reminder Buddy.app to Applications and open it there before enabling login startup.");
                Directory.CreateDirectory(Path.GetDirectoryName(LoginFile));
                var plist = new XDocument(new XDeclaration("1.0", "utf-8", null),
                    new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
                    new XElement("plist", new XAttribute("version", "1.0"), new XElement("dict",
                        new XElement("key", "Label"), new XElement("string", "com.reminderbuddy.app"),
                        new XElement("key", "ProgramArguments"), new XElement("array", new XElement("string", executable), new XElement("string", "--background")),
                        new XElement("key", "RunAtLoad"), new XElement("true"))));
                plist.Save(LoginFile);
            }
            else throw new PlatformNotSupportedException("Login startup is supported on Windows and macOS.");
        }

        public static void PlaySound(string sound)
        {
            if (sound == "None") return;
            if (OperatingSystem.IsWindows()) MessageBeep(sound == "Bell" ? 0x30u : 0x40u);
            else if (OperatingSystem.IsMacOS())
            {
                var info = new ProcessStartInfo("/usr/bin/afplay") { UseShellExecute = false };
                info.ArgumentList.Add(sound == "Bell" ? "/System/Library/Sounds/Glass.aiff" : "/System/Library/Sounds/Ping.aiff");
                try { Process.Start(info)?.Dispose(); } catch (Exception ex) { BuddyApp.Log(ex); }
            }
        }

        public static void ConfigureOverlay(Window window)
        {
            var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero) return;
            if (OperatingSystem.IsWindows())
            {
                int style = GetWindowLong(handle, -20);
                SetWindowLong(handle, -20, style | 0x08000000 | 0x00000080);
                Win32Properties.AddWndProcHookCallback(window, NoActivate);
            }
            // ShowActivated=false and Topmost are handled by Avalonia's native macOS backend.
        }

        private static IntPtr NoActivate(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == 0x0021) { handled = true; return new IntPtr(3); }
            return IntPtr.Zero;
        }

        public static void ActivateExisting()
        {
            if (!OperatingSystem.IsWindows()) return;
            foreach (var process in Process.GetProcessesByName("ReminderBuddy"))
            {
                using (process)
                    if (process.Id != Environment.ProcessId && process.MainWindowHandle != IntPtr.Zero)
                    { ShowWindow(process.MainWindowHandle, 9); SetForegroundWindow(process.MainWindowHandle); }
            }
        }

        public static bool IsFullScreen(Window window)
        {
            if (!OperatingSystem.IsWindows()) return false;
            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero || handle == GetShellWindow() || handle == GetDesktopWindow()) return false;
            var name = new StringBuilder(256);
            GetClassName(handle, name, name.Capacity);
            if (name.ToString() == "WorkerW" || name.ToString() == "Progman") return false;
            if (!GetWindowRect(handle, out var rect)) return false;
            var screen = window.Screens.ScreenFromPoint(new Avalonia.PixelPoint(rect.Left, rect.Top));
            if (screen == null) return false;
            var bounds = screen.Bounds;
            return rect.Left <= bounds.X && rect.Top <= bounds.Y && rect.Right >= bounds.Right && rect.Bottom >= bounds.Bottom;
        }

        [StructLayout(LayoutKind.Sequential)] private struct RectNative { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")] private static extern bool MessageBeep(uint type);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr h, int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr h, int index, int value);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out RectNative rect);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr h, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr h, int command);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr h);
    }
}
