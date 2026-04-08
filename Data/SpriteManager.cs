using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Fridays_Adventure.Data
{
    public static class SpriteManager
    {
        private static readonly Dictionary<string, Bitmap> _cache =
            new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

        // Tracks file names already confirmed missing so ResolvePath is never
        // called again for sprites that don't exist on disk.  This eliminates
        // the catastrophic per-frame Directory.GetFiles(AllDirectories) scan
        // that was consuming 63 % of all CPU.
        private static readonly HashSet<string> _negativeLookups =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pre-indexed vendor directory map: filename → full path.
        // Built lazily on first vendor-directory probe instead of
        // calling Directory.GetFiles(AllDirectories) on every miss.
        private static Dictionary<string, string> _vendorIndex;
        private static bool _vendorIndexBuilt;

        private static string SpritesPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites");

        private static string AssetsPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

        /// <summary>
        /// Third-party vendor root where downloaded CC0 asset packs live.
        /// <remarks>PHASE 2 - Team 8: Automatic Asset Downloader fallback path</remarks>
        /// </summary>
        private static string ThirdPartyPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "third_party", "vendor");

        private static string ResolvePath(string fileName)
        {
            // 1) Primary: Assets/Sprites/
            string p = Path.Combine(SpritesPath, fileName);
            if (File.Exists(p)) return p;

            // 2) Secondary: Assets/
            p = Path.Combine(AssetsPath, fileName);
            if (File.Exists(p)) return p;

            // 3) Fallback: indexed lookup in third-party vendor directory.
            //    The index is built once (lazily) instead of scanning the
            //    1,400+ file directory tree on every cache-miss call.
            EnsureVendorIndex();
            if (_vendorIndex != null &&
                _vendorIndex.TryGetValue(fileName, out string vendorPath))
                return vendorPath;

            return null;
        }

        /// <summary>
        /// Builds a filename→path index of all files under the third-party
        /// vendor directory.  Called once on first vendor-path probe.
        /// </summary>
        private static void EnsureVendorIndex()
        {
            if (_vendorIndexBuilt) return;
            _vendorIndexBuilt = true;
            string vendorDir = ThirdPartyPath;
            if (!Directory.Exists(vendorDir)) return;
            try
            {
                string[] files = Directory.GetFiles(vendorDir, "*.*",
                    SearchOption.AllDirectories);
                _vendorIndex = new Dictionary<string, string>(
                    files.Length, StringComparer.OrdinalIgnoreCase);
                foreach (string f in files)
                {
                    string name = Path.GetFileName(f);
                    // First file wins for duplicate names across sub-dirs
                    if (!_vendorIndex.ContainsKey(name))
                        _vendorIndex[name] = f;
                }
            }
            catch { /* directory enumeration failure — non-critical */ }
        }

        public static Bitmap Get(string fileName)
        {
            if (_cache.TryGetValue(fileName, out Bitmap cached)) return cached;

            // Skip files already confirmed missing — avoids redundant disk probes
            if (_negativeLookups.Contains(fileName)) return null;

            string path = ResolvePath(fileName);
            if (path == null)
            {
                // Remember this miss so we never probe for it again
                _negativeLookups.Add(fileName);
                AssetGapDetector.RecordMiss(fileName);
                return null;
            }
            try
            {
                var bmp = new Bitmap(path);
                _cache[fileName] = bmp;
                return bmp;
            }
            catch
            {
                _negativeLookups.Add(fileName);
                AssetGapDetector.RecordMiss(fileName);
                return null;
            }
        }

        // Returns a pre-scaled copy — call once at startup, not every frame.
        // Uses fast Bilinear interpolation for good quality without freeze times.
        // Bicubic was causing 10+ second freeze; NearestNeighbor was too pixelated.
        // Bilinear provides the best balance: 5x faster than Bicubic, much better quality than NN.
        public static Bitmap GetScaled(string fileName, int w, int h)
        {
            string key = $"{fileName}_{w}x{h}";
            if (_cache.TryGetValue(key, out Bitmap cached)) return cached;
            var src = Get(fileName);
            if (src == null) return null;
            var scaled = new Bitmap(w, h);
            using (var g = Graphics.FromImage(scaled))
            {
                // Bilinear interpolation: 5x faster than HighQualityBicubic, smooth quality
                g.InterpolationMode  = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                g.SmoothingMode      = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                g.PixelOffsetMode    = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(src, 0, 0, w, h);
            }
            _cache[key] = scaled;
            return scaled;
        }

        public static void DisposeAll()
        {
            foreach (var b in _cache.Values) b?.Dispose();
            _cache.Clear();
            _negativeLookups.Clear();
            _vendorIndex = null;
            _vendorIndexBuilt = false;
        }

        /// <summary>
        /// Warms the bitmap cache for the given file name.
        /// Safe to call from a background thread — simply calls Get which is idempotent.
        /// </summary>
        public static void Preload(string fileName) => Get(fileName);

        /// <summary>
        /// Clears the entire sprite cache so that sprites are reloaded and
        /// re-scaled with the latest quality settings on next access.
        /// </summary>
        public static void InvalidateCache()
        {
            foreach (var b in _cache.Values) b?.Dispose();
            _cache.Clear();
            _negativeLookups.Clear();
            _vendorIndex = null;
            _vendorIndexBuilt = false;
        }
    }
}
