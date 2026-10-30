using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ReminderBuddy
{
    [DataContract]
    public class Reminder
    {
        [DataMember] public string Id = Guid.NewGuid().ToString("N");
        [DataMember] public string Name = "New reminder";
        [DataMember] public string Message = "Time for a little break.";
        [DataMember] public int Days = 62;
        [DataMember] public bool Enabled = true;
        [DataMember] public bool IsInterval;
        [DataMember] public int TimeMinutes = 17 * 60 + 30;
        [DataMember] public int IntervalMinutes = 60;
        [DataMember] public int StartMinutes = 9 * 60;
        [DataMember] public int EndMinutes = 18 * 60;
        [DataMember] public bool Persistent = true;
        [DataMember] public int DisplaySeconds = 15;
        [DataMember] public string Sound = "None";
        [DataMember] public string CharacterId = "buddy";
        [DataMember] public DateTime LastDelivered = DateTime.MinValue;

        public Reminder Copy(bool newIdentity)
        {
            var copy = (Reminder)MemberwiseClone();
            if (newIdentity) { copy.Id = Guid.NewGuid().ToString("N"); copy.LastDelivered = DateTime.MinValue; }
            return copy;
        }

        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80) return "Enter a name between 1 and 80 characters.";
            if (string.IsNullOrWhiteSpace(Message) || Message.Length > 220) return "Enter a message between 1 and 220 characters.";
            if (Days <= 0 || Days > 127) return "Select at least one day.";
            if (TimeMinutes < 0 || TimeMinutes > 1439 || StartMinutes < 0 || StartMinutes > 1439 || EndMinutes < 0 || EndMinutes > 1439) return "Use times between 00:00 and 23:59.";
            if (IntervalMinutes < 1 || IntervalMinutes > 1440) return "Choose an interval between 1 and 1440 minutes.";
            if (IsInterval && StartMinutes == EndMinutes) return "Start and end times must be different.";
            if (DisplaySeconds < 5 || DisplaySeconds > 300) return "Auto-hide duration must be between 5 and 300 seconds.";
            if (Sound != "None" && Sound != "Chime" && Sound != "Bell") return "Select a valid sound.";
            if (!CharacterCatalog.IsValid(CharacterId)) return "Select a valid character.";
            return null;
        }

        public string Summary
        {
            get
            {
                string days = Days == 127 ? "Every day" : Days == 62 ? "Weekdays" : string.Join(", ", Enumerable.Range(0, 7).Where(d => (Days & (1 << d)) != 0).Select(d => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[d]));
                return days + "  |  " + (IsInterval ? "Every " + IntervalMinutes + " min, " + FormatTime(StartMinutes) + " - " + FormatTime(EndMinutes) : FormatTime(TimeMinutes));
            }
        }
        public static string FormatTime(int minutes) { return string.Format("{0:00}:{1:00}", minutes / 60, minutes % 60); }
    }

    public class CharacterChoice
    {
        public string Id;
        public string Name;
    }

    public static class CharacterCatalog
    {
        public static readonly CharacterChoice[] All =
        {
            new CharacterChoice { Id = "buddy", Name = "Buddy" },
            new CharacterChoice { Id = "abu", Name = "Abu" },
            new CharacterChoice { Id = "shibu", Name = "Shibu" },
            new CharacterChoice { Id = "tom", Name = "Tom" }
        };

        public static string NormalizeId(string id)
        {
            if (id == "original") return "buddy";
            if (id == "mohanlal" || id == "mellow-monarch") return "abu";
            if (id == "ronaldo" || id == "crimson-comet") return "shibu";
            if (id == "messi" || id == "skyline-maestro") return "tom";
            return string.IsNullOrEmpty(id) ? "buddy" : id;
        }
        public static bool IsValid(string id) { return All.Any(c => c.Id == NormalizeId(id)); }
        public static CharacterChoice Find(string id) { return All.FirstOrDefault(c => c.Id == NormalizeId(id)) ?? All[0]; }
    }

    [DataContract]
    public class Preferences
    {
        [DataMember] public List<Reminder> Reminders = new List<Reminder>();
        [DataMember] public bool DeferFullScreen = true;
        [DataMember] public DateTime PauseUntil = DateTime.MinValue;
        [DataMember] public List<SnoozedReminder> Snoozes = new List<SnoozedReminder>();
    }

    [DataContract]
    public class SnoozedReminder
    {
        [DataMember] public string ReminderId;
        [DataMember] public DateTime Due;
    }

    public static class Schedule
    {
        // Return only the latest occurrence: wake-up never replays a backlog.
        public static DateTime? Latest(Reminder r, DateTime now)
        {
            DateTime? latest = null;
            for (int offset = 0; offset <= 7; offset++)
            {
                DateTime date = now.Date.AddDays(-offset);
                if ((r.Days & (1 << (int)date.DayOfWeek)) == 0) continue;
                DateTime due;
                if (!r.IsInterval) due = date.AddMinutes(r.TimeMinutes);
                else
                {
                    DateTime start = date.AddMinutes(r.StartMinutes);
                    DateTime end = date.AddMinutes(r.EndMinutes);
                    if (end <= start) end = end.AddDays(1);
                    if (now < start) continue;
                    // End is exclusive; the first occurrence is at the start time.
                    DateTime limit = now < end ? now : end.AddTicks(-1);
                    long steps = (long)Math.Floor((limit - start).TotalMinutes / r.IntervalMinutes);
                    due = start.AddMinutes(steps * r.IntervalMinutes);
                }
                if (due <= now && (!latest.HasValue || due > latest.Value)) latest = due;
            }
            return latest;
        }

        public static DateTime? Due(Reminder r, DateTime now)
        {
            if (!r.Enabled || r.Validate() != null) return null;
            var latest = Latest(r, now);
            // A five-minute grace catches sleep/brief delays, but not yesterday's alarms.
            if (latest.HasValue && latest.Value > r.LastDelivered && now - latest.Value <= TimeSpan.FromMinutes(5)) return latest;
            return null;
        }
    }

    public class Store
    {
        public readonly string FilePath;
        public Store(string path) { FilePath = path; }
        private static DataContractJsonSerializer Serializer()
        {
            return new DataContractJsonSerializer(typeof(Preferences), new DataContractJsonSerializerSettings { DateTimeFormat = new DateTimeFormat("o") });
        }

        public Preferences Load()
        {
            if (!File.Exists(FilePath)) return new Preferences();
            try { return Read(FilePath); }
            catch (Exception ex)
            {
                if (!(ex is SerializationException) && !(ex is ArgumentException) && !(ex is System.Xml.XmlException)) throw;
                string copy = FilePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Copy(FilePath, copy, true);
                if (File.Exists(FilePath + ".bak"))
                {
                    try { return Read(FilePath + ".bak"); } catch (SerializationException) { }
                }
                throw new InvalidDataException("Settings are damaged. Your original file was preserved at " + copy, ex);
            }
        }

        private static Preferences Read(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var data = (Preferences)Serializer().ReadObject(stream);
                if (data == null || data.Reminders == null) throw new SerializationException("Invalid settings.");
                if (data.Snoozes == null) data.Snoozes = new List<SnoozedReminder>();
                foreach (var reminder in data.Reminders) if (reminder != null) reminder.CharacterId = CharacterCatalog.NormalizeId(reminder.CharacterId);
                if (data.Reminders.Any(r => r == null || string.IsNullOrWhiteSpace(r.Id) || r.Validate() != null) || data.Reminders.Select(r => r.Id).Distinct().Count() != data.Reminders.Count) throw new SerializationException("Invalid reminder.");
                return data;
            }
        }

        public void Save(Preferences data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temp = FilePath + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Serializer().WriteObject(stream, data);
                stream.Flush(true);
            }
            if (File.Exists(FilePath)) File.Replace(temp, FilePath, FilePath + ".bak");
            else File.Move(temp, FilePath);
        }
    }
}
