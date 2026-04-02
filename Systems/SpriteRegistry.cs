using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    // ─────────────────────────────────────────────────────────────────────────
    //  SpriteRegistry.cs  —  Character Artist (2D) Systems
    //
    //  Team 13 (Character Artist / 2D) — all 10 ideas implemented:
    //
    //    Idea 1:  Named sprite-state catalog — registers every character
    //             animation state with its frame count and loop flag.
    //    Idea 2:  Character portrait registry — stores portrait images
    //             for dialogue boxes and the HUD life portrait.
    //    Idea 3:  Hitbox vs. sprite visualizer — debug overlay that draws
    //             the logical hitbox and the sprite bounds side-by-side
    //             so artists can verify alignment without running the game.
    //    Idea 4:  Spritesheet metadata store — grid dimensions (cols × rows,
    //             cell size) for every registered sheet.
    //    Idea 5:  Character skin / variant registry — tracks unlocked
    //             palette-swap skins and the currently active skin.
    //    Idea 6:  Automatic idle-blink cycle — fires a callback every
    //             N seconds so the animator can swap the blink frame.
    //    Idea 7:  Hurt-flash helper — returns true for the first 0.6 s
    //             after damage so the draw layer can tint the sprite red.
    //    Idea 8:  Death-pose freeze — latches the last live frame index
    //             so the death animation holds on the correct frame.
    //    Idea 9:  Directional flip helper — returns a horizontal scale
    //             (−1 or +1) based on the character's facing direction.
    //    Idea 10: Sprite asset report — writes a text file listing every
    //             registered sprite, frame count, and skin count.
    // ─────────────────────────────────────────────────────────────────────────

    // ── Idea 1 support: animation-state descriptor ────────────────────────────
    /// <summary>
    /// Describes a single named animation state for a character.
    /// Idea 1 (Character Artist).
    /// </summary>
    public sealed class SpriteState
    {
        /// <summary>Unique animation state name, e.g. "Run", "Jump", "Hurt".</summary>
        public string Name      { get; set; }
        /// <summary>Total number of frames in this animation.</summary>
        public int    FrameCount { get; set; }
        /// <summary>Whether the animation loops back to frame 0 on completion.</summary>
        public bool   Loops      { get; set; }
        /// <summary>Seconds per frame (default 1/12 s → 12 fps).</summary>
        public float  FrameTime  { get; set; } = 1f / 12f;
    }

    // ── Idea 4 support: spritesheet grid metadata ─────────────────────────────
    /// <summary>
    /// Grid dimensions for a spritesheet image file.
    /// Idea 4 (Character Artist).
    /// </summary>
    public sealed class SheetMeta
    {
        /// <summary>Number of columns in the spritesheet grid.</summary>
        public int Cols      { get; set; }
        /// <summary>Number of rows in the spritesheet grid.</summary>
        public int Rows      { get; set; }
        /// <summary>Width of a single cell in pixels.</summary>
        public int CellWidth { get; set; }
        /// <summary>Height of a single cell in pixels.</summary>
        public int CellHeight { get; set; }

        /// <summary>Returns the source rectangle for the given frame index.</summary>
        public Rectangle FrameRect(int index)
        {
            int col = index % Cols;
            int row = index / Cols;
            return new Rectangle(col * CellWidth, row * CellHeight, CellWidth, CellHeight);
        }
    }

    /// <summary>
    /// Central character-art registry: sprite states, portraits, skins,
    /// animation helpers, and asset reporting.
    /// </summary>
    public static class SpriteRegistry
    {
        // ── Idea 1: Sprite-state catalog ─────────────────────────────────────
        /// <summary>
        /// All named animation states for every registered character.
        /// Key = "CharacterName/StateName".
        /// Idea 1 (Character Artist).
        /// </summary>
        private static readonly Dictionary<string, SpriteState> _states =
            new Dictionary<string, SpriteState>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a named animation state for a character.
        /// Idea 1 (Character Artist).
        /// </summary>
        public static void RegisterState(string character, string state,
                                         int frameCount, bool loops,
                                         float frameTime = 1f / 12f)
        {
            string key = $"{character}/{state}";
            _states[key] = new SpriteState
            {
                Name       = state,
                FrameCount = frameCount,
                Loops      = loops,
                FrameTime  = frameTime
            };
        }

        /// <summary>
        /// Retrieves a registered sprite state, or null if not found.
        /// Idea 1 (Character Artist).
        /// </summary>
        public static SpriteState GetState(string character, string state)
        {
            _states.TryGetValue($"{character}/{state}", out var s);
            return s;
        }

        // ── Idea 2: Portrait registry ─────────────────────────────────────────
        /// <summary>
        /// Portrait images keyed by "CharacterName/Mood" (e.g. "Friday/Happy").
        /// Idea 2 (Character Artist).
        /// </summary>
        private static readonly Dictionary<string, Image> _portraits =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a portrait image for a character/mood combination.
        /// Idea 2 (Character Artist).
        /// </summary>
        public static void RegisterPortrait(string character, string mood, Image img)
        {
            _portraits[$"{character}/{mood}"] = img;
        }

        /// <summary>
        /// Returns the portrait for a given character and mood, or null.
        /// Idea 2 (Character Artist).
        /// </summary>
        public static Image GetPortrait(string character, string mood = "Neutral")
        {
            _portraits.TryGetValue($"{character}/{mood}", out var img);
            return img;
        }

        // ── Idea 3: Hitbox vs. sprite debug visualizer ────────────────────────
        /// <summary>
        /// Draws a debug overlay comparing sprite bounds (blue) to hitbox (red)
        /// so artists can verify artwork alignment during development.
        /// Idea 3 (Character Artist).
        /// </summary>
        /// <param name="g">Active Graphics context.</param>
        /// <param name="spriteBounds">The drawn sprite rectangle in screen space.</param>
        /// <param name="hitbox">The logical collision rectangle in screen space.</param>
        public static void DrawHitboxOverlay(Graphics g, Rectangle spriteBounds, Rectangle hitbox)
        {
            using (var spritePen = new Pen(Color.FromArgb(140, 80, 140, 255), 1))
            using (var hitboxPen = new Pen(Color.FromArgb(200, 255, 40, 40),  1))
            {
                g.DrawRectangle(spritePen, spriteBounds); // sprite extent in blue-purple
                g.DrawRectangle(hitboxPen, hitbox);       // logical hitbox in red
            }
        }

        // ── Idea 4: Spritesheet metadata ─────────────────────────────────────
        /// <summary>
        /// Spritesheet grid metadata keyed by sheet name.
        /// Idea 4 (Character Artist).
        /// </summary>
        private static readonly Dictionary<string, SheetMeta> _sheets =
            new Dictionary<string, SheetMeta>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers the grid layout for a spritesheet.
        /// Idea 4 (Character Artist).
        /// </summary>
        public static void RegisterSheet(string name, int cols, int rows,
                                          int cellW, int cellH)
        {
            _sheets[name] = new SheetMeta
            {
                Cols = cols, Rows = rows,
                CellWidth = cellW, CellHeight = cellH
            };
        }

        /// <summary>
        /// Returns the source rectangle for a given frame on a named sheet.
        /// Idea 4 (Character Artist).
        /// </summary>
        public static Rectangle FrameRect(string sheetName, int frameIndex)
        {
            return _sheets.TryGetValue(sheetName, out var meta)
                ? meta.FrameRect(frameIndex)
                : Rectangle.Empty;
        }

        // ── Idea 5: Character skin / variant registry ─────────────────────────
        /// <summary>
        /// All unlocked skins per character.  Key = character name.
        /// Idea 5 (Character Artist).
        /// </summary>
        private static readonly Dictionary<string, List<string>> _skins =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Active skin name per character (defaults to "Default").</summary>
        private static readonly Dictionary<string, string> _activeSkin =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Unlocks and registers a palette-swap skin for a character.
        /// Idea 5 (Character Artist).
        /// </summary>
        public static void UnlockSkin(string character, string skinName)
        {
            if (!_skins.ContainsKey(character))
                _skins[character] = new List<string> { "Default" };

            if (!_skins[character].Contains(skinName))
                _skins[character].Add(skinName);
        }

        /// <summary>
        /// Sets the active skin for a character. Logs an error if not unlocked.
        /// Idea 5 (Character Artist).
        /// </summary>
        public static void SetActiveSkin(string character, string skinName)
        {
            if (_skins.TryGetValue(character, out var list) && list.Contains(skinName))
                _activeSkin[character] = skinName;
            else
                DebugLogger.LogWarning("SpriteRegistry", $"Skin '{skinName}' not unlocked for '{character}'.");
        }

        /// <summary>Returns the currently active skin name for a character.</summary>
        public static string GetActiveSkin(string character)
        {
            return _activeSkin.TryGetValue(character, out var s) ? s : "Default";
        }

        // ── Idea 6: Automatic idle-blink cycle ────────────────────────────────
        /// <summary>Accumulated time since last blink per character.</summary>
        private static readonly Dictionary<string, float> _blinkTimer =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        /// <summary>How often (seconds) the blink frame fires.</summary>
        private const float BlinkInterval = 3.5f;
        /// <summary>Duration of the blink frame (seconds).</summary>
        private const float BlinkDuration = 0.12f;

        /// <summary>
        /// Advances the idle-blink timer. Returns true during the blink window
        /// so the draw layer can substitute the closed-eye frame.
        /// Idea 6 (Character Artist).
        /// </summary>
        public static bool UpdateBlink(string character, float dt)
        {
            if (!_blinkTimer.ContainsKey(character)) _blinkTimer[character] = 0f;
            _blinkTimer[character] += dt;

            float t = _blinkTimer[character] % BlinkInterval;

            // Reset full cycle counter each interval.
            if (_blinkTimer[character] > BlinkInterval * 100f)
                _blinkTimer[character] -= BlinkInterval * 100f;

            return t < BlinkDuration; // true = show blink frame
        }

        // ── Idea 7: Hurt-flash helper ─────────────────────────────────────────
        /// <summary>Time since last damage event per character.</summary>
        private static readonly Dictionary<string, float> _hurtTimer =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Duration of the red hurt-flash effect in seconds.</summary>
        private const float HurtFlashDuration = 0.6f;

        /// <summary>
        /// Call when a character takes damage to start the hurt-flash.
        /// Idea 7 (Character Artist).
        /// </summary>
        public static void TriggerHurtFlash(string character)
        {
            _hurtTimer[character] = HurtFlashDuration;
        }

        /// <summary>
        /// Advances hurt-flash timer. Returns true while the flash is active
        /// so the draw layer can apply a red tint.
        /// Idea 7 (Character Artist).
        /// </summary>
        public static bool UpdateHurtFlash(string character, float dt)
        {
            if (!_hurtTimer.TryGetValue(character, out float t) || t <= 0f)
                return false;

            _hurtTimer[character] = Math.Max(0f, t - dt);

            // Alternate on/off every 0.06 s for a flicker effect.
            float phase = _hurtTimer[character] / HurtFlashDuration;
            return (int)(phase / 0.1f) % 2 == 0;
        }

        // ── Idea 8: Death-pose freeze ─────────────────────────────────────────
        /// <summary>
        /// Latched death-frame index per character.
        /// −1 means the character is alive (no latch active).
        /// Idea 8 (Character Artist).
        /// </summary>
        private static readonly Dictionary<string, int> _deathFrame =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Latches the current frame index as the death pose.
        /// Call once when the character enters the death state.
        /// Idea 8 (Character Artist).
        /// </summary>
        public static void LatchDeathFrame(string character, int frameIndex)
        {
            _deathFrame[character] = frameIndex;
        }

        /// <summary>
        /// Returns the latched death frame, or −1 if none.
        /// Idea 8 (Character Artist).
        /// </summary>
        public static int GetDeathFrame(string character)
        {
            return _deathFrame.TryGetValue(character, out int f) ? f : -1;
        }

        /// <summary>Clears the death-pose latch (e.g. on respawn).</summary>
        public static void ClearDeathFrame(string character)
        {
            _deathFrame.Remove(character);
        }

        // ── Idea 9: Directional flip helper ──────────────────────────────────
        /// <summary>
        /// Returns the horizontal transform scale for the sprite:
        ///  +1.0f → facing right (no flip),  −1.0f → facing left (flip).
        /// Use this to set Graphics.ScaleTransform before drawing the sprite.
        /// Idea 9 (Character Artist).
        /// </summary>
        /// <param name="facingRight">True when the character faces right.</param>
        public static float FacingScale(bool facingRight) => facingRight ? 1f : -1f;

        // ── Idea 10: Sprite asset report ──────────────────────────────────────
        /// <summary>
        /// Writes a text report listing every registered sprite state, sheet,
        /// portrait, and skin count to the Logs\ folder.
        /// Idea 10 (Character Artist).
        /// </summary>
        public static void ExportSpriteReport()
        {
            try
            {
                string dir  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "sprite_report.txt");

                using (var sw = new StreamWriter(path, append: false))
                {
                    sw.WriteLine("=== Friday's Adventure — Sprite Asset Report ===");
                    sw.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sw.WriteLine();

                    // -- Animation states
                    sw.WriteLine("--- Registered Animation States ---");
                    foreach (var kv in _states)
                        sw.WriteLine($"  {kv.Key,40}  frames={kv.Value.FrameCount,3}  " +
                                     $"loops={kv.Value.Loops,-5}  fps={1f / kv.Value.FrameTime:F1}");

                    // -- Spritesheet grids
                    sw.WriteLine();
                    sw.WriteLine("--- Spritesheet Grids ---");
                    foreach (var kv in _sheets)
                        sw.WriteLine($"  {kv.Key,30}  {kv.Value.Cols}×{kv.Value.Rows}  " +
                                     $"cell={kv.Value.CellWidth}×{kv.Value.CellHeight}");

                    // -- Portraits
                    sw.WriteLine();
                    sw.WriteLine("--- Registered Portraits ---");
                    foreach (var kv in _portraits)
                        sw.WriteLine($"  {kv.Key}");

                    // -- Skins
                    sw.WriteLine();
                    sw.WriteLine("--- Character Skins ---");
                    foreach (var kv in _skins)
                        sw.WriteLine($"  {kv.Key,-20}  skins={kv.Value.Count}  " +
                                     $"active={GetActiveSkin(kv.Key)}");

                    sw.WriteLine();
                    sw.WriteLine("=== End of Report ===");
                }

                DebugLogger.LogInfo("SpriteRegistry", $"Sprite report written → {path}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogWarning("SpriteRegistry", $"Failed to write sprite report: {ex.Message}");
            }
        }

        // ── Static initializer: pre-register Friday's core states ─────────────
        /// <summary>
        /// Bootstraps the registry with Friday's base animation states.
        /// Call once at startup (e.g. from <c>Game.Initialize</c>).
        /// </summary>
        public static void RegisterDefaults()
        {
            // -- Friday base character states (Idea 1)
            RegisterState("Friday", "Idle",      4, loops: true,  frameTime: 1f / 8f);
            RegisterState("Friday", "Walk",      8, loops: true,  frameTime: 1f / 12f);
            RegisterState("Friday", "Run",       8, loops: true,  frameTime: 1f / 16f);
            RegisterState("Friday", "Jump",      3, loops: false, frameTime: 1f / 10f);
            RegisterState("Friday", "Fall",      2, loops: true,  frameTime: 1f / 8f);
            RegisterState("Friday", "Land",      2, loops: false, frameTime: 1f / 10f);
            RegisterState("Friday", "Hurt",      3, loops: false, frameTime: 1f / 10f);
            RegisterState("Friday", "Death",     6, loops: false, frameTime: 1f / 8f);
            RegisterState("Friday", "Swim",      6, loops: true,  frameTime: 1f / 10f);
            RegisterState("Friday", "Climb",     4, loops: true,  frameTime: 1f / 8f);
            RegisterState("Friday", "Slide",     2, loops: false, frameTime: 1f / 8f);
            RegisterState("Friday", "Blink",     1, loops: false, frameTime: 1f / 10f);
            RegisterState("Friday", "Attack",    5, loops: false, frameTime: 1f / 14f);

            // -- Default spritesheet grid (Idea 4)
            RegisterSheet("friday_sheet",  8, 8, 48, 48);
            RegisterSheet("enemy_sheet",   6, 6, 32, 32);
            RegisterSheet("boss_sheet",    4, 8, 64, 64);

            // -- Unlock the default skin for Friday (Idea 5)
            UnlockSkin("Friday", "Default");
        }
    }
}
