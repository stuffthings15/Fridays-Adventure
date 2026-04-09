// ────────────────────────────────────────────────────────────
// TEXT RPG — Game Manager (Core Logic Controller)
// Purpose: World building, navigation, combat, item management,
//          NPC interaction, save/load orchestration.
//          Supports two modes sharing the same engine:
//            RPG Mode — classic text adventure
//            Miss Friday Mode — narrative-driven with preset protagonist
// ────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;

namespace TextRPG
{
    /// <summary>
    /// Game mode determines world flavor text and protagonist.
    /// Both modes use the same engine, rooms, and combat system.
    /// </summary>
    public enum GameMode { RPG, MissFriday }

    /// <summary>
    /// Central game logic controller. UI screens call GameManager
    /// methods for all game actions; GameManager fires events to
    /// notify the UI of messages and state changes.
    /// </summary>
    public class GameManager
    {
        public Player Player { get; private set; }
        public Dictionary<string, Room> Rooms { get; private set; }
        public Room CurrentRoom => (Player != null && Rooms.ContainsKey(Player.CurrentRoomId))
            ? Rooms[Player.CurrentRoomId] : null;
        public Enemy CurrentEnemy { get; set; }

        /// <summary>Current game mode (RPG or Miss Friday).</summary>
        public GameMode Mode { get; set; } = GameMode.RPG;

        /// <summary>
        /// The currently active save slot (1–3). Set when the player
        /// starts a new game into a slot or loads an existing slot.
        /// Used by the in-game save button so the player doesn't have
        /// to re-pick a slot every time they save.
        /// </summary>
        public int ActiveSlot { get; set; } = 1;

        /// <summary>Fired when the game wants to show a toast/notification.</summary>
        public event Action<string> OnNotification;

        private readonly Random _rng = new Random();

        // ── New Game ──────────────────────────────────────────────

        /// <summary>Start a fresh game with the given player name.</summary>
        public void StartNewGame(string playerName)
        {
            Player = new Player(playerName);
            if (Mode == GameMode.MissFriday)
                BuildFridayWorld();
            else
                BuildWorld();
        }

        // ── Save / Load ──────────────────────────────────────────

        /// <summary>Persist the current game state to the active save slot.</summary>
        public void SaveGame()
        {
            SaveGame(ActiveSlot);
        }

        /// <summary>Persist the current game state to a specific save slot (1–3).</summary>
        public void SaveGame(int slot)
        {
            var state = new GameState
            {
                PlayerName = Player.Name,
                Health = Player.Health,
                MaxHealth = Player.MaxHealth,
                BaseAttack = Player.BaseAttack,
                BaseDefense = Player.BaseDefense,
                CurrentRoomId = Player.CurrentRoomId,
                EquippedWeaponName = Player.EquippedWeapon?.Name ?? "",
                EquippedArmorName = Player.EquippedArmor?.Name ?? "",
                InventoryNames = new List<string>(),
                ClearedRoomIds = new List<string>(),
                GameModeName = Mode.ToString()
            };
            foreach (var item in Player.Inventory)
                state.InventoryNames.Add(item.Name);
            // Track rooms whose items/enemies have been cleared
            foreach (var kv in Rooms)
                if (kv.Value.Items.Count == 0 && kv.Value.Enemy == null)
                    state.ClearedRoomIds.Add(kv.Key);

            SaveSystem.Save(state, slot);
            ActiveSlot = slot;
            OnNotification?.Invoke($"Game saved to Slot {slot}!");
        }

        /// <summary>Load a previously saved game from a specific slot. Returns false if empty.</summary>
        public bool LoadGame(int slot)
        {
            var state = SaveSystem.Load(slot);
            if (state == null) return false;

            Player = new Player
            {
                Name = state.PlayerName,
                Health = state.Health,
                MaxHealth = state.MaxHealth,
                BaseAttack = state.BaseAttack,
                BaseDefense = state.BaseDefense,
                CurrentRoomId = state.CurrentRoomId,
                Inventory = new List<Item>()
            };

            // Restore the game mode so the correct world map is rebuilt.
            if (string.Equals(state.GameModeName, "MissFriday", StringComparison.OrdinalIgnoreCase))
            {
                Mode = GameMode.MissFriday;
                BuildFridayWorld();
            }
            else
            {
                Mode = GameMode.RPG;
                BuildWorld();
            }

            // Restore inventory from saved item names
            foreach (var name in state.InventoryNames)
            {
                var item = MakeItem(name);
                if (item != null) Player.Inventory.Add(item);
            }
            // Re-equip saved equipment
            if (!string.IsNullOrEmpty(state.EquippedWeaponName))
                Player.EquippedWeapon = Player.Inventory.Find(i => i.Name == state.EquippedWeaponName);
            if (!string.IsNullOrEmpty(state.EquippedArmorName))
                Player.EquippedArmor = Player.Inventory.Find(i => i.Name == state.EquippedArmorName);
            // Mark cleared rooms
            foreach (var rid in state.ClearedRoomIds)
                if (Rooms.ContainsKey(rid))
                { Rooms[rid].Items.Clear(); Rooms[rid].Enemy = null; }

            ActiveSlot = slot;
            return true;
        }

