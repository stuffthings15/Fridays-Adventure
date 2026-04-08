// ────────────────────────────────────────────
// PHASE 2 - Team 10: Engine Programmer
// Feature: Blank Screen Detector
// Purpose: Detects levels that render as completely blank/black
//          by instantiating each scene, rendering one frame into
//          a bitmap, and analyzing pixel variance.  Blank screens
//          indicate missing backgrounds, broken Draw overrides,
//          or un-wired asset dependencies.
// ────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// Automated test that catches levels rendering as blank/black screens.
    /// Each level scene is instantiated, given a single Update tick, then
    /// rendered into an off-screen bitmap.  The bitmap is sampled for pixel
    /// variance — if the vast majority of pixels are a single dark color,
    /// the test flags the level as "blank."
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Blank Screen Detector</remarks>
    public static class BlankScreenDetector
    {
        /// <summary>Result for a single level's render check.</summary>
        public sealed class RenderResult
        {
            public string LevelId   { get; set; }
            public string LevelName { get; set; }
            public string SceneType { get; set; }
            public bool   IsBlank   { get; set; }
            public float  DarkPct   { get; set; }
            public int    UniqueColors { get; set; }
            public string Notes     { get; set; }
            public bool   HadError  { get; set; }
        }

        // ── All 17 campaign levels ────────────────────────────────────────
        private static readonly (string id, string name)[] AllLevels =
        {
            ("dino",            "Dinosaur Island"),
            ("storm1",          "Storm Belt"),
            ("sky",             "Sky Island"),
            ("blockade",        "Marine Blockade"),
            ("wano",            "Blade Nation"),
            ("warlord1",        "Warlord: Sudo"),
            ("harbor",          "Harbor Town"),
            ("coral",           "Coral Reef"),
            ("tundra",          "Tundra Peak"),
            ("storm2",          "Tempest Strait"),
            ("warlord2",        "Warlord: Vanta"),
            ("dive_gate",       "Dive Gate"),
            ("sunken_gate",     "Sunken Gate"),
            ("kelp",            "Kelp Maze"),
            ("boiling_vent",    "Vent Ruins"),
            ("abyss",           "Abyss"),
            ("centipede_final", "Centipede"),
        };

        /// <summary>
        /// Runs the blank-screen test on all 17 levels.
        /// Returns a list of results — one per level.
        /// </summary>
        public static List<RenderResult> RunAll()
        {
            var results = new List<RenderResult>();
            int W = Game.Instance?.CanvasWidth  ?? 960;
            int H = Game.Instance?.CanvasHeight ?? 600;

            foreach (var (id, name) in AllLevels)
            {
                var r = TestLevel(id, name, W, H);
                results.Add(r);

                string status = r.IsBlank ? "❌ BLANK" : "✅ OK";
                string extra  = r.HadError ? " [ERROR]" : "";
                System.Diagnostics.Debug.WriteLine(
                    $"[BlankScreenDetector] {status} {r.LevelName} " +
                    $"dark={r.DarkPct:P0} colors={r.UniqueColors}{extra} " +
                    $"({r.SceneType})");
            }

            // Write summary
            int blank = 0, ok = 0, err = 0;
            foreach (var r in results)
            {
                if (r.HadError) err++;
                else if (r.IsBlank) blank++;
                else ok++;
            }
            System.Diagnostics.Debug.WriteLine(
                $"[BlankScreenDetector] ══ SUMMARY: {ok} OK, {blank} BLANK, {err} ERROR ══");

            return results;
        }

        /// <summary>
        /// Tests a single level for blank-screen rendering.
        /// </summary>
        private static RenderResult TestLevel(string levelId, string levelName, int W, int H)
        {
            var result = new RenderResult
            {
                LevelId   = levelId,
                LevelName = levelName,
            };

            Scene scene = null;
            try
            {
                // Set the node ID so scenes that read it (UnderwaterScene) get the right layout
                if (Game.Instance?.Save != null)
                    Game.Instance.Save.CurrentNodeId = levelId;

                scene = LevelSceneFactory.Create(levelId, levelName);
                result.SceneType = scene.GetType().Name;

                // OnEnter initializes player, platforms, background, etc.
                scene.OnEnter();

                // One update tick so enemies/animations initialize
                scene.Update(0.016f);

                // Render into an off-screen bitmap
                using (var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Black);
                        scene.Draw(g);
                    }

                    AnalyzeBitmap(bmp, result);
                }
            }
            catch (Exception ex)
            {
                result.HadError = true;
                result.Notes    = ex.GetType().Name + ": " + ex.Message +
                                  " | Stack: " + ex.StackTrace?.Replace("\r\n", " → ");
                result.IsBlank  = true; // Treat errors as blank — scene is broken
            }
            finally
            {
                try { scene?.OnExit(); }
                catch { /* cleanup best-effort */ }
            }

            return result;
        }

        /// <summary>
        /// Samples the rendered bitmap and determines if the screen is blank.
        /// A screen is "blank" if:
        ///   - More than 95% of sampled pixels are very dark (R+G+B &lt; 40), OR
        ///   - Fewer than 8 unique colors appear in the sample (solid fill).
        /// </summary>
        private static void AnalyzeBitmap(Bitmap bmp, RenderResult result)
        {
            int W = bmp.Width;
            int H = bmp.Height;
            int sampleCount = 0;
            int darkCount   = 0;
            var colors = new HashSet<int>();

            // Sample a grid of pixels (every 8th pixel) for speed
            int step = 8;
            for (int y = step; y < H - step; y += step)
            {
                for (int x = step; x < W - step; x += step)
                {
                    Color c = bmp.GetPixel(x, y);
                    sampleCount++;

                    // "Dark" threshold: R+G+B < 40 means nearly black
                    if (c.R + c.G + c.B < 40)
                        darkCount++;

                    // Quantize to reduce noise (group similar shades)
                    int quantized = ((c.R >> 4) << 8) | ((c.G >> 4) << 4) | (c.B >> 4);
                    colors.Add(quantized);
                }
            }

            result.DarkPct      = sampleCount > 0 ? (float)darkCount / sampleCount : 1f;
            result.UniqueColors = colors.Count;

            // Blank if >95% dark pixels OR fewer than 8 unique quantized colors
            result.IsBlank = result.DarkPct > 0.95f || result.UniqueColors < 8;

            if (result.IsBlank && string.IsNullOrEmpty(result.Notes))
            {
                if (result.DarkPct > 0.95f)
                    result.Notes = $"Screen is {result.DarkPct:P0} dark pixels — appears blank/black";
                else
                    result.Notes = $"Only {result.UniqueColors} unique colors — appears as solid fill";
            }
        }

        /// <summary>
        /// Writes the test results to a log file in the Logs directory.
        /// </summary>
        public static void WriteReport(List<RenderResult> results, string logDir)
        {
            try
            {
                if (!System.IO.Directory.Exists(logDir))
                    System.IO.Directory.CreateDirectory(logDir);

                string path = System.IO.Path.Combine(logDir,
                    $"blank_screen_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                var lines = new List<string>
                {
                    "════════════════════════════════════════════════════════════",
                    "BLANK SCREEN DETECTION REPORT",
                    $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    "════════════════════════════════════════════════════════════",
                    ""
                };

                int blank = 0, ok = 0, err = 0;
                foreach (var r in results)
                {
                    string status;
                    if (r.HadError) { status = "ERROR"; err++; }
                    else if (r.IsBlank) { status = "BLANK"; blank++; }
                    else { status = "OK"; ok++; }

                    lines.Add($"[{status,-5}] {r.LevelName,-25} Scene={r.SceneType,-22} " +
                              $"Dark={r.DarkPct:P0} Colors={r.UniqueColors,3}");
                    if (!string.IsNullOrEmpty(r.Notes))
                        lines.Add($"        → {r.Notes}");
                }

                lines.Add("");
                lines.Add($"SUMMARY: {ok} OK, {blank} BLANK, {err} ERROR  (total {results.Count})");

                System.IO.File.WriteAllLines(path, lines);
                System.Diagnostics.Debug.WriteLine(
                    $"[BlankScreenDetector] Report written to {path}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[BlankScreenDetector] Failed to write report: {ex.Message}");
            }
        }
    }
}
