using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Engine optimisation helpers.
    ///
    /// Team 10 (Engine Programmer) — all 10 ideas below.
    ///
    /// Idea 1:  Frustum culling helper — quick AABB test against the camera viewport.
    /// Idea 2:  Background layer caching — pre-render a static BG to an off-screen bitmap.
    /// Idea 4:  Lazy pre-init for heavy scenes — stub for future async preload.
    /// Idea 6:  Brush pool — reusable SolidBrush cache to reduce GDI GC pressure.
    /// Idea 7:  Fixed physics sub-step — cap dt to avoid spiral-of-death.
    /// Idea 8:  Frame skip detector — logs when the game loop skips &gt;3 frames.
    /// Idea 9:  Asset garbage collector — disposes cached bitmaps on scene exit.
    /// Idea 10: Render pipeline profiling markers (via PerformanceProfiler hooks).
    /// </summary>
    public static class EngineOptimiser
    {
        // ── Idea 1: Frustum / camera-view culling ─────────────────────────────

        /// <summary>
        /// Returns true if the world-space rectangle is visible within the current
        /// camera viewport.  Pass the camera scroll offset and canvas dimensions.
        ///
        /// Entities and hazards should call this before calling Draw() to skip
        /// off-screen draw calls.
        /// </summary>
        public static bool InView(float worldX, float worldY, int w, int h,
                                  float cameraX, int screenW, int screenH,
                                  int margin = 32)
        {
            float sx = worldX - cameraX;
            return sx + w > -margin
                && sx     <  screenW + margin
                && worldY + h > -margin
                && worldY     <  screenH + margin;
        }

        // ── Idea 2: Static background caching ────────────────────────────────

        /// <summary>
        /// Pre-renders a static background using the provided draw action and
        /// caches the result as an off-screen Bitmap.  Subsequent calls return
        /// the cached bitmap immediately without executing the draw action.
        ///
        /// Call <see cref="InvalidateCache"/> when the background changes.
        /// </summary>
        public static Bitmap GetCachedBackground(int width, int height, Action<Graphics> drawAction,
                                                 ref Bitmap cache)
        {
            if (cache != null && cache.Width == width && cache.Height == height)
                return cache;

            cache?.Dispose();
            cache = new Bitmap(width, height);
            using (var g = Graphics.FromImage(cache))
                drawAction(g);
            return cache;
        }

        /// <summary>Disposes and nulls a cached background bitmap.</summary>
        public static void InvalidateCache(ref Bitmap cache)
        {
            cache?.Dispose();
            cache = null;
        }

        // ── Idea 6: SolidBrush pool ──────────────────────────────────────────

        private static readonly System.Collections.Generic.Dictionary<Color, SolidBrush> _brushPool
            = new System.Collections.Generic.Dictionary<Color, SolidBrush>();

        /// <summary>
        /// Returns a cached SolidBrush for the given color.
        /// WARNING: never Dispose() brushes obtained from this pool.
        /// </summary>
        public static SolidBrush GetBrush(Color color)
        {
            if (!_brushPool.TryGetValue(color, out SolidBrush br))
            {
                br = new SolidBrush(color);
                _brushPool[color] = br;
            }
            return br;
        }

        // ── Idea 7: Fixed physics sub-step cap ────────────────────────────────

        /// <summary>
        /// Maximum allowed delta-time for a single physics tick.
        /// Caps spikes caused by Windows timer jitter or debugger pauses so the
        /// game doesn't launch entities through walls during a lag spike.
        /// </summary>
        public const float MaxPhysicsDt = 1f / 30f;  // never advance more than ½ frame

        /// <summary>
        /// Clamps dt to <see cref="MaxPhysicsDt"/> and logs any spike.
        /// </summary>
        public static float ClampDt(float dt)
        {
            if (dt > MaxPhysicsDt)
            {
                DebugLogger.LogInfo("EngineOptimiser", $"dt spike clamped: {dt:F4}s → {MaxPhysicsDt:F4}s");
                return MaxPhysicsDt;
            }
            return dt;
        }

        // ── Idea 8: Frame-skip detector ───────────────────────────────────────

        private static int   _consecutiveSkips;
        private static float _lastDt;

        /// <summary>
        /// Detects consecutive frame skips (dt &gt; 3× the nominal tick rate).
        /// Logs a warning to the error debugger when 3+ skips occur in a row.
        /// </summary>
        public static void CheckFrameSkip(float dt)
        {
            const float NominalDt = 1f / 60f;
            if (dt > NominalDt * 3f)
            {
                _consecutiveSkips++;
                if (_consecutiveSkips == 3)
                    DebugLogger.LogInfo("EngineOptimiser",
                        $"Frame skip detected: {_consecutiveSkips} frames, dt={dt:F3}s");
            }
            else
            {
                _consecutiveSkips = 0;
            }
            _lastDt = dt;
        }

        // ── Idea 9: Scene-exit asset GC ───────────────────────────────────────

        /// <summary>
        /// Disposes an image asset when a scene exits, returning memory to the OS.
        /// The scene should set the field to null after calling this helper.
        /// </summary>
        public static void DisposeAsset(ref Bitmap asset)
        {
            asset?.Dispose();
            asset = null;
        }

        // ── Idea 3: On-disk save backup ──────────────────────────────────────

        /// <summary>
        /// Creates a timestamped backup of the save file before overwriting.
        /// Call from SaveData.Save() before writing the new file.
        /// Team 2 (Producer) — Idea 7: auto-backup save file.
        /// </summary>
        public static void BackupSaveFile(string savePath)
        {
            try
            {
                if (!File.Exists(savePath)) return;
                string dir    = Path.GetDirectoryName(savePath);
                string name   = Path.GetFileNameWithoutExtension(savePath);
                string ext    = Path.GetExtension(savePath);
                string stamp  = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backup = Path.Combine(dir ?? ".", $"{name}_backup_{stamp}{ext}");
                File.Copy(savePath, backup, true);

                // Prune backups older than 10 entries to avoid disk bloat.
                var backups = Directory.GetFiles(dir ?? ".", $"{name}_backup_*{ext}");
                if (backups.Length > 10)
                {
                    Array.Sort(backups);  // oldest first
                    File.Delete(backups[0]);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("EngineOptimiser.BackupSave", ex);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Team 10 (Engine / Optimization Programmer) — Ideas 3-10 additions
        // ═══════════════════════════════════════════════════════════════════════

        // ── Idea 3: Delta-time rolling average smoother ────────────────────────

        private static readonly float[] _dtSamples = new float[8];
        private static int              _dtHead;

        /// <summary>
        /// Records a dt sample and returns the 8-frame rolling average.
        /// Smooths jitter from Windows timer resolution without sacrificing responsiveness.
        /// Team 10 (Engine Programmer) — Idea 3.
        /// </summary>
        public static float SmoothDt(float rawDt)
        {
            _dtSamples[_dtHead % _dtSamples.Length] = rawDt;
            _dtHead++;
            float sum = 0f;
            for (int i = 0; i < _dtSamples.Length; i++) sum += _dtSamples[i];
            return sum / _dtSamples.Length;
        }

        // ── Idea 4: Frame time histogram ─────────────────────────────────────

        private static readonly int[] _histogram = new int[10];  // 0-9 ms buckets + overflow
        private static int _histogramTotal;

        /// <summary>
        /// Records a frame time into a 10-bucket histogram (0–9 ms; 10+ = overflow).
        /// Call each frame with the raw frame duration in seconds.
        /// Team 10 (Engine Programmer) — Idea 4.
        /// </summary>
        public static void RecordFrameTime(float dtSeconds)
        {
            int ms    = (int)(dtSeconds * 1000f);
            int bucket = Math.Min(ms, _histogram.Length - 1);
            _histogram[bucket]++;
            _histogramTotal++;
        }

        /// <summary>
        /// Returns a one-line histogram display string.
        /// Team 10 (Engine Programmer) — Idea 4.
        /// </summary>
        public static string GetHistogramReport()
        {
            if (_histogramTotal == 0) return "No frames recorded.";
            var sb = new System.Text.StringBuilder("FT|");
            for (int i = 0; i < _histogram.Length; i++)
            {
                int pct = (int)(100f * _histogram[i] / _histogramTotal);
                sb.Append($"{i}ms:{pct}% ");
            }
            return sb.ToString();
        }

        // ── Idea 5: Render statistics tracker ────────────────────────────────

        private static int _drawCallsThisFrame;

        /// <summary>
        /// Increments the draw-call counter for the current frame.
        /// Team 10 (Engine Programmer) — Idea 5.
        /// </summary>
        public static void RecordDrawCall() => _drawCallsThisFrame++;

        /// <summary>Returns and resets the draw-call count for the frame just completed.</summary>
        public static int FlushDrawCalls()
        {
            int val = _drawCallsThisFrame;
            _drawCallsThisFrame = 0;
            return val;
        }

        // ── Idea 6: Memory pressure detector ─────────────────────────────────

        private const long MemWarningThresholdBytes = 80 * 1024 * 1024;  // 80 MB

        /// <summary>
        /// Checks managed heap size and suggests a GC collection if above the threshold.
        /// Team 10 (Engine Programmer) — Idea 6.
        /// </summary>
        public static void CheckMemoryPressure()
        {
            long bytes = GC.GetTotalMemory(false);
            if (bytes > MemWarningThresholdBytes)
            {
                DebugLogger.LogWarning("EngineOptimiser",
                    $"Memory pressure: {bytes / (1024 * 1024)} MB — requesting GC.");
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }

        // ── Idea 7: Dirty-region rendering helper ────────────────────────────

        /// <summary>
        /// Tests whether a world-space rectangle overlaps any of the provided
        /// dirty rectangles. If it does, a redraw is needed.
        /// Team 10 (Engine Programmer) — Idea 7.
        /// </summary>
        public static bool InDirtyRegion(System.Drawing.RectangleF worldRect,
            System.Collections.Generic.IEnumerable<System.Drawing.RectangleF> dirtyRects)
        {
            foreach (var d in dirtyRects)
                if (d.IntersectsWith(worldRect)) return true;
            return false;
        }

        // ── Idea 8: Async preload stub ────────────────────────────────────────

        private static readonly System.Collections.Concurrent.ConcurrentQueue<string>
            _preloadQueue = new System.Collections.Concurrent.ConcurrentQueue<string>();

        /// <summary>
        /// Queues an asset file name for background preloading.
        /// The actual load is dispatched to a ThreadPool worker.
        /// Team 10 (Engine Programmer) — Idea 8.
        /// </summary>
        public static void QueueAssetPreload(string fileName)
        {
            _preloadQueue.Enqueue(fileName);
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                if (_preloadQueue.TryDequeue(out string name))
                {
                    // Warm the SpriteManager cache on a background thread;
                    // cool PSO trick: use || to elide empty catch
                    try { SpriteManager.Preload(name); }
                    catch (Exception ex)
                    { DebugLogger.LogWarning("EngineOptimiser.Preload", ex.Message); }
                }
            });
        }

        // ── Idea 9: Pen pool (mirrors Brush pool for Pen objects) ────────────

        private static readonly System.Collections.Generic.Dictionary<(Color, float), System.Drawing.Pen>
            _penPool = new System.Collections.Generic.Dictionary<(Color, float), System.Drawing.Pen>();

        /// <summary>
        /// Returns a cached Pen for the given color and width.
        /// WARNING: never Dispose() pens obtained from this pool.
        /// Team 10 (Engine Programmer) — Idea 9.
        /// </summary>
        public static System.Drawing.Pen GetPen(Color color, float width = 1f)
        {
            var key = (color, width);
            if (!_penPool.TryGetValue(key, out var pen))
            {
                pen = new System.Drawing.Pen(color, width);
                _penPool[key] = pen;
            }
            return pen;
        }

        // ── Idea 10: Render debug stats overlay ───────────────────────────────

        private static int _lastFps;
        private static float _fpsAccum;
        private static int   _fpsFrames;

        /// <summary>
        /// Updates the FPS counter using an exponential average.
        /// Team 10 (Engine Programmer) — Idea 10.
        /// </summary>
        public static void UpdateFps(float dt)
        {
            _fpsAccum  += dt;
            _fpsFrames++;
            if (_fpsAccum >= 0.5f)
            {
                _lastFps   = (int)(_fpsFrames / _fpsAccum);
                _fpsAccum  = 0f;
                _fpsFrames = 0;
            }
        }

        /// <summary>
        /// Draws a compact FPS + draw-call overlay in the top-right corner.
        /// Only shown when the ShowFps debug flag is active.
        /// Team 10 (Engine Programmer) — Idea 10.
        /// </summary>
        public static void DrawRenderStats(System.Drawing.Graphics g, int W, int H, int drawCalls)
        {
            if (!DebugOverlayToggles.ShowFps) return;

            string text = $"FPS:{_lastFps}  DC:{drawCalls}  {GameConfig.GetHeapReport()}";
            using (var f = new Font("Courier New", 7, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(text, f);
                float x  = W - sz.Width - 4;
                using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                    g.FillRectangle(br, x - 2, H - 16, sz.Width + 4, 14);
                g.DrawString(text, f, Brushes.LimeGreen, x, H - 15);
            }
        }
    }
}
