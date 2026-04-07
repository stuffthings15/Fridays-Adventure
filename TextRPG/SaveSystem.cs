// ────────────────────────────────────────────────────────────
// TEXT RPG — Save / Load System
// Purpose: Persist and restore game state using simple text format.
//          No external dependencies — pure System.IO.
// ────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO;

namespace TextRPG
{
    /// <summary>
    /// Serializes/deserializes GameState to a key=value text file.
    /// File is saved next to the executable as "savegame.txt".
    /// </summary>
    public static class SaveSystem
    {
        private static readonly string SavePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "savegame.txt");

        /// <summary>Write the current game state to disk.</summary>
        public static void Save(GameState state)
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
                "ClearedRooms=" + string.Join("|", state.ClearedRoomIds)
            };
            File.WriteAllLines(SavePath, lines);
        }

        /// <summary>Load a previously saved game state, or null if none exists.</summary>
        public static GameState Load()
        {
            if (!File.Exists(SavePath)) return null;
            try
            {
                // Parse key=value pairs into a dictionary
                var dict = new Dictionary<string, string>();
                foreach (var line in File.ReadAllLines(SavePath))
                {
                    int eq = line.IndexOf('=');
                    if (eq > 0)
                        dict[line.Substring(0, eq)] = line.Substring(eq + 1);
                }

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

        /// <summary>True if a save file exists on disk.</summary>
        public static bool SaveExists() => File.Exists(SavePath);
    }
}