        /// <summary>Legacy overload — loads from slot 1 or old single file.</summary>
        public bool LoadGame()
        {
            // Try slot 1 with fallback to legacy single-file
            var state = SaveSystem.Load();
            if (state == null) return false;

            Player = new Player
            {
                Name = state.PlayerName,
                Health = state.Health,
                MaxHealth = state.MaxHealth,
                BaseAttack = state.BaseAttack,
                BaseDefense = state.BaseDefense,
                CurrentRoomId = state.CurrentRoomId,
                Inventory = new List<Item>()
            };

            // Restore the game mode so the correct world map is rebuilt.
            if (string.Equals(state.GameModeName, "MissFriday", StringComparison.OrdinalIgnoreCase))
            {
                Mode = GameMode.MissFriday;
                BuildFridayWorld();
            }
            else
            {
                Mode = GameMode.RPG;
                BuildWorld();
            }

            foreach (var name in state.InventoryNames)
            {
                var item = MakeItem(name);
                if (item != null) Player.Inventory.Add(item);
            }
            if (!string.IsNullOrEmpty(state.EquippedWeaponName))
                Player.EquippedWeapon = Player.Inventory.Find(i => i.Name == state.EquippedWeaponName);
            if (!string.IsNullOrEmpty(state.EquippedArmorName))
                Player.EquippedArmor = Player.Inventory.Find(i => i.Name == state.EquippedArmorName);
            foreach (var rid in state.ClearedRoomIds)
                if (Rooms.ContainsKey(rid))
                { Rooms[rid].Items.Clear(); Rooms[rid].Enemy = null; }

            ActiveSlot = 1;
            return true;
        }

        // ── Navigation ───────────────────────────────────────────

        /// <summary>Move the player in a direction. Auto-collects items.</summary>
        public MoveResult MovePlayer(Direction dir)
        {
            if (!CurrentRoom.Exits.ContainsKey(dir))
                return new MoveResult { Message = "You can't go that way." };

            Player.CurrentRoomId = CurrentRoom.Exits[dir];
            var result = new MoveResult();

            // Auto-collect items in the new room
            if (CurrentRoom.Items.Count > 0)
            {
                foreach (var item in CurrentRoom.Items)
                {
                    Player.Inventory.Add(item);
                    result.Message += $"\u2726 Found: {item.Name} \u2014 {item.Description}\n";
                }
                CurrentRoom.Items.Clear();
            }

            // Check for enemy encounter
            if (CurrentRoom.Enemy != null && CurrentRoom.Enemy.IsAlive)
            {
                CurrentEnemy = CurrentRoom.Enemy;
                result.EnteredCombat = true;
                result.Message += $"\n\u2694 A {CurrentEnemy.Name} blocks your path!";
            }

            return result;
        }

        /// <summary>Teleport via the portal in the current room.</summary>
        public MoveResult UsePortal()
        {
            if (string.IsNullOrEmpty(CurrentRoom.PortalTargetId))
                return new MoveResult { Message = "No portal here." };

            Player.CurrentRoomId = CurrentRoom.PortalTargetId;
            var result = new MoveResult();

            if (CurrentRoom.Enemy != null && CurrentRoom.Enemy.IsAlive)
            {
                CurrentEnemy = CurrentRoom.Enemy;
                result.EnteredCombat = true;
                result.Message = $"\u2694 A {CurrentEnemy.Name} awaits you!";
            }
            return result;
        }

        // ── Combat ───────────────────────────────────────────────

        /// <summary>
        /// Execute one round: player attacks, then enemy retaliates.
        /// Returns a log string and status flags.
        /// </summary>
        public CombatResult PlayerAttack()
        {
            var r = new CombatResult();

            // Player strike
            int dmg = Math.Max(1, Player.TotalAttack - CurrentEnemy.Defense + _rng.Next(-3, 4));
            CurrentEnemy.Health = Math.Max(0, CurrentEnemy.Health - dmg);
            r.Log = $"You strike the {CurrentEnemy.Name} for {dmg} damage!";

            if (!CurrentEnemy.IsAlive)
            {
                r.Log += $"\n\n\u2605 The {CurrentEnemy.Name} has been defeated!";
                r.EnemyDefeated = true;
                if (CurrentEnemy.Loot != null)
                    foreach (var loot in CurrentEnemy.Loot)
                    {
                        Player.Inventory.Add(loot);
                        r.Log += $"\n\u2726 Obtained: {loot.Name}";
                    }
                CurrentRoom.Enemy = null;
                // Victory condition: defeat the Shadow Dragon
                if (CurrentRoom.Id == "dragon_lair")
                    r.Victory = true;
                return r;
            }

            // Enemy retaliates
            int eDmg = Math.Max(1, CurrentEnemy.Attack - Player.TotalDefense + _rng.Next(-2, 3));
            Player.Health = Math.Max(0, Player.Health - eDmg);
            r.Log += $"\nThe {CurrentEnemy.Name} strikes back for {eDmg} damage!";

            if (Player.Health <= 0)
            {
                r.Log += "\n\n\u2620 You have been slain...";
                r.PlayerDied = true;
            }
            return r;
        }

