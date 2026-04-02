using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    // ─────────────────────────────────────────────────────────────────────────
    //  EnvironmentArtist.cs  —  Environment & Background Art Systems
    //
    //  Team 14 (Environment / Background Artist 2D) — all 10 ideas implemented:
    //
    //    Idea 1:  Tileset registry — names, tile sizes, and world associations.
    //    Idea 2:  Parallax layer speed-coefficient config per world.
    //    Idea 3:  World decoration catalog — named prop types per biome.
    //    Idea 4:  Background crossfade transition — alpha-blend between two
    //             background colours / images across a scene change.
    //    Idea 5:  Animated-tile registry — tiles with multi-frame animations
    //             (water surface shimmer, lava bubbles, coin block).
    //    Idea 6:  Platform edge auto-tiling — resolves correct left/mid/right/
    //             solo tile variant based on neighbour occupancy.
    //    Idea 7:  Level boundary visual markers — draws the sky top, floor
    //             bottom, and world-edge columns so artists see level bounds.
    //    Idea 8:  Foreground prop manager — props drawn in front of the player
    //             (foliage, fence posts, hanging decorations).
    //    Idea 9:  Weather art integration — maps weather states to art changes
    //             (fog tint, rain overlay alpha, snow particle colour).
    //    Idea 10: Environment debug overlay — tile grid lines, prop placement
    //             dots, and boundary markers in one toggle.
    // ─────────────────────────────────────────────────────────────────────────

    // ── Idea 1 support: tileset descriptor ───────────────────────────────────
    /// <summary>
    /// Describes a named tileset and its grid geometry.
    /// Idea 1 (Environment Artist).
    /// </summary>
    public sealed class TilesetInfo
    {
        /// <summary>Unique name used to reference this tileset.</summary>
        public string Name      { get; set; }
        /// <summary>Width of a single tile in pixels.</summary>
        public int    TileW     { get; set; }
        /// <summary>Height of a single tile in pixels.</summary>
        public int    TileH     { get; set; }
        /// <summary>World number(s) that use this tileset.</summary>
        public int[]  Worlds    { get; set; }
        /// <summary>Path to the tileset image asset, relative to Assets\.</summary>
        public string AssetPath { get; set; }
    }

    // ── Idea 5 support: animated-tile descriptor ──────────────────────────────
    /// <summary>
    /// Describes a tile that cycles through multiple frames automatically.
    /// Idea 5 (Environment Artist).
    /// </summary>
    public sealed class AnimatedTile
    {
        /// <summary>Unique tile-type name, e.g. "Water", "Lava", "CoinBlock".</summary>
        public string   Name       { get; set; }
        /// <summary>Number of animation frames.</summary>
        public int      FrameCount { get; set; }
        /// <summary>Seconds per frame.</summary>
        public float    FrameTime  { get; set; }
        /// <summary>Accumulated time — advanced each update tick.</summary>
        public float    Timer      { get; set; }
        /// <summary>Current frame index (0-based).</summary>
        public int      Frame      => (int)(Timer / FrameTime) % FrameCount;
    }

    // ── Idea 8 support: foreground prop descriptor ────────────────────────────
    /// <summary>
    /// A prop rendered in front of the player layer.
    /// Idea 8 (Environment Artist).
    /// </summary>
    public sealed class ForegroundProp
    {
        /// <summary>Name / type of prop (e.g. "FoliagePatch", "FencePost").</summary>
        public string Name { get; set; }
        /// <summary>Screen-space or world-space draw position.</summary>
        public PointF Position { get; set; }
        /// <summary>Tint colour applied when drawing.</summary>
        public Color  Tint     { get; set; } = Color.White;
        /// <summary>Depth sort value — higher values draw on top.</summary>
        public float  Depth    { get; set; }
    }

    /// <summary>
    /// Central environment and background-art system: tileset registry,
    /// parallax config, decorations, crossfades, animated tiles,
    /// auto-tiling, boundary markers, foreground props, weather art, and
    /// debug overlays.
    /// </summary>
    public static class EnvironmentArtist
    {
        // ── Idea 1: Tileset registry ──────────────────────────────────────────
        /// <summary>
        /// All registered tilesets keyed by name.
        /// Idea 1 (Environment Artist).
        /// </summary>
        private static readonly Dictionary<string, TilesetInfo> _tilesets =
            new Dictionary<string, TilesetInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a tileset descriptor.
        /// Idea 1 (Environment Artist).
        /// </summary>
        public static void RegisterTileset(string name, int tileW, int tileH,
                                            string assetPath, params int[] worlds)
        {
            _tilesets[name] = new TilesetInfo
            {
                Name = name, TileW = tileW, TileH = tileH,
                AssetPath = assetPath, Worlds = worlds
            };
        }

        /// <summary>Returns the tileset info for the given name, or null.</summary>
        public static TilesetInfo GetTileset(string name)
        {
            _tilesets.TryGetValue(name, out var t); return t;
        }

        // ── Idea 2: Parallax layer speed-coefficient config ────────────────────
        /// <summary>
        /// Per-layer horizontal scroll speed coefficients for each world.
        /// Index 0 = furthest back (slowest); higher indices scroll faster.
        /// Idea 2 (Environment Artist).
        /// </summary>
        private static readonly Dictionary<int, float[]> _parallaxSpeeds =
            new Dictionary<int, float[]>
            {
                { 1, new[] { 0.10f, 0.22f, 0.40f, 0.70f } }, // World 1 — Grass Plains
                { 2, new[] { 0.12f, 0.25f, 0.45f, 0.75f } }, // World 2 — Desert
                { 3, new[] { 0.08f, 0.18f, 0.35f, 0.65f } }, // World 3 — Ocean
                { 4, new[] { 0.15f, 0.30f, 0.55f, 0.80f } }, // World 4 — Giant Land
                { 5, new[] { 0.10f, 0.20f, 0.38f, 0.68f } }, // World 5 — Sky
                { 6, new[] { 0.06f, 0.14f, 0.28f, 0.55f } }, // World 6 — Ice
                { 7, new[] { 0.05f, 0.12f, 0.25f, 0.50f } }, // World 7 — Storm / Pipe
                { 8, new[] { 0.18f, 0.35f, 0.60f, 0.88f } }, // World 8 — Dark Land
            };

        /// <summary>
        /// Returns the parallax speed coefficients for the given world.
        /// Idea 2 (Environment Artist).
        /// </summary>
        public static float[] GetParallaxSpeeds(int world)
        {
            return _parallaxSpeeds.TryGetValue(world, out var s)
                ? s
                : new[] { 0.10f, 0.22f, 0.40f, 0.70f };
        }

        // ── Idea 3: World decoration catalog ─────────────────────────────────
        /// <summary>
        /// Named prop / decoration types that appear in each biome.
        /// Idea 3 (Environment Artist).
        /// </summary>
        private static readonly Dictionary<string, List<string>> _decorCatalog =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Grassland",    new List<string> { "Bush1","Bush2","Tree","Flower","Hill","Cloud" } },
                { "Desert",       new List<string> { "Cactus","DryBush","Pyramid","Skull","Dune"   } },
                { "Ocean",        new List<string> { "Seaweed","Coral","Clam","Fish","Starfish"    } },
                { "Sky",          new List<string> { "Cloud1","Cloud2","CloudPlatform","Sun","Bird" } },
                { "Ice",          new List<string> { "IceCrystal","SnowMound","IcePillar","Penguin" } },
                { "Underground",  new List<string> { "Rock","Mushroom","Bone","WaterDrip","Torch"  } },
                { "DarkLand",     new List<string> { "Tombstone","LavaPillar","Fortress","Chain"   } },
            };

        /// <summary>
        /// Returns the decoration list for a named biome, or an empty list.
        /// Idea 3 (Environment Artist).
        /// </summary>
        public static IReadOnlyList<string> GetDecorations(string biome)
        {
            return _decorCatalog.TryGetValue(biome, out var list)
                ? list
                : new List<string>();
        }

        // ── Idea 4: Background crossfade transition ────────────────────────────
        /// <summary>
        /// Current crossfade blend alpha (0 = source, 1 = destination).
        /// Idea 4 (Environment Artist).
        /// </summary>
        public static float CrossfadeAlpha  { get; private set; } = 1f;
        /// <summary>Whether a crossfade is currently running.</summary>
        public static bool  CrossfadeActive { get; private set; }

        private static float _crossfadeDuration;
        private static float _crossfadeTimer;

        /// <summary>
        /// Starts a background crossfade over the given duration in seconds.
        /// Idea 4 (Environment Artist).
        /// </summary>
        public static void StartCrossfade(float durationSeconds = 0.5f)
        {
            _crossfadeDuration = Math.Max(0.01f, durationSeconds);
            _crossfadeTimer    = 0f;
            CrossfadeAlpha     = 0f;
            CrossfadeActive    = true;
        }

        /// <summary>
        /// Advances the crossfade. Call once per update tick.
        /// Idea 4 (Environment Artist).
        /// </summary>
        public static void UpdateCrossfade(float dt)
        {
            if (!CrossfadeActive) return;
            _crossfadeTimer += dt;
            CrossfadeAlpha   = Math.Min(1f, _crossfadeTimer / _crossfadeDuration);
            if (CrossfadeAlpha >= 1f) CrossfadeActive = false;
        }

        // ── Idea 5: Animated-tile registry ────────────────────────────────────
        /// <summary>
        /// All registered animated tile types.
        /// Idea 5 (Environment Artist).
        /// </summary>
        private static readonly Dictionary<string, AnimatedTile> _animTiles =
            new Dictionary<string, AnimatedTile>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers an animated tile type.
        /// Idea 5 (Environment Artist).
        /// </summary>
        public static void RegisterAnimTile(string name, int frames, float frameTime)
        {
            _animTiles[name] = new AnimatedTile
            {
                Name = name, FrameCount = frames, FrameTime = frameTime
            };
        }

        /// <summary>
        /// Advances all animated tile timers. Call once per update tick.
        /// Idea 5 (Environment Artist).
        /// </summary>
        public static void UpdateAnimTiles(float dt)
        {
            foreach (var tile in _animTiles.Values)
                tile.Timer += dt;
        }

        /// <summary>
        /// Returns the current animation frame index for the named tile type.
        /// Idea 5 (Environment Artist).
        /// </summary>
        public static int GetAnimTileFrame(string name)
        {
            return _animTiles.TryGetValue(name, out var t) ? t.Frame : 0;
        }

        // ── Idea 6: Platform edge auto-tiling ────────────────────────────────
        /// <summary>
        /// Tile variant enum for platform edge resolution.
        /// Idea 6 (Environment Artist).
        /// </summary>
        public enum TileVariant { Solo, Left, Middle, Right }

        /// <summary>
        /// Returns the correct tile variant based on whether the left and right
        /// neighbours are occupied.  Mimics SMB3 platform edge auto-tiling.
        /// Idea 6 (Environment Artist).
        /// </summary>
        /// <param name="hasLeft">True when the tile to the left is occupied.</param>
        /// <param name="hasRight">True when the tile to the right is occupied.</param>
        public static TileVariant ResolveEdgeTile(bool hasLeft, bool hasRight)
        {
            if (!hasLeft && !hasRight) return TileVariant.Solo;
            if (!hasLeft && hasRight)  return TileVariant.Left;
            if (hasLeft  && !hasRight) return TileVariant.Right;
            return TileVariant.Middle;
        }

        // ── Idea 7: Level boundary visual markers ────────────────────────────
        /// <summary>
        /// Draws dashed boundary lines at the sky top, floor bottom,
        /// and world left/right edges so artists can see level limits.
        /// Idea 7 (Environment Artist).
        /// </summary>
        /// <param name="g">Active Graphics context (screen space).</param>
        /// <param name="levelBounds">The full level rectangle in screen space.</param>
        public static void DrawBoundaryMarkers(Graphics g, Rectangle levelBounds)
        {
            using (var pen = new Pen(Color.FromArgb(120, 0, 220, 255), 1))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                // Top (sky)
                g.DrawLine(pen, levelBounds.Left, levelBounds.Top,
                                levelBounds.Right, levelBounds.Top);
                // Bottom (floor)
                g.DrawLine(pen, levelBounds.Left, levelBounds.Bottom,
                                levelBounds.Right, levelBounds.Bottom);
                // Left edge
                g.DrawLine(pen, levelBounds.Left, levelBounds.Top,
                                levelBounds.Left, levelBounds.Bottom);
                // Right edge
                g.DrawLine(pen, levelBounds.Right, levelBounds.Top,
                                levelBounds.Right, levelBounds.Bottom);
            }
        }

        // ── Idea 8: Foreground prop manager ──────────────────────────────────
        /// <summary>
        /// Active foreground props for the current scene, sorted by depth.
        /// Idea 8 (Environment Artist).
        /// </summary>
        private static readonly List<ForegroundProp> _fgProps = new List<ForegroundProp>();

        /// <summary>Adds a foreground prop to the render list.</summary>
        public static void AddForegroundProp(string name, PointF pos,
                                              Color tint, float depth = 0f)
        {
            _fgProps.Add(new ForegroundProp { Name = name, Position = pos,
                                              Tint = tint, Depth = depth });
            _fgProps.Sort((a, b) => a.Depth.CompareTo(b.Depth));
        }

        /// <summary>Clears all foreground props (call on scene unload).</summary>
        public static void ClearForegroundProps() => _fgProps.Clear();

        /// <summary>
        /// Read-only view of foreground props for the scene draw pass.
        /// Idea 8 (Environment Artist).
        /// </summary>
        public static IReadOnlyList<ForegroundProp> ForegroundProps => _fgProps;

        // ── Idea 9: Weather → art integration ────────────────────────────────
        /// <summary>
        /// Maps weather state strings to art modifiers.
        /// Returns (fogAlpha, rainAlpha, snowColor) for the given weather name.
        /// Idea 9 (Environment Artist).
        /// </summary>
        public static (float FogAlpha, float RainAlpha, Color SnowColor)
            GetWeatherArtParams(string weatherState)
        {
            switch (weatherState?.ToLowerInvariant())
            {
                case "rain":       return (0.10f, 0.55f, Color.Transparent);
                case "heavyrain":  return (0.25f, 0.80f, Color.Transparent);
                case "snow":       return (0.05f, 0.00f, Color.White);
                case "blizzard":   return (0.35f, 0.00f, Color.AliceBlue);
                case "fog":        return (0.50f, 0.00f, Color.Transparent);
                case "storm":      return (0.30f, 0.70f, Color.Transparent);
                default:           return (0.00f, 0.00f, Color.Transparent);
            }
        }

        // ── Idea 10: Environment debug overlay ───────────────────────────────
        /// <summary>
        /// Whether the environment debug overlay is active.
        /// Idea 10 (Environment Artist).
        /// </summary>
        public static bool DebugOverlayEnabled { get; set; }

        /// <summary>
        /// Draws tile grid lines, foreground prop dots, and boundary markers
        /// over the scene when <see cref="DebugOverlayEnabled"/> is true.
        /// Idea 10 (Environment Artist).
        /// </summary>
        /// <param name="g">Active Graphics context.</param>
        /// <param name="viewport">Visible viewport rectangle.</param>
        /// <param name="tileW">Tile width used to draw the grid.</param>
        /// <param name="tileH">Tile height used to draw the grid.</param>
        public static void DrawDebugOverlay(Graphics g, Rectangle viewport,
                                             int tileW = 32, int tileH = 32)
        {
            if (!DebugOverlayEnabled) return;

            // -- Tile grid
            using (var gridPen = new Pen(Color.FromArgb(40, 200, 200, 255), 1))
            {
                for (int x = viewport.Left; x < viewport.Right; x += tileW)
                    g.DrawLine(gridPen, x, viewport.Top, x, viewport.Bottom);
                for (int y = viewport.Top; y < viewport.Bottom; y += tileH)
                    g.DrawLine(gridPen, viewport.Left, y, viewport.Right, y);
            }

            // -- Foreground prop placement dots
            using (var brush = new SolidBrush(Color.FromArgb(200, 255, 165, 0)))
            {
                foreach (var prop in _fgProps)
                {
                    g.FillEllipse(brush,
                        prop.Position.X - 4, prop.Position.Y - 4, 8, 8);
                }
            }

            // -- Boundary markers (uses full viewport as level bounds)
            DrawBoundaryMarkers(g, viewport);
        }

        // ── Static initializer ────────────────────────────────────────────────
        /// <summary>
        /// Seeds the tileset registry and animated-tile registry with defaults.
        /// Call once at startup.
        /// </summary>
        public static void RegisterDefaults()
        {
            // -- Tilesets (Idea 1)
            RegisterTileset("grassland",   32, 32, "Tiles\\grassland.png",   1);
            RegisterTileset("desert",      32, 32, "Tiles\\desert.png",      2);
            RegisterTileset("ocean",       32, 32, "Tiles\\ocean.png",       3);
            RegisterTileset("sky_island",  32, 32, "Tiles\\sky_island.png",  5);
            RegisterTileset("ice",         32, 32, "Tiles\\ice.png",         6);
            RegisterTileset("underground", 32, 32, "Tiles\\underground.png", 4, 7);
            RegisterTileset("dark_land",   32, 32, "Tiles\\dark_land.png",   8);

            // -- Animated tiles (Idea 5)
            RegisterAnimTile("Water",     4, 0.15f); // 4 frames, ~6.7 fps shimmer
            RegisterAnimTile("Lava",      4, 0.25f); // 4 frames, 4 fps bubble
            RegisterAnimTile("CoinBlock", 4, 0.08f); // 4 frames, 12.5 fps spin
            RegisterAnimTile("Waterfall", 2, 0.10f); // 2 frames, 10 fps fall
        }
    }
}
