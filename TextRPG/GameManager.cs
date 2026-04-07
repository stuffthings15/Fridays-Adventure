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
                ClearedRoomIds = new List<string>()
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

            BuildWorld();

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

            BuildWorld();

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

        /// <summary>50% chance to flee from combat.</summary>
        public bool TryFlee() => _rng.Next(100) < 50;

        // ── Items ────────────────────────────────────────────────

        /// <summary>Use a potion from inventory (heals HP).</summary>
        public string UseItem(Item item)
        {
            if (item.Type == ItemType.Potion)
            {
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

            // 1. Village Square — starting area
            Rooms["village_square"] = new Room
            {
                Id = "village_square", Name = "Village Square",
                Description = "A quiet cobblestone square with a fountain at its center. " +
                    "Torches flicker along the stone walls. A signpost reads:\n" +
                    "  North \u2192 Dark Forest\n  East \u2192 Riverbank\n  South \u2192 Library",
                Exits = { {Direction.North,"forest"}, {Direction.East,"riverbank"}, {Direction.South,"library"} },
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
                        new DialogueOption { Text = "I'll be on my way.",
                            Response = "May fortune favor you, adventurer." }
                    }
                }
            };

            // 2. Dark Forest — contains Iron Sword
            Rooms["forest"] = new Room
            {
                Id = "forest", Name = "Dark Forest",
                Description = "Tall pines block the sunlight, casting everything in shadow. " +
                    "Moss-covered stones line a narrow path. Something glints at the base of an oak.",
                Exits = { {Direction.South,"village_square"}, {Direction.East,"goblin_cave"} },
                Items = { new Item("Iron Sword", "A sturdy blade with a leather grip (+7 ATK)", ItemType.Weapon, 7) }
            };

            // 3. Goblin Cave — enemy encounter
            Rooms["goblin_cave"] = new Room
            {
                Id = "goblin_cave", Name = "Goblin's Cave",
                Description = "A damp cave reeking of smoke. Crude drawings mark the walls. " +
                    "A small green creature snarls at you from the shadows!",
                Exits = { {Direction.West,"forest"} },
                Enemy = new Enemy
                {
                    Name = "Cave Goblin", Health = 40, MaxHealth = 40,
                    Attack = 8, Defense = 2,
                    Loot = { new Item("Goblin Tooth", "A sharp fang \u2014 proof of your kill.", ItemType.Misc, 0) }
                }
            };

            // 4. Ancient Library — NPC + Health Potion
            Rooms["library"] = new Room
            {
                Id = "library", Name = "Ancient Library",
                Description = "Dusty bookshelves reach to the vaulted ceiling. A single candle " +
                    "illuminates a reading desk. A corked red vial sits on a shelf nearby.",
                Exits = { {Direction.North,"village_square"}, {Direction.East,"crystal_hall"} },
                Items = { new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40) },
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

            // 5. Misty Riverbank — contains Leather Armor
            Rooms["riverbank"] = new Room
            {
                Id = "riverbank", Name = "Misty Riverbank",
                Description = "A wide river flows beside a muddy path. Fog clings to the water. " +
                    "A worn leather vest lies abandoned on the bank. A bridge spans north.",
                Exits = { {Direction.West,"village_square"}, {Direction.North,"troll_bridge"} },
                Items = { new Item("Leather Armor", "Tough hide armor (+5 DEF)", ItemType.Armor, 5) }
            };

            // 6. Troll's Bridge — tough enemy
            Rooms["troll_bridge"] = new Room
            {
                Id = "troll_bridge", Name = "Troll's Bridge",
                Description = "A massive stone bridge arches over a gorge. A hulking troll " +
                    "blocks the far end, dragging a gnarled wooden club.",
                Exits = { {Direction.South,"riverbank"}, {Direction.North,"shrine"} },
                Enemy = new Enemy
                {
                    Name = "Bridge Troll", Health = 70, MaxHealth = 70,
                    Attack = 14, Defense = 6,
                    Loot = { new Item("Troll's Club", "A massive cudgel of surprising quality (+8 ATK)", ItemType.Weapon, 8) }
                }
            };

            // 7. Ancient Shrine — bonus armor item
            Rooms["shrine"] = new Room
            {
                Id = "shrine", Name = "Ancient Shrine",
                Description = "A peaceful stone shrine bathed in golden light. An altar holds " +
                    "a shimmering amulet shaped like a dragon scale. Runes pulse on the walls.",
                Exits = { {Direction.South,"troll_bridge"} },
                Items = { new Item("Dragon Scale Amulet", "Ancient protective charm (+8 DEF)", ItemType.Armor, 8) }
            };

            // 8. Crystal Hall — portal to dragon
            Rooms["crystal_hall"] = new Room
            {
                Id = "crystal_hall", Name = "Crystal Hall",
                Description = "A vast chamber of translucent crystal pillars. A swirling purple " +
                    "portal hovers above a stone dais. The air hums with energy.",
                Exits = { {Direction.West,"library"} },
                PortalTargetId = "dragon_lair"
            };

            // 9. Dragon's Lair — final boss
            Rooms["dragon_lair"] = new Room
            {
                Id = "dragon_lair", Name = "Dragon's Lair",
                Description = "An enormous cavern glowing with rivers of lava. Mountains of gold " +
                    "and bones line the walls. A colossal shadow stirs \u2014 the Shadow Dragon!",
                Exits = { },
                Enemy = new Enemy
                {
                    Name = "Shadow Dragon", Health = 120, MaxHealth = 120,
                    Attack = 20, Defense = 10,
                    Loot = { new Item("Dragon's Heart", "A pulsing gem of immense power.", ItemType.Key, 0) }
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

            // 1. Harbor Docks — starting area (narrative style)
            Rooms["village_square"] = new Room
            {
                Id = "village_square", Name = "Harbor Docks",
                Description = "The salt-worn planks creak beneath Miss Friday's boots as she " +
                    "steps ashore. Lanterns swing in the evening breeze, casting dancing " +
                    "shadows across barrel stacks and coiled rope. A weathered signpost reads:\n" +
                    "  North → Darkwood Trail\n  East → Coral Cove\n  South → Lighthouse Archive",
                Exits = { {Direction.North,"forest"}, {Direction.East,"riverbank"}, {Direction.South,"library"} },
                Npc = new NPC
                {
                    Name = "Captain Crow",
                    Greeting = "Ahoy, Miss Friday! The Sea Serpent has been terrorizing the ruins " +
                        "beyond the Crystal Caverns. You'll need steel and courage before facing it. " +
                        "I once sailed those waters — let me tell you what I know.",
                    Options = new List<DialogueOption>
                    {
                        new DialogueOption { Text = "Where can I find a good weapon?",
                            Response = "Head north through the Darkwood Trail. A fallen adventurer's " +
                                "blade still gleams beneath the old oak — finest iron I've ever seen." },
                        new DialogueOption { Text = "Tell me about the Sea Serpent.",
                            Response = "The Sea Serpent is ancient as the tides. Its scales deflect " +
                                "most blades. Equip your best gear, stock potions, and enter through " +
                                "the Crystal Caverns portal." },
                        new DialogueOption { Text = "I'm ready to set sail.",
                            Response = "That's the Friday spirit! May the winds be at your back." },
                        new DialogueOption { Text = "Any advice for a pirate?",
                            Response = "Save often, explore every room, and never fight a boss " +
                                "without a potion in your pocket. Trust me — I've lost three ships " +
                                "learning that lesson." }
                    }
                }
            };

            // 2. Darkwood Trail — same item, richer prose
            Rooms["forest"] = new Room
            {
                Id = "forest", Name = "Darkwood Trail",
                Description = "Gnarled trees arch overhead like the ribs of a shipwreck. " +
                    "Fireflies drift through the undergrowth, illuminating a faint " +
                    "metallic gleam at the base of an ancient oak. Miss Friday kneels — " +
                    "a blade, still sharp despite years in the wild.",
                Exits = { {Direction.South,"village_square"}, {Direction.East,"goblin_cave"} },
                Items = { new Item("Iron Sword", "A sturdy blade with a leather grip (+7 ATK)", ItemType.Weapon, 7) }
            };

            // 3. Goblin Hideout — narrative combat room
            Rooms["goblin_cave"] = new Room
            {
                Id = "goblin_cave", Name = "Goblin Hideout",
                Description = "Torchlight flickers off damp stone walls covered in crude " +
                    "charcoal drawings. The air is thick with smoke and menace. A pair " +
                    "of yellow eyes glint from the darkness — something snarls.",
                Exits = { {Direction.West,"forest"} },
                Enemy = new Enemy
                {
                    Name = "Cave Goblin", Health = 40, MaxHealth = 40,
                    Attack = 8, Defense = 2,
                    Loot = { new Item("Goblin Tooth", "A sharp fang — proof of your kill.", ItemType.Misc, 0) }
                }
            };

            // 4. Lighthouse Archive — unique NPC + Health Potion
            Rooms["library"] = new Room
            {
                Id = "library", Name = "Lighthouse Archive",
                Description = "The spiral staircase opens into a circular room lined with " +
                    "salt-stained charts and leather-bound journals. A lantern hangs from " +
                    "the rafters. A crimson vial glows softly on the map table.",
                Exits = { {Direction.North,"village_square"}, {Direction.East,"crystal_hall"} },
                Items = { new Item("Health Potion", "A crimson elixir that restores 40 HP.", ItemType.Potion, 40) },
                Npc = new NPC
                {
                    Name = "Keeper Iris",
                    Greeting = "Welcome to the archive, Miss Friday. These walls hold the " +
                        "secrets of every creature that haunts this coast. The Crystal " +
                        "Caverns to the east contain a portal — but it leads somewhere dangerous.",
                    Options = new List<DialogueOption>
                    {
                        new DialogueOption { Text = "What's through the portal?",
                            Response = "The Serpent's Grotto. Only those armed to the teeth " +
                                "should dare enter. The Sea Serpent guards a legendary artifact." },
                        new DialogueOption { Text = "Any survival tips for a pirate?",
                            Response = "Equip your best weapon and armor. Use potions during combat " +
                                "from the Inventory screen. And remember — fleeing is no shame!" },
                        new DialogueOption { Text = "Thank you, Keeper.",
                            Response = "Sail safe, Miss Friday. The coast needs its guardian." }
                    }
                }
            };

            // 5. Coral Cove — contains Leather Armor
            Rooms["riverbank"] = new Room
            {
                Id = "riverbank", Name = "Coral Cove",
                Description = "Turquoise waves lap against a crescent of white sand. Colorful " +
                    "coral formations rise from the shallows. Half-buried in the sand, a " +
                    "leather vest has washed ashore — still serviceable.",
                Exits = { {Direction.West,"village_square"}, {Direction.North,"troll_bridge"} },
                Items = { new Item("Leather Armor", "Tough hide armor (+5 DEF)", ItemType.Armor, 5) }
            };

            // 6. Smuggler's Crossing — tough enemy
            Rooms["troll_bridge"] = new Room
            {
                Id = "troll_bridge", Name = "Smuggler's Crossing",
                Description = "A rope bridge sways over a misty ravine. On the far side, " +
                    "a massive figure blocks the path — a troll with barnacles on its " +
                    "skin, dragging a driftwood club.",
                Exits = { {Direction.South,"riverbank"}, {Direction.North,"shrine"} },
                Enemy = new Enemy
                {
                    Name = "Bridge Troll", Health = 70, MaxHealth = 70,
                    Attack = 14, Defense = 6,
                    Loot = { new Item("Troll's Club", "A massive cudgel of surprising quality (+8 ATK)", ItemType.Weapon, 8) }
                }
            };

            // 7. Tidal Shrine — bonus armor item
            Rooms["shrine"] = new Room
            {
                Id = "shrine", Name = "Tidal Shrine",
                Description = "Moonlight streams through a hole in the cave roof, illuminating " +
                    "an ancient altar carved from coral. A shimmering amulet shaped like " +
                    "a serpent scale rests on the stone. The walls pulse with bioluminescence.",
                Exits = { {Direction.South,"troll_bridge"} },
                Items = { new Item("Dragon Scale Amulet", "Ancient protective charm (+8 DEF)", ItemType.Armor, 8) }
            };

            // 8. Crystal Caverns — portal
            Rooms["crystal_hall"] = new Room
            {
                Id = "crystal_hall", Name = "Crystal Caverns",
                Description = "Stalactites drip with phosphorescent water. At the cave's " +
                    "heart, a swirling portal of deep blue energy hovers above a stone " +
                    "dais etched with nautical runes.",
                Exits = { {Direction.West,"library"} },
                PortalTargetId = "dragon_lair"
            };

            // 9. Serpent's Grotto — final boss
            Rooms["dragon_lair"] = new Room
            {
                Id = "dragon_lair", Name = "Serpent's Grotto",
                Description = "An enormous underwater cavern lit by veins of molten rock. " +
                    "Mountains of treasure and shipwrecks line the walls. The water " +
                    "churns — a colossal shadow rises from the deep. The Sea Serpent!",
                Exits = { },
                Enemy = new Enemy
                {
                    Name = "Sea Serpent", Health = 120, MaxHealth = 120,
                    Attack = 20, Defense = 10,
                    Loot = { new Item("Dragon's Heart", "A pulsing gem of immense power.", ItemType.Key, 0) }
                }
            };
        }

        /// <summary>Reconstruct an Item by name for save/load.</summary>
        private Item MakeItem(string name)
        {
            switch (name)
            {
                case "Iron Sword":           return new Item(name, "+7 ATK blade", ItemType.Weapon, 7);
                case "Leather Armor":        return new Item(name, "+5 DEF hide armor", ItemType.Armor, 5);
                case "Health Potion":        return new Item(name, "Restores 40 HP", ItemType.Potion, 40);
                case "Goblin Tooth":         return new Item(name, "A trophy", ItemType.Misc, 0);
                case "Troll's Club":         return new Item(name, "+8 ATK cudgel", ItemType.Weapon, 8);
                case "Dragon Scale Amulet":  return new Item(name, "+8 DEF charm", ItemType.Armor, 8);
                case "Dragon's Heart":       return new Item(name, "Pulsing gem", ItemType.Key, 0);
                default: return null;
            }
        }
    }
}