        /// <summary>70% chance to flee from combat — generous odds for a fun adventure.</summary>
        public bool TryFlee() => _rng.Next(100) < 70;

        // ── Items ────────────────────────────────────────────────

        /// <summary>Use a potion from inventory (heals HP or grants a permanent buff).</summary>
        public string UseItem(Item item)
        {
            if (item.Type == ItemType.Potion)
            {
                // Special buff potions that permanently boost base stats
                if (item.Name == "Strength Elixir" || item.Name == "Pirate's Grog")
                {
                    Player.BaseAttack += item.StatBonus;
                    Player.Inventory.Remove(item);
                    return $"Used {item.Name}. ATK permanently increased by {item.StatBonus}! (ATK: {Player.TotalAttack})";
                }

                // Standard healing potion
                int healed = Math.Min(item.StatBonus, Player.MaxHealth - Player.Health);
                Player.Health += healed;
                Player.Inventory.Remove(item);
                return $"Used {item.Name}. Restored {healed} HP! (HP: {Player.Health}/{Player.MaxHealth})";
            }
            return $"Can't use {item.Name} right now.";
        }

        /// <summary>Equip a weapon or armor item.</summary>
        public string EquipItem(Item item)
        {
            if (item.Type == ItemType.Weapon)
            {
                Player.EquippedWeapon = item;
                return $"Equipped {item.Name}. Attack is now {Player.TotalAttack}.";
            }
            if (item.Type == ItemType.Armor)
            {
                Player.EquippedArmor = item;
                return $"Equipped {item.Name}. Defense is now {Player.TotalDefense}.";
            }
            return $"{item.Name} can't be equipped.";
        }

        // ── World Definition ─────────────────────────────────────

        private void BuildWorld()
        {
            Rooms = new Dictionary<string, Room>();

            // 1. Village Square — starting area with a free starter potion
            Rooms["village_square"] = new Room
            {
                Id = "village_square", Name = "Village Square",
                Description = "A quiet cobblestone square with a fountain at its center. " +
                    "Torches flicker along the stone walls. A signpost reads:\n" +
                    "  North \u2192 Dark Forest\n  East \u2192 Riverbank\n  South \u2192 Library\n  West \u2192 Merchant's Alley",
                Exits = { {Direction.North,"forest"}, {Direction.East,"riverbank"}, {Direction.South,"library"}, {Direction.West,"merchant_alley"} },
                Items = { new Item("Small Healing Tonic", "A bubbly green vial that restores 25 HP.", ItemType.Potion, 25) },
                Npc = new NPC
                {
                    Name = "Elder Mathis",
                    Greeting = "Greetings, traveler! A Shadow Dragon nests in the ruins " +
                        "beyond the Crystal Hall. Gather weapons and armor before you face it.",
                    Options = new List<DialogueOption>
                    {
                        new DialogueOption { Text = "Where can I find weapons?",
                            Response = "The Dark Forest to the north holds an iron sword " +
                                "left by a fallen knight. It should serve you well." },
                        new DialogueOption { Text = "Tell me about the dragon.",
                            Response = "The Shadow Dragon is ancient and powerful. Equip your " +
                                "best gear and stock up on potions. The Crystal Hall portal leads to its lair." },
                        new DialogueOption { Text = "Any secret treasure around here?",
                            Response = "I've heard whispers of a hidden vault beyond the Ancient Shrine. " +
                                "Defeat the troll on the bridge and seek the runes... riches await the brave!" },
                        new DialogueOption { Text = "I'll be on my way.",
                            Response = "May fortune favor you, adventurer." }
                    }
                }
            };

            // NEW — Merchant's Alley: a bonus shop area with free potions and a fun item
            Rooms["merchant_alley"] = new Room
            {
                Id = "merchant_alley", Name = "Merchant's Alley",
                Description = "A narrow lane buzzing with merchants hawking their wares. " +
                    "Colorful awnings shade barrels of supplies. A friendly trader " +
                    "has left some samples on the counter for adventurers.",
                Exits = { {Direction.East,"village_square"} },
                Items = {
                    new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40),
                    new Item("Strength Elixir", "A golden brew — permanently boosts ATK by 3!", ItemType.Potion, 3),
                    new Item("Lucky Clover", "A four-leaf clover. Feels lucky! (+2 DEF)", ItemType.Armor, 2)
                },
                Npc = new NPC
                {
                    Name = "Trader Gus",
                    Greeting = "Welcome to the alley! Help yourself to the samples on the counter. " +
                        "Every adventurer deserves a fighting chance!",
                    Options = new List<DialogueOption>
                    {
                        new DialogueOption { Text = "Thanks for the free stuff!",
                            Response = "Don't mention it! Just come back alive, and tell your friends about old Gus!" },
                        new DialogueOption { Text = "Got any tips?",
                            Response = "Stock up on potions before every boss fight. And don't forget " +
                                "to equip your best gear — I've seen too many heroes forget that!" },
                        new DialogueOption { Text = "See you later.",
                            Response = "Safe travels, friend!" }
                    }
                }
            };

