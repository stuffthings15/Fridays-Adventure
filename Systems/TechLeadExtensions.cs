using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  TechLeadExtensions.cs  —  Technical Lead: 10 NEW ideas
    //
    //  Idea 1:  Frame-time histogram — buckets frame durations for spike analysis.
    //  Idea 2:  Assertion framework — Dev-mode contract checking with rich messages.
    //  Idea 3:  Null guard — fluent null-check helpers to reduce NullReferenceErrors.
    //  Idea 4:  Hot-reload config — watches a JSON config file and reloads on change.
    //  Idea 5:  Memory leak detector — periodic GC-usage snapshots logged as warnings.
    //  Idea 6:  GC pressure reducer — object pool for commonly allocated structs.
    //  Idea 7:  Shutdown hook queue — ordered cleanup callbacks for graceful exit.
    //  Idea 8:  Performance budget — per-system CPU budget with overflow warnings.
    //  Idea 9:  Stack-overflow guard — wraps risky recursive calls with depth limit.
    //  Idea 10: Unit test stubs — minimal in-process test runner for critical paths.
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Idea 1: Frame-time histogram ──────────────────────────────────────────
    /// <summary>
    /// Records per-frame delta-time values into histogram buckets.
    /// Bucket ranges (ms): 0–8, 8–16, 16–33, 33–66, 66+.
    /// Use in the dev menu or QA report to detect frame-time spikes.
    /// Team 3 (Technical Lead) — Idea 1.
    /// </summary>
    public static class FrameTimeHistogram
    {
        // Bucket edges in milliseconds.
        private static readonly float[] _edges    = { 8f, 16f, 33f, 66f };
        private static readonly string[] _labels  = { "0–8ms", "8–16ms", "16–33ms", "33–66ms", "66+ms" };
        private static readonly int[]    _buckets = new int[5];
        private static int  _totalFrames;
        private static float _worstMs;

        /// <summary>Records a frame delta. Call once per game tick.</summary>
        public static void RecordFrame(float dt)
        {
            float ms = dt * 1000f;
            _totalFrames++;
            if (ms > _worstMs) _worstMs = ms;

            int b = _buckets.Length - 1;
            for (int i = 0; i < _edges.Length; i++)
                if (ms < _edges[i]) { b = i; break; }
            _buckets[b]++;
        }

        /// <summary>Returns a formatted histogram summary string.</summary>
        public static string GetSummary()
        {
            var sb = new StringBuilder();
            sb.Append($"FrameTime Histogram  (total:{_totalFrames}  worst:{_worstMs:F1}ms)\n");
            for (int i = 0; i < _buckets.Length; i++)
            {
                float pct = _totalFrames > 0 ? _buckets[i] * 100f / _totalFrames : 0f;
                sb.Append($"  {_labels[i],-10}: {_buckets[i],5} frames  ({pct:F1}%)\n");
            }
            return sb.ToString();
        }

        /// <summary>Resets all buckets.</summary>
        public static void Reset()
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            _totalFrames = 0;
            _worstMs = 0f;
        }
    }

    // ── Idea 2: Assertion framework ───────────────────────────────────────────
    /// <summary>
    /// Dev-mode contract assertions.  In DEBUG builds a failed assertion logs an
    /// ERROR and (optionally) throws; in RELEASE builds it is a no-op.
    /// Team 3 (Technical Lead) — Idea 2.
    /// </summary>
    public static class Asserter
    {
        /// <summary>
        /// When true (default), failed assertions throw InvalidOperationException.
        /// Set to false during QA sessions to log-only without crashing.
        /// </summary>
        public static bool ThrowOnFailure { get; set; } = true;

        /// <summary>
        /// Asserts that <paramref name="condition"/> is true.
        /// Team 3 (Technical Lead) — Idea 2.
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void IsTrue(bool condition, string context, string message = "Assertion failed")
        {
            if (condition) return;
            string full = $"[ASSERT] {context}: {message}";
            DebugLogger.LogError(context, full);
            if (ThrowOnFailure)
                throw new InvalidOperationException(full);
        }

        /// <summary>Asserts that <paramref name="obj"/> is not null.</summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void NotNull(object obj, string context, string paramName = "value")
        {
            if (obj != null) return;
            string msg = $"{paramName} must not be null";
            DebugLogger.LogError(context, $"[ASSERT NULL] {msg}");
            if (ThrowOnFailure)
                throw new ArgumentNullException(paramName, $"[{context}] {msg}");
        }

        /// <summary>Asserts that a numeric value is within [min, max].</summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void InRange(float value, float min, float max,
            string context, string paramName = "value")
        {
            if (value >= min && value <= max) return;
            string msg = $"{paramName}={value:F3} out of range [{min},{max}]";
            DebugLogger.LogError(context, $"[ASSERT RANGE] {msg}");
            if (ThrowOnFailure)
                throw new ArgumentOutOfRangeException(paramName, value, $"[{context}] {msg}");
        }
    }

    // ── Idea 3: Null guard ────────────────────────────────────────────────────
    /// <summary>
    /// Fluent null-check helpers that log a warning and return a safe fallback
    /// instead of throwing NullReferenceException in production.
    /// Team 3 (Technical Lead) — Idea 3.
    /// </summary>
    public static class NullGuard
    {
        /// <summary>
        /// Returns <paramref name="obj"/> if not null; otherwise logs a warning
        /// and returns <paramref name="fallback"/>.
        /// </summary>
        public static T OrDefault<T>(T obj, T fallback, string context, string paramName)
        {
            if (obj != null) return obj;
            DebugLogger.LogWarning(context, $"NullGuard: {paramName} was null — using fallback.");
            return fallback;
        }

        /// <summary>
        /// Invokes <paramref name="action"/> only if <paramref name="obj"/> is not null;
        /// otherwise logs a warning.
        /// </summary>
        public static void IfNotNull<T>(T obj, Action<T> action, string context, string paramName)
            where T : class
        {
            if (obj != null) { action(obj); return; }
            DebugLogger.LogWarning(context, $"NullGuard: skipped action on null {paramName}.");
        }
    }

    // ── Idea 4: Hot-reload config watcher ─────────────────────────────────────
    /// <summary>
    /// Watches <c>Assets\game-config.txt</c> for changes and fires
    /// <see cref="ConfigReloadedEvent"/> via the EventBus when the file changes.
    /// Team 3 (Technical Lead) — Idea 4.
    /// </summary>
    public static class HotReloadConfig
    {
        private static FileSystemWatcher _watcher;
        private static volatile bool     _pendingReload;

        /// <summary>
        /// Starts watching the config file for changes.
        /// Team 3 (Technical Lead) — Idea 4.
        /// </summary>
        public static void StartWatching()
        {
            string dir  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            string file = "game-config.txt";
            if (!Directory.Exists(dir)) return;
            try
            {
                _watcher = new FileSystemWatcher(dir, file)
                {
                    NotifyFilter = NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };
                _watcher.Changed += (s, e) => _pendingReload = true;
                DebugLogger.LogInfo("HotReloadConfig", "Watching Assets\\game-config.txt for changes.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogWarning("HotReloadConfig.StartWatching", ex.Message);
            }
        }

        /// <summary>
        /// Checks for a pending reload and fires the event if needed.
        /// Call from the game tick loop once per frame.
        /// Team 3 (Technical Lead) — Idea 4.
        /// </summary>
        public static void Tick()
        {
            if (!_pendingReload) return;
            _pendingReload = false;
            GameConfig.Reload();
            EventBus.Publish(new ConfigReloadedEvent());
            DebugLogger.LogInfo("HotReloadConfig", "Config reloaded.");
        }

        /// <summary>Stops watching the config file.</summary>
        public static void Stop() { _watcher?.Dispose(); _watcher = null; }
    }

    /// <summary>Published when the hot-reload config watcher detects a file change.</summary>
    public sealed class ConfigReloadedEvent { }

    // ── Idea 5: Memory leak detector ──────────────────────────────────────────
    /// <summary>
    /// Periodically records managed memory usage and logs a WARNING when memory
    /// grows by more than <see cref="GrowthThresholdMB"/> MB between samples.
    /// Team 3 (Technical Lead) — Idea 5.
    /// </summary>
    public static class MemoryLeakDetector
    {
        private static long  _lastSnapshot;
        private static float _timer;
        private const  float SampleInterval      = 30f;   // seconds
        private const  float GrowthThresholdMB   = 50f;   // MB growth before warning

        /// <summary>Tick from game loop.</summary>
        public static void Tick(float dt)
        {
            _timer += dt;
            if (_timer < SampleInterval) return;
            _timer = 0f;

            long now = GC.GetTotalMemory(false);
            if (_lastSnapshot > 0)
            {
                float growthMB = (now - _lastSnapshot) / (1024f * 1024f);
                if (growthMB > GrowthThresholdMB)
                {
                    DebugLogger.LogWarning("MemoryLeakDetector",
                        $"Memory grew by {growthMB:F1} MB in {SampleInterval}s " +
                        $"(now {now / (1024f * 1024f):F1} MB). Possible leak.");
                }
                else
                {
                    DebugLogger.LogInfo("MemoryLeakDetector",
                        $"Memory snapshot: {now / (1024f * 1024f):F1} MB (Δ{growthMB:F1} MB)");
                }
            }
            _lastSnapshot = now;
        }
    }

    // ── Idea 6: GC pressure reducer ───────────────────────────────────────────
    /// <summary>
    /// A thread-safe object pool for commonly-allocated types to reduce GC pressure.
    /// Team 3 (Technical Lead) — Idea 6.
    /// </summary>
    public sealed class GcPool<T> where T : class, new()
    {
        private readonly Stack<T>    _pool    = new Stack<T>();
        private readonly int         _maxSize;
        private readonly Action<T>   _reset;

        /// <param name="maxSize">Max objects held in the pool.</param>
        /// <param name="reset">Action applied to reset an object before re-use.</param>
        public GcPool(int maxSize = 64, Action<T> reset = null)
        {
            _maxSize = maxSize;
            _reset   = reset;
        }

        /// <summary>Borrows an object from the pool (or creates a new one).</summary>
        public T Borrow()
        {
            lock (_pool) return _pool.Count > 0 ? _pool.Pop() : new T();
        }

        /// <summary>Returns an object to the pool.</summary>
        public void Return(T obj)
        {
            if (obj == null) return;
            _reset?.Invoke(obj);
            lock (_pool)
            {
                if (_pool.Count < _maxSize)
                    _pool.Push(obj);
            }
        }

        /// <summary>Current pool depth.</summary>
        public int Depth { get { lock (_pool) return _pool.Count; } }
    }

    // ── Idea 7: Shutdown hook queue ───────────────────────────────────────────
    /// <summary>
    /// Ordered list of callbacks executed when <see cref="RunAll"/> is called
    /// at application exit.  Register sub-system cleanup here so each system
    /// doesn't need direct teardown calls in Form1.
    /// Team 3 (Technical Lead) — Idea 7.
    /// </summary>
    public static class ShutdownHooks
    {
        private static readonly List<(string Name, Action Callback)> _hooks =
            new List<(string, Action)>();

        /// <summary>Registers a named shutdown callback.</summary>
        public static void Register(string name, Action callback)
        {
            _hooks.Add((name, callback));
            DebugLogger.LogDebug("ShutdownHooks", $"Registered: {name}");
        }

        /// <summary>Executes all registered hooks in registration order.</summary>
        public static void RunAll()
        {
            foreach (var (name, cb) in _hooks)
            {
                try { cb(); DebugLogger.LogInfo("ShutdownHooks", $"OK: {name}"); }
                catch (Exception ex) { DebugLogger.LogError($"ShutdownHooks.{name}", ex); }
            }
        }
    }

    // ── Idea 8: Performance budget ────────────────────────────────────────────
    /// <summary>
    /// Tracks per-system CPU time budgets.  When a system exceeds its budget,
    /// a WARNING is logged and the overflow is accumulated for the QA report.
    /// Team 3 (Technical Lead) — Idea 8.
    /// </summary>
    public static class PerformanceBudget
    {
        private static readonly Dictionary<string, float> _budgets  = new Dictionary<string, float>();
        private static readonly Dictionary<string, float> _overflows = new Dictionary<string, float>();
        private static long _lastTicks;

        /// <summary>Sets a CPU budget for a named system (in ms per frame).</summary>
        public static void SetBudget(string system, float ms) => _budgets[system] = ms;

        /// <summary>
        /// Measures elapsed time since the last call and checks against the budget.
        /// Call BeginMeasure before a system update and EndMeasure after.
        /// </summary>
        public static long BeginMeasure()
        {
            return _lastTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        }

        /// <summary>Ends measurement for a named system, logs overflow if over budget.</summary>
        public static float EndMeasure(string system, long startTicks)
        {
            float ms = (System.Diagnostics.Stopwatch.GetTimestamp() - startTicks) * 1000f
                       / System.Diagnostics.Stopwatch.Frequency;
            if (_budgets.TryGetValue(system, out float budget) && ms > budget)
            {
                float over = ms - budget;
                _overflows[system] = (_overflows.TryGetValue(system, out float existing) ? existing : 0f) + over;
                DebugLogger.LogWarning("PerformanceBudget",
                    $"{system} exceeded budget by {over:F2}ms ({ms:F2}ms / {budget:F2}ms)");
            }
            return ms;
        }

        /// <summary>Returns total overflow (ms) for a system this session.</summary>
        public static float GetTotalOverflow(string system) =>
            _overflows.TryGetValue(system, out float v) ? v : 0f;
    }

    // ── Idea 9: Stack-overflow guard ──────────────────────────────────────────
    /// <summary>
    /// Wraps recursive calls with a depth counter, aborting and logging an ERROR
    /// when the call depth exceeds <see cref="MaxDepth"/>.
    /// Team 3 (Technical Lead) — Idea 9.
    /// </summary>
    public static class RecursionGuard
    {
        private static readonly ThreadLocal<int> _depth = new ThreadLocal<int>(() => 0);

        /// <summary>Maximum recursion depth before aborting.</summary>
        public const int MaxDepth = 64;

        /// <summary>
        /// Executes <paramref name="action"/> only if the current call depth is below
        /// <see cref="MaxDepth"/>.  Returns false if the guard tripped.
        /// Team 3 (Technical Lead) — Idea 9.
        /// </summary>
        public static bool Execute(string context, Action action)
        {
            if (_depth.Value >= MaxDepth)
            {
                DebugLogger.LogError("RecursionGuard",
                    $"Recursion limit ({MaxDepth}) reached at [{context}]. Aborting call.");
                return false;
            }
            _depth.Value++;
            try { action(); return true; }
            finally { _depth.Value--; }
        }
    }

    // ── Idea 10: In-process unit test stubs ───────────────────────────────────
    /// <summary>
    /// Minimal in-process test runner for critical-path functions.
    /// Register test cases with <see cref="Register"/> and run them from the
    /// dev menu or on startup with <see cref="RunAll"/>.
    /// Team 3 (Technical Lead) — Idea 10.
    /// </summary>
    public static class InProcessTests
    {
        private static readonly List<(string Name, Func<bool> Test)> _tests =
            new List<(string, Func<bool>)>();

        /// <summary>Registers a named test function that returns true on pass.</summary>
        public static void Register(string name, Func<bool> test) =>
            _tests.Add((name, test));

        /// <summary>
        /// Runs all registered tests and logs results.
        /// Returns the number of failures.
        /// Team 3 (Technical Lead) — Idea 10.
        /// </summary>
        public static int RunAll()
        {
            int failures = 0;
            DebugLogger.LogInfo("InProcessTests", $"Running {_tests.Count} tests...");
            foreach (var (name, test) in _tests)
            {
                try
                {
                    bool pass = test();
                    if (pass)
                        DebugLogger.LogInfo("InProcessTests", $"  PASS: {name}");
                    else
                    {
                        DebugLogger.LogError("InProcessTests", $"  FAIL: {name}");
                        failures++;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("InProcessTests", $"  EXCEPTION: {name} — {ex.Message}");
                    failures++;
                }
            }
            DebugLogger.LogInfo("InProcessTests",
                $"Done: {_tests.Count - failures} passed, {failures} failed.");
            return failures;
        }
    }
}
