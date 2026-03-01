using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fridays_Adventure.Data
{
    public sealed class WaterEntry
    {
        public DateTime Time   { get; set; }
        public int      Amount { get; set; }
        public WaterEntry(int amount) { Amount = amount; Time = DateTime.Now; }
    }

    public sealed class LogbookData
    {
        public int    DailyGoalMl     { get; set; } = 2000;
        public int    TodayTotalMl    { get; private set; }
        public List<WaterEntry> TodayLog { get; } = new List<WaterEntry>();
        public List<string>     CargoItems { get; } = new List<string>();
        public List<bool>       CargoDone  { get; } = new List<bool>();
        public string           Notes      { get; set; } = "";
        public DateTime         LastReset  { get; set; } = DateTime.Today;

        private static string SavePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "MissFridaysAdventure", "logbook.dat");

        public void AddWater(int ml)
        {
            TodayLog.Add(new WaterEntry(ml));
            TodayTotalMl += ml;
        }

        public void ClearToday()
        {
            TodayLog.Clear();
            TodayTotalMl = 0;
        }

        public void ResetIfNewDay()
        {
            if (LastReset.Date < DateTime.Today)
            { ClearToday(); LastReset = DateTime.Today; }
        }

        public float GoalPercent => DailyGoalMl > 0
            ? Math.Min(1f, (float)TodayTotalMl / DailyGoalMl) : 0f;

        public void AddCargoItem(string item)
        {
            CargoItems.Add(item);
            CargoDone.Add(false);
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                var sb = new StringBuilder();
                sb.AppendLine("DailyGoal=" + DailyGoalMl);
                sb.AppendLine("LastReset=" + LastReset.ToString("o"));
                sb.AppendLine("Notes=" + Notes.Replace("\n", "\\n"));
                foreach (var e in TodayLog)
                    sb.AppendLine($"Water={e.Amount}|{e.Time:HH:mm}");
                for (int i = 0; i < CargoItems.Count; i++)
                    sb.AppendLine($"Cargo={CargoItems[i]}|{CargoDone[i]}");
                File.WriteAllText(SavePath, sb.ToString());
            }
            catch { }
        }

        public static LogbookData Load()
        {
            var d = new LogbookData();
            if (!File.Exists(SavePath)) { AddDefaultCargo(d); return d; }
            try
            {
                foreach (string line in File.ReadAllLines(SavePath))
                {
                    int eq = line.IndexOf('='); if (eq < 0) continue;
                    string k = line.Substring(0, eq), v = line.Substring(eq + 1);
                    if (k == "DailyGoal") d.DailyGoalMl = int.Parse(v);
                    else if (k == "LastReset") d.LastReset = DateTime.Parse(v);
                    else if (k == "Notes") d.Notes = v.Replace("\\n", "\n");
                    else if (k == "Water")
                    {
                        var parts = v.Split('|');
                        var entry = new WaterEntry(int.Parse(parts[0]));
                        if (parts.Length > 1 && TimeSpan.TryParse(parts[1], out var ts))
                            entry.Time = DateTime.Today.Add(ts);
                        d.TodayLog.Add(entry);
                        d.TodayTotalMl += entry.Amount;
                    }
                    else if (k == "Cargo")
                    {
                        var parts = v.Split('|');
                        d.CargoItems.Add(parts[0]);
                        d.CargoDone.Add(parts.Length > 1 && bool.Parse(parts[1]));
                    }
                }
            }
            catch { AddDefaultCargo(d); }
            d.ResetIfNewDay();
            return d;
        }

        private static void AddDefaultCargo(LogbookData d)
        {
            string[] defaults = { "Rations (3 days)", "Rope (50m)", "Medical kit", "Navigation charts", "Sea Stone detector" };
            foreach (var item in defaults) d.AddCargoItem(item);
        }
    }
}