            // 2. Dark Forest — contains Iron Sword + a healing herb
            Rooms["forest"] = new Room
            {
                Id = "forest", Name = "Dark Forest",
                Description = "Tall pines block the sunlight, casting everything in shadow. " +
                    "Moss-covered stones line a narrow path. Something glints at the base of an oak. " +
                    "A fragrant herb grows beside it.",
                Exits = { {Direction.South,"village_square"}, {Direction.East,"goblin_cave"} },
                Items = {
                    new Item("Iron Sword", "A sturdy blade with a leather grip (+7 ATK)", ItemType.Weapon, 7),
                    new Item("Forest Herb", "A soothing wild herb that restores 20 HP.", ItemType.Potion, 20)
                }
            };

            // 3. Goblin Cave — easier enemy encounter with bonus loot
            Rooms["goblin_cave"] = new Room
            {
                Id = "goblin_cave", Name = "Goblin's Cave",
                Description = "A damp cave reeking of smoke. Crude drawings mark the walls. " +
                    "A small green creature snarls at you from the shadows!",
                Exits = { {Direction.West,"forest"} },
                Items = { new Item("Cave Mushroom", "A glowing mushroom that heals 15 HP.", ItemType.Potion, 15) },
                Enemy = new Enemy
                {
                    Name = "Cave Goblin", Health = 28, MaxHealth = 28,
                    Attack = 6, Defense = 1,
                    Loot = {
                        new Item("Goblin Tooth", "A sharp fang \u2014 proof of your kill.", ItemType.Misc, 0),
                        new Item("Goblin's Lucky Ring", "A tiny ring that sparkles (+3 DEF).", ItemType.Armor, 3)
                    }
                }
            };

            // 4. Ancient Library — NPC + multiple healing items
            Rooms["library"] = new Room
            {
                Id = "library", Name = "Ancient Library",
                Description = "Dusty bookshelves reach to the vaulted ceiling. A single candle " +
                    "illuminates a reading desk. Multiple vials sit on a shelf nearby.",
                Exits = { {Direction.North,"village_square"}, {Direction.East,"crystal_hall"} },
                Items = {
                    new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40),
                    new Item("Scholar's Elixir", "A potent azure potion that restores 50 HP.", ItemType.Potion, 50)
                },
                Npc = new NPC
                {
                    Name = "Scholar Elara",
                    Greeting = "Ah, a visitor! I've been studying the Crystal Hall to the east. " +
                        "It contains a magical portal \u2014 but beware what lies beyond.",
                    Options = new List<DialogueOption>
                    {
                        new DialogueOption { Text = "What's through the portal?",
                            Response = "The portal leads to the Dragon's Lair. Only the bravest " +
                                "and best-equipped should enter." },
                        new DialogueOption { Text = "Any survival tips?",
                            Response = "Equip your best weapon and armor. Use potions during combat " +
                                "from the Inventory screen. You can always try to flee!" },
                        new DialogueOption { Text = "Thank you.",
                            Response = "Good luck. The world needs heroes like you." }
                    }
                }
            };

            // 5. Misty Riverbank — Leather Armor + healing item
            Rooms["riverbank"] = new Room
            {
                Id = "riverbank", Name = "Misty Riverbank",
                Description = "A wide river flows beside a muddy path. Fog clings to the water. " +
                    "A worn leather vest and a small satchel lie abandoned on the bank. A bridge spans north.",
                Exits = { {Direction.West,"village_square"}, {Direction.North,"troll_bridge"} },
                Items = {
                    new Item("Leather Armor", "Tough hide armor (+5 DEF)", ItemType.Armor, 5),
                    new Item("Riverbank Herb", "A cool mint herb that restores 20 HP.", ItemType.Potion, 20)
                }
            };

