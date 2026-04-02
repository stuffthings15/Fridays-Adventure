using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Lightweight publish-subscribe event bus for decoupled game-system communication.
    ///
    /// Team 8 (Systems Programmer) — event bus for designer-friendly loose coupling.
    /// Team 1 (Game Director)      — achievement triggers subscribe to gameplay events.
    /// Team 19 (QA Tester)         — QA hooks subscribe to events for telemetry.
    ///
    /// Usage:
    ///   EventBus.Subscribe&lt;PlayerDeathEvent&gt;(OnPlayerDeath);
    ///   EventBus.Publish(new PlayerDeathEvent { Position = player.Position });
    ///   EventBus.Unsubscribe&lt;PlayerDeathEvent&gt;(OnPlayerDeath);
    /// </summary>
    public static class EventBus
    {
        // Stores lists of delegates keyed by the event type.
        private static readonly Dictionary<Type, List<Delegate>> _handlers =
            new Dictionary<Type, List<Delegate>>();

        // ── Subscribe ─────────────────────────────────────────────────────────
        /// <summary>
        /// Registers a handler for events of type <typeparamref name="T"/>.
        /// Duplicate registrations are silently ignored.
        /// </summary>
        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            var key = typeof(T);
            if (!_handlers.ContainsKey(key))
                _handlers[key] = new List<Delegate>();
            if (!_handlers[key].Contains(handler))
                _handlers[key].Add(handler);
        }

        // ── Unsubscribe ───────────────────────────────────────────────────────
        /// <summary>Removes a previously registered handler.</summary>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            var key = typeof(T);
            if (_handlers.ContainsKey(key))
                _handlers[key].Remove(handler);
        }

        // ── Publish ───────────────────────────────────────────────────────────
        /// <summary>
        /// Dispatches an event to all registered handlers of type <typeparamref name="T"/>.
        /// Exceptions in individual handlers are logged and do not block other handlers.
        /// </summary>
        public static void Publish<T>(T evt)
        {
            var key = typeof(T);
            if (!_handlers.ContainsKey(key)) return;
            // Copy to allow handlers to unsubscribe during iteration.
            var copy = new List<Delegate>(_handlers[key]);
            foreach (var d in copy)
            {
                try
                {
                    ((Action<T>)d)(evt);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError($"EventBus.Publish<{typeof(T).Name}>", ex);
                }
            }
        }

        /// <summary>Removes all subscriptions (call on scene reset / game over).</summary>
        public static void ClearAll() => _handlers.Clear();
    }

    // ── Built-in game events ──────────────────────────────────────────────────

    /// <summary>Fired when the player dies.</summary>
    public struct PlayerDeathEvent
    {
        public float X, Y;
    }

    /// <summary>Fired when an enemy is defeated.</summary>
    public struct EnemyDefeatedEvent
    {
        public string EnemyType;
        public float  X, Y;
        public int    ScoreValue;
    }

    /// <summary>Fired when a level is completed.</summary>
    public struct LevelCompleteEvent
    {
        public string LevelName;
        public int    BonusAwarded;
    }

    /// <summary>Fired when a boss is defeated.</summary>
    public struct BossDefeatedEvent
    {
        public string BossName;
    }

    /// <summary>Fired when a power-up is collected.</summary>
    public struct PowerUpCollectedEvent
    {
        public string PowerUpType;
        public float  X, Y;
    }

    /// <summary>Fired when a checkpoint is reached.</summary>
    public struct CheckpointEvent
    {
        public int CheckpointIndex;
    }

    /// <summary>Fired when a berry / coin is collected.</summary>
    public struct BerryCollectedEvent
    {
        public int Count;
    }

    // ── Team 8 (Systems Programmer) — Idea 1: additional game events ──────────

    /// <summary>Fired when the player picks up a star coin. Idea 1 (Systems Programmer).</summary>
    public struct StarCoinCollectedEvent
    {
        /// <summary>0-based index of the collected star coin (0, 1, or 2).</summary>
        public int Index;
        /// <summary>World number the coin belongs to.</summary>
        public int World;
        /// <summary>Level number the coin belongs to.</summary>
        public int Level;
    }

    /// <summary>Fired when a suit/power-up is applied to the player. Idea 1 (Systems Programmer).</summary>
    public struct SuitAppliedEvent
    {
        /// <summary>The suit type that was applied.</summary>
        public SuitType Suit;
    }

    /// <summary>Fired when the level timer hits the urgency threshold (&lt;100 s). Idea 1 (Systems Programmer).</summary>
    public struct TimerUrgentEvent { }

    /// <summary>Fired when the level timer expires (time-up). Idea 1 (Systems Programmer).</summary>
    public struct TimerExpiredEvent { }

    /// <summary>Fired when a combo chain is broken. Idea 1 (Systems Programmer).</summary>
    public struct ComboBreakEvent
    {
        /// <summary>Length of the chain that just ended.</summary>
        public int FinalChainLength;
    }

    /// <summary>Fired when a new combo milestone (4, 8, 16 …) is reached. Idea 1 (Systems Programmer).</summary>
    public struct ComboMilestoneEvent
    {
        /// <summary>Chain length at the milestone.</summary>
        public int ChainLength;
        /// <summary>Bonus score awarded.</summary>
        public int BonusScore;
    }

    /// <summary>Fired when a warp pipe is entered. Idea 1 (Systems Programmer).</summary>
    public struct WarpPipeEnteredEvent
    {
        public string Label;
        public float  DestinationX;
    }

    /// <summary>Fired when the overworld map node state changes. Idea 1 (Systems Programmer).</summary>
    public struct NodeStateChangedEvent
    {
        public int World;
        public int Level;
        /// <summary>New state value: 0=locked, 1=open, 2=cleared, 3=starred.</summary>
        public int State;
    }

    /// <summary>Fired when a log book entry is unlocked. Idea 1 (Systems Programmer).</summary>
    public struct LogEntryUnlockedEvent
    {
        public string ChapterId;
    }

    /// <summary>Fired when the player respawns after death. Idea 1 (Systems Programmer).</summary>
    public struct PlayerRespawnEvent
    {
        public float X, Y;
        public int   LivesRemaining;
    }

    // ── Team 8 — Idea 2: typed deferred event queue ───────────────────────────

    /// <summary>
    /// Queues an event to be dispatched at the end of the current frame.
    /// Prevents handler re-entrancy during mid-update event chains.
    /// Team 8 (Systems Programmer) — Idea 2: deferred event dispatch.
    /// </summary>
    public static class DeferredEventQueue
    {
        private static readonly System.Collections.Generic.Queue<System.Action> _queue =
            new System.Collections.Generic.Queue<System.Action>();

        /// <summary>Schedules an event for deferred publication.</summary>
        public static void Enqueue<T>(T evt) where T : struct
        {
            var captured = evt;
            _queue.Enqueue(() => EventBus.Publish(captured));
        }

        /// <summary>
        /// Flushes all queued events in order.
        /// Call once per frame from the game loop after Update().
        /// </summary>
        public static void Flush()
        {
            while (_queue.Count > 0)
                _queue.Dequeue()?.Invoke();
        }
    }

    // ── Team 8 — Idea 3: Score manager ────────────────────────────────────────

    /// <summary>
    /// Centralised score manager with multiplier support.
    /// Team 8 (Systems Programmer) — Idea 3.
    /// </summary>
    public static class ScoreManager
    {
        /// <summary>Current session score.</summary>
        public static long Score { get; private set; }

        /// <summary>Active score multiplier (default 1.0; P-Meter charges this up).</summary>
        public static float Multiplier { get; private set; } = 1.0f;

        /// <summary>Adds <paramref name="base"/> points multiplied by the active multiplier.</summary>
        public static void Add(int @base)
        {
            long delta = (long)(@base * Multiplier);
            Score += delta;
            if (delta > 0)
                Engine.Game.Instance?.FloatingText?.Spawn(
                    $"+{delta}", 0, 0, Color.Gold, large: delta >= 1000);
        }

        /// <summary>Sets the score multiplier (capped at 4×).</summary>
        public static void SetMultiplier(float m)
        {
            Multiplier = Math.Max(1f, Math.Min(m, 4f));
        }

        /// <summary>Resets score and multiplier for a new game.</summary>
        public static void Reset() { Score = 0; Multiplier = 1f; }
    }

    // ── Team 8 — Idea 4: Hazard zone registry ────────────────────────────────

    /// <summary>
    /// Registry of active hazard rectangles (lava, spike pits, kill zones).
    /// Team 8 (Systems Programmer) — Idea 4.
    /// </summary>
    public static class HazardZoneRegistry
    {
        private static readonly System.Collections.Generic.List<System.Drawing.RectangleF> _zones =
            new System.Collections.Generic.List<System.Drawing.RectangleF>();

        /// <summary>Registers a new hazard zone. Clears on scene exit.</summary>
        public static void Register(System.Drawing.RectangleF rect) => _zones.Add(rect);

        /// <summary>Removes all hazard zones (call from scene OnExit).</summary>
        public static void Clear() => _zones.Clear();

        /// <summary>
        /// Returns true when <paramref name="rect"/> intersects any hazard zone.
        /// Team 8 (Systems Programmer) — Idea 4.
        /// </summary>
        public static bool IsInHazard(System.Drawing.RectangleF rect)
        {
            foreach (var z in _zones)
                if (z.IntersectsWith(rect)) return true;
            return false;
        }
    }

    // ── Team 8 — Idea 5: Debug overlay toggle system (F1–F12 layers) ─────────

    /// <summary>
    /// Manages which debug overlay layers are currently visible.
    /// Bind each F-key to its layer in the form's KeyDown handler.
    /// Team 8 (Systems Programmer) — Idea 5.
    /// </summary>
    public static class DebugOverlayToggles
    {
        private static readonly bool[] _layers = new bool[12];

        /// <summary>Toggles the overlay layer at index (0-based, so F1=0, F2=1 …).</summary>
        public static void Toggle(int fKeyIndex) { if (fKeyIndex >= 0 && fKeyIndex < 12) _layers[fKeyIndex] = !_layers[fKeyIndex]; }

        /// <summary>Returns true when the given F-key overlay layer is active.</summary>
        public static bool IsOn(int fKeyIndex) => fKeyIndex >= 0 && fKeyIndex < 12 && _layers[fKeyIndex];

        /// <summary>Convenience: F1 = hitboxes, F2 = fps, F3 = entities, F4 = collisions.</summary>
        public static bool ShowHitboxes     => IsOn(0) || GameConfig.IsDebugFlagSet("ShowHitboxes");
        public static bool ShowFps          => IsOn(1) || GameConfig.IsDebugFlagSet("ShowFps");
        public static bool ShowEntityCount  => IsOn(2) || GameConfig.IsDebugFlagSet("ShowEntityCount");
        public static bool ShowCollisions   => IsOn(3);
        public static bool ShowParticles    => IsOn(4);
        public static bool ShowAudio        => IsOn(5);
        public static bool ShowNarrative    => IsOn(6);
        public static bool ShowEventBus     => IsOn(7);
    }

    // ── Team 8 — Idea 6: Object pool stat helper ─────────────────────────────

    /// <summary>
    /// Named pool registry so scenes can register, retrieve, and report on all pools.
    /// Team 8 (Systems Programmer) — Idea 6.
    /// </summary>
    public static class PoolRegistry
    {
        private static readonly System.Collections.Generic.Dictionary<string, int> _stats =
            new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>Records a pool 'hit' (item reused from pool) for telemetry.</summary>
        public static void RecordHit(string poolName)
        {
            if (!_stats.ContainsKey(poolName)) _stats[poolName] = 0;
            _stats[poolName]++;
        }

        /// <summary>Returns a formatted report of pool usage this session.</summary>
        public static string GetReport()
        {
            var sb = new System.Text.StringBuilder("Pool Hits This Session:\n");
            foreach (var kv in _stats)
                sb.AppendLine($"  {kv.Key}: {kv.Value} reuses");
            return sb.ToString();
        }
    }

    // ── Team 8 — Idea 7: Debug label system ──────────────────────────────────

    /// <summary>
    /// Registers temporary one-frame debug labels drawn at world positions.
    /// Team 8 (Systems Programmer) — Idea 7.
    /// </summary>
    public static class DebugLabels
    {
        private struct Label { public float X, Y; public string Text; public System.Drawing.Color C; }
        private static readonly System.Collections.Generic.List<Label> _labels =
            new System.Collections.Generic.List<Label>(32);

        /// <summary>Queues a label to draw at the given world position this frame.</summary>
        public static void Add(float x, float y, string text, System.Drawing.Color color)
        {
            _labels.Add(new Label { X = x, Y = y, Text = text, C = color });
        }

        /// <summary>
        /// Draws all queued labels at screen coordinates (world - camX) and clears the list.
        /// </summary>
        public static void DrawAndClear(System.Drawing.Graphics g, float camX)
        {
            using (var f = new System.Drawing.Font("Courier New", 7))
            {
                foreach (var l in _labels)
                    g.DrawString(l.Text, f, new System.Drawing.SolidBrush(l.C), l.X - camX, l.Y);
            }
            _labels.Clear();
        }
    }

    // ── Team 8 — Idea 8: Telemetry sampler (fires every N frames) ────────────

    /// <summary>
    /// Low-overhead periodic sampler: runs a callback every <c>intervalFrames</c> updates.
    /// Team 8 (Systems Programmer) — Idea 8.
    /// </summary>
    public sealed class PeriodicSampler
    {
        private readonly int    _interval;
        private readonly Action _action;
        private int             _frame;

        /// <param name="intervalFrames">Callback fires every this many frames.</param>
        /// <param name="action">Work to perform on each sample tick.</param>
        public PeriodicSampler(int intervalFrames, Action action)
        {
            _interval = Math.Max(1, intervalFrames);
            _action   = action;
        }

        /// <summary>Advances the counter; calls the action when interval is reached.</summary>
        public void Tick()
        {
            _frame = (_frame + 1) % _interval;
            if (_frame == 0) _action?.Invoke();
        }
    }

    // ── Team 8 — Idea 9: Scene object counter ────────────────────────────────

    /// <summary>
    /// Tracks entity counts per category for performance and design metrics.
    /// Team 8 (Systems Programmer) — Idea 9.
    /// </summary>
    public static class SceneObjectCounter
    {
        private static readonly System.Collections.Generic.Dictionary<string, int> _counts =
            new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>Reports one live entity of the given category.</summary>
        public static void Report(string category)
        {
            if (!_counts.ContainsKey(category)) _counts[category] = 0;
            _counts[category]++;
        }

        /// <summary>Resets all counts (call at the start of each Draw() pass).</summary>
        public static void Clear() => _counts.Clear();

        /// <summary>Returns the current count for the given category.</summary>
        public static int Get(string category) =>
            _counts.ContainsKey(category) ? _counts[category] : 0;

        /// <summary>Returns a formatted summary string for the debug overlay.</summary>
        public static string GetSummary()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kv in _counts) sb.Append($"{kv.Key}:{kv.Value}  ");
            return sb.ToString().TrimEnd();
        }
    }

    // ── Team 8 — Idea 10: Replay event log ───────────────────────────────────

    /// <summary>
    /// Lightweight ordered log of gameplay events for QA session replay export.
    /// Team 8 (Systems Programmer) — Idea 10.
    /// </summary>
    public static class ReplayLog
    {
        private static readonly System.Collections.Generic.List<string> _lines =
            new System.Collections.Generic.List<string>(512);

        /// <summary>Records a timestamped event string.</summary>
        public static void Record(string category, string details)
        {
            _lines.Add($"{DateTime.Now:HH:mm:ss.fff}|{category}|{details}");
        }

        /// <summary>Returns all recorded event lines.</summary>
        public static System.Collections.Generic.IReadOnlyList<string> Lines => _lines;

        /// <summary>
        /// Exports the replay log to a CSV file alongside the session error log.
        /// Team 8 (Systems Programmer) — Idea 10.
        /// </summary>
        public static void ExportCsv()
        {
            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Logs",
                $"replay-{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            var sb = new System.Text.StringBuilder("timestamp,category,details\n");
            foreach (var l in _lines)
            {
                var parts = l.Split('|');
                sb.AppendLine(string.Join(",", parts));
            }
            System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
            DebugLogger.LogInfo("ReplayLog", $"Exported to {path}");
        }

        /// <summary>Clears the log (call on game start / new session).</summary>
        public static void Clear() => _lines.Clear();
    }
}
