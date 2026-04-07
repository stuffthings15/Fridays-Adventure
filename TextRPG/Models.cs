// ────────────────────────────────────────────────────────────
// TEXT RPG — All Game Model Classes
// Purpose: Player, Enemy, Item, Room, NPC, GameState, result types
// ────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;

namespace TextRPG
{
    // ── Enums ──────────────────────────────────────────────────────

    /// <summary>Item categories determining equip/use behavior.</summary>
    public enum ItemType { Weapon, Armor, Potion, Key, Misc }

    /// <summary>Cardinal directions for room navigation.</summary>
    public enum Direction { North, South, East, West }

    // ── Item ───────────────────────────────────────────────────────

    /// <summary>
    /// A collectible game item. StatBonus meaning depends on ItemType:
    /// Weapon → attack, Armor → defense, Potion → heal amount.
    /// </summary>
    public class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ItemType Type { get; set; }
        public int StatBonus { get; set; }

        public Item() { }
        public Item(string name, string desc, ItemType type, int bonus)
        { Name = name; Description = desc; Type = type; StatBonus = bonus; }

        public override string ToString() => Name;
    }

    // ── Player ────────────────────────────────────────────────────

    /// <summary>
    /// The player character. Attack/Defense include equipment bonuses.
    /// </summary>
    public class Player
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int BaseAttack { get; set; }
        public int BaseDefense { get; set; }
        public List<Item> Inventory { get; set; } = new List<Item>();
        public Item EquippedWeapon { get; set; }
        public Item EquippedArmor { get; set; }
        public string CurrentRoomId { get; set; }

        /// <summary>Total attack = base + weapon bonus.</summary>
        public int TotalAttack => BaseAttack + (EquippedWeapon?.StatBonus ?? 0);

        /// <summary>Total defense = base + armor bonus.</summary>
        public int TotalDefense => BaseDefense + (EquippedArmor?.StatBonus ?? 0);

        public Player() { Inventory = new List<Item>(); }

        public Player(string name)
        {
            Name = name;
            Health = 100; MaxHealth = 100;
            BaseAttack = 10; BaseDefense = 3;
            Inventory = new List<Item>();
            CurrentRoomId = "village_square";
        }
    }

    // ── Enemy ─────────────────────────────────────────────────────

    /// <summary>A hostile creature fought in turn-based combat.</summary>
    public class Enemy
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public List<Item> Loot { get; set; } = new List<Item>();
        public bool IsAlive => Health > 0;
    }

    // ── NPC / Dialogue ────────────────────────────────────────────

    /// <summary>A dialogue choice with the NPC's response.</summary>
    public class DialogueOption
    {
        public string Text { get; set; }
        public string Response { get; set; }
    }

    /// <summary>Non-player character with branching dialogue.</summary>
    public class NPC
    {
        public string Name { get; set; }
        public string Greeting { get; set; }
        public List<DialogueOption> Options { get; set; } = new List<DialogueOption>();
    }

    // ── Room ──────────────────────────────────────────────────────

    /// <summary>
    /// A location in the game world with exits, items, enemies,
    /// NPCs, and optional portal links to other rooms.
    /// </summary>
    public class Room
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<Direction, string> Exits { get; set; } = new Dictionary<Direction, string>();
        public List<Item> Items { get; set; } = new List<Item>();
        public Enemy Enemy { get; set; }
        public NPC Npc { get; set; }
        /// <summary>If set, this room has a portal to the target room.</summary>
        public string PortalTargetId { get; set; }
    }

    // ── Result types ──────────────────────────────────────────────

    /// <summary>Result of a player move action.</summary>
    public class MoveResult
    {
        public string Message { get; set; } = "";
        public bool EnteredCombat { get; set; }
    }

    /// <summary>Result of one round of combat.</summary>
    public class CombatResult
    {
        public string Log { get; set; } = "";
        public bool EnemyDefeated { get; set; }
        public bool PlayerDied { get; set; }
        public bool Victory { get; set; }
    }

    // ── Save data ─────────────────────────────────────────────────

    /// <summary>Serializable snapshot of the full game state.</summary>
    public class GameState
    {
        public string PlayerName { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int BaseAttack { get; set; }
        public int BaseDefense { get; set; }
        public string CurrentRoomId { get; set; }
        public string EquippedWeaponName { get; set; }
        public string EquippedArmorName { get; set; }
        public List<string> InventoryNames { get; set; } = new List<string>();
        public List<string> ClearedRoomIds { get; set; } = new List<string>();
    }
}
