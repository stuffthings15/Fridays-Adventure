using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  NarrativeExtensions.cs  —  Narrative Designer / Writer: 10 NEW ideas
    //
    //  Idea 1:  Villain monologue system — scripted taunts mid-level from bosses.
    //  Idea 2:  Captain's letter — readable letter scene from story NPCs.
    //  Idea 3:  Journal entry system — player-collected lore fragments.
    //  Idea 4:  NPC mini-conversation — 2-line exchanges on overworld nodes.
    //  Idea 5:  Lore codex — indexed database of world lore accessible from menu.
    //  Idea 6:  Secret ending flag — unlocked by completing all side objectives.
    //  Idea 7:  World-end dialogue — brief narrative text after each world clear.
    //  Idea 8:  Letter seal animation trigger — visual effect when letter opens.
    //  Idea 9:  Prologue narration lines — loading-screen story text.
    //  Idea 10: Voice line subtitles — in-game subtitle bar for audio cues.
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Idea 1: Villain monologue system ──────────────────────────────────────
    /// <summary>
    /// Displays a timed villain monologue bar at the top of the screen mid-level.
    /// Call <see cref="Trigger"/> from boss or event scenes.
    /// Team 6 (Narrative Designer) — Idea 1.
    /// </summary>
    public static class VillainMonologue
    {
        private static string _line;
        private static float  _timer;
        private static string _speaker;
        private const  float  DisplayTime = 4.5f;

        /// <summary>Triggers a villain monologue line.</summary>
        public static void Trigger(string speaker, string line)
        {
            _speaker = speaker ?? "???";
            _line    = line    ?? string.Empty;
            _timer   = DisplayTime;
            DebugLogger.LogInfo("VillainMonologue", $"{_speaker}: {_line}");
        }

        /// <summary>Advances the display timer.</summary>
        public static void Tick(float dt) { if (_timer > 0f) _timer -= dt; }

        /// <summary>
        /// Draws the villain monologue bar. Call from gameplay scene Draw() after the world.
        /// Team 6 (Narrative Designer) — Idea 1.
        /// </summary>
        public static void Draw(Graphics g, int W)
        {
            if (_timer <= 0f) return;
            float alpha = _timer < 0.5f ? _timer / 0.5f : 1f;
            int a = (int)(220 * alpha);

            using (var br = new SolidBrush(Color.FromArgb(a, 10, 5, 20)))
                g.FillRectangle(br, 0, 0, W, 50);
            using (var pen = new Pen(Color.FromArgb(a, 180, 60, 60), 2))
                g.DrawLine(pen, 0, 49, W, 49);
            using (var fS = new Font("Courier New", 9, FontStyle.Bold))
            using (var fL = new Font("Courier New", 11))
            using (var brS = new SolidBrush(Color.FromArgb(a, 220, 80, 60)))
            using (var brL = new SolidBrush(Color.FromArgb(a, 230, 230, 230)))
            {
                g.DrawString($"{_speaker}:", fS, brS, 12, 8);
                g.DrawString(_line,          fL, brL, 12, 26);
            }
        }
    }

    // ── Idea 2: Captain's letter ───────────────────────────────────────────────
    /// <summary>
    /// A readable letter scene pushed on the stack for 5 seconds then auto-popped.
    /// Team 6 (Narrative Designer) — Idea 2.
    /// </summary>
    public static class LetterContent
    {
        // ── Letter database ────────────────────────────────────────────────────
        private static readonly Dictionary<string, (string From, string[] Lines)> _letters =
            new Dictionary<string, (string, string[])>(StringComparer.OrdinalIgnoreCase)
        {
            ["intro"] = ("Captain Morrow", new[]
            {
                "Miss Friday —",
                "",
                "The Sea Serpent has been spotted near the Grand Line.",
                "Take the crew and investigate. Do not engage alone.",
                "The Ice-Ice Fruit user has been seen nearby.",
                "",
                "Be careful out there.",
                "                            — Captain"
            }),
            ["warlord_warning"] = ("Anonymous", new[]
            {
                "To whoever finds this —",
                "",
                "The Warlord Sudo has taken control of the eastern routes.",
                "His Devil Fruit grants him dominion over stone.",
                "Normal attacks will not work. Aim for the gem on his chest.",
                "",
                "Good luck. You'll need it."
            }),
            ["secret"] = ("Toad Elder", new[]
            {
                "Brave adventurer —",
                "",
                "If you have collected all King Coins,",
                "the true ending awaits beyond the Grand Line.",
                "Seek the white warp pipe in the final world.",
                "",
                "The secret path is open only to those who have seen all.",
            }),
        };

        /// <summary>Returns a letter's content by ID, or null if not found.</summary>
        public static (string From, string[] Lines)? Get(string id) =>
            _letters.TryGetValue(id, out var v) ? v : ((string, string[])?)null;
    }

    // ── Idea 3: Journal entry system ──────────────────────────────────────────
    /// <summary>
    /// Tracks collected lore journal entries (up to 20 per playthrough).
    /// Entries are keyed strings shown in the CaptainsLog / logbook form.
    /// Team 6 (Narrative Designer) — Idea 3.
    /// </summary>
    public static class JournalSystem
    {
        private static readonly List<JournalEntry> _entries = new List<JournalEntry>();

        public sealed class JournalEntry
        {
            public string Title;
            public string Body;
            public string WorldContext;
            public DateTime Collected;
        }

        /// <summary>Adds a new journal entry if not already collected.</summary>
        public static void Collect(string title, string body, string world)
        {
            foreach (var e in _entries)
                if (string.Equals(e.Title, title, StringComparison.OrdinalIgnoreCase)) return;

            _entries.Add(new JournalEntry { Title = title, Body = body,
                WorldContext = world, Collected = DateTime.Now });
            DebugLogger.LogInfo("Journal", $"Entry collected: {title}");
            EventBus.Publish(new JournalEntryCollectedEvent { Title = title });
        }

        /// <summary>Returns all collected entries.</summary>
        public static IReadOnlyList<JournalEntry> All => _entries;
    }

    /// <summary>Fired when a new journal entry is collected. Team 6 — Idea 3.</summary>
    public sealed class JournalEntryCollectedEvent { public string Title; }

    // ── Idea 4: NPC mini-conversation ─────────────────────────────────────────
    /// <summary>
    /// Short 2-line NPC exchange rendered as a dialogue bubble.
    /// Team 6 (Narrative Designer) — Idea 4.
    /// </summary>
    public static class NPCConversation
    {
        // Table of NPC conversations keyed by overworld node ID.
        private static readonly Dictionary<string, (string Speaker, string Line)[]> _table =
            new Dictionary<string, (string, string)[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["dino"]    = new[] { ("Villager",  "Watch out for the dinosaurs in the jungle!"),
                                  ("Guard",     "The path to Sky Island is blocked by storms.") },
            ["sky"]     = new[] { ("Scholar",   "The sky temples hold ancient technology."),
                                  ("Merchant",  "I'll trade Star Coins for items — find me here!") },
            ["harbor"]  = new[] { ("Sailor",    "The Marine Blockade has three checkpoints."),
                                  ("Fisherman", "Tuna schooling near the reef today.") },
            ["tundra"]  = new[] { ("Elder",     "Only the Frog Suit grants true underwater freedom."),
                                  ("Hunter",    "Beware the ice caves — Thwomps abound.") },
        };

        /// <summary>Returns random conversation lines for a given node, or null.</summary>
        public static (string Speaker, string Line)? GetLine(string nodeId)
        {
            if (!_table.TryGetValue(nodeId, out var lines) || lines.Length == 0) return null;
            var rng = new Random();
            return lines[rng.Next(lines.Length)];
        }
    }

    // ── Idea 5: Lore codex ────────────────────────────────────────────────────
    /// <summary>
    /// Indexed lore database for the in-game encyclopaedia.
    /// Entries are revealed as the player progresses.
    /// Team 6 (Narrative Designer) — Idea 5.
    /// </summary>
    public static class LoreCodex
    {
        public sealed class LoreEntry
        {
            public string Id;
            public string Category;  // "Character", "Location", "Devil Fruit", etc.
            public string Title;
            public string Text;
            public bool   Revealed;
        }

        private static readonly List<LoreEntry> _entries = BuildCodex();

        private static List<LoreEntry> BuildCodex()
        {
            return new List<LoreEntry>
            {
                new LoreEntry { Id="friday",    Category="Character",  Title="Miss Friday",
                    Text="A skilled ice-fruit user and navigator of the Grand Line. Her devil fruit grants control over ice and cold temperature.", Revealed=true },
                new LoreEntry { Id="orca",      Category="Character",  Title="Orca Companion",
                    Text="Friday's battle partner. An intelligent orca who gained partial human form via a mysterious Devil Fruit.", Revealed=false },
                new LoreEntry { Id="swan",      Category="Character",  Title="Swan Companion",
                    Text="A graceful Swan-human hybrid with gliding abilities. Joined the crew after being rescued from the Marine Blockade.", Revealed=false },
                new LoreEntry { Id="ice_fruit", Category="Devil Fruit", Title="Ice-Ice Fruit",
                    Text="A Logia-type Devil Fruit that transforms the user into an ice elemental. The user can create ice structures and freeze opponents.", Revealed=true },
                new LoreEntry { Id="sea_stone", Category="Devil Fruit", Title="Sea Stone",
                    Text="A rare mineral found on the ocean floor. It negates Devil Fruit powers and is used by the Marines to suppress users.", Revealed=false },
                new LoreEntry { Id="grand_line",Category="Location",   Title="Grand Line",
                    Text="A treacherous ocean route that circles the globe. Every pirate dreams of sailing it. Unpredictable weather and powerful enemies make it deadly.", Revealed=true },
                new LoreEntry { Id="sky_island",Category="Location",   Title="Sky Island",
                    Text="A floating island sustained by a rare mineral called Vearith Stone. Home to ancient automatons and forgotten technology.", Revealed=false },
                new LoreEntry { Id="sudo",      Category="Character",  Title="Warlord Sudo",
                    Text="A former Marine Vice Admiral turned Warlord. His Stone-Stone Fruit gives him tremendous defensive power.", Revealed=false },
            };
        }

        /// <summary>Returns all lore entries.</summary>
        public static IReadOnlyList<LoreEntry> All => _entries;

        /// <summary>Reveals a codex entry by ID.</summary>
        public static void Reveal(string id)
        {
            foreach (var e in _entries)
                if (e.Id == id && !e.Revealed)
                {
                    e.Revealed = true;
                    DebugLogger.LogInfo("LoreCodex", $"Revealed: {e.Title}");
                    EventBus.Publish(new LoreRevealedEvent { EntryId = id });
                    return;
                }
        }

        /// <summary>Returns revealed entries only.</summary>
        public static IEnumerable<LoreEntry> RevealedEntries()
        {
            foreach (var e in _entries) if (e.Revealed) yield return e;
        }
    }

    /// <summary>Published when a lore entry is revealed. Team 6 — Idea 5.</summary>
    public sealed class LoreRevealedEvent { public string EntryId; }

    // ── Idea 6: Secret ending flag ────────────────────────────────────────────
    /// <summary>
    /// Tracks progress toward the secret ending.
    /// All three conditions must be met before the secret path unlocks.
    /// Team 6 (Narrative Designer) — Idea 6.
    /// </summary>
    public static class SecretEnding
    {
        /// <summary>True when all King Coins in World 1 are collected.</summary>
        public static bool World1KingCoinsCollected  { get; set; }

        /// <summary>True when the Lore Codex has at least 6 entries revealed.</summary>
        public static bool LoreCodexHalfRevealed =>
            System.Linq.Enumerable.Count(LoreCodex.RevealedEntries()) >= 6;

        /// <summary>True when the hidden bonus room has been cleared.</summary>
        public static bool BonusRoomCleared          { get; set; }

        /// <summary>Returns true when all conditions for the secret ending are met.</summary>
        public static bool IsUnlocked =>
            World1KingCoinsCollected && LoreCodexHalfRevealed && BonusRoomCleared;

        /// <summary>Checks and logs secret ending status.</summary>
        public static void Check()
        {
            if (IsUnlocked)
                DebugLogger.LogInfo("SecretEnding", "SECRET ENDING UNLOCKED.");
        }
    }

    // ── Idea 7: World-end dialogue ────────────────────────────────────────────
    /// <summary>
    /// Returns narrative text shown after each world is cleared.
    /// Team 6 (Narrative Designer) — Idea 7.
    /// </summary>
    public static class WorldEndDialogue
    {
        private static readonly Dictionary<int, string> _text = new Dictionary<int, string>
        {
            [1] = "The sea lanes are clear — for now.\nBut the Grand Line still lies ahead.",
            [2] = "Warlord Sudo has fallen.\nThe crew grows stronger with every battle.",
            [3] = "The deep secrets of the ocean reveal themselves.\nThe final challenge approaches.",
        };

        /// <summary>Returns dialogue for the cleared world, or a default.</summary>
        public static string Get(int world) =>
            _text.TryGetValue(world, out string t) ? t
            : "The adventure continues beyond the horizon...";
    }

    // ── Idea 8: Letter seal animation trigger ─────────────────────────────────
    /// <summary>
    /// Manages the wax-seal animation state for the letter scene.
    /// Team 6 (Narrative Designer) — Idea 8.
    /// </summary>
    public static class LetterSealAnimation
    {
        private static float _timer;
        private const float Duration = 1.2f;

        /// <summary>Starts the seal break animation.</summary>
        public static void Trigger() => _timer = Duration;

        /// <summary>Tick from letter scene Update.</summary>
        public static void Tick(float dt) { if (_timer > 0f) _timer -= dt; }

        /// <summary>0→1 progress of the seal animation.</summary>
        public static float Progress => _timer > 0f ? 1f - _timer / Duration : 1f;

        /// <summary>True while the seal is animating.</summary>
        public static bool IsPlaying => _timer > 0f;

        /// <summary>Draws the wax seal overlay.</summary>
        public static void Draw(Graphics g, int cx, int cy)
        {
            if (Progress >= 1f) return;
            float scale = 1f - EasingFunctions.EaseOutCubic(Progress);
            int r = (int)(60 * scale);
            using (var br = new SolidBrush(Color.FromArgb((int)(200 * scale), 160, 30, 30)))
                g.FillEllipse(br, cx - r, cy - r, r * 2, r * 2);
            using (var f  = new Font("Courier New", (int)(20 * scale + 1), FontStyle.Bold))
            using (var brT = new SolidBrush(Color.FromArgb((int)(255 * scale), Color.Gold)))
                g.DrawString("✦", f, brT, cx - 10, cy - 12);
        }
    }

    // ── Idea 9: Prologue narration lines ──────────────────────────────────────
    /// <summary>
    /// Story text shown on the loading / prologue screen.
    /// Team 6 (Narrative Designer) — Idea 9.
    /// </summary>
    public static class PrologueNarration
    {
        private static readonly string[] _lines =
        {
            "In a world of endless seas, only the bold dare sail the Grand Line.",
            "Miss Friday — ice-fruit user, navigator, and reluctant hero.",
            "A mysterious distress signal from the ancient Sky Islands...",
            "The Sea Serpent stirs. The Warlords gather their forces.",
            "One crew. One ship. One chance to set things right.",
        };

        private static int _lineIndex;

        /// <summary>Returns the next prologue line.</summary>
        public static string NextLine()
        {
            string l = _lines[_lineIndex % _lines.Length];
            _lineIndex++;
            return l;
        }

        /// <summary>Returns all prologue lines.</summary>
        public static IReadOnlyList<string> All => _lines;
    }

    // ── Idea 10: Voice-line subtitles ─────────────────────────────────────────
    /// <summary>
    /// Displays a subtitle bar for audio voice-line cues.
    /// Team 6 (Narrative Designer) — Idea 10.
    /// </summary>
    public static class Subtitles
    {
        private static string _text;
        private static float  _timer;
        private const  float  DefaultDuration = 3.5f;

        /// <summary>Displays a subtitle for a given duration.</summary>
        public static void Show(string text, float duration = DefaultDuration)
        {
            _text  = text;
            _timer = duration;
        }

        /// <summary>Tick from game loop.</summary>
        public static void Tick(float dt) { if (_timer > 0f) _timer -= dt; }

        /// <summary>Draws the subtitle bar at the bottom of the screen.</summary>
        public static void Draw(Graphics g, int W, int H)
        {
            if (_timer <= 0f || string.IsNullOrEmpty(_text)) return;
            float alpha = _timer < 0.4f ? _timer / 0.4f : 1f;
            using (var br = new SolidBrush(Color.FromArgb((int)(180 * alpha), 0, 0, 0)))
                g.FillRectangle(br, 0, H - 44, W, 44);
            using (var f  = new Font("Courier New", 11))
            using (var brT = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.White)))
            {
                SizeF sz = g.MeasureString(_text, f);
                g.DrawString(_text, f, brT, (W - sz.Width) / 2f, H - 32);
            }
        }
    }
}
