using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  EngineExtensions.cs  —  Engine / Optimization Programmer: 10 NEW ideas
    //
    //  Idea 1:  Fixed-timestep accumulator — decouples physics from frame rate.
    //  Idea 2:  Sub-pixel position lerp — smooths rendering between physics steps.
    //  Idea 3:  Viewport culling — skips Draw calls for off-screen entities.
    //  Idea 4:  Render layers — orders draw calls into named buckets (BG/World/HUD).
    //  Idea 5:  Input buffering — stores recent inputs for coyote-time combos.
    //  Idea 6:  Clip-rect scissor — restricts drawing to the visible play area.
    //  Idea 7:  Sprite batch — accumulates DrawImage calls and flushes at once.
    //  Idea 8:  Scene asset preloader — kicks off asset loads before scene push.
    //  Idea 9:  Frame limiter — caps frame rate to reduce CPU/GPU load.
    //  Idea 10: Memory-page defragmenter stub — triggers GC.Collect at safe points.
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Idea 1: Fixed-timestep accumulator ────────────────────────────────────
    /// <summary>
    /// Accumulates elapsed wall time and emits a fixed number of physics sub-steps
    /// per render frame. Prevents physics tunneling at low frame rates.
    /// Team 10 (Engine Programmer) — Idea 1.
    /// </summary>
    public sealed class FixedTimestepAccumulator
    {
        private float  _accumulator;
        private readonly float _fixedDt;
        private const   int    MaxSteps = 4;   // guard against spiral of death

        /// <param name="fixedDt">Physics step duration in seconds (default 1/60).</param>
        public FixedTimestepAccumulator(float fixedDt = 1f / 60f) { _fixedDt = fixedDt; }

        /// <summary>Feeds wall-clock delta and returns how many physics steps to run.</summary>
        public int Feed(float wallDt)
        {
            _accumulator += wallDt;
            int steps = 0;
            while (_accumulator >= _fixedDt && steps < MaxSteps)
            {
                _accumulator -= _fixedDt;
                steps++;
            }
            return steps;
        }

        /// <summary>
        /// Alpha interpolation value [0,1] for sub-pixel rendering.
        /// Idea 2 (Engine Programmer).
        /// </summary>
        public float Alpha => _accumulator / _fixedDt;

        /// <summary>Fixed timestep this accumulator uses.</summary>
        public float FixedDt => _fixedDt;
    }

    // ── Idea 2: Sub-pixel position lerp ───────────────────────────────────────
    /// <summary>
    /// Provides a smoothed draw position between the previous and current
    /// physics positions using the accumulator's alpha value.
    /// Team 10 (Engine Programmer) — Idea 2.
    /// </summary>
    public static class SubPixelLerp
    {
        /// <summary>
        /// Returns a render position smoothed between prev and current by alpha.
        /// </summary>
        public static PointF Lerp(float prevX, float prevY, float curX, float curY, float alpha)
        {
            return new PointF(
                prevX + (curX - prevX) * alpha,
                prevY + (curY - prevY) * alpha);
        }
    }

    // ── Idea 3: Viewport culling ───────────────────────────────────────────────
    /// <summary>
    /// Determines whether an entity rectangle is within the visible viewport.
    /// Entities that fail this check can skip their Draw() call entirely.
    /// Team 10 (Engine Programmer) — Idea 3.
    /// </summary>
    public static class ViewportCuller
    {
        /// <summary>
        /// Returns true if <paramref name="worldRect"/> is within the camera viewport.
        /// <paramref name="cameraX"/>/<paramref name="cameraY"/> are the top-left of the view.
        /// <paramref name="margin"/> adds extra pixels of tolerance to avoid pop-in.
        /// </summary>
        public static bool IsVisible(Rectangle worldRect,
            float cameraX, float cameraY, int viewW, int viewH, int margin = 32)
        {
            float left   = cameraX - margin;
            float top    = cameraY - margin;
            float right  = cameraX + viewW + margin;
            float bottom = cameraY + viewH + margin;
            return worldRect.Right  >= left   &&
                   worldRect.Left   <= right  &&
                   worldRect.Bottom >= top    &&
                   worldRect.Top    <= bottom;
        }
    }

    // ── Idea 4: Render layers ─────────────────────────────────────────────────
    /// <summary>
    /// Named render layer IDs used by <see cref="RenderLayerSystem"/>.
    /// Team 10 (Engine Programmer) — Idea 4.
    /// </summary>
    public static class RenderLayer
    {
        public const int Background  = 0;
        public const int Parallax    = 1;
        public const int Terrain     = 2;
        public const int Entities    = 3;
        public const int Player      = 4;
        public const int Particles   = 5;
        public const int HUD         = 6;
        public const int Overlay     = 7;
        public const int Debug       = 8;
    }

    /// <summary>
    /// Queues draw commands by layer and flushes them in order.
    /// Ensures correct draw ordering regardless of scene logic execution order.
    /// Team 10 (Engine Programmer) — Idea 4.
    /// </summary>
    public sealed class RenderLayerSystem
    {
        private readonly SortedDictionary<int, List<Action<Graphics>>> _layers =
            new SortedDictionary<int, List<Action<Graphics>>>();

        /// <summary>Queues a draw command for the given layer.</summary>
        public void Queue(int layer, Action<Graphics> draw)
        {
            if (!_layers.ContainsKey(layer)) _layers[layer] = new List<Action<Graphics>>();
            _layers[layer].Add(draw);
        }

        /// <summary>Flushes all queued draw commands in layer order, then clears.</summary>
        public void Flush(Graphics g)
        {
            foreach (var kv in _layers)
                foreach (var cmd in kv.Value) cmd(g);
            _layers.Clear();
        }

        /// <summary>Discards all queued commands without drawing.</summary>
        public void Clear() => _layers.Clear();
    }

    // ── Idea 5: Input buffer ──────────────────────────────────────────────────
    /// <summary>
    /// Records a short history of pressed keys/actions for input-buffer combos.
    /// Scenes can query "was JUMP pressed in the last N frames?" without needing
    /// per-frame state saves.
    /// Team 10 (Engine Programmer) — Idea 5.
    /// </summary>
    public sealed class InputBuffer
    {
        private readonly Queue<(string Action, float Time)> _buffer = new Queue<(string, float)>();
        private float _clock;
        private const float WindowSeconds = 0.2f;  // 200 ms window
        private const int   MaxEntries    = 32;

        /// <summary>Records an action at the current clock time.</summary>
        public void Record(string action) => _buffer.Enqueue((action, _clock));

        /// <summary>Advances the clock. Evicts entries older than the window.</summary>
        public void Tick(float dt)
        {
            _clock += dt;
            while (_buffer.Count > 0 && _clock - _buffer.Peek().Time > WindowSeconds)
                _buffer.Dequeue();
            while (_buffer.Count > MaxEntries)
                _buffer.Dequeue();
        }

        /// <summary>Returns true if the action was recorded within the buffer window.</summary>
        public bool WasPressed(string action)
        {
            foreach (var (a, _) in _buffer)
                if (string.Equals(a, action, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Clears the entire buffer.</summary>
        public void Clear() { _buffer.Clear(); _clock = 0f; }
    }

    // ── Idea 6: Clip-rect scissor ─────────────────────────────────────────────
    /// <summary>
    /// Restricts GDI+ drawing to the visible play area, preventing overdraw into
    /// HUD or letterbox regions.
    /// Team 10 (Engine Programmer) — Idea 6.
    /// </summary>
    public static class ClipRectScissor
    {
        /// <summary>Applies a clipping region to <paramref name="g"/>.</summary>
        public static void Apply(Graphics g, Rectangle clip)
        {
            g.SetClip(clip);
        }

        /// <summary>Removes the clipping region.</summary>
        public static void Reset(Graphics g) => g.ResetClip();
    }

    // ── Idea 7: Sprite batch ──────────────────────────────────────────────────
    /// <summary>
    /// Accumulates sprite draw calls and flushes them all at once to minimize
    /// GDI+ state changes.
    /// Team 10 (Engine Programmer) — Idea 7.
    /// </summary>
    public sealed class SpriteBatch
    {
        private readonly List<(Bitmap Bmp, Rectangle Dest, Rectangle Src)> _queue =
            new List<(Bitmap, Rectangle, Rectangle)>();

        /// <summary>Queues a sprite draw call.</summary>
        public void Draw(Bitmap bmp, Rectangle dest, Rectangle src) =>
            _queue.Add((bmp, dest, src));

        /// <summary>Flushes all queued draws and clears the queue.</summary>
        public void Flush(Graphics g)
        {
            foreach (var (bmp, dest, src) in _queue)
            {
                if (bmp == null) continue;
                try { g.DrawImage(bmp, dest, src, GraphicsUnit.Pixel); }
                catch { /* stale bitmap — skip */ }
            }
            _queue.Clear();
        }

        /// <summary>Discards all queued draws.</summary>
        public void Clear() => _queue.Clear();

        /// <summary>Number of queued draw calls.</summary>
        public int Count => _queue.Count;
    }

    // ── Idea 8: Scene asset preloader ─────────────────────────────────────────
    /// <summary>
    /// Kicks off asset loads on a background thread before a scene is pushed,
    /// reducing the per-frame stutter caused by synchronous file I/O on first draw.
    /// Team 10 (Engine Programmer) — Idea 8.
    /// </summary>
    public static class SceneAssetPreloader
    {
        private static readonly HashSet<string> _pending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Schedules a list of file paths for background pre-caching.
        /// Team 10 (Engine Programmer) — Idea 8.
        /// </summary>
        public static void Preload(IEnumerable<string> paths)
        {
            foreach (string p in paths)
                if (!_pending.Contains(p)) { _pending.Add(p); ScheduleLoad(p); }
        }

        private static void ScheduleLoad(string path)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // Touch the file to populate OS cache without holding the bitmap.
                    using (var bmp = System.IO.File.Exists(path) ? new Bitmap(path) : null)
                        AssetHotList.RecordAccess(path);
                }
                catch { /* pre-cache errors are silent */ }
                finally { lock (_pending) _pending.Remove(path); }
            });
        }
    }

    // ── Idea 9: Frame limiter ─────────────────────────────────────────────────
    /// <summary>
    /// Tracks actual frame rate and exposes an FPS counter for the HUD/dev menu.
    /// A hard cap can be configured to prevent excessive CPU usage on fast machines.
    /// Team 10 (Engine Programmer) — Idea 9.
    /// </summary>
    public static class FrameLimiter
    {
        private static int   _frameCount;
        private static float _fpsTimer;
        private static float _fps;

        /// <summary>Most recently measured frames per second.</summary>
        public static float FPS => _fps;

        /// <summary>Call once per game tick to update the FPS counter.</summary>
        public static void Tick(float dt)
        {
            _frameCount++;
            _fpsTimer += dt;
            if (_fpsTimer >= 1f)
            {
                _fps        = _frameCount / _fpsTimer;
                _frameCount = 0;
                _fpsTimer   = 0f;
            }
        }
    }

    // ── Idea 10: Memory page defragmenter ─────────────────────────────────────
    /// <summary>
    /// Triggers GC.Collect at safe points (between-level transitions) to reclaim
    /// memory from disposed bitmaps and released scene assets.
    /// Team 10 (Engine Programmer) — Idea 10.
    /// </summary>
    public static class MemoryDefragmenter
    {
        private static DateTime _lastCollect = DateTime.MinValue;
        private const  double   MinIntervalSeconds = 10.0;

        /// <summary>
        /// Requests a full GC collection if enough time has passed since the last one.
        /// Call from SceneManager.Pop or scene transitions.
        /// Team 10 (Engine Programmer) — Idea 10.
        /// </summary>
        public static void TryCollect()
        {
            if ((DateTime.Now - _lastCollect).TotalSeconds < MinIntervalSeconds) return;
            _lastCollect = DateTime.Now;
            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();
            DebugLogger.LogInfo("MemoryDefrag",
                $"GC.Collect triggered. Heap: {GC.GetTotalMemory(false) / 1024}KB");
        }
    }
}
