// ────────────────────────────────────────────
// PHASE 2 - Team 8: Systems Programmer
// Feature: Asset Auto-Resolver
// Purpose: Maps detected missing assets to available CC0 vendor files
//          using keyword matching, category inference, and style
//          compatibility. Also generates placeholder assets when no
//          suitable vendor match is found.
// ────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Fridays_Adventure.Data
{
    /// <summary>
    /// Resolves missing asset gaps by searching downloaded CC0 vendor packs
    /// for matching files, or by generating procedural placeholders.
    /// <remarks>PHASE 2 - Team 8: Self-Healing Asset Pipeline — Auto-Resolver</remarks>
    /// </summary>
    public static class AssetAutoResolver
    {
        // ── Vendor file index: built once, searched many times ────────
        private static readonly Dictionary<string, List<string>> _vendorIndex =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        private static bool _indexed;

        // ── Known Kenney tile mappings for common game entities ───────
        // These map game-engine entity names to specific Kenney Pixel
        // Platformer tiles that visually represent them.
        private static readonly Dictionary<string, string> _knownMappings =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Enemies — Kenney Pixel Platformer character tiles
            { "enemy_goomba",       "tile_0012.png" },
            { "enemy_koopa",        "tile_0015.png" },
            { "enemy_hammer_bro",   "tile_0021.png" },
            { "enemy_spike",        "tile_0024.png" },
            { "enemy_slime",        "tile_0011.png" },
            { "enemy_bat",          "tile_0018.png" },
            // Items
            { "item_coin",          "tile_0003.png" },
            { "item_gem",           "tile_0005.png" },
            { "item_key",           "tile_0027.png" },
            { "item_heart",         "tile_0044.png" },
            // UI elements — Kenney Pixel UI Pack
            { "ui_button",          "button_square_depth_flat.png" },
            { "ui_panel",           "panel_square.png" },
        };

        // ── Category-to-folder keyword mapping for vendor search ─────
        private static readonly Dictionary<AssetCategory, string[]> _categoryKeywords =
            new Dictionary<AssetCategory, string[]>
        {
            { AssetCategory.Enemy,           new[] { "character", "enemy", "monster", "creature" } },
            { AssetCategory.PlayerCharacter, new[] { "character", "player", "hero" } },
            { AssetCategory.Boss,            new[] { "character", "boss", "large" } },
            { AssetCategory.Portrait,        new[] { "character", "face", "portrait" } },
            { AssetCategory.Background,      new[] { "background", "sky", "landscape" } },
            { AssetCategory.UI,              new[] { "ui", "button", "panel", "hud" } },
            { AssetCategory.Tile,            new[] { "tile", "terrain", "block", "ground" } },
            { AssetCategory.Audio,           new[] { "sound", "sfx", "music" } },
        };

        // ── Public API ───────────────────────────────────────────────

        /// <summary>
        /// Attempts to resolve a missing asset entry by finding a matching
        /// vendor file or generating a placeholder. Returns true if resolved.
        /// </summary>
        /// <param name="entry">The missing asset to resolve.</param>
        /// <param name="spritesDir">Target directory for resolved sprite files.</param>
        /// <returns>True if the asset was successfully resolved.</returns>
        public static bool TryResolve(MissingAssetEntry entry, string spritesDir)
        {
            if (entry == null || string.IsNullOrEmpty(entry.FileName)) return false;

            // Ensure vendor index is built
            EnsureIndexed();

            string baseName = Path.GetFileNameWithoutExtension(entry.FileName);
            string ext = Path.GetExtension(entry.FileName);
            string destPath = Path.Combine(spritesDir, entry.FileName);

            // If the file already exists, nothing to do
            if (File.Exists(destPath))
            {
                entry.Resolved = true;
                entry.ResolvedPath = destPath;
                return true;
            }

            // ── Strategy 1: Known mapping (exact match to Kenney tile) ──
            if (_knownMappings.TryGetValue(baseName, out string knownTile))
            {
                string vendorPath = FindVendorFile(knownTile);
                if (vendorPath != null)
                {
                    CopyAndScale(vendorPath, destPath, entry.Category);
                    entry.Resolved = true;
                    entry.ResolvedPath = destPath;
                    return true;
                }
            }

            // ── Strategy 2: Keyword search in vendor index ──────────────
            string bestMatch = FindBestVendorMatch(entry);
            if (bestMatch != null)
            {
                CopyAndScale(bestMatch, destPath, entry.Category);
                entry.Resolved = true;
                entry.ResolvedPath = destPath;
                return true;
            }

            // ── Strategy 3: Generate procedural placeholder ─────────────
            if (IsSpriteCategory(entry.Category))
            {
                GeneratePlaceholder(destPath, entry);
                entry.Resolved = true;
                entry.ResolvedPath = destPath;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves all unresolved missing assets in the gap detector.
        /// Returns the number of newly resolved assets.
        /// </summary>
        public static int ResolveAllGaps()
        {
            string spritesDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites");
            if (!Directory.Exists(spritesDir))
                Directory.CreateDirectory(spritesDir);

            // Also resolve to the project source sprites directory
            string projectRoot = AssetDownloader.FindProjectRoot();
            string projectSpritesDir = projectRoot != null
                ? Path.Combine(projectRoot, "Assets", "Sprites")
                : null;

            var misses = AssetGapDetector.GetUnresolvedMisses();
            int resolved = 0;

            foreach (var miss in misses)
            {
                if (TryResolve(miss, spritesDir))
                {
                    resolved++;
                    AssetGapDetector.MarkResolved(miss.FileName, miss.ResolvedPath);

                    // Also copy to project source dir for permanent integration
                    if (projectSpritesDir != null && !string.IsNullOrEmpty(miss.ResolvedPath))
                    {
                        try
                        {
                            string projDest = Path.Combine(projectSpritesDir, miss.FileName);
                            if (!File.Exists(projDest) && File.Exists(miss.ResolvedPath))
                            {
                                Directory.CreateDirectory(projectSpritesDir);
                                File.Copy(miss.ResolvedPath, projDest, false);
                            }
                        }
                        catch { /* non-critical — project dir may not be writable */ }
                    }
                }
            }

            return resolved;
        }

        // ── Vendor index ─────────────────────────────────────────────

        /// <summary>
        /// Builds a filename-to-path index of all PNG/WAV files in the
        /// third-party vendor directory tree. Runs once on first use.
        /// </summary>
        private static void EnsureIndexed()
        {
            if (_indexed) return;

            string vendorRoot = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets", "third_party", "vendor");

            if (Directory.Exists(vendorRoot))
            {
                try
                {
                    foreach (string file in Directory.GetFiles(vendorRoot, "*.*",
                        SearchOption.AllDirectories))
                    {
                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext != ".png" && ext != ".wav" && ext != ".jpg") continue;

                        string name = Path.GetFileName(file).ToLowerInvariant();
                        if (!_vendorIndex.ContainsKey(name))
                            _vendorIndex[name] = new List<string>();
                        _vendorIndex[name].Add(file);
                    }
                }
                catch { /* directory scan failure — non-critical */ }
            }

            _indexed = true;
        }

        /// <summary>
        /// Finds a vendor file by exact filename (case-insensitive).
        /// </summary>
        private static string FindVendorFile(string fileName)
        {
            EnsureIndexed();
            string lower = fileName.ToLowerInvariant();
            if (_vendorIndex.TryGetValue(lower, out var paths) && paths.Count > 0)
                return paths[0];
            return null;
        }

        /// <summary>
        /// Searches the vendor index for the best matching file based on
        /// the entry's keywords and category.
        /// </summary>
        private static string FindBestVendorMatch(MissingAssetEntry entry)
        {
            if (entry.Keywords == null || entry.Keywords.Count == 0) return null;

            string bestPath = null;
            int bestScore = 0;

            foreach (var kv in _vendorIndex)
            {
                string vendorName = Path.GetFileNameWithoutExtension(kv.Key);
                string vendorLower = vendorName.ToLowerInvariant();

                int score = 0;
                foreach (string keyword in entry.Keywords)
                {
                    if (vendorLower.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 2;
                }

                // Bonus for matching file extension
                string entryExt = Path.GetExtension(entry.FileName).ToLowerInvariant();
                string vendorExt = Path.GetExtension(kv.Key).ToLowerInvariant();
                if (entryExt == vendorExt) score += 1;

                if (score > bestScore && kv.Value.Count > 0)
                {
                    bestScore = score;
                    bestPath = kv.Value[0];
                }
            }

            // Require minimum score of 3 to avoid false matches
            return bestScore >= 3 ? bestPath : null;
        }

        // ── File operations ──────────────────────────────────────────

        /// <summary>
        /// Copies a vendor file to the destination, scaling it up if the
        /// source is small (e.g., 18×18 Kenney tiles scaled to entity size).
        /// </summary>
        private static void CopyAndScale(string sourcePath, string destPath, AssetCategory category)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));

                using (var src = new Bitmap(sourcePath))
                {
                    // Determine target size based on category
                    int targetW, targetH;
                    GetTargetSize(category, src.Width, src.Height, out targetW, out targetH);

                    if (src.Width == targetW && src.Height == targetH)
                    {
                        // No scaling needed — direct copy
                        File.Copy(sourcePath, destPath, true);
                    }
                    else
                    {
                        // Scale to target size using high-quality interpolation
                        using (var scaled = new Bitmap(targetW, targetH))
                        {
                            using (var g = Graphics.FromImage(scaled))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                g.DrawImage(src, 0, 0, targetW, targetH);
                            }
                            scaled.Save(destPath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback: try direct copy without scaling
                try { File.Copy(sourcePath, destPath, true); }
                catch
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[AssetAutoResolver] Failed to copy {sourcePath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Determines the appropriate target size for an asset based on its category.
        /// Small Kenney tiles (18×18) are upscaled to usable entity dimensions.
        /// </summary>
        private static void GetTargetSize(AssetCategory category, int srcW, int srcH,
            out int targetW, out int targetH)
        {
            // If source is already a reasonable size (> 32px), keep it
            if (srcW >= 32 && srcH >= 32)
            {
                targetW = srcW;
                targetH = srcH;
                return;
            }

            // Upscale small tiles based on category
            switch (category)
            {
                case AssetCategory.Enemy:
                    targetW = 48; targetH = 48; break;
                case AssetCategory.Boss:
                    targetW = 80; targetH = 80; break;
                case AssetCategory.PlayerCharacter:
                    targetW = 48; targetH = 64; break;
                case AssetCategory.Portrait:
                    targetW = 80; targetH = 80; break;
                case AssetCategory.UI:
                    targetW = 64; targetH = 64; break;
                default:
                    // Default 2× upscale
                    targetW = srcW * 2;
                    targetH = srcH * 2;
                    break;
            }
        }

        /// <summary>
        /// Generates a simple colored placeholder sprite for a missing asset.
        /// The placeholder uses category-specific colors and draws the asset
        /// name so it's obvious this is a placeholder during gameplay.
        /// </summary>
        private static void GeneratePlaceholder(string destPath, MissingAssetEntry entry)
        {
            try
            {
                // Determine placeholder size
                int w, h;
                GetTargetSize(entry.Category, 18, 18, out w, out h);

                // Category-specific colors
                Color bgColor, borderColor;
                GetPlaceholderColors(entry.Category, out bgColor, out borderColor);

                using (var bmp = new Bitmap(w, h))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        // Fill with category color
                        using (var bg = new SolidBrush(bgColor))
                            g.FillRectangle(bg, 0, 0, w, h);

                        // Draw border
                        using (var pen = new Pen(borderColor, 2))
                            g.DrawRectangle(pen, 1, 1, w - 3, h - 3);

                        // Draw an X pattern so it's clearly a placeholder
                        using (var xPen = new Pen(Color.FromArgb(80, 0, 0, 0), 1))
                        {
                            g.DrawLine(xPen, 0, 0, w, h);
                            g.DrawLine(xPen, w, 0, 0, h);
                        }

                        // Draw short label text
                        string label = Path.GetFileNameWithoutExtension(entry.FileName);
                        if (label.Length > 10) label = label.Substring(0, 10);
                        float fontSize = Math.Max(6, Math.Min(w / 8f, 9));
                        using (var f = new Font("Arial", fontSize, FontStyle.Bold))
                        using (var br = new SolidBrush(Color.White))
                        {
                            var sf = new StringFormat
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center
                            };
                            g.DrawString(label, f, br, w / 2f, h / 2f, sf);
                        }
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                    bmp.Save(destPath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AssetAutoResolver] Failed to generate placeholder: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns category-specific placeholder colors.
        /// </summary>
        private static void GetPlaceholderColors(AssetCategory cat,
            out Color bg, out Color border)
        {
            switch (cat)
            {
                case AssetCategory.Enemy:
                    bg = Color.FromArgb(180, 200, 60, 60);      // Red
                    border = Color.FromArgb(255, 255, 80, 80);
                    break;
                case AssetCategory.Boss:
                    bg = Color.FromArgb(180, 150, 40, 150);      // Purple
                    border = Color.FromArgb(255, 200, 80, 200);
                    break;
                case AssetCategory.PlayerCharacter:
                    bg = Color.FromArgb(180, 60, 120, 200);      // Blue
                    border = Color.FromArgb(255, 80, 160, 255);
                    break;
                case AssetCategory.Portrait:
                    bg = Color.FromArgb(180, 200, 180, 60);      // Gold
                    border = Color.FromArgb(255, 255, 220, 80);
                    break;
                case AssetCategory.Background:
                    bg = Color.FromArgb(180, 40, 100, 40);       // Green
                    border = Color.FromArgb(255, 60, 160, 60);
                    break;
                case AssetCategory.UI:
                    bg = Color.FromArgb(180, 80, 80, 120);       // Gray-blue
                    border = Color.FromArgb(255, 120, 120, 180);
                    break;
                default:
                    bg = Color.FromArgb(180, 100, 100, 100);     // Neutral gray
                    border = Color.FromArgb(255, 160, 160, 160);
                    break;
            }
        }

        /// <summary>
        /// Returns true for categories that produce image files (not audio).
        /// </summary>
        private static bool IsSpriteCategory(AssetCategory cat)
        {
            return cat != AssetCategory.Audio;
        }

        /// <summary>
        /// Forces a re-scan of the vendor directory on next use.
        /// Call after downloading new asset packs.
        /// </summary>
        public static void InvalidateIndex()
        {
            _vendorIndex.Clear();
            _indexed = false;
        }
    }
}
