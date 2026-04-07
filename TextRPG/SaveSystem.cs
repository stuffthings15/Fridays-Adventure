// ────────────────────────────────────────────────────────────
// TEXT RPG — Save / Load System (Multi-Slot)
// Purpose: Persist and restore game state using simple text format.
//          Supports 3 save slots (Zelda-style) stored as
//          savegame_slot1.txt, savegame_slot2.txt, savegame_slot3.txt.
//          No external dependencies — pure System.IO.
// ────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO;

namespace TextRPG
{
    /// <summary>
    /// Serializes/deserializes GameState to key=value text files.
    /// Supports 3 independent save slots (1, 2, 3) like classic Zelda.
    /// Each slot is stored as "savegame_slotN.txt" next to the executable.
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>Total number of available save slots.</summary>
        public const int SlotCount = 3;

        /// <summary>Returns the file path for a given save slot (1-based).</summary>
        private static string GetSlotPath(int slot)
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"savegame_slot{slot}.txt");
        }

        // ── Legacy single-file path (for backward compatibility) ──────
        private static readonly string LegacySavePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "savegame.txt");

        /// <summary>
        /// Write the current game state to the specified save slot.
        /// Also records the save timestamp for the slot summary display.
        /// </summary>
        public static void Save(GameState state, int slot)
        {
            var lines = new List<string>
            {
                "PlayerName=" + state.PlayerName,
                "Health=" + state.Health,
                "MaxHealth=" + state.MaxHealth,
                "BaseAttack=" + state.BaseAttack,
                "BaseDefense=" + state.BaseDefense,
                "CurrentRoomId=" + state.CurrentRoomId,
                "EquippedWeapon=" + (state.EquippedWeaponName ?? ""),
                "EquippedArmor=" + (state.EquippedArmorName ?? ""),
                "Inventory=" + string.Join("|", state.InventoryNames),
                "ClearedRooms=" + string.Join("|", state.ClearedRoomIds),
                "SaveTime=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            File.WriteAllLines(GetSlotPath(slot), lines);
        }

        /// <summary>Legacy overload — saves to slot 1 for backward compatibility.</summary>
        public static void Save(GameState state) => Save(state, 1);

        /// <summary>Load a previously saved game from the specified slot, or null if empty.</summary>
        public static GameState Load(int slot)
        {
            string path = GetSlotPath(slot);
            if (!File.Exists(path)) return null;
            return LoadFromFile(path);
        }

        /// <summary>Legacy overload — loads from slot 1, falls back to old savegame.txt.</summary>
        public static GameState Load()
        {
            // Try slot 1 first; fall back to legacy single-file for old saves
            var state = Load(1);
            if (state != null) return state;
            if (File.Exists(LegacySavePath))
                return LoadFromFile(LegacySavePath);
            return null;
        }

        /// <summary>True if the specified save slot has data on disk.</summary>
        public static bool SaveExists(int slot) => File.Exists(GetSlotPath(slot));

        /// <summary>Legacy overload — checks slot 1 or old single file.</summary>
        public static bool SaveExists() => SaveExists(1) || File.Exists(LegacySavePath);

        /// <summary>
        /// Returns a human-readable summary string for a save slot,
        /// showing the player name, HP, location, and save date.
        /// Returns null if the slot is empty.
        /// </summary>
        public static SlotSummary GetSlotSummary(int slot)
        {
            string path = GetSlotPath(slot);
            if (!File.Exists(path)) return null;

            try
            {
                var dict = ParseFile(path);
                return new SlotSummary
                {
                    PlayerName  = dict.ContainsKey("PlayerName") ? dict["PlayerName"] : "???",
                    Health      = dict.ContainsKey("Health") ? int.Parse(dict["Health"]) : 0,
                    MaxHealth   = dict.ContainsKey("MaxHealth") ? int.Parse(dict["MaxHealth"]) : 0,
                    RoomId      = dict.ContainsKey("CurrentRoomId") ? dict["CurrentRoomId"] : "",
                    SaveTime    = dict.ContainsKey("SaveTime") ? dict["SaveTime"] : "Unknown",
                    ItemCount   = dict.ContainsKey("Inventory")
                        ? dict["Inventory"].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Length
                        : 0
                };
            }
            catch { return null; }
        }

        /// <summary>Delete a save slot from disk.</summary>
        public static void DeleteSlot(int slot)
        {
            string path = GetSlotPath(slot);
            if (File.Exists(path))
                File.Delete(path);
        }

        // ── Internal helpers ──────────────────────────────────────────

        /// <summary>Parse a key=value save file into a dictionary.</summary>
        private static Dictionary<string, string> ParseFile(string path)
        {
            var dict = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(path))
            {
                int eq = line.IndexOf('=');
                if (eq > 0)
                    dict[line.Substring(0, eq)] = line.Substring(eq + 1);
            }
            return dict;
        }

        /// <summary>Deserialize a GameState from a save file path.</summary>
        private static GameState LoadFromFile(string path)
        {
            try
            {
                var dict = ParseFile(path);
                return new GameState
                {
                    PlayerName = dict["PlayerName"],
                    Health = int.Parse(dict["Health"]),
                    MaxHealth = int.Parse(dict["MaxHealth"]),
                    BaseAttack = int.Parse(dict["BaseAttack"]),
                    BaseDefense = int.Parse(dict["BaseDefense"]),
                    CurrentRoomId = dict["CurrentRoomId"],
                    EquippedWeaponName = dict.ContainsKey("EquippedWeapon") ? dict["EquippedWeapon"] : "",
                    EquippedArmorName = dict.ContainsKey("EquippedArmor") ? dict["EquippedArmor"] : "",
                    InventoryNames = new List<string>(
                        dict["Inventory"].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)),
                    ClearedRoomIds = new List<string>(
                        dict["ClearedRooms"].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                };
            }
            catch { return null; }
        }
    }

    /// <summary>
    /// Lightweight summary of a save slot for the slot-selection UI.
    /// Contains just enough info to display without fully deserializing.
    /// </summary>
    public class SlotSummary
    {
        public string PlayerName { get; set; }
        public int    Health     { get; set; }
        public int    MaxHealth  { get; set; }
        public string RoomId    { get; set; }
        public string SaveTime  { get; set; }
        public int    ItemCount { get; set; }

        /// <summary>Friendly room name from the room ID.</summary>
        public string RoomDisplayName
        {
            get
            {
                switch (RoomId)
                {
                    case "village_square": return "Village Square";
                    case "forest":        return "Dark Forest";
                    case "goblin_cave":   return "Goblin's Cave";
                    case "library":       return "Ancient Library";
                    case "riverbank":     return "Misty Riverbank";
                    case "troll_bridge":  return "Troll's Bridge";
                    case "shrine":        return "Ancient Shrine";
                    case "crystal_hall":  return "Crystal Hall";
                    case "dragon_lair":   return "Dragon's Lair";
                    default:              return RoomId ?? "Unknown";
                }
            }
        }
    }
}
