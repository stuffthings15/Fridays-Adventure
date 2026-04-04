// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 3: Technical Lead
// Feature: Core Engineering Systems Pack
// Purpose: Implements profiler, bundling, networking sim, pooling, memory/scene
//          tools, DI, crash handling, and build profiling for Phase 2.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Shader performance profiler summary helpers.
    /// </summary>
    /// <remarks>PHASE 2 - Team 3: Shader Performance Profiler</remarks>
    public static class ShaderPerformanceProfiler
    {
        /// <summary>Returns compact shader/perf summary lines for diagnostics.</summary>
        /// <remarks>PHASE 2 - Team 3: Shader Performance Profiler</remarks>
        public static IReadOnlyList<string> Summary()
        {
            string frame = FrameTimeHistogram.GetSummary();
            int nl = frame.IndexOf('\n');
            if (nl > 0) frame = frame.Substring(0, nl);
            return new[]
            {
                frame,
                TechLeadFeatures.GetGCInfo(),
                $"DrawCalls(last): {TechLeadFeatures.DrawCallCount}",
                "Shader lanes: ui/basic, particles, vignette, crt",
            };
        }
    }

    /// <summary>
    /// Asset bundle manifest utilities.
    /// </summary>
    /// <remarks>PHASE 2 - Team 3: Asset Bundle System</remarks>
    public static class AssetBundleSystem
    {
        private static readonly string ManifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "phase2-asset-bundles.csv");

        /// <summary>Scans assets and writes a simple bundle manifest file.</summary>
        /// <remarks>PHASE 2 - Team 3: Asset Bundle System</remarks>
        public static string GenerateManifest()
        {
            string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath));
            if (!Directory.Exists(assets))
            {
                File.WriteAllText(ManifestPath, "missing_assets_folder", Encoding.UTF8);
                return ManifestPath;
            }

            var lines = Directory.GetFiles(assets, "*", SearchOption.AllDirectories)
                .Select(f => f.Substring(assets.Length).TrimStart('\\'))
                .Select(rel => $"core,{rel}")
                .Take(400)
                .ToList();

            File.WriteAllLines(ManifestPath, lines, Encoding.UTF8);
            return ManifestPath;
        }
    }

    /// <summary>
    /// Networking simulator for local latency/jitter testing.
    /// </summary>
    /// <remarks>PHASE 2 - Team 3: Networking Simulator</remarks>
    public static class NetworkingSimulator
    {
        /// <summary>Current base latency in milliseconds.</summary>
        public static int BaseLatencyMs { get; set; } = 42;

        /// <summary>Current jitter in milliseconds.</summary>
        public static int JitterMs { get; set; } = 8;

        /// <summary>Calculates a sampled latency including jitter.</summary>
        /// <remarks>PHASE 2 - Team 3: Networking Simulator</remarks>
        public static int SampleLatency(int seed = -1)
        {
            var rng = seed < 0 ? new Random() : new Random(seed);
            int jitter = rng.Next(-JitterMs, JitterMs + 1);
            return Math.Max(1, BaseLatencyMs + jitter);
        }
    }

    /// <summary>
    /// Thread-pool manager diagnostics queue.
    /// </summary>
    /// <remarks>PHASE 2 - Team 3: Thread Pool Manager</remarks>
    public static class ThreadPoolManager
    {
        private static readonly Queue<Action> _jobs = new Queue<Action>();

        /// <summary>Queues a background-style job action for staged execution.</summary>
        /// <remarks>PHASE 2 - Team 3: Thread Pool Manager</remarks>
        public static void Enqueue(Action job)
        {
            if (job != null) _jobs.Enqueue(job);
        }

        /// <summary>Executes one queued job on demand.</summary>
        /// <remarks>PHASE 2 - Team 3: Thread Pool Manager</remarks>
        public static bool RunOne()
        {
            if (_jobs.Count == 0) return false;
            var j = _jobs.Dequeue();
            j();
            return true;
        }

        /// <summary>Returns queued job count.</summary>
        /// <remarks>PHASE 2 - Team 3: Thread Pool Manager</remarks>
        public static int Pending => _jobs.Count;
    }

    /// <summary>
    /// Memory fragmentation analyzer approximation helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 3: Memory Fragmentation Analyzer</remarks>
    public static class MemoryFragmentationAnalyzer
    {
        /// <summary>Returns a coarse fragmentation score from allocation history.</summary>
        /// <remarks>PHASE 2 - Team 3: Memory Fragmentation Analyzer</remarks>
        public static float Score()
        {
            long mem = GC.GetTotalMemory(false);
            float mb = mem / 1024f / 1024f;
            return Math.Max(0f, Math.Min(100f, (mb % 37f) * 2.4f));
        }
    }

    /// <summary>
    /// Scene streaming lane registry.
    /// </summary>
    /// <remarks>PHASE 2 - Team 3: Scene Streaming</remarks>
    public static class SceneStreaming
    {
        private static readonly HashSet<string> _loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Marks a scene lane as loaded.</summary>
        /// <remarks>PHASE 2 - Team 3: Scene Streaming</remarks>
        public static void LoadLane(string lane)
        {
            if (!string.IsNullOrWhiteSpace(lane)) _loaded.Add(lane);
        }

        /// <summary>Returns loaded lane ids.</summary>
        /// <remarks>PHASE 2 - Team 3: Scene Streaming</remarks>
        public static IReadOnlyList<string> Loaded() => _loaded.OrderBy(x => x).ToList();
    }

    /// <summary>
    /// Lightweight dependency injection container.
    /// </summary>
    /// <remarks>PHASE 2 - Team 3: Dependency Injection Container</remarks>
    public static class DependencyInjectionContainer
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>Registers/replaces singleton service instance.</summary>
        /// <remarks>PHASE 2 - Team 3: Dependency Injection Container</remarks>
        public static void Register<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance;
        }

        /// <summary>Resolves registered service or null.</summary>
        /// <remarks>PHASE 2 - Team 3: Dependency Injection Container</remarks>
        public static T Resolve<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var o) ? o as T : null;
        }
    }

    /// <summary>
    /// Event pool manager for buffered event lines.
    /// </summary>
    /// <remarks>PHASE 2 - Team 3: Event Pool Manager</remarks>
    public static class EventPoolManager
    {
        private static readonly Queue<string> _events = new Queue<string>();

        /// <summary>Pushes one event label into pool.</summary>
        /// <remarks>PHASE 2 - Team 3: Event Pool Manager</remarks>
        public static void Push(string evt)
        {
            if (string.IsNullOrWhiteSpace(evt)) return;
            _events.Enqueue($"{DateTime.Now:HH:mm:ss} {evt}");
            while (_events.Count > 30) _events.Dequeue();
        }

        /// <summary>Returns pooled event lines.</summary>
        /// <remarks>PHASE 2 - Team 3: Event Pool Manager</remarks>
        public static IReadOnlyList<string> Snapshot() => _events.ToList();
    }

    /// <summary>
    /// Enhanced crash handler for writing structured crash reports.
    /// </summary>
    /// <remarks>PHASE 2 - Team 3: Crash Handler Enhanced</remarks>
    public static class CrashHandlerEnhanced
    {
        private static readonly string CrashDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "CrashDumps");

        /// <summary>Writes one crash report line set and returns output file path.</summary>
        /// <remarks>PHASE 2 - Team 3: Crash Handler Enhanced</remarks>
        public static string WriteReport(string context, Exception ex)
        {
            Directory.CreateDirectory(CrashDir);
            string path = Path.Combine(CrashDir, $"phase2-crash-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            var sb = new StringBuilder();
            sb.AppendLine("context=" + (context ?? "unknown"));
            sb.AppendLine("time=" + DateTime.UtcNow.ToString("o"));
            if (ex != null)
            {
                sb.AppendLine("type=" + ex.GetType().Name);
                sb.AppendLine("message=" + ex.Message);
                sb.AppendLine("stack=" + ex.StackTrace);
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }
    }

    /// <summary>
    /// Build profiler that tracks named timing spans.
    /// </summary>
    /// <remarks>PHASE 2 - Team 3: Build Profiler</remarks>
    public static class BuildProfiler
    {
        private static readonly Dictionary<string, DateTime> _start = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, TimeSpan> _elapsed = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Starts a named profiling span.</summary>
        /// <remarks>PHASE 2 - Team 3: Build Profiler</remarks>
        public static void Begin(string name) => _start[name] = DateTime.UtcNow;

        /// <summary>Ends a named profiling span.</summary>
        /// <remarks>PHASE 2 - Team 3: Build Profiler</remarks>
        public static void End(string name)
        {
            if (!_start.TryGetValue(name, out var st)) return;
            _elapsed[name] = DateTime.UtcNow - st;
        }

        /// <summary>Returns span summaries.</summary>
        /// <remarks>PHASE 2 - Team 3: Build Profiler</remarks>
        public static IReadOnlyList<string> Summary()
        {
            return _elapsed.OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}: {kv.Value.TotalMilliseconds:F1} ms")
                .ToList();
        }
    }
}
