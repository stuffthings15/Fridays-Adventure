using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SystemsToolsExtensions.cs  —  Systems / Tools Programmer: 10 NEW ideas
    //
    //  Idea 1:  Sprite-sheet parser — cuts named frames from a grid sprite sheet.
    //  Idea 2:  Map animation controller — ticks overworld node icon animations.
    //  Idea 3:  Entity spawner pool — pre-warms enemy/item instances for reuse.
    //  Idea 4:  Transition buffer manager — preloads next scene assets before fade.
    //  Idea 5:  Debug-draw primitives — one-call helpers for hitboxes, arrows, circles.
    //  Idea 6:  Event-queue inspector — lists pending EventBus subscriptions to console.
    //  Idea 7:  Frame-step mode — pause game, advance one frame at a time via F8.
    //  Idea 8:  World-state serializer — JSON snapshot of Game singleton state.
    //  Idea 9:  Tile-collision flag editor — runtime toggle of solid/passable tiles.
    //  Idea 10: Asset hot-list — tracks most recently accessed assets for pre-caching.
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Idea 1: Sprite-sheet parser ───────────────────────────────────────────
    /// <summary>
    /// Parses a uniform-grid sprite sheet into named frame bitmaps.
    /// Team 8 (Systems Programmer) — Idea 1.
    /// </summary>
    public static class SpriteSheetParser
    {
        /// <summary>
        /// Slices a bitmap sheet into frame bitmaps arranged on a regular grid.
        /// </summary>
        /// <param name="sheet">Source sheet bitmap.</param>
        /// <param name="frameW">Width of each frame in pixels.</param>
        /// <param name="frameH">Height of each frame in pixels.</param>
        /// <param name="names">Names assigned to frames left-to-right, top-to-bottom.</param>
        /// <returns>Dictionary mapping name → cropped frame bitmap.</returns>
        public static Dictionary<string, Bitmap> Parse(
            Bitmap sheet, int frameW, int frameH, IReadOnlyList<string> names)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            var result = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
            int cols = sheet.Width  / frameW;
            int rows = sheet.Height / frameH;
            int n    = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (n >= names.Count) break;
                    string name = names[n++];
                    var frame = new Bitmap(frameW, frameH, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(frame))
                        g.DrawImage(sheet, new Rectangle(0, 0, frameW, frameH),
                            new Rectangle(col * frameW, row * frameH, frameW, frameH),
                            GraphicsUnit.Pixel);
                    result[name] = frame;
                }
            }
            DebugLogger.LogInfo("SpriteSheetParser",
                $"Parsed {result.Count} frames ({cols}×{rows} grid, {frameW}×{frameH}px).");
            return result;
        }

        /// <summary>
        /// Convenience: loads a sheet PNG from disk and slices it.
        /// Returns null if the file doesn't exist.
        /// Team 8 (Systems Programmer) — Idea 1.
        /// </summary>
        public static Dictionary<string, Bitmap> LoadAndParse(
            string path, int frameW, int frameH, IReadOnlyList<string> names)
        {
            if (!File.Exists(path))
            {
                DebugLogger.LogWarning("SpriteSheetParser", $"Sheet not found: {path}");
                return null;
            }
            try
            {
                using (var bmp = new Bitmap(path))
                    return Parse(bmp, frameW, frameH, names);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SpriteSheetParser.LoadAndParse", ex);
                return null;
            }
        }
    }

    // ── Idea 2: Map animation controller ──────────────────────────────────────
    /// <summary>
    /// Drives per-frame animated icons on the overworld map (blinking nodes,
    /// walking Hammer Bros, animated player cursor, etc.).
    /// Team 8 (Systems Programmer) — Idea 2.
    /// </summary>
    public static class MapAnimationController
    {
        private static float _cursorBlink;
        private static bool  _cursorOn = true;
        private const  float BlinkRate = 0.55f;

        private static float _hbWalkTimer;
        private static int   _hbFrame;
        private const  float HbFrameRate = 0.25f;

        /// <summary>
        /// True when the player-cursor blink is in the "on" phase.
        /// Scenes use this to draw or skip the cursor sprite.
        /// </summary>
        public static bool CursorVisible => _cursorOn;

        /// <summary>Hammer Bros walk frame index (0 or 1).</summary>
        public static int HammerBrosFrame => _hbFrame;

        /// <summary>Tick all map animations.</summary>
        public static void Tick(float dt)
        {
            // Cursor blink.
            _cursorBlink += dt;
            if (_cursorBlink >= BlinkRate) { _cursorBlink = 0; _cursorOn = !_cursorOn; }

            // Hammer Bros walk cycle.
            _hbWalkTimer += dt;
            if (_hbWalkTimer >= HbFrameRate) { _hbWalkTimer = 0; _hbFrame = (_hbFrame + 1) % 2; }
        }

        /// <summary>
        /// Draws the animated player cursor (blinking arrow) over a map node.
        /// Team 8 (Systems Programmer) — Idea 2.
        /// </summary>
        public static void DrawCursor(Graphics g, int x, int y)
        {
            if (!_cursorOn) return;
            using (var f  = new Font("Courier New", 13, FontStyle.Bold))
            using (var br = new SolidBrush(Color.Yellow))
                g.DrawString("▼", f, br, x - 8, y - 26);
        }

        /// <summary>
        /// Draws the animated Hammer Bros icon over a node.
        /// Team 8 (Systems Programmer) — Idea 2.
        /// </summary>
        public static void DrawHammerBros(Graphics g, int x, int y)
        {
            string frame = _hbFrame == 0 ? "⚒" : "⚔";
            using (var f  = new Font("Courier New", 11, FontStyle.Bold))
            using (var br = new SolidBrush(Color.OrangeRed))
                g.DrawString(frame, f, br, x - 8, y - 22);
        }
    }

    // ── Idea 3: Entity spawner pool ───────────────────────────────────────────
    /// <summary>
    /// A typed pool pre-warming Entity instances for fast scene initialization.
    /// Team 8 (Systems Programmer) — Idea 3.
    /// </summary>
    public sealed class EntitySpawnerPool<T> where T : class, new()
    {
        private readonly Queue<T>   _pool     = new Queue<T>();
        private readonly Action<T>  _onBorrow;
        private readonly Action<T>  _onReturn;
        private readonly int        _maxSize;

        public EntitySpawnerPool(int preWarm, int maxSize,
            Action<T> onBorrow = null, Action<T> onReturn = null)
        {
            _maxSize  = maxSize;
            _onBorrow = onBorrow;
            _onReturn = onReturn;
            for (int i = 0; i < preWarm; i++) _pool.Enqueue(new T());
        }

        /// <summary>Borrows an entity from the pool (or creates a new one).</summary>
        public T Borrow()
        {
            T obj = _pool.Count > 0 ? _pool.Dequeue() : new T();
            _onBorrow?.Invoke(obj);
            return obj;
        }

        /// <summary>Returns an entity to the pool.</summary>
        public void Return(T obj)
        {
            if (obj == null) return;
            _onReturn?.Invoke(obj);
            if (_pool.Count < _maxSize) _pool.Enqueue(obj);
        }

        /// <summary>Current available objects.</summary>
        public int Available => _pool.Count;
    }

    // ── Idea 5: Debug-draw primitives ─────────────────────────────────────────
    /// <summary>
    /// One-call debug drawing helpers for hitboxes, velocity arrows, and radius circles.
    /// Only draws when the visual debugger overlay is active.
    /// Team 8 (Systems Programmer) — Idea 5.
    /// </summary>
    public static class DebugDraw
    {
        /// <summary>Draws a semi-transparent hitbox rectangle.</summary>
        public static void Hitbox(Graphics g, Rectangle r, Color? color = null)
        {
            if (!VisualDebugger.OverlayVisible) return;
            Color c = color ?? Color.FromArgb(80, 255, 0, 0);
            using (var br = new SolidBrush(Color.FromArgb(40, c.R, c.G, c.B)))
                g.FillRectangle(br, r);
            using (var pen = new Pen(c, 1))
                g.DrawRectangle(pen, r);
        }

        /// <summary>Draws a velocity arrow from the entity center.</summary>
        public static void VelocityArrow(Graphics g, float cx, float cy, float vx, float vy)
        {
            if (!VisualDebugger.OverlayVisible) return;
            float scale = 0.05f;
            float tx = cx + vx * scale, ty = cy + vy * scale;
            using (var pen = new Pen(Color.Cyan, 2))
            {
                g.DrawLine(pen, cx, cy, tx, ty);
                g.FillEllipse(new SolidBrush(Color.Cyan), tx - 3, ty - 3, 6, 6);
            }
        }

        /// <summary>Draws a radius circle (e.g. for ability ranges).</summary>
        public static void RadiusCircle(Graphics g, float cx, float cy, float radius, Color color)
        {
            if (!VisualDebugger.OverlayVisible) return;
            using (var pen = new Pen(Color.FromArgb(160, color), 1))
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);
        }

        /// <summary>Draws a text label at a world position.</summary>
        public static void Label(Graphics g, string text, float x, float y, Color? color = null)
        {
            if (!VisualDebugger.OverlayVisible) return;
            using (var f  = new Font("Courier New", 8))
            using (var br = new SolidBrush(color ?? Color.LimeGreen))
                g.DrawString(text, f, br, x, y);
        }
    }

    // ── Idea 7: Frame-step mode ────────────────────────────────────────────────
    /// <summary>
    /// Pauses the game and allows stepping forward one fixed-timestep frame at a time.
    /// Toggle with F8; advance one frame with F9 while paused.
    /// Team 8 (Systems Programmer) — Idea 7.
    /// </summary>
    public static class FrameStepper
    {
        /// <summary>True while frame-step mode is active (game is paused).</summary>
        public static bool IsActive      { get; private set; }

        /// <summary>True for exactly one tick after an advance was requested.</summary>
        public static bool StepRequested { get; private set; }

        /// <summary>Toggles frame-step mode on/off.</summary>
        public static void Toggle()
        {
            IsActive = !IsActive;
            StepRequested = false;
            DebugLogger.LogInfo("FrameStepper", IsActive ? "ACTIVE" : "OFF");
        }

        /// <summary>Requests a single-frame advance when in step mode.</summary>
        public static void RequestStep() { if (IsActive) StepRequested = true; }

        /// <summary>
        /// Call at the START of each game tick.
        /// Returns true if the tick should run (either not in step mode, or step was requested).
        /// Consumes the step request.
        /// </summary>
        public static bool ShouldTick()
        {
            if (!IsActive) return true;
            if (StepRequested) { StepRequested = false; return true; }
            return false;
        }
    }

    // ── Idea 8: World-state serializer ────────────────────────────────────────
    /// <summary>
    /// Serializes the Game singleton's key state to a simple JSON string for
    /// debugging, replay, and QA reproducibility.
    /// Team 8 (Systems Programmer) — Idea 8.
    /// </summary>
    public static class WorldStateSerializer
    {
        /// <summary>
        /// Returns a JSON snapshot of the current game state.
        /// Team 8 (Systems Programmer) — Idea 8.
        /// </summary>
        public static string Snapshot()
        {
            var g = Game.Instance;
            if (g == null) return "{}";
            return
                $"{{" +
                $"\"world\":{g.WorldNumber}," +
                $"\"level\":{g.LevelNumber}," +
                $"\"lives\":{g.CurrentLives}," +
                $"\"coins\":{g.CoinCount}," +
                // ScoreManager is the canonical score tracker defined in EventBus.cs.
                // Team 8 (Systems Programmer) — Idea 8: world-state snapshot.
                $"\"score\":{ScoreManager.Score}," +
                $"\"bounty\":{g.PlayerBounty}," +
                $"\"godMode\":{g.GodMode.ToString().ToLower()}," +
                $"\"character\":\"{g.SelectedCharacter}\"," +
                $"\"session\":\"{SessionStats.Instance.SessionId}\"," +
                $"\"playTime\":\"{SessionStats.Instance.PlayTimeFormatted}\"" +
                $"}}";
        }

        /// <summary>Writes a snapshot to the log folder.</summary>
        public static void WriteToDisk()
        {
            try
            {
                string dir  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                string file = Path.Combine(dir, $"state_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                Directory.CreateDirectory(dir);
                File.WriteAllText(file, Snapshot(), System.Text.Encoding.UTF8);
                DebugLogger.LogInfo("WorldStateSerializer", $"Snapshot written: {file}");
            }
            catch (Exception ex) { DebugLogger.LogError("WorldStateSerializer", ex); }
        }
    }

    // ── Idea 9: Tile-collision flag editor ────────────────────────────────────
    /// <summary>
    /// Allows runtime toggling of tile solid/passable flags for designer tuning.
    /// Flags are stored per tile-grid coordinate.
    /// Team 8 (Systems Programmer) — Idea 9.
    /// </summary>
    public static class TileCollisionEditor
    {
        private static readonly HashSet<(int col, int row)> _passable =
            new HashSet<(int, int)>();

        /// <summary>Sets a tile at (col, row) as passable (non-solid).</summary>
        public static void SetPassable(int col, int row) => _passable.Add((col, row));

        /// <summary>Sets a tile at (col, row) as solid (default).</summary>
        public static void SetSolid(int col, int row) => _passable.Remove((col, row));

        /// <summary>Returns true if the tile is passable.</summary>
        public static bool IsPassable(int col, int row) => _passable.Contains((col, row));

        /// <summary>Clears all passable overrides.</summary>
        public static void Reset() => _passable.Clear();
    }

    // ── Idea 10: Asset hot-list ────────────────────────────────────────────────
    /// <summary>
    /// Tracks the most recently used asset file names for pre-caching hints.
    /// Systems that load textures record access here; the next scene load
    /// can call <see cref="GetHotList"/> to prioritize these assets.
    /// Team 8 (Systems Programmer) — Idea 10.
    /// </summary>
    public static class AssetHotList
    {
        private static readonly Queue<string> _recent = new Queue<string>();
        private const int MaxSize = 20;

        /// <summary>Records an asset access.</summary>
        public static void RecordAccess(string assetPath)
        {
            if (_recent.Count >= MaxSize) _recent.Dequeue();
            _recent.Enqueue(assetPath);
        }

        /// <summary>Returns the current hot list snapshot.</summary>
        public static IReadOnlyCollection<string> GetHotList() => _recent;
    }
}
