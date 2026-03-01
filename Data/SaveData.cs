using System;
using System.Collections.Generic;
using System.IO;

namespace Fridays_Adventure.Data
{
    public sealed class SaveData
    {
        public int    PlayerBounty  { get; set; }
        public float  ThreatLevel   { get; set; }
        public int    CrewBonds     { get; set; }
        public int    ShipHealth    { get; set; } = 100;
        public int    Water         { get; set; } = 50;
        public int    Food          { get; set; } = 30;
        public int    SeaStoneCount { get; set; }
        public string CurrentNodeId { get; set; } = "start";
        public int    MusicVolume   { get; set; } = 80;
        public int    SfxVolume     { get; set; } = 80;

        public Dictionary<string, string> PlaylistData { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, bool> _flags    = new Dictionary<string, bool>();
        private readonly Dictionary<string, int>  _progress = new Dictionary<string, int>();

        private static string SavePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "MissFridaysAdventure", "save.dat");

        public void SetFlag(string flag, bool value = true) => _flags[flag] = value;
        public bool GetFlag(string flag) => _flags.TryGetValue(flag, out bool v) && v;
        public void SetProgress(string island, int value) => _progress[island] = value;
        public int  GetProgress(string island) => _progress.TryGetValue(island, out int v) ? v : 0;

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                var lines = new System.Text.StringBuilder();
                lines.AppendLine("PlayerBounty=" + PlayerBounty);
                lines.AppendLine("ThreatLevel="  + ThreatLevel);
                lines.AppendLine("CrewBonds="    + CrewBonds);
                lines.AppendLine("ShipHealth="   + ShipHealth);
                lines.AppendLine("Water="        + Water);
                lines.AppendLine("Food="         + Food);
                lines.AppendLine("SeaStoneCount="+ SeaStoneCount);
                lines.AppendLine("CurrentNodeId="+ CurrentNodeId);
                lines.AppendLine("MusicVolume="  + MusicVolume);
                lines.AppendLine("SfxVolume="    + SfxVolume);
                foreach (var kv in PlaylistData)
                    lines.AppendLine("Playlist." + kv.Key + "=" + kv.Value);
                foreach (var kv in _flags)
                    lines.AppendLine("Flag." + kv.Key + "=" + kv.Value);
                foreach (var kv in _progress)
                    lines.AppendLine("Progress." + kv.Key + "=" + kv.Value);
                File.WriteAllText(SavePath, lines.ToString());
            }
            catch { /* non-fatal */ }
        }

        public static SaveData Load()
        {
            var data = new SaveData();
            if (!File.Exists(SavePath)) return data;
            try
            {
                foreach (string line in File.ReadAllLines(SavePath))
                {
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case "PlayerBounty":   data.PlayerBounty  = int.Parse(val);    break;
                        case "ThreatLevel":    data.ThreatLevel   = float.Parse(val);  break;
                        case "CrewBonds":      data.CrewBonds     = int.Parse(val);    break;
                        case "ShipHealth":     data.ShipHealth    = int.Parse(val);    break;
                        case "Water":          data.Water         = int.Parse(val);    break;
                        case "Food":           data.Food          = int.Parse(val);    break;
                        case "SeaStoneCount":  data.SeaStoneCount = int.Parse(val);    break;
                        case "CurrentNodeId":  data.CurrentNodeId = val;               break;
                        case "MusicVolume":    data.MusicVolume   = int.Parse(val);    break;
                        case "SfxVolume":      data.SfxVolume     = int.Parse(val);    break;
                        default:
                            if (key.StartsWith("Playlist."))
                                data.PlaylistData[key.Substring(9)] = val;
                            else if (key.StartsWith("Flag."))
                                data._flags[key.Substring(5)] = bool.Parse(val);
                            else if (key.StartsWith("Progress."))
                                data._progress[key.Substring(9)] = int.Parse(val);
                            break;
                    }
                }
            }
            catch { /* return defaults on corrupt save */ }
            return data;
        }
    }
}
