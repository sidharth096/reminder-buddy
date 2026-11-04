using System;
using System.Threading;
using Avalonia;

namespace ReminderBuddy
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            bool smoke = Array.IndexOf(args, "--smoke-test") >= 0;
            using var mutex = new Mutex(false, (OperatingSystem.IsWindows() ? "Local\\" : "") + "ReminderBuddy-v1" + (smoke ? "-test" : ""));
            bool acquired;
            try { acquired = mutex.WaitOne(0, false); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) { Platform.ActivateExisting(); return; }
            try
            {
                AppBuilder.Configure<BuddyApp>().UsePlatformDetect()
                    .With(new MacOSPlatformOptions { ShowInDock = false })
                    .LogToTrace().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex) { BuddyApp.Log(ex); throw; }
            finally { mutex.ReleaseMutex(); }
        }
    }
}
