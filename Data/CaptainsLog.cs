using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Data
{
    // ─────────────────────────────────────────────────────────────────────────
    //  CaptainsLog.cs  —  Narrative Lore & Dialogue Systems
    //
    //  Team 6 (Narrative Designer / Writer) — all 10 ideas implemented below:
    //
    //    Idea 1:  Captain's log journal — per-chapter unlock entries.
    //    Idea 2:  Environmental signpost text — in-world readable blocks.
    //    Idea 3:  Chapter title card — full-screen text at world transitions.
    //    Idea 4:  Character lore codex — unlockable bios per character.
    //    Idea 5:  NPC dialogue trigger zones — proximity-start conversations.
    //    Idea 6:  Prophecy text reveal — gradual mystery text unlock sequence.
    //    Idea 7:  Antagonist monologue — scripted speech before boss encounters.
    //    Idea 8:  Crew bond dialogue — bond-level-triggered crew banter.
    //    Idea 9:  World map lore blurbs — flavor text per overworld node.
    //    Idea 10: Victory / defeat epilogue slides — end-of-run story panels.
    // ─────────────────────────────────────────────────────────────────────────

    // ── Idea 1: Captain's log entry ───────────────────────────────────────────

    /// <summary>
    /// One entry in the captain's log journal.
    /// Idea 1 (Narrative Designer).
    /// </summary>
    public sealed class LogEntry
    {
        /// <summary>Chapter identifier (e.g. "ch1", "ch2_storm").</summary>
        public string ChapterId  { get; set; }
        /// <summary>Displayed title in the log list.</summary>
        public string Title      { get; set; }
        /// <summary>Full log text (supports newlines).</summary>
        public string Body       { get; set; }
        /// <summary>True once the player has unlocked and read this entry.</summary>
        public bool   IsUnlocked { get; set; }
        /// <summary>True once the entry has been opened in the log UI.</summary>
        public bool   IsRead     { get; set; }
    }

    // ── Idea 4: Character lore entry ──────────────────────────────────────────

    /// <summary>
    /// A short lore bio for a character in the in-game codex.
    /// Idea 4 (Narrative Designer).
    /// </summary>
    public sealed class CharacterBio
    {
        /// <summary>Internal character key (e.g. "friday", "orca", "swan").</summary>
        public string Key         { get; set; }
        /// <summary>Display name for the codex list.</summary>
        public string Name        { get; set; }
        /// <summary>Short description paragraph shown in the codex.</summary>
        public string Description { get; set; }
        /// <summary>True once the player has unlocked this bio.</summary>
        public bool   IsUnlocked  { get; set; }
    }

    // ── Idea 10: Epilogue slide ───────────────────────────────────────────────

    /// <summary>
    /// One slide in the end-of-run epilogue / defeat sequence.
    /// Idea 10 (Narrative Designer).
    /// </summary>
    public sealed class EpilogueSlide
    {
        /// <summary>Slide flavour: "Victory" or "Defeat".</summary>
        public string Category { get; set; }
        /// <summary>Header text (large).</summary>
        public string Header   { get; set; }
        /// <summary>Body text (small, wraps to multiple lines).</summary>
        public string Body     { get; set; }
    }

    /// <summary>
    /// Centralised narrative content store.
    /// Contains the captain's log, signpost text, chapter title cards,
    /// character codex, prophecy sequence, antagonist monologues,
    /// crew-bond dialogue, world-map lore, and epilogue slides.
    /// </summary>
    public static class CaptainsLog
    {
        // ── Idea 1: Captain's log data ────────────────────────────────────────
        public static readonly List<LogEntry> Entries = new List<LogEntry>
        {
            new LogEntry
            {
                ChapterId = "ch1",
                Title     = "Day 1 — We Set Sail",
                Body      = "The Grand Line stretches before us like a dare. Friday won't stop grinning.\n" +
                            "\"Adventure,\" she keeps saying. We barely have enough berries to feed the crew.\n" +
                            "Still — I believe her.",
                IsUnlocked = true
            },
            new LogEntry
            {
                ChapterId = "ch2_storm",
                Title     = "Day 14 — The Storm Belt",
                Body      = "The lightning hasn't stopped in three days. Two crewmates lost overboard.\n" +
                            "Friday held the mast with both hands and laughed. I think she is unbreakable.\n" +
                            "I pray I am wrong, for unbreakable things rarely bend — they shatter.",
                IsUnlocked = false
            },
            new LogEntry
            {
                ChapterId = "ch3_sky",
                Title     = "Day 29 — The Sky Islands",
                Body      = "Cities in the clouds. The Marines would never think to look up here.\n" +
                            "Orca sang for the first time since we left port. That alone was worth the climb.",
                IsUnlocked = false
            },
            new LogEntry
            {
                ChapterId = "ch4_warlord",
                Title     = "Day 41 — Warlord's Gate",
                Body      = "His name is Sudo. He does not deal in mercy.\n" +
                            "Swan says we can out-think him. Friday says we punch straight through.\n" +
                            "Both of them are right. Both of them terrify me.",
                IsUnlocked = false
            },
            new LogEntry
            {
                ChapterId = "ch5_final",
                Title     = "Day 57 — The World's End",
                Body      = "The Red Line. The New World. Everything the log warned was true.\n" +
                            "Friday still grins. I think I am starting to understand why.",
                IsUnlocked = false
            },
        };

        /// <summary>
        /// Unlocks the log entry for the given chapter ID.
        /// Idea 1 (Narrative Designer).
        /// </summary>
        public static void Unlock(string chapterId)
        {
            foreach (var e in Entries)
                if (e.ChapterId.Equals(chapterId, StringComparison.OrdinalIgnoreCase))
                { e.IsUnlocked = true; break; }
        }

        /// <summary>Returns all unlocked log entries.</summary>
        public static List<LogEntry> GetUnlocked()
        {
            var result = new List<LogEntry>();
            foreach (var e in Entries) if (e.IsUnlocked) result.Add(e);
            return result;
        }

        // ── Idea 2: Environmental signpost text ───────────────────────────────
        /// <summary>
        /// Key/value store of readable in-world signpost messages.
        /// Key = level node id + index (e.g. "island_1_sign0").
        /// Idea 2 (Narrative Designer).
        /// </summary>
        private static readonly Dictionary<string, string> _signposts =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "island_1_sign0",  "→ BERRY GROVE  ★ Collect 3 star coins!" },
                { "island_1_sign1",  "WARNING: Spike traps ahead. Proceed carefully." },
                { "storm_1_sign0",   "Lightning rods ACTIVE — avoid metallic platforms." },
                { "sky_1_sign0",     "WIND ZONE. Jump direction will shift." },
                { "warlord_1_sign0", "You have come this far.  Turn back now." },
            };

        /// <summary>Returns signpost text for the given key, or null if not found.</summary>
        public static string GetSignpost(string key)
        {
            _signposts.TryGetValue(key, out string text);
            return text;
        }

        // ── Idea 3: Chapter title cards ───────────────────────────────────────
        /// <summary>
        /// Chapter title card definitions per world transition.
        /// Idea 3 (Narrative Designer).
        /// </summary>
        public static readonly Dictionary<int, string[]> ChapterCards =
            new Dictionary<int, string[]>
            {
                { 1, new[] { "CHAPTER ONE",  "\"The Open Sea\"" } },
                { 2, new[] { "CHAPTER TWO",  "\"Storm and Lightning\"" } },
                { 3, new[] { "CHAPTER THREE","\"Kingdoms in the Clouds\"" } },
                { 4, new[] { "CHAPTER FOUR", "\"The Warlord's Domain\"" } },
                { 5, new[] { "CHAPTER FIVE", "\"Edge of the World\"" } },
            };

        // ── Idea 4: Character codex ────────────────────────────────────────────
        public static readonly List<CharacterBio> Codex = new List<CharacterBio>
        {
            new CharacterBio
            {
                Key         = "friday",
                Name        = "Miss Friday",
                Description = "Fearless crew captain with ice-forged resolve.\n" +
                              "Her IceWall and FlashFreeze abilities were self-taught\n" +
                              "after a harrowing encounter in the Tundra Peaks.",
                IsUnlocked  = true
            },
            new CharacterBio
            {
                Key         = "orca",
                Name        = "Orca",
                Description = "Tidal warrior from the deep ocean tribes.\n" +
                              "Tidal Slam draws power from sea-stone reserves\n" +
                              "and creates wide defensive walls on command.",
                IsUnlocked  = false
            },
            new CharacterBio
            {
                Key         = "swan",
                Name        = "Swan",
                Description = "Wind-born scout who never touches the ground by choice.\n" +
                              "Wing Dash lets her pierce enemy formations;\n" +
                              "her extended glide makes vertical puzzles trivial.",
                IsUnlocked  = false
            },
        };

        /// <summary>Unlocks a character bio by key.</summary>
        public static void UnlockBio(string key)
        {
            foreach (var b in Codex)
                if (b.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                { b.IsUnlocked = true; break; }
        }

        // ── Idea 5: NPC dialogue trigger zones ────────────────────────────────
        /// <summary>
        /// Registered NPC dialogue zones for the current scene.
        /// Populated by scenes before level start.
        /// Idea 5 (Narrative Designer).
        /// </summary>
        public static readonly List<DialogueTriggerZone> DialogueZones =
            new List<DialogueTriggerZone>();

        /// <summary>Clears all dialogue zones (call on scene exit).</summary>
        public static void ClearDialogueZones() => DialogueZones.Clear();

        /// <summary>
        /// Returns the first unread dialogue zone the player overlaps.
        /// Idea 5 (Narrative Designer).
        /// </summary>
        public static DialogueTriggerZone CheckDialogueZone(RectangleF playerRect)
        {
            foreach (var z in DialogueZones)
                if (!z.HasFired && z.Bounds.IntersectsWith(playerRect))
                    return z;
            return null;
        }

        // ── Idea 6: Prophecy text reveal ──────────────────────────────────────
        private static readonly string[] _prophecyLines =
        {
            "\"The one who walks on ice will reach the storm...\"",
            "\"...the storm will reveal the key to the sky...\"",
            "\"...and the sky will lead to the warlord's end.\"",
            "\"She will not be stopped.\"",
        };
        private static int _prophecyIndex;

        /// <summary>
        /// Returns the next prophecy line to reveal. Returns null when all revealed.
        /// Idea 6 (Narrative Designer).
        /// </summary>
        public static string RevealNextProphecyLine()
        {
            if (_prophecyIndex >= _prophecyLines.Length) return null;
            return _prophecyLines[_prophecyIndex++];
        }

        /// <summary>All prophecy lines revealed so far.</summary>
        public static IEnumerable<string> RevealedProphecyLines()
        {
            for (int i = 0; i < _prophecyIndex; i++)
                yield return _prophecyLines[i];
        }

        // ── Idea 7: Antagonist monologue lines ────────────────────────────────
        /// <summary>
        /// Pre-boss monologue lines keyed by boss identifier.
        /// Idea 7 (Narrative Designer).
        /// </summary>
        public static readonly Dictionary<string, string[]> BossMonologues =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "sudo", new[]
                    {
                        "Sudo: \"You sailed through the storm? Impressive.\"",
                        "Sudo: \"Let's see if your ice holds against my flames.\"",
                        "Sudo: \"This is where your adventure ends.\""
                    }
                },
                {
                    "warlord_final", new[]
                    {
                        "Grand Admiral: \"You are a long way from home, Miss Friday.\"",
                        "Grand Admiral: \"The New World does not forgive idealism.\"",
                        "Grand Admiral: \"I will not underestimate you. Not again.\""
                    }
                }
            };

        // ── Idea 8: Crew bond dialogue ────────────────────────────────────────
        /// <summary>
        /// Crew banter lines triggered at specific crew-bond thresholds.
        /// Threshold = number of levels cleared together.
        /// Idea 8 (Narrative Designer).
        /// </summary>
        public static readonly Dictionary<int, string> BondDialogue =
            new Dictionary<int, string>
            {
                { 3,  "Orca: \"You keep finding a way.  I'm starting to believe it's not luck.\"" },
                { 6,  "Swan: \"Three worlds behind us.  Whatever's ahead — we face it together.\"" },
                { 10, "Friday: \"Thank you.  Both of you.  I could not do this without you.\"" },
            };

        /// <summary>
        /// Returns the bond dialogue line for the exact threshold, or null.
        /// Idea 8 (Narrative Designer).
        /// </summary>
        public static string GetBondDialogue(int levelsCleared)
        {
            return BondDialogue.ContainsKey(levelsCleared) ? BondDialogue[levelsCleared] : null;
        }

        // ── Idea 9: World map lore blurbs ─────────────────────────────────────
        /// <summary>
        /// Short flavor text per overworld node used on the world map.
        /// Idea 9 (Narrative Designer).
        /// </summary>
        public static readonly Dictionary<string, string> WorldLore =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "island",   "DINOSAUR ISLAND — Where prehistory never ended." },
                { "storm",    "STORM BELT — Navigate or be swallowed." },
                { "sky",      "SKY ISLAND — Beyond the clouds, beyond the law." },
                { "wano",     "BLADE NATION — Honour demands a steep price." },
                { "blockade", "MARINE BLOCKADE — The Navy's iron wall." },
                { "warlord",  "WARLORD'S DOMAIN — Fire and ambition." },
                { "abyss",    "THE ABYSS — Nothing returns from here." },
            };

        // ── Idea 10: Epilogue slides ───────────────────────────────────────────
        /// <summary>
        /// Post-run story slides shown in VictoryScene and GameOverScene.
        /// Idea 10 (Narrative Designer).
        /// </summary>
        public static readonly List<EpilogueSlide> VictorySlides = new List<EpilogueSlide>
        {
            new EpilogueSlide
            {
                Category = "Victory",
                Header   = "THE GRAND LINE CONQUERED",
                Body     = "Miss Friday planted her crew's flag at the edge of the New World.\n" +
                           "The Marines watched from a safe distance.\n" +
                           "The adventure was only beginning."
            },
        };

        public static readonly List<EpilogueSlide> DefeatSlides = new List<EpilogueSlide>
        {
            new EpilogueSlide
            {
                Category = "Defeat",
                Header   = "THE SEA DOES NOT WAIT",
                Body     = "She will rest.  She will heal.\n" +
                           "And then she will sail again —\n" +
                           "because that is what she does."
            },
        };
    }

    // ── Idea 5 helper: NPC dialogue trigger zone ──────────────────────────────

    /// <summary>
    /// Describes one NPC dialogue trigger zone placed in a level.
    /// Idea 5 (Narrative Designer).
    /// </summary>
    public sealed class DialogueTriggerZone
    {
        /// <summary>World-space area that activates the dialogue on player overlap.</summary>
        public RectangleF Bounds      { get; set; }
        /// <summary>NPC name shown in the dialogue box header.</summary>
        public string     NpcName    { get; set; }
        /// <summary>Lines of dialogue to display in sequence.</summary>
        public string[]   Lines      { get; set; }
        /// <summary>True once this zone has been triggered (prevents re-fire).</summary>
        public bool       HasFired   { get; set; }
    }
}
