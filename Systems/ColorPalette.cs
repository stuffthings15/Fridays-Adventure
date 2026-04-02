using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fridays_Adventure.Systems
{
    // ─────────────────────────────────────────────────────────────────────────
    //  ColorPalette.cs  —  Art Direction & Visual Style Guide System
    //
    //  Team 12 (Art Director / Lead 2D Artist) — all 10 ideas implemented:
    //
    //    Idea 1:  Named color palette registry keyed by world number.
    //    Idea 2:  Active world theme palette swapped at scene transitions.
    //    Idea 3:  Palette validator — logs when code uses off-palette colors.
    //    Idea 4:  Character silhouette helper — high-contrast outline render.
    //    Idea 5:  Art asset catalog — list of all registered sprite names.
    //    Idea 6:  Consistent pixel scale enforcer (2× / 3× integer scale).
    //    Idea 7:  Scene color tint — post-process tint overlay per world.
    //    Idea 8:  Lighting temperature presets (warm/cool/neutral).
    //    Idea 9:  Visual reference screenshot capture for QA art review.
    //    Idea 10: Style guide checklist report written to Logs\ folder.
    // ─────────────────────────────────────────────────────────────────────────

    // ── Idea 8: Lighting temperature enum ────────────────────────────────────
    /// <summary>Scene lighting temperature presets. Idea 8 (Art Director).</summary>
    public enum LightingTemp { Neutral, Warm, Cool, Dusk, Underground }

    /// <summary>
    /// Central art direction system: world palettes, tints, scale rules,
    /// silhouette draw helpers, and the art asset catalog.
    /// </summary>
    public static class ColorPalette
    {
        // ── Idea 1: World palette registry ───────────────────────────────────

        /// <summary>
        /// One palette entry: background colour, primary/secondary/accent/text colours.
        /// Idea 1 (Art Director).
        /// </summary>
        public sealed class WorldPalette
        {
            public string Name       { get; set; }
            public Color  Background { get; set; }
            public Color  Primary    { get; set; }
            public Color  Secondary  { get; set; }
            public Color  Accent     { get; set; }
            public Color  TextColor  { get; set; }
            /// <summary>Additive tint colour (post-process overlay). Idea 7.</summary>
            public Color  TintColor  { get; set; }
        }

        /// <summary>All registered world palettes keyed by world number.</summary>
        public static readonly Dictionary<int, WorldPalette> Palettes =
            new Dictionary<int, WorldPalette>
            {
                {
                    1, new WorldPalette   // World 1 — Dinosaur Island
                    {
                        Name       = "Dinosaur Island",
                        Background = Color.FromArgb(8, 20, 40),
                        Primary    = Color.FromArgb(30, 120, 60),
                        Secondary  = Color.FromArgb(120, 70, 30),
                        Accent     = Color.Gold,
                        TextColor  = Color.White,
                        TintColor  = Color.FromArgb(15, 0, 40, 10),
                    }
                },
                {
                    2, new WorldPalette   // World 2 — Storm Belt
                    {
                        Name       = "Storm Belt",
                        Background = Color.FromArgb(12, 12, 32),
                        Primary    = Color.FromArgb(40, 50, 100),
                        Secondary  = Color.FromArgb(80, 80, 160),
                        Accent     = Color.Cyan,
                        TextColor  = Color.LightCyan,
                        TintColor  = Color.FromArgb(20, 0, 0, 30),
                    }
                },
                {
                    3, new WorldPalette   // World 3 — Sky Island
                    {
                        Name       = "Sky Island",
                        Background = Color.FromArgb(20, 60, 120),
                        Primary    = Color.FromArgb(80, 160, 220),
                        Secondary  = Color.FromArgb(180, 200, 240),
                        Accent     = Color.LimeGreen,
                        TextColor  = Color.White,
                        TintColor  = Color.FromArgb(10, 0, 10, 30),
                    }
                },
                {
                    4, new WorldPalette   // World 4 — Warlord's Domain
                    {
                        Name       = "Warlord Domain",
                        Background = Color.FromArgb(20, 8, 8),
                        Primary    = Color.FromArgb(140, 40, 20),
                        Secondary  = Color.FromArgb(60, 30, 10),
                        Accent     = Color.OrangeRed,
                        TextColor  = Color.LightSalmon,
                        TintColor  = Color.FromArgb(20, 30, 0, 0),
                    }
                },
                {
                    5, new WorldPalette   // World 5 — Abyss / Final
                    {
                        Name       = "The Abyss",
                        Background = Color.FromArgb(4, 4, 12),
                        Primary    = Color.FromArgb(30, 20, 60),
                        Secondary  = Color.FromArgb(60, 40, 80),
                        Accent     = Color.MediumOrchid,
                        TextColor  = Color.Lavender,
                        TintColor  = Color.FromArgb(15, 10, 0, 20),
                    }
                },
            };

        // ── Idea 2: Active palette ─────────────────────────────────────────────

        private static WorldPalette _active;

        /// <summary>
        /// The currently active world palette.
        /// Idea 2 (Art Director).
        /// </summary>
        public static WorldPalette Active =>
            _active ?? (Palettes.ContainsKey(1) ? Palettes[1] : null);

        /// <summary>
        /// Switches to the palette for the given world number.
        /// Call from scenes on enter.
        /// Idea 2 (Art Director).
        /// </summary>
        public static void SetWorld(int world)
        {
            if (Palettes.ContainsKey(world))
            {
                _active = Palettes[world];
                DebugLogger.LogInfo("ColorPalette", $"Palette → World {world}: {_active.Name}");
            }
        }

        // ── Idea 3: Palette validator ─────────────────────────────────────────

        /// <summary>
        /// Returns true if <paramref name="color"/> closely matches any named
        /// palette color (within a Euclidean RGB distance of 30).
        /// Log a warning in dev builds when this returns false.
        /// Idea 3 (Art Director).
        /// </summary>
        public static bool IsOnPalette(Color color)
        {
            if (_active == null) return true;
            foreach (var c in new[] { _active.Background, _active.Primary,
                                      _active.Secondary, _active.Accent, _active.TextColor })
            {
                double dist = Math.Sqrt(
                    Math.Pow(color.R - c.R, 2) +
                    Math.Pow(color.G - c.G, 2) +
                    Math.Pow(color.B - c.B, 2));
                if (dist < 30) return true;
            }
            return false;
        }

        // ── Idea 4: Character silhouette helper ───────────────────────────────

        /// <summary>
        /// Draws a solid black silhouette one pixel behind and below the entity's
        /// bounding rectangle for high-contrast readability against busy backgrounds.
        /// Idea 4 (Art Director).
        /// </summary>
        public static void DrawSilhouette(Graphics g, float x, float y, int w, int h,
                                          int offsetX = 2, int offsetY = 2)
        {
            using (var br = new SolidBrush(Color.FromArgb(120, Color.Black)))
                g.FillRectangle(br, x + offsetX, y + offsetY, w, h);
        }

        // ── Idea 5: Art asset catalog ─────────────────────────────────────────

        /// <summary>
        /// Registry of every sprite file name registered by scenes and entities.
        /// Allows the Art Director to audit all assets used in the game.
        /// Idea 5 (Art Director).
        /// </summary>
        private static readonly HashSet<string> _catalog =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Registers a sprite file in the catalog.</summary>
        public static void RegisterAsset(string fileName) => _catalog.Add(fileName);

        /// <summary>Returns all registered asset file names.</summary>
        public static IEnumerable<string> GetCatalog() => _catalog;

        /// <summary>Returns the total number of registered assets.</summary>
        public static int AssetCount => _catalog.Count;

        // ── Idea 6: Pixel scale enforcer ─────────────────────────────────────

        /// <summary>
        /// Target integer pixel scale for all sprite rendering (2× by default).
        /// Set from GameConfig or OptionsScene.
        /// Idea 6 (Art Director).
        /// </summary>
        public static int PixelScale { get; set; } = 2;

        /// <summary>
        /// Snaps a world coordinate to the nearest pixel grid boundary.
        /// Use this when drawing pixel-art sprites to avoid sub-pixel blurring.
        /// Idea 6 (Art Director).
        /// </summary>
        public static float SnapToPixel(float v) =>
            (float)(Math.Round(v / PixelScale) * PixelScale);

        // ── Idea 7: Scene color tint overlay ─────────────────────────────────

        /// <summary>
        /// Draws the active palette's post-process tint overlay.
        /// Call at the very end of each scene's Draw() method.
        /// Idea 7 (Art Director).
        /// </summary>
        public static void DrawTintOverlay(Graphics g, int W, int H)
        {
            var palette = Active;
            if (palette == null || palette.TintColor.A == 0) return;
            using (var br = new SolidBrush(palette.TintColor))
                g.FillRectangle(br, 0, 0, W, H);
        }

        // ── Idea 8: Lighting temperature tint ────────────────────────────────

        /// <summary>
        /// Returns the additive tint color for a given lighting temperature.
        /// Apply with <see cref="DrawTintOverlay"/> or a manual FillRectangle.
        /// Idea 8 (Art Director).
        /// </summary>
        public static Color GetLightingTint(LightingTemp temp)
        {
            switch (temp)
            {
                case LightingTemp.Warm:        return Color.FromArgb(25, 60, 20, 0);
                case LightingTemp.Cool:        return Color.FromArgb(25, 0,  10, 40);
                case LightingTemp.Dusk:        return Color.FromArgb(30, 40, 10, 10);
                case LightingTemp.Underground: return Color.FromArgb(40, 10, 0,  20);
                default:                       return Color.FromArgb(0,  0,  0,   0);
            }
        }

        // ── Idea 9: Visual reference screenshot ──────────────────────────────

        /// <summary>
        /// Saves the current canvas frame as a reference screenshot for art review.
        /// File written to Logs\art-ref-TIMESTAMP.png.
        /// Idea 9 (Art Director).
        /// </summary>
        public static void CaptureArtReference(Bitmap frame)
        {
            if (frame == null) return;
            try
            {
                string dir  = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Logs", "ArtRef");
                System.IO.Directory.CreateDirectory(dir);
                string path = System.IO.Path.Combine(dir,
                    $"art-ref-{DateTime.Now:yyyyMMdd_HHmmss}.png");
                frame.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                DebugLogger.LogInfo("ColorPalette", $"Art reference saved: {path}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogWarning("ColorPalette.ArtRef", ex.Message);
            }
        }

        // ── Idea 10: Style guide checklist report ─────────────────────────────

        /// <summary>
        /// Writes a style guide compliance checklist to Logs\style-guide-report.txt.
        /// Reports pixel scale setting, active palette, and asset count.
        /// Idea 10 (Art Director).
        /// </summary>
        public static void WriteStyleGuideReport()
        {
            try
            {
                string path = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Logs", "style-guide-report.txt");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== Friday's Adventure — Art Style Guide Report ===");
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Build: {BuildInfo.Summary}");
                sb.AppendLine();
                sb.AppendLine($"Active Palette : {Active?.Name ?? "(none)"}");
                sb.AppendLine($"Pixel Scale    : {PixelScale}×");
                sb.AppendLine($"Assets Logged  : {AssetCount}");
                sb.AppendLine();
                sb.AppendLine("--- Registered Assets ---");
                foreach (string a in GetCatalog()) sb.AppendLine("  " + a);
                sb.AppendLine();
                sb.AppendLine("--- World Palettes ---");
                foreach (var kv in Palettes)
                    sb.AppendLine($"  World {kv.Key}: {kv.Value.Name}  BG={kv.Value.Background.Name}");
                System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
                DebugLogger.LogInfo("ColorPalette", $"Style guide report saved: {path}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogWarning("ColorPalette.StyleReport", ex.Message);
            }
        }
    }
}
