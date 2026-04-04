// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 3: Technical Lead
// Feature: Tech Lead Ops Scene
// Purpose: In-game validation panel for Team 3 Phase 2 engineering systems.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 3 Phase 2 tools and diagnostics.
    /// </summary>
    public sealed class Phase2TechLeadOpsScene : Scene
    {
        private readonly string[] _tabs = { "ShaderPerf", "Assets", "Network", "ThreadPool", "Memory", "Streaming", "DI", "Events", "Crash", "BuildProf" };
        private int _tab;
        private string _status = "Use tab actions.";

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 1 && input.IsPressed(System.Windows.Forms.Keys.G))
                _status = "Manifest: " + AssetBundleSystem.GenerateManifest();
            if (_tab == 3 && input.IsPressed(System.Windows.Forms.Keys.J))
                ThreadPoolManager.Enqueue(() => EventPoolManager.Push("job executed"));
            if (_tab == 3 && input.IsPressed(System.Windows.Forms.Keys.K))
                _status = ThreadPoolManager.RunOne() ? "Ran one queued job." : "No queued jobs.";
            if (_tab == 5 && input.IsPressed(System.Windows.Forms.Keys.S))
            {
                SceneStreaming.LoadLane("island_lane");
                SceneStreaming.LoadLane("boss_lane");
            }
            if (_tab == 6 && input.IsPressed(System.Windows.Forms.Keys.R))
            {
                DependencyInjectionContainer.Register<string>("DI_OK");
                _status = "DI registered string service.";
            }
            if (_tab == 7 && input.IsPressed(System.Windows.Forms.Keys.E))
                EventPoolManager.Push("sample_event");
            if (_tab == 8 && input.IsPressed(System.Windows.Forms.Keys.C))
                _status = "Crash report: " + CrashHandlerEnhanced.WriteReport("manual_test", new Exception("phase2 simulated exception"));
            if (_tab == 9 && input.IsPressed(System.Windows.Forms.Keys.B))
            {
                BuildProfiler.Begin("bundle");
                System.Threading.Thread.Sleep(5);
                BuildProfiler.End("bundle");
            }

            if (input.PausePressed || input.InteractPressed) Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 16, 26))) g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold)) g.DrawString("PHASE 2 TECH LEAD OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);
            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawLines(g, body, "Shader Performance Profiler", ShaderPerformanceProfiler.Summary()); break;
                case 1: DrawLines(g, body, "Asset Bundle System", new[] { "G: generate manifest", _status }); break;
                case 2: DrawLines(g, body, "Networking Simulator", new[] { $"Base: {NetworkingSimulator.BaseLatencyMs}ms", $"Jitter: {NetworkingSimulator.JitterMs}ms", $"Sample: {NetworkingSimulator.SampleLatency()}ms" }); break;
                case 3: DrawLines(g, body, "Thread Pool Manager", new[] { "J: enqueue   K: run one", $"Pending: {ThreadPoolManager.Pending}", _status }); break;
                case 4: DrawLines(g, body, "Memory Fragmentation Analyzer", new[] { $"Fragmentation Score: {MemoryFragmentationAnalyzer.Score():F1}" }); break;
                case 5: DrawLines(g, body, "Scene Streaming", Merge(new[] { "S: load sample lanes" }, SceneStreaming.Loaded())); break;
                case 6:
                    string di = DependencyInjectionContainer.Resolve<string>() ?? "<null>";
                    DrawLines(g, body, "Dependency Injection Container", new[] { "R: register sample service", "Resolved string: " + di, _status });
                    break;
                case 7: DrawLines(g, body, "Event Pool Manager", Merge(new[] { "E: push sample event" }, EventPoolManager.Snapshot())); break;
                case 8: DrawLines(g, body, "Crash Handler Enhanced", new[] { "C: write sample crash report", _status }); break;
                default: DrawLines(g, body, "Build Profiler", Merge(new[] { "B: record sample span" }, BuildProfiler.Summary())); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right tab   Esc/Enter back   G/J/K/S/R/E/C/B actions", f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 100;
                if (x + w > W - 20) break;
                var r = new Rectangle(x, 52, w, 28);
                using (var br = new SolidBrush(sel ? Color.FromArgb(70, 130, 220) : Color.FromArgb(40, 40, 55))) g.FillRectangle(br, r);
                g.DrawRectangle(sel ? Pens.Cyan : Pens.Gray, r);
                using (var f = new Font("Courier New", 8, FontStyle.Bold)) g.DrawString(_tabs[i], f, sel ? Brushes.Cyan : Brushes.LightGray, x + 6, 60);
                x += w + 6;
            }
        }

        private static void DrawLines(Graphics g, Rectangle body, string title, System.Collections.Generic.IReadOnlyList<string> lines)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString(title, f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var line in lines)
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                    if (y > body.Bottom - 12) break;
                }
        }

        private static string[] Merge(System.Collections.Generic.IReadOnlyList<string> a, System.Collections.Generic.IReadOnlyList<string> b)
        {
            var list = new System.Collections.Generic.List<string>();
            if (a != null) list.AddRange(a);
            if (b != null) list.AddRange(b);
            return list.ToArray();
        }
    }
}
