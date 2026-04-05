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

        private static string SpritesPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites");

        private static string AssetsPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

        private static string ResolvePath(string fileName)
        {
            string p = Path.Combine(SpritesPath, fileName);
            if (File.Exists(p)) return p;
            p = Path.Combine(AssetsPath, fileName);
            if (File.Exists(p)) return p;
            return null;
        }

        public static Bitmap Get(string fileName)
        {
            if (_cache.TryGetValue(fileName, out Bitmap cached)) return cached;
            string path = ResolvePath(fileName);
            if (path == null) return null;
            try
            {
                var bmp = new Bitmap(path);
                _cache[fileName] = bmp;
                return bmp;
            }
            catch { return null; }
        }

        // Returns a pre-scaled copy — call once at startup, not every frame.
        // Uses the full high-quality GDI+ pipeline so sprites stay sharp.
        public static Bitmap GetScaled(string fileName, int w, int h)
        {
            string key = $"{fileName}_{w}x{h}";
            if (_cache.TryGetValue(key, out Bitmap cached)) return cached;
            var src = Get(fileName);
            if (src == null) return null;
            var scaled = new Bitmap(w, h);
            using (var g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode  = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode      = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
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
        }
    }
}
