using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Fridays_Adventure.Data
{
    public sealed class HighScoreEntry
    {
        public string Name  { get; set; }
        public int    Score { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    // PHASE 2 - Team 8: Systems / Tools Programmer
    // Feature: JSON save/load snapshot support
    // Purpose: Export/import game progress as a readable JSON file.
    // ────────────────────────────────────────────────────────────
    [DataContract]
    internal sealed class SaveDataJsonModel
    {
        [DataMember] public int PlayerBounty { get; set; }
        [DataMember] public float ThreatLevel { get; set; }
        [DataMember] public int CrewBonds { get; set; }
        [DataMember] public int ShipHealth { get; set; }
        [DataMember] public int Water { get; set; }
        [DataMember] public int Food { get; set; }
        [DataMember] public int SeaStoneCount { get; set; }
        [DataMember] public string CurrentNodeId { get; set; }
        [DataMember] public int MusicVolume { get; set; }
        [DataMember] public int SfxVolume { get; set; }
        [DataMember] public Dictionary<string, string> PlaylistData { get; set; }
        [DataMember] public Dictionary<string, bool> Flags { get; set; }
        [DataMember] public Dictionary<string, int> Progress { get; set; }
        [DataMember] public Dictionary<string, int> Ints { get; set; }
        [DataMember] public List<HighScoreEntry> HighScores { get; set; }
    }

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

        // ── Team 1 (Game Director) — generic int store for star ratings, P-Wings, etc. ──
        // Idea 3 & 7: integer key-value pairs persisted alongside flags.
        private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();

        public List<HighScoreEntry> HighScores { get; } = new List<HighScoreEntry>();

        public void AddHighScore(string name, int score)
        {
            HighScores.Add(new HighScoreEntry { Name = name, Score = score });
            HighScores.Sort((a, b) => b.Score.CompareTo(a.Score));
            while (HighScores.Count > 10) HighScores.RemoveAt(HighScores.Count - 1);
        }

        public List<HighScoreEntry> GetTopScores(int count = 5)
        {
            return HighScores.Take(count).ToList();
        }

        private static string SavePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "MissFridaysAdventure", "save.dat");

        /// <summary>
        /// Default JSON save path used by dev tools and emergency backups.
        /// </summary>
        public static string JsonSavePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "MissFridaysAdventure", "save.json");

        public void SetFlag(string flag, bool value = true) => _flags[flag] = value;
        public bool GetFlag(string flag) => _flags.TryGetValue(flag, out bool v) && v;
        public void SetProgress(string island, int value) => _progress[island] = value;
        public int  GetProgress(string island) => _progress.TryGetValue(island, out int v) ? v : 0;

        /// <summary>
        /// Stores an arbitrary integer value by key (e.g. star ratings, P-Wing count).
        /// Team 1 (Game Director) — Idea 3: per-level star ratings.
        /// </summary>
        public void SetInt(string key, int value) => _ints[key] = value;

        /// <summary>
        /// Retrieves an integer stored by <see cref="SetInt"/>.
        /// Returns <paramref name="defaultValue"/> if the key has never been written.
        /// Team 1 (Game Director) — Idea 3.
        /// </summary>
        public int GetInt(string key, int defaultValue = 0) =>
            _ints.TryGetValue(key, out int v) ? v : defaultValue;

        // ── Team 1 (Game Director) — Save slot support ────────────────────────
        // Idea 7: slot 0/1/2 map to three separate save files.

        /// <summary>
        /// Returns the file path for the given save slot (0–2).
        /// Team 1 (Game Director) — Idea 7.
        /// </summary>
        public static string SavePathForSlot(int slot) =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "MissFridaysAdventure",
                         slot == 0 ? "save.dat" : $"save_slot{slot}.dat");

        /// <summary>
        /// Loads the save file for the given slot.
        /// Falls back to a fresh <see cref="SaveData"/> if the file is missing or corrupt.
        /// Team 1 (Game Director) — Idea 7.
        /// </summary>
        public static SaveData LoadSlot(int slot)
        {
            return LoadFromPath(SavePathForSlot(slot));
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                var lines = new StringBuilder();
                lines.AppendLine("PlayerBounty=" + PlayerBounty);
                lines.AppendLine("ThreatLevel="  + ThreatLevel.ToString(CultureInfo.InvariantCulture));
                lines.AppendLine("CrewBonds="    + CrewBonds);
                lines.AppendLine("ShipHealth="   + ShipHealth);
                lines.AppendLine("Water="        + Water);
                lines.AppendLine("Food="         + Food);
                lines.AppendLine("SeaStoneCount=" + SeaStoneCount);
                lines.AppendLine("CurrentNodeId=" + CurrentNodeId);
                lines.AppendLine("MusicVolume="  + MusicVolume);
                lines.AppendLine("SfxVolume="    + SfxVolume);
                foreach (var kv in PlaylistData)
                    lines.AppendLine("Playlist." + kv.Key + "=" + kv.Value);
                foreach (var kv in _flags)
                    lines.AppendLine("Flag." + kv.Key + "=" + kv.Value);
                foreach (var kv in _progress)
                    lines.AppendLine("Progress." + kv.Key + "=" + kv.Value);
                // Persist generic ints (star ratings, P-Wing count, etc.)
                foreach (var kv in _ints)
                    lines.AppendLine("Int." + kv.Key + "=" + kv.Value);
                for (int i = 0; i < HighScores.Count; i++)
                    lines.AppendLine("HighScore." + i + "=" + HighScores[i].Name + "|" + HighScores[i].Score);
                File.WriteAllText(SavePath, lines.ToString());
            }
            catch { /* non-fatal */ }
        }

        public static SaveData Load() => LoadFromPath(SavePath);

        /// <summary>
        /// Loads a <see cref="SaveData"/> from a specific file path.
        /// Used by <see cref="Load()"/> and <see cref="LoadSlot(int)"/>.
        /// Team 1 (Game Director) — Idea 7: save-slot support.
        /// </summary>
        public static SaveData LoadFromPath(string path)
        {
            var data = new SaveData();
            if (!File.Exists(path)) return data;
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case "PlayerBounty":   data.PlayerBounty  = int.Parse(val);    break;
                        case "ThreatLevel":    data.ThreatLevel   = float.Parse(val, CultureInfo.InvariantCulture);  break;
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
                            else if (key.StartsWith("Int."))
                                data._ints[key.Substring(4)] = int.Parse(val);
                            else if (key.StartsWith("HighScore."))
                            {
                                int sep = val.IndexOf('|');
                                if (sep > 0)
                                    data.HighScores.Add(new HighScoreEntry
                                    {
                                        Name  = val.Substring(0, sep),
                                        Score = int.Parse(val.Substring(sep + 1))
                                    });
                            }
                            break;
                    }
                }
            }
            catch { /* return defaults on corrupt save */ }
            return data;
        }

        /// <summary>
        /// Exports the current save state to a JSON file.
        /// </summary>
        /// <remarks>PHASE 2 - Team 8: JSON save export.</remarks>
        public void SaveJson(string path = null)
        {
            string finalPath = string.IsNullOrWhiteSpace(path) ? JsonSavePath : path;
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath));
            File.WriteAllText(finalPath, ToJson(), Encoding.UTF8);
        }

        /// <summary>
        /// Loads a save state from a JSON file path.
        /// Returns default save data if missing/corrupt.
        /// </summary>
        /// <remarks>PHASE 2 - Team 8: JSON save import.</remarks>
        public static SaveData LoadJson(string path = null)
        {
            string finalPath = string.IsNullOrWhiteSpace(path) ? JsonSavePath : path;
            if (!File.Exists(finalPath)) return new SaveData();
            try
            {
                string json = File.ReadAllText(finalPath, Encoding.UTF8);
                return FromJson(json);
            }
            catch
            {
                return new SaveData();
            }
        }

        /// <summary>
        /// Serializes save data to JSON text.
        /// </summary>
        /// <remarks>PHASE 2 - Team 8: JSON save export.</remarks>
        public string ToJson()
        {
            var model = new SaveDataJsonModel
            {
                PlayerBounty = PlayerBounty,
                ThreatLevel = ThreatLevel,
                CrewBonds = CrewBonds,
                ShipHealth = ShipHealth,
                Water = Water,
                Food = Food,
                SeaStoneCount = SeaStoneCount,
                CurrentNodeId = CurrentNodeId,
                MusicVolume = MusicVolume,
                SfxVolume = SfxVolume,
                PlaylistData = new Dictionary<string, string>(PlaylistData, StringComparer.OrdinalIgnoreCase),
                Flags = new Dictionary<string, bool>(_flags),
                Progress = new Dictionary<string, int>(_progress),
                Ints = new Dictionary<string, int>(_ints),
                HighScores = HighScores.Select(h => new HighScoreEntry { Name = h.Name, Score = h.Score }).ToList()
            };

            var serializer = new DataContractJsonSerializer(typeof(SaveDataJsonModel));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, model);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Deserializes JSON text into a <see cref="SaveData"/> instance.
        /// </summary>
        /// <remarks>PHASE 2 - Team 8: JSON save import.</remarks>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new SaveData();

            var serializer = new DataContractJsonSerializer(typeof(SaveDataJsonModel));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var model = serializer.ReadObject(ms) as SaveDataJsonModel;
                if (model == null) return new SaveData();

                var data = new SaveData
                {
                    PlayerBounty = model.PlayerBounty,
                    ThreatLevel = model.ThreatLevel,
                    CrewBonds = model.CrewBonds,
                    ShipHealth = model.ShipHealth,
                    Water = model.Water,
                    Food = model.Food,
                    SeaStoneCount = model.SeaStoneCount,
                    CurrentNodeId = string.IsNullOrWhiteSpace(model.CurrentNodeId) ? "start" : model.CurrentNodeId,
                    MusicVolume = model.MusicVolume <= 0 ? 80 : model.MusicVolume,
                    SfxVolume = model.SfxVolume <= 0 ? 80 : model.SfxVolume
                };

                if (model.PlaylistData != null)
                    foreach (var kv in model.PlaylistData)
                        data.PlaylistData[kv.Key] = kv.Value;

                if (model.Flags != null)
                    foreach (var kv in model.Flags)
                        data._flags[kv.Key] = kv.Value;

                if (model.Progress != null)
                    foreach (var kv in model.Progress)
                        data._progress[kv.Key] = kv.Value;

                if (model.Ints != null)
                    foreach (var kv in model.Ints)
                        data._ints[kv.Key] = kv.Value;

                if (model.HighScores != null)
                    foreach (var hs in model.HighScores)
                        data.HighScores.Add(new HighScoreEntry { Name = hs.Name, Score = hs.Score });

                return data;
            }
        }
    }
}
