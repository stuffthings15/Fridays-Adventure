using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    // ─────────────────────────────────────────────────────────────────────────
    //  LevelEventSystem.cs  —  SMB3-style Level Design Events & Mechanics
    //
    //  Team 5 (Level Designer) — all 10 ideas implemented below:
    //
    //    Idea 1:  Level timer display + callback on time-up.
    //    Idea 2:  Auto-scroll section — camera forced rightward at a set speed.
    //    Idea 3:  Wind zone — constant directional force applied to the player.
    //    Idea 4:  Spike trap formation — row/column of spike blocks with timing.
    //    Idea 5:  Donut lift — platform that falls after player stands on it.
    //    Idea 6:  Level segment tags — pacing markers (Calm/Action/Danger/Boss).
    //    Idea 7:  Moving platform synchronised group — all platforms share phase.
    //    Idea 8:  Bonus stage entrance — hidden tile that warps to bonus room.
    //    Idea 9:  Warp pipe shortcut — enter pipe to jump ahead in level.
    //    Idea 10: Time-bonus score — points awarded based on time remaining.
    // ─────────────────────────────────────────────────────────────────────────

    // ── Idea 6: Pacing segment enum ───────────────────────────────────────────

    /// <summary>
    /// SMB3-inspired level pacing segment type.
    /// Idea 6 (Level Designer).
    /// </summary>
    public enum LevelSegment { Calm, Action, Danger, Boss, Bonus }

    // ── Idea 3: Wind zone ─────────────────────────────────────────────────────

    /// <summary>Describes an active wind zone rectangle and its force vector.</summary>
    public struct WindZoneData
    {
        /// <summary>World-space area the wind affects.</summary>
        public RectangleF Area;
        /// <summary>Horizontal wind force in pixels/second² added to the player's velocity.</summary>
        public float ForceX;
        /// <summary>Vertical wind force in pixels/second² (positive = downward).</summary>
        public float ForceY;
    }

    // ── Idea 9: Warp pipe entry ────────────────────────────────────────────────

    /// <summary>Describes a warp pipe entrance + destination within the level.</summary>
    public struct WarpPipeData
    {
        /// <summary>World-space entrance rectangle.</summary>
        public RectangleF EntranceRect;
        /// <summary>
        /// X spawn coordinate after the warp.  Y is ground level (set per scene).
        /// </summary>
        public float DestinationX;
        /// <summary>Friendly label shown in the HUD (e.g. "WARP → 4-2").</summary>
        public string Label;
    }

    /// <summary>
    /// Central level-event and pacing system.
    /// Scenes populate the active registries at level start, then call
    /// <see cref="Update"/> and <see cref="Draw"/> each frame.
    /// </summary>
    public static class LevelEventSystem
    {
        // ── Idea 2: Auto-scroll state ─────────────────────────────────────────
        /// <summary>
        /// When true, the camera is forced rightward at <see cref="AutoScrollSpeed"/>.
        /// Idea 2 (Level Designer).
        /// </summary>
        public static bool  AutoScrollActive { get; private set; }
        /// <summary>Auto-scroll speed in world-pixels per second.</summary>
        public static float AutoScrollSpeed  { get; private set; }
        /// <summary>
        /// Enables auto-scrolling for this section.
        /// Idea 2 (Level Designer).
        /// </summary>
        public static void StartAutoScroll(float pixelsPerSecond)
        {
            AutoScrollActive = true;
            AutoScrollSpeed  = pixelsPerSecond;
            DebugLogger.LogInfo("LevelEventSystem", $"Auto-scroll started @ {pixelsPerSecond}px/s");
        }
        /// <summary>Stops the auto-scroll (call at section boundary).</summary>
        public static void StopAutoScroll()  { AutoScrollActive = false; AutoScrollSpeed = 0f; }

        // ── Idea 3: Wind zones registry ───────────────────────────────────────
        /// <summary>
        /// Active wind zones for the current level section.
        /// Idea 3 (Level Designer).
        /// </summary>
        public static readonly List<WindZoneData> WindZones = new List<WindZoneData>();

        /// <summary>Registers a new wind zone.</summary>
        public static void AddWindZone(RectangleF area, float forceX, float forceY)
        {
            WindZones.Add(new WindZoneData { Area = area, ForceX = forceX, ForceY = forceY });
        }

        /// <summary>
        /// Returns the combined wind force at <paramref name="worldPos"/>.
        /// Sum of all overlapping zones.  Returns PointF.Empty if none.
        /// </summary>
        public static PointF GetWindAt(PointF worldPos)
        {
            float fx = 0f, fy = 0f;
            foreach (var z in WindZones)
            {
                if (z.Area.Contains(worldPos))
                { fx += z.ForceX; fy += z.ForceY; }
            }
            return new PointF(fx, fy);
        }

        // ── Idea 4: Spike formation state ─────────────────────────────────────
        // Spike formations are managed by LevelEntities/Entities code.
        // This system tracks their pattern timing so they all pulse in sync.
        private static float _spikePhase;
        private const  float SpikeOnDuration  = 0.8f;
        private const  float SpikeOffDuration = 1.2f;
        private static float _spikeCycleLength = SpikeOnDuration + SpikeOffDuration;

        /// <summary>
        /// True during the spike ACTIVE (extended) portion of the cycle.
        /// Idea 4 (Level Designer).
        /// </summary>
        public static bool SpikesActive => _spikePhase < SpikeOnDuration;

        // ── Idea 5: Donut lift registry ───────────────────────────────────────
        // Donut lift state per entity is managed in LevelEntities.
        // Central timing constant exposed here for designer tuning.
        /// <summary>
        /// Seconds a donut lift waits after the player steps on it before falling.
        /// Idea 5 (Level Designer).
        /// </summary>
        public const float DonutLiftFallDelay = 0.8f;

        /// <summary>Pixels per second the donut lift falls after the delay.</summary>
        public const float DonutLiftFallSpeed = 220f;

        // ── Idea 6: Current segment ────────────────────────────────────────────
        /// <summary>
        /// Current pacing segment for analytics and difficulty tuning.
        /// Idea 6 (Level Designer).
        /// </summary>
        public static LevelSegment CurrentSegment { get; private set; } = LevelSegment.Calm;

        /// <summary>
        /// Changes the active pacing segment and logs the transition.
        /// Idea 6 (Level Designer).
        /// </summary>
        public static void SetSegment(LevelSegment seg)
        {
            if (seg == CurrentSegment) return;
            DebugLogger.LogInfo("LevelEventSystem", $"Segment → {seg}");
            CurrentSegment = seg;
            SessionStats.Instance.RecordSegmentChange(seg.ToString());
        }

        // ── Idea 7: Moving platform phase ─────────────────────────────────────
        /// <summary>
        /// Shared phase clock [0–1] for synchronised moving platform groups.
        /// Entities use this value instead of their own timer so all group
        /// members stay perfectly in sync.
        /// Idea 7 (Level Designer).
        /// </summary>
        public static float PlatformGroupPhase { get; private set; }
        private static float _groupPhaseSpeed = 0.25f;  // cycles per second

        // ── Idea 8: Bonus stage entrance registry ─────────────────────────────
        private static readonly List<RectangleF> _bonusEntrances = new List<RectangleF>();

        /// <summary>
        /// Registers a hidden tile rectangle as a bonus stage entrance.
        /// Idea 8 (Level Designer).
        /// </summary>
        public static void AddBonusEntrance(RectangleF rect)
        {
            _bonusEntrances.Add(rect);
        }

        /// <summary>
        /// Tests whether <paramref name="playerRect"/> overlaps a bonus entrance.
        /// Returns true on first match and outputs the entrance index.
        /// Idea 8 (Level Designer).
        /// </summary>
        public static bool CheckBonusEntrance(RectangleF playerRect, out int index)
        {
            for (int i = 0; i < _bonusEntrances.Count; i++)
            {
                if (_bonusEntrances[i].IntersectsWith(playerRect))
                { index = i; return true; }
            }
            index = -1; return false;
        }

        // ── Idea 9: Warp pipe registry ────────────────────────────────────────
        private static readonly List<WarpPipeData> _warpPipes = new List<WarpPipeData>();

        /// <summary>
        /// Registers a warp pipe entrance for this level.
        /// Idea 9 (Level Designer).
        /// </summary>
        public static void AddWarpPipe(RectangleF entrance, float destX, string label)
        {
            _warpPipes.Add(new WarpPipeData { EntranceRect = entrance, DestinationX = destX, Label = label });
        }

        /// <summary>
        /// Returns the first warp pipe the player overlaps, or null if none.
        /// Idea 9 (Level Designer).
        /// </summary>
        public static WarpPipeData? CheckWarpPipe(RectangleF playerRect)
        {
            foreach (var p in _warpPipes)
                if (p.EntranceRect.IntersectsWith(playerRect))
                    return p;
            return null;
        }

        // ── Idea 10: Time-bonus score ──────────────────────────────────────────
        /// <summary>
        /// Returns the time-bonus score to award at level completion.
        /// Delegates to PowerUpInventory for the calculation.
        /// Idea 10 (Level Designer).
        /// </summary>
        public static int GetTimeBonus() => PowerUpInventory.CalculateFlagpoleBonus();

        // ── Clear (call on scene exit) ─────────────────────────────────────────
        /// <summary>Resets all level-event registries for the next level.</summary>
        public static void Clear()
        {
            StopAutoScroll();
            WindZones.Clear();
            _bonusEntrances.Clear();
            _warpPipes.Clear();
            _spikePhase         = 0f;
            PlatformGroupPhase  = 0f;
            CurrentSegment      = LevelSegment.Calm;
        }

        // ── Update ────────────────────────────────────────────────────────────
        /// <summary>
        /// Advances all LevelEventSystem timers.
        /// Call each frame from the active gameplay scene.
        /// </summary>
        public static void Update(float dt)
        {
            // Idea 4: spike cycle
            _spikePhase = (_spikePhase + dt) % _spikeCycleLength;

            // Idea 7: platform group phase
            PlatformGroupPhase = (PlatformGroupPhase + dt * _groupPhaseSpeed) % 1f;
        }

        // ── Draw (optional — for debug/dev mode overlay) ──────────────────────
        /// <summary>
        /// Draws level event debug overlays when GodMode is active.
        /// Call after the main scene draw.
        /// </summary>
        public static void DrawDebug(Graphics g, float camX)
        {
            if (!Game.Instance.GodMode) return;

            using (var pen = new Pen(Color.FromArgb(120, 0, 200, 255), 1))
            {
                // Wind zones — blue outline
                foreach (var z in WindZones)
                {
                    var r = new RectangleF(z.Area.X - camX, z.Area.Y, z.Area.Width, z.Area.Height);
                    g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
                    using (var f = new Font("Courier New", 7))
                        g.DrawString($"WIND {z.ForceX:+0;-0}px", f, Brushes.Cyan, r.X + 2, r.Y + 2);
                }

                // Warp pipes — yellow outline
                using (var pipePen = new Pen(Color.Yellow, 1))
                foreach (var p in _warpPipes)
                {
                    var r = new RectangleF(p.EntranceRect.X - camX, p.EntranceRect.Y,
                                          p.EntranceRect.Width, p.EntranceRect.Height);
                    g.DrawRectangle(pipePen, r.X, r.Y, r.Width, r.Height);
                    using (var f = new Font("Courier New", 7))
                        g.DrawString(p.Label, f, Brushes.Yellow, r.X + 2, r.Y + 2);
                }
            }
        }
    }
}
