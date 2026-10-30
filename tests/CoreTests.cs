using System;
using System.IO;
using ReminderBuddy;

class CoreTests
{
    static int checks;
    static void Check(bool result, string title) { if (!result) throw new Exception("FAIL: " + title); checks++; Console.WriteLine("PASS: " + title); }
    static DateTime Monday = new DateTime(2026, 10, 26, 9, 0, 0);
    static int Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "ReminderBuddy-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var r = new Reminder { TimeMinutes = 540 };
            Check(Schedule.Due(r, Monday) == Monday, "Fixed alarm at selected time");
            Check(!Schedule.Due(r, Monday.AddSeconds(-1)).HasValue, "No early fixed alarm");
            Check(Schedule.Due(r, Monday.AddMinutes(4)).HasValue, "Grace after sleep");
            Check(!Schedule.Due(r, Monday.AddMinutes(6)).HasValue, "Ignore stale alarm");
            r.LastDelivered = Monday;
            Check(!Schedule.Due(r, Monday.AddSeconds(1)).HasValue, "No duplicate occurrence");
            Check(!Schedule.Due(r, Monday.AddSeconds(-1)).HasValue, "Clock rollback does not replay");
            r.LastDelivered = DateTime.MinValue; r.Enabled = false;
            Check(!Schedule.Due(r, Monday).HasValue, "Disabled reminder");
            r.Enabled = true; r.Days = 1 << (int)DayOfWeek.Tuesday;
            Check(!Schedule.Due(r, Monday).HasValue, "Selected weekdays respected");
            r.Days = 62; r.IsInterval = true;
            Check(Schedule.Due(r, Monday) == Monday, "Interval starts at active start");
            Check(Schedule.Due(r, Monday.AddHours(1)) == Monday.AddHours(1), "Hourly occurrence");
            Check(!Schedule.Due(r, Monday.AddMinutes(59)).HasValue, "No mid-interval occurrence");
            Check(!Schedule.Due(r, Monday.AddHours(9)).HasValue, "Active end is exclusive");
            Check(Schedule.Latest(r, Monday.AddHours(5).AddMinutes(2)) == Monday.AddHours(5), "Backlog collapses to latest");
            r.StartMinutes = 22 * 60; r.EndMinutes = 2 * 60; r.Days = 1 << (int)DayOfWeek.Monday;
            var overnight = Monday.Date.AddDays(1).AddHours(1);
            Check(Schedule.Due(r, overnight) == overnight, "Overnight uses starting weekday");
            Check(!Schedule.Due(r, overnight.AddHours(1)).HasValue, "Overnight end excluded");
            r.IntervalMinutes = 0;
            Check(!Schedule.Due(r, overnight).HasValue, "Invalid interval safely ignored");
            r.IntervalMinutes = 60;
            var copy = r.Copy(true);
            Check(copy.Id != r.Id && copy.Message == r.Message, "Duplicate has new identity");
            Check(CharacterCatalog.Find("tom").Name == "Tom", "Known character selection");
            Check(CharacterCatalog.Find("messi").Id == "tom", "First legacy character name migrates");
            Check(CharacterCatalog.Find("skyline-maestro").Id == "tom", "Second legacy character name migrates");
            Check(CharacterCatalog.Find("missing").Id == "buddy", "Missing character falls back safely");
            var store = new Store(Path.Combine(dir, "settings.json"));
            var data = new Preferences(); data.Reminders.Add(r);
            data.Snoozes.Add(new SnoozedReminder { ReminderId = r.Id, Due = Monday.AddMinutes(5) });
            store.Save(data); var loaded = store.Load();
            Check(loaded.Reminders[0].Id == r.Id, "JSON persistence");
            Check(loaded.Snoozes[0].Due == Monday.AddMinutes(5), "Snooze survives restart");
            data.PauseUntil = Monday.AddHours(1); store.Save(data);
            Check(store.Load().PauseUntil == data.PauseUntil && File.Exists(store.FilePath + ".bak"), "Atomic replace and backup");
            File.WriteAllText(store.FilePath, "invalid JSON");
            Check(store.Load().Reminders.Count == 1, "Recover corrupted settings from backup");
            Console.WriteLine("All " + checks + " checks passed."); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}
