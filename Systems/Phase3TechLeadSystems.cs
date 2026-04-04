// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 3: Technical Lead
// Feature: Wave 1 Tech Foundation Systems
// Purpose: Modding framework, server architecture, analytics, performance
//          suite, patch distribution, and anti-cheat baseline services.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>Phase 3 Team 3 Idea 1: Minimal mod package descriptor.</summary>
    public sealed class ModPackage
    {
        /// <summary>Unique package ID.</summary>
        public string Id { get; set; }
        /// <summary>Display package name.</summary>
        public string Name { get; set; }
        /// <summary>Package version string.</summary>
        public string Version { get; set; }
        /// <summary>Absolute descriptor path.</summary>
        public string DescriptorPath { get; set; }
        /// <summary>Whether the descriptor passed baseline validation.</summary>
        public bool Valid { get; set; }
    }

    /// <summary>
    /// Team 3 Idea 1: Modding Framework.
    /// Discovers and validates mod descriptors from the <c>Mods</c> directory.
    /// </summary>
    public static class ModdingFramework
    {
        private static readonly string ModsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");

        /// <summary>Discovers all mod packages and validates required fields.</summary>
        public static IReadOnlyList<ModPackage> DiscoverPackages()
        {
            Directory.CreateDirectory(ModsDir);
            var packages = new List<ModPackage>();

            foreach (string file in Directory.GetFiles(ModsDir, "*.modinfo", SearchOption.TopDirectoryOnly))
            {
                var map = Parse(file);
                string id = map.ContainsKey("id") ? map["id"] : Path.GetFileNameWithoutExtension(file);
                string name = map.ContainsKey("name") ? map["name"] : id;
                string version = map.ContainsKey("version") ? map["version"] : "1.0.0";
                bool valid = !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name);

                packages.Add(new ModPackage
                {
                    Id = id,
                    Name = name,
                    Version = version,
                    DescriptorPath = file,
                    Valid = valid,
                });
            }

            return packages.OrderBy(p => p.Name).ToList();
        }

        private static Dictionary<string, string> Parse(string path)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int idx = line.IndexOf('=');
                if (idx <= 0) continue;
                map[line.Substring(0, idx).Trim()] = line.Substring(idx + 1).Trim();
            }
            return map;
        }
    }

    /// <summary>
    /// Team 3 Idea 3: Server Architecture simulation.
    /// Provides a local node registry and region-aware node selection.
    /// </summary>
    public static class ServerArchitecture
    {
        /// <summary>Represents one server node in the topology model.</summary>
        public sealed class Node
        {
            /// <summary>Node label.</summary>
            public string Name { get; set; }
            /// <summary>Region identifier.</summary>
            public string Region { get; set; }
            /// <summary>Observed latency in ms.</summary>
            public int LatencyMs { get; set; }
            /// <summary>Online state.</summary>
            public bool Online { get; set; }
        }

        private static readonly List<Node> _nodes = new List<Node>
        {
            new Node { Name = "friday-edge-east", Region = "us-east", LatencyMs = 42, Online = true },
            new Node { Name = "friday-edge-west", Region = "us-west", LatencyMs = 58, Online = true },
            new Node { Name = "friday-edge-eu",   Region = "eu",      LatencyMs = 94, Online = true },
        };

        /// <summary>Returns all server nodes.</summary>
        public static IReadOnlyList<Node> GetNodes() => _nodes;

        /// <summary>Returns the best available node for a region hint.</summary>
        public static Node SelectBest(string regionHint)
        {
            var candidates = _nodes.Where(n => n.Online);
            if (!string.IsNullOrWhiteSpace(regionHint))
            {
                var regional = candidates.Where(n => n.Region.Equals(regionHint, StringComparison.OrdinalIgnoreCase)).ToList();
                if (regional.Count > 0) return regional.OrderBy(n => n.LatencyMs).First();
            }
            return candidates.OrderBy(n => n.LatencyMs).FirstOrDefault();
        }
    }

    /// <summary>
    /// Team 3 Idea 4: Data Analytics Pipeline.
    /// Buffers analytics events and flushes to NDJSON for offline processing.
    /// </summary>
    public static class DataAnalyticsPipeline
    {
        private static readonly string AnalyticsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "phase3-analytics.ndjson");
        private static readonly List<string> _buffer = new List<string>(128);

        /// <summary>Queues one analytics event for pipeline flush.</summary>
        public static void Enqueue(string eventName, string payload)
        {
            string safeName = (eventName ?? "event").Replace('"', '\'');
            string safePayload = (payload ?? string.Empty).Replace('"', '\'').Replace('\n', ' ');
            _buffer.Add($"{{\"ts\":\"{DateTime.UtcNow:o}\",\"event\":\"{safeName}\",\"payload\":\"{safePayload}\"}}");
        }

        /// <summary>Writes buffered events to disk and clears memory queue.</summary>
        public static int Flush()
        {
            if (_buffer.Count == 0) return 0;
            Directory.CreateDirectory(Path.GetDirectoryName(AnalyticsPath));
            File.AppendAllLines(AnalyticsPath, _buffer, Encoding.UTF8);
            int count = _buffer.Count;
            _buffer.Clear();
            DebugLogger.LogInfo("DataAnalyticsPipeline", $"Flushed {count} analytics events.");
            return count;
        }
    }

    /// <summary>
    /// Team 3 Idea 5: Performance Optimization Suite.
    /// Creates compact runtime snapshots for diagnostics and trend tracking.
    /// </summary>
    public static class PerformanceOptimizationSuite
    {
        /// <summary>Returns a diagnostics summary for current runtime state.</summary>
        public static IReadOnlyList<string> Snapshot()
        {
            long heap = GC.GetTotalMemory(false);
            string frame = FrameTimeHistogram.GetSummary();
            int idx = frame.IndexOf('\n');
            if (idx > 0) frame = frame.Substring(0, idx);

            return new[]
            {
                $"Heap: {heap / 1024.0 / 1024.0:F2} MB",
                TechLeadFeatures.GetGCInfo(),
                frame,
                $"Draw calls (last): {TechLeadFeatures.DrawCallCount}",
            };
        }
    }

    /// <summary>
    /// Team 3 Idea 8: Patch Distribution System.
    /// Scans patch packages from <c>Assets\Patches</c> and marks applied state.
    /// </summary>
    public static class PatchDistributionSystem
    {
        private static readonly string PatchesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Patches");
        private static readonly string AppliedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "phase3-applied-patches.log");

        /// <summary>Returns discoverable patch package names.</summary>
        public static IReadOnlyList<string> Discover()
        {
            Directory.CreateDirectory(PatchesDir);
            EnsureSamplePatch();
            return Directory.GetFiles(PatchesDir, "*.patchpkg", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(x => x)
                .ToList();
        }

        /// <summary>Marks a patch package as applied in local state log.</summary>
        public static void MarkApplied(string patchName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AppliedPath));
            File.AppendAllText(AppliedPath, $"{DateTime.UtcNow:o}|{patchName}{Environment.NewLine}", Encoding.UTF8);
            DebugLogger.LogInfo("PatchDistributionSystem", $"Applied patch marker: {patchName}");
        }

        /// <summary>Returns names of previously marked-applied patch packages.</summary>
        public static IReadOnlyList<string> GetApplied()
        {
            if (!File.Exists(AppliedPath)) return new List<string>();
            return File.ReadAllLines(AppliedPath, Encoding.UTF8)
                .Select(l => l.Split('|'))
                .Where(p => p.Length >= 2)
                .Select(p => p[1])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
        }

        private static void EnsureSamplePatch()
        {
            string sample = Path.Combine(PatchesDir, "wave1-bootstrap.patchpkg");
            if (!File.Exists(sample))
                File.WriteAllText(sample, "phase3_patch=true", Encoding.UTF8);
        }
    }

    /// <summary>
    /// Team 3 Idea 9: Anti-Cheat Framework baseline.
    /// Performs lightweight runtime sanity checks for impossible values.
    /// </summary>
    public static class AntiCheatFramework
    {
        /// <summary>Runs validation checks and returns warning lines for suspicious state.</summary>
        public static IReadOnlyList<string> RunChecks()
        {
            var list = new List<string>();
            var game = Engine.Game.Instance;
            if (game == null) return new[] { "No active game instance." };

            if (game.PlayerBounty < 0) list.Add("Suspicious: negative bounty value.");
            if (game.CrewBonds < 0) list.Add("Suspicious: negative crew bonds.");
            if (game.CurrentLives > 99) list.Add("Suspicious: lives exceed expected cap.");
            if (game.ThreatLevel < 0f || game.ThreatLevel > 5f) list.Add("Suspicious: threat level out of expected range.");

            if (list.Count == 0) list.Add("No anti-cheat anomalies detected.");
            return list;
        }
    }

    /// <summary>
    /// Team 3 Idea 2: Cross-Platform Sync.
    /// Saves and loads a portable runtime snapshot payload.
    /// </summary>
    public static class CrossPlatformSync
    {
        private static readonly string SyncPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "phase3-sync.snapshot");

        /// <summary>Exports a minimal portable snapshot for sync handoff.</summary>
        public static string ExportSnapshot()
        {
            var game = Engine.Game.Instance;
            if (game == null) return null;

            Directory.CreateDirectory(Path.GetDirectoryName(SyncPath));
            var sb = new StringBuilder();
            sb.AppendLine($"ts={DateTime.UtcNow:o}");
            sb.AppendLine($"playerName={game.PlayerName}");
            sb.AppendLine($"bounty={game.PlayerBounty}");
            sb.AppendLine($"world={game.WorldNumber}");
            sb.AppendLine($"level={game.LevelNumber}");
            sb.AppendLine($"lives={game.CurrentLives}");
            sb.AppendLine($"crew={game.CrewBonds}");
            File.WriteAllText(SyncPath, sb.ToString(), Encoding.UTF8);
            return SyncPath;
        }

        /// <summary>Imports a snapshot and applies it to runtime save keys.</summary>
        public static bool ImportSnapshot()
        {
            var game = Engine.Game.Instance;
            if (game == null || !File.Exists(SyncPath)) return false;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in File.ReadAllLines(SyncPath, Encoding.UTF8))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                map[raw.Substring(0, eq).Trim()] = raw.Substring(eq + 1).Trim();
            }

            if (map.TryGetValue("playerName", out string name)) game.PlayerName = name;
            if (map.TryGetValue("bounty", out string bounty) && int.TryParse(bounty, out int b)) game.PlayerBounty = b;
            if (map.TryGetValue("world", out string world) && int.TryParse(world, out int w)) game.WorldNumber = Math.Max(1, w);
            if (map.TryGetValue("level", out string level) && int.TryParse(level, out int l)) game.LevelNumber = Math.Max(1, l);
            if (map.TryGetValue("lives", out string lives) && int.TryParse(lives, out int lv)) game.CurrentLives = Math.Max(1, lv);
            if (map.TryGetValue("crew", out string crew) && int.TryParse(crew, out int c)) game.CrewBonds = Math.Max(0, c);

            DebugLogger.LogInfo("CrossPlatformSync", "Snapshot imported from local sync payload.");
            return true;
        }
    }

    /// <summary>
    /// Team 3 Idea 6: Procedural Generation Engine.
    /// Deterministically generates simple platform lanes from a seed.
    /// </summary>
    public static class ProceduralGenerationEngine
    {
        /// <summary>Generates deterministic platform rectangles for diagnostics.</summary>
        public static IReadOnlyList<System.Drawing.Rectangle> GeneratePlatforms(int seed, int width, int height, int count = 18)
        {
            var rng = new Random(seed);
            var list = new List<System.Drawing.Rectangle>();
            int groundY = Math.Max(120, height - 80);

            for (int i = 0; i < count; i++)
            {
                int w = 80 + rng.Next(120);
                int h = 18;
                int x = rng.Next(Math.Max(1, width - w));
                int y = groundY - rng.Next(40, Math.Max(60, height - 120));
                y = Math.Max(40, Math.Min(groundY - 24, y));
                list.Add(new System.Drawing.Rectangle(x, y, w, h));
            }

            return list;
        }
    }

    /// <summary>
    /// Team 3 Idea 7: Replay System Advanced.
    /// Records frame snapshots and persists replay files.
    /// </summary>
    public static class ReplaySystemAdvanced
    {
        private static readonly string ReplayDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Replays");
        private static readonly List<string> _frames = new List<string>(2048);
        private static bool _recording;

        /// <summary>Starts recording frame snapshots.</summary>
        public static void StartRecording()
        {
            _frames.Clear();
            _recording = true;
        }

        /// <summary>Stops recording and saves replay file to disk.</summary>
        public static string StopAndSave()
        {
            _recording = false;
            Directory.CreateDirectory(ReplayDir);
            string path = Path.Combine(ReplayDir, $"replay-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            File.WriteAllLines(path, _frames, Encoding.UTF8);
            return path;
        }

        /// <summary>Captures one frame snapshot if recording is active.</summary>
        public static void CaptureFrame(float dt)
        {
            if (!_recording) return;
            var g = Engine.Game.Instance;
            if (g == null) return;
            _frames.Add($"{DateTime.UtcNow:o}|dt={dt:F4}|w={g.WorldNumber}|l={g.LevelNumber}|b={g.PlayerBounty}|lives={g.CurrentLives}");
        }

        /// <summary>Returns whether recording is currently active.</summary>
        public static bool IsRecording => _recording;
    }

    /// <summary>
    /// Team 3 Idea 10: Multi-Client Support.
    /// Local in-process client registry and message fan-out mock.
    /// </summary>
    public static class MultiClientSupport
    {
        private static readonly HashSet<string> _clients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Queue<string> _messages = new Queue<string>();

        /// <summary>Registers a client name for local multi-client simulation.</summary>
        public static void RegisterClient(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId)) return;
            _clients.Add(clientId.Trim());
        }

        /// <summary>Returns all registered client IDs.</summary>
        public static IReadOnlyList<string> GetClients() => _clients.OrderBy(x => x).ToList();

        /// <summary>Broadcasts one message to all clients in local queue.</summary>
        public static void Broadcast(string payload)
        {
            foreach (string c in _clients)
                _messages.Enqueue($"{DateTime.UtcNow:o}|to={c}|{payload}");
        }

        /// <summary>Drains queued messages for diagnostics display.</summary>
        public static IReadOnlyList<string> DrainMessages(int max = 24)
        {
            var list = new List<string>();
            while (_messages.Count > 0 && list.Count < max)
                list.Add(_messages.Dequeue());
            return list;
        }
    }
}