            // 6. Troll's Bridge — easier troll with better loot
            Rooms["troll_bridge"] = new Room
            {
                Id = "troll_bridge", Name = "Troll's Bridge",
                Description = "A massive stone bridge arches over a gorge. A hulking troll " +
                    "blocks the far end, dragging a gnarled wooden club.",
                Exits = { {Direction.South,"riverbank"}, {Direction.North,"shrine"} },
                Enemy = new Enemy
                {
                    Name = "Bridge Troll", Health = 55, MaxHealth = 55,
                    Attack = 11, Defense = 4,
                    Loot = {
                        new Item("Troll's Club", "A massive cudgel of surprising quality (+8 ATK)", ItemType.Weapon, 8),
                        new Item("Troll Skin Shield", "Tough troll-hide buckler (+6 DEF).", ItemType.Armor, 6),
                        new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40)
                    }
                }
            };

            // 7. Ancient Shrine — bonus armor + healing + leads to treasure vault
            Rooms["shrine"] = new Room
            {
                Id = "shrine", Name = "Ancient Shrine",
                Description = "A peaceful stone shrine bathed in golden light. An altar holds " +
                    "a shimmering amulet shaped like a dragon scale. Runes pulse on the walls.\n" +
                    "  East \u2192 A hidden passage glows faintly behind the altar...",
                Exits = { {Direction.South,"troll_bridge"}, {Direction.East,"treasure_vault"} },
                Items = {
                    new Item("Dragon Scale Amulet", "Ancient protective charm (+8 DEF)", ItemType.Armor, 8),
                    new Item("Shrine Blessing Potion", "Holy water that restores 60 HP.", ItemType.Potion, 60)
                }
            };

            // NEW — Treasure Vault: a hidden bonus room loaded with goodies
            Rooms["treasure_vault"] = new Room
            {
                Id = "treasure_vault", Name = "Treasure Vault",
                Description = "A dazzling chamber of gold and gems! Ancient adventurers stashed " +
                    "their finest treasures here. Chests overflow with useful supplies. " +
                    "A magical fountain in the center hums with restorative energy.",
                Exits = { {Direction.West,"shrine"} },
                Items = {
                    new Item("Hero's Blade", "A legendary sword crackling with energy (+12 ATK).", ItemType.Weapon, 12),
                    new Item("Grand Health Potion", "A massive flask that restores 80 HP.", ItemType.Potion, 80),
                    new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40),
                    new Item("Phoenix Feather", "A glowing feather — fully restores HP!", ItemType.Potion, 999),
                    new Item("Golden Crown", "A shiny crown that makes you feel powerful (+10 DEF).", ItemType.Armor, 10)
                }
            };

            // 8. Crystal Hall — portal to dragon + pre-boss healing
            Rooms["crystal_hall"] = new Room
            {
                Id = "crystal_hall", Name = "Crystal Hall",
                Description = "A vast chamber of translucent crystal pillars. A swirling purple " +
                    "portal hovers above a stone dais. The air hums with energy.\n" +
                    "A shelf of emergency supplies sits beside the portal.",
                Exits = { {Direction.West,"library"} },
                Items = {
                    new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40),
                    new Item("Mystic Crystal Potion", "A sparkling potion that restores 50 HP.", ItemType.Potion, 50)
                },
                PortalTargetId = "dragon_lair"
            };

            // 9. Dragon's Lair — easier final boss with epic loot
            Rooms["dragon_lair"] = new Room
            {
                Id = "dragon_lair", Name = "Dragon's Lair",
                Description = "An enormous cavern glowing with rivers of lava. Mountains of gold " +
                    "and bones line the walls. A colossal shadow stirs \u2014 the Shadow Dragon!",
                Exits = { },
                Enemy = new Enemy
                {
                    Name = "Shadow Dragon", Health = 90, MaxHealth = 90,
                    Attack = 16, Defense = 7,
                    Loot = {
                        new Item("Dragon's Heart", "A pulsing gem of immense power.", ItemType.Key, 0),
                        new Item("Dragon Fang Sword", "The ultimate weapon, forged in dragonfire (+15 ATK).", ItemType.Weapon, 15)
                    }
                }
            };
        }

        // ── Miss Friday World — narrative-driven variant ──────────

        /// <summary>
        /// Builds the same room structure but with narrative-rich descriptions
        /// and a unique NPC (Captain Crow) exclusive to Miss Friday mode.
        /// Uses the same items, enemies, and game mechanics.
        /// </summary>
        private void BuildFridayWorld()
        {
            Rooms = new Dictionary<string, Room>();

            // 1. Harbor Docks — starting area with a starter potion
            Rooms["village_square"] = new Room
            {
                Id = "village_square", Name = "Harbor Docks",
                Description = "The salt-worn planks creak beneath Miss Friday's boots as she " +
                    "steps ashore. Lanterns swing in the evening breeze, casting dancing " +
                    "shadows across barrel stacks and coiled rope. A weathered signpost reads:\n" +
                    "  North \u2192 Darkwood Trail\n  East \u2192 Coral Cove\n  South \u2192 Lighthouse Archive\n  West \u2192 Pirate Bazaar",
                Exits = { {Direction.North,"forest"}, {Direction.East,"riverbank"}, {Direction.South,"library"}, {Direction.West,"merchant_alley"} },
                Items = { new Item("Sailor's Rum", "A fortifying sip that restores 25 HP.", ItemType.Potion, 25) },
                Npc = new NPC
                {
                    Name = "Captain Crow",
                    Greeting = "Ahoy, Miss Friday! The Sea Serpent has been terrorizing the ruins " +
                        "beyond the Crystal Caverns. You'll need steel and courage before facing it. " +
                        "I once sailed those waters \u2014 let me tell you what I know.",
                    Options = new List<DialogueOption>
                    {
                        new DialogueOption { Text = "Where can I find a good weapon?",
                            Response = "Head north through the Darkwood Trail. A fallen adventurer's " +
                                "blade still gleams beneath the old oak \u2014 finest iron I've ever seen." },
                        new DialogueOption { Text = "Tell me about the Sea Serpent.",
                            Response = "The Sea Serpent is ancient as the tides. Its scales deflect " +
                                "most blades. Equip your best gear, stock potions, and enter through " +
                                "the Crystal Caverns portal." },
                        new DialogueOption { Text = "Is there hidden treasure around here?",
                            Response = "Aye! Beyond the Tidal Shrine, there's a Sunken Vault " +
                                "filled with pirate gold and legendary gear. Clear the troll first!" },
                        new DialogueOption { Text = "I'm ready to set sail.",
                            Response = "That's the Friday spirit! May the winds be at your back." },
                        new DialogueOption { Text = "Any advice for a pirate?",
                            Response = "Save often, explore every room, and never fight a boss " +
                                "without a potion in your pocket. Trust me \u2014 I've lost three ships " +
                                "learning that lesson." }
                    }
                }
            };

            // NEW \u2014 Pirate Bazaar: bonus shop area with free supplies
            Rooms["merchant_alley"] = new Room
            {
                Id = "merchant_alley", Name = "Pirate Bazaar",
                Description = "A rowdy open-air market built from old ship hulls. Vendors " +
                    "shout deals over the sound of sea shanties. A generous merchant " +
                    "has laid out free samples for any brave pirate willing to face the Serpent.",
                Exits = { {Direction.East,"village_square"} },
                Items = {
                    new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40),
                    new Item("Pirate's Grog", "A fearsome brew \u2014 permanently boosts ATK by 3!", ItemType.Potion, 3),
                    new Item("Barnacle Charm", "A crusty good-luck charm from the deep (+2 DEF).", ItemType.Armor, 2)
                },
                Npc = new NPC
                {
                    Name = "Old Salt Pete",
                    Greeting = "Ahoy! Take what ye need from the counter. Every pirate " +
                        "deserves a fair shot at glory!",
                    Options = new List<DialogueOption>
                    {
                        new DialogueOption { Text = "Thanks for the freebies!",
                            Response = "Don't mention it! Just bring back a tale worth telling at the tavern!" },
                        new DialogueOption { Text = "Got any tips for a pirate?",
                            Response = "Always carry potions, always equip your best gear, " +
                                "and never trust a calm sea \u2014 there's always something lurking below!" },
                        new DialogueOption { Text = "See you around.",
                            Response = "Fair winds, Miss Friday!" }
                    }
                }
            };

            // 2. Darkwood Trail \u2014 Iron Sword + healing herb
            Rooms["forest"] = new Room
            {
                Id = "forest", Name = "Darkwood Trail",
                Description = "Gnarled trees arch overhead like the ribs of a shipwreck. " +
                    "Fireflies drift through the undergrowth, illuminating a faint " +
                    "metallic gleam at the base of an ancient oak. Miss Friday kneels \u2014 " +
                    "a blade, still sharp despite years in the wild. A medicinal herb grows nearby.",
                Exits = { {Direction.South,"village_square"}, {Direction.East,"goblin_cave"} },
                Items = {
                    new Item("Iron Sword", "A sturdy blade with a leather grip (+7 ATK)", ItemType.Weapon, 7),
                    new Item("Jungle Herb", "A soothing tropical herb that restores 20 HP.", ItemType.Potion, 20)
                }
            };

            // 3. Goblin Hideout \u2014 easier enemy with bonus loot
            Rooms["goblin_cave"] = new Room
            {
                Id = "goblin_cave", Name = "Goblin Hideout",
                Description = "Torchlight flickers off damp stone walls covered in crude " +
                    "charcoal drawings. The air is thick with smoke and menace. A pair " +
                    "of yellow eyes glint from the darkness \u2014 something snarls.",
                Exits = { {Direction.West,"forest"} },
                Items = { new Item("Glowing Mushroom", "A bioluminescent mushroom that heals 15 HP.", ItemType.Potion, 15) },
                Enemy = new Enemy
                {
                    Name = "Cave Goblin", Health = 28, MaxHealth = 28,
                    Attack = 6, Defense = 1,
                    Loot = {
                        new Item("Goblin Tooth", "A sharp fang \u2014 proof of your kill.", ItemType.Misc, 0),
                        new Item("Goblin's Pearl Ring", "A tiny stolen ring that sparkles (+3 DEF).", ItemType.Armor, 3)
                    }
                }
            };

            // 4. Lighthouse Archive \u2014 unique NPC + multiple healing items
            Rooms["library"] = new Room
            {
                Id = "library", Name = "Lighthouse Archive",
                Description = "The spiral staircase opens into a circular room lined with " +
                    "salt-stained charts and leather-bound journals. A lantern hangs from " +
                    "the rafters. Several vials glow softly on the map table.",
                Exits = { {Direction.North,"village_square"}, {Direction.East,"crystal_hall"} },
                Items = {
                    new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40),
                    new Item("Keeper's Elixir", "A potent azure potion that restores 50 HP.", ItemType.Potion, 50)
                },
                Npc = new NPC
                {
                    Name = "Keeper Iris",
                    Greeting = "Welcome to the archive, Miss Friday. These walls hold the " +
                        "secrets of every creature that haunts this coast. The Crystal " +
                        "Caverns to the east contain a portal \u2014 but it leads somewhere dangerous.",
                    Options = new List<DialogueOption>
                    {
                        new DialogueOption { Text = "What's through the portal?",
                            Response = "The Serpent's Grotto. Only those armed to the teeth " +
                                "should dare enter. The Sea Serpent guards a legendary artifact." },
                        new DialogueOption { Text = "Any survival tips for a pirate?",
                            Response = "Equip your best weapon and armor. Use potions during combat " +
                                "from the Inventory screen. And remember \u2014 fleeing is no shame!" },
                        new DialogueOption { Text = "Thank you, Keeper.",
                            Response = "Sail safe, Miss Friday. The coast needs its guardian." }
                    }
                }
            };

            // 5. Coral Cove \u2014 Leather Armor + healing item
            Rooms["riverbank"] = new Room
            {
                Id = "riverbank", Name = "Coral Cove",
                Description = "Turquoise waves lap against a crescent of white sand. Colorful " +
                    "coral formations rise from the shallows. Half-buried in the sand, a " +
                    "leather vest and a small pouch have washed ashore.",
                Exits = { {Direction.West,"village_square"}, {Direction.North,"troll_bridge"} },
                Items = {
                    new Item("Leather Armor", "Tough hide armor (+5 DEF)", ItemType.Armor, 5),
                    new Item("Coconut Water", "Refreshing island water that restores 20 HP.", ItemType.Potion, 20)
                }
            };

            // 6. Smuggler's Crossing \u2014 easier troll with better loot
            Rooms["troll_bridge"] = new Room
            {
                Id = "troll_bridge", Name = "Smuggler's Crossing",
                Description = "A rope bridge sways over a misty ravine. On the far side, " +
                    "a massive figure blocks the path \u2014 a troll with barnacles on its " +
                    "skin, dragging a driftwood club.",
                Exits = { {Direction.South,"riverbank"}, {Direction.North,"shrine"} },
                Enemy = new Enemy
                {
                    Name = "Bridge Troll", Health = 55, MaxHealth = 55,
                    Attack = 11, Defense = 4,
                    Loot = {
                        new Item("Troll's Club", "A massive cudgel of surprising quality (+8 ATK)", ItemType.Weapon, 8),
                        new Item("Troll Barnacle Shield", "Tough troll-hide buckler (+6 DEF).", ItemType.Armor, 6),
                        new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40)
                    }
                }
            };

            // 7. Tidal Shrine \u2014 bonus armor + healing + leads to sunken vault
            Rooms["shrine"] = new Room
            {
                Id = "shrine", Name = "Tidal Shrine",
                Description = "Moonlight streams through a hole in the cave roof, illuminating " +
                    "an ancient altar carved from coral. A shimmering amulet shaped like " +
                    "a serpent scale rests on the stone. The walls pulse with bioluminescence.\n" +
                    "  East \u2192 A hidden passage glows faintly behind the altar...",
                Exits = { {Direction.South,"troll_bridge"}, {Direction.East,"treasure_vault"} },
                Items = {
                    new Item("Dragon Scale Amulet", "Ancient protective charm (+8 DEF)", ItemType.Armor, 8),
                    new Item("Tidal Blessing Potion", "Holy seawater that restores 60 HP.", ItemType.Potion, 60)
                }
            };

            // NEW \u2014 Sunken Vault: hidden bonus room loaded with pirate treasure
            Rooms["treasure_vault"] = new Room
            {
                Id = "treasure_vault", Name = "Sunken Vault",
                Description = "A breathtaking underwater cavern filled with sunken pirate treasure! " +
                    "Gold doubloons carpet the floor. Enchanted chests overflow with legendary gear. " +
                    "A magical coral fountain hums with restorative energy.",
                Exits = { {Direction.West,"shrine"} },
                Items = {
                    new Item("Cutlass of the Deep", "A legendary pirate sword crackling with sea energy (+12 ATK).", ItemType.Weapon, 12),
                    new Item("Grand Health Potion", "A massive flask that restores 80 HP.", ItemType.Potion, 80),
                    new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40),
                    new Item("Mermaid's Tear", "A glowing pearl \u2014 fully restores HP!", ItemType.Potion, 999),
                    new Item("Pirate King's Coat", "A legendary coat that radiates power (+10 DEF).", ItemType.Armor, 10)
                }
            };

            // 8. Crystal Caverns \u2014 portal + pre-boss healing supplies
            Rooms["crystal_hall"] = new Room
            {
                Id = "crystal_hall", Name = "Crystal Caverns",
                Description = "Stalactites drip with phosphorescent water. At the cave's " +
                    "heart, a swirling portal of deep blue energy hovers above a stone " +
                    "dais etched with nautical runes.\n" +
                    "A shelf of emergency supplies sits beside the portal.",
                Exits = { {Direction.West,"library"} },
                Items = {
                    new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40),
                    new Item("Crystal Cave Potion", "A sparkling potion that restores 50 HP.", ItemType.Potion, 50)
                },
                PortalTargetId = "dragon_lair"
            };

            // 9. Serpent's Grotto \u2014 easier final boss with epic loot
            Rooms["dragon_lair"] = new Room
            {
                Id = "dragon_lair", Name = "Serpent's Grotto",
                Description = "An enormous underwater cavern lit by veins of molten rock. " +
                    "Mountains of treasure and shipwrecks line the walls. The water " +
                    "churns \u2014 a colossal shadow rises from the deep. The Sea Serpent!",
                Exits = { },
                Enemy = new Enemy
                {
                    Name = "Sea Serpent", Health = 90, MaxHealth = 90,
                    Attack = 16, Defense = 7,
                    Loot = {
                        new Item("Dragon's Heart", "A pulsing gem of immense power.", ItemType.Key, 0),
                        new Item("Serpent Fang Cutlass", "The ultimate pirate weapon, forged in the deep (+15 ATK).", ItemType.Weapon, 15)
                    }
                }
            };
        }

        /// <summary>Reconstruct an Item by name for save/load.</summary>
        private Item MakeItem(string name)
        {
            switch (name)
            {
                // ── Weapons ──────────────────────────────────────────
                case "Iron Sword":           return new Item(name, "+7 ATK blade", ItemType.Weapon, 7);
                case "Troll's Club":         return new Item(name, "+8 ATK cudgel", ItemType.Weapon, 8);
                case "Hero's Blade":         return new Item(name, "+12 ATK legendary sword", ItemType.Weapon, 12);
                case "Cutlass of the Deep":  return new Item(name, "+12 ATK pirate sword", ItemType.Weapon, 12);
                case "Dragon Fang Sword":    return new Item(name, "+15 ATK dragonfire blade", ItemType.Weapon, 15);
                case "Serpent Fang Cutlass":  return new Item(name, "+15 ATK deep-sea cutlass", ItemType.Weapon, 15);

                // ── Armor ────────────────────────────────────────────
                case "Leather Armor":        return new Item(name, "+5 DEF hide armor", ItemType.Armor, 5);
                case "Lucky Clover":         return new Item(name, "+2 DEF lucky charm", ItemType.Armor, 2);
                case "Barnacle Charm":       return new Item(name, "+2 DEF lucky charm", ItemType.Armor, 2);
                case "Goblin's Lucky Ring":  return new Item(name, "+3 DEF goblin ring", ItemType.Armor, 3);
                case "Goblin's Pearl Ring":  return new Item(name, "+3 DEF goblin ring", ItemType.Armor, 3);
                case "Troll Skin Shield":    return new Item(name, "+6 DEF troll buckler", ItemType.Armor, 6);
                case "Troll Barnacle Shield":return new Item(name, "+6 DEF troll buckler", ItemType.Armor, 6);
                case "Dragon Scale Amulet":  return new Item(name, "+8 DEF charm", ItemType.Armor, 8);
                case "Golden Crown":         return new Item(name, "+10 DEF golden crown", ItemType.Armor, 10);
                case "Pirate King's Coat":   return new Item(name, "+10 DEF legendary coat", ItemType.Armor, 10);

                // ── Potions / Healing ────────────────────────────────
                case "Cave Mushroom":        return new Item(name, "Heals 15 HP", ItemType.Potion, 15);
                case "Glowing Mushroom":     return new Item(name, "Heals 15 HP", ItemType.Potion, 15);
                case "Forest Herb":          return new Item(name, "Heals 20 HP", ItemType.Potion, 20);
                case "Jungle Herb":          return new Item(name, "Heals 20 HP", ItemType.Potion, 20);
                case "Riverbank Herb":       return new Item(name, "Heals 20 HP", ItemType.Potion, 20);
                case "Coconut Water":        return new Item(name, "Heals 20 HP", ItemType.Potion, 20);
                case "Small Healing Tonic":  return new Item(name, "Heals 25 HP", ItemType.Potion, 25);
                case "Sailor's Rum":         return new Item(name, "Heals 25 HP", ItemType.Potion, 25);
                case "Strength Elixir":      return new Item(name, "Boosts ATK by 3", ItemType.Potion, 3);
                case "Pirate's Grog":        return new Item(name, "Boosts ATK by 3", ItemType.Potion, 3);
                case "Health Potion":        return new Item(name, "Restores 40 HP", ItemType.Potion, 40);
                case "Scholar's Elixir":     return new Item(name, "Heals 50 HP", ItemType.Potion, 50);
                case "Keeper's Elixir":      return new Item(name, "Heals 50 HP", ItemType.Potion, 50);
                case "Mystic Crystal Potion":return new Item(name, "Heals 50 HP", ItemType.Potion, 50);
                case "Crystal Cave Potion":  return new Item(name, "Heals 50 HP", ItemType.Potion, 50);
                case "Shrine Blessing Potion":return new Item(name, "Heals 60 HP", ItemType.Potion, 60);
                case "Tidal Blessing Potion":return new Item(name, "Heals 60 HP", ItemType.Potion, 60);
                case "Grand Health Potion":  return new Item(name, "Heals 80 HP", ItemType.Potion, 80);
                case "Phoenix Feather":      return new Item(name, "Full HP restore", ItemType.Potion, 999);
                case "Mermaid's Tear":       return new Item(name, "Full HP restore", ItemType.Potion, 999);

                // ── Misc / Key ───────────────────────────────────────
                case "Goblin Tooth":         return new Item(name, "A trophy", ItemType.Misc, 0);
                case "Dragon's Heart":       return new Item(name, "Pulsing gem", ItemType.Key, 0);

                default: return null;
            }
        }
    }
}
