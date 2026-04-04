// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 9: UI Programmer + Team 3: Technical Lead
// Feature: Tech Lead Ops Scene
// Purpose: UI surface for Phase 3 tech foundation services.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Linq;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Interactive scene for Phase 3 Team 3 systems validation.
    /// </summary>
    public sealed class Phase3TechLeadOpsScene : Scene
    {
        private readonly string[] _tabs =
        {
            "Modding",
            "Server",
            "Analytics",
            "Perf Suite",
            "Patches",
            "Anti-Cheat",
            "Cross-Sync",
            "Proc-Gen",
            "Replay",
            "Multi-Client"
        };

        private int _tab;
        private string _analyticsStatus = "Press E to enqueue analytics sample, F to flush.";
        private string _patchStatus = "Press P to mark first discovered patch as applied.";
        private string _syncStatus = "Press O to export sync snapshot, I to import.";
        private string _replayStatus = "Press R to start/stop replay recording.";

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 2)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.E))
                {
                    DataAnalyticsPipeline.Enqueue("ui.open", "Phase3TechLeadOpsScene");
                    _analyticsStatus = "Queued sample analytics event.";
                }
                if (input.IsPressed(System.Windows.Forms.Keys.F))
                {
                    int flushed = DataAnalyticsPipeline.Flush();
                    _analyticsStatus = $"Flushed events: {flushed}";
                }
            }

            if (_tab == 4 && input.IsPressed(System.Windows.Forms.Keys.P))
            {
                var d = PatchDistributionSystem.Discover();
                if (d.Count > 0)
                {
                    PatchDistributionSystem.MarkApplied(d[0]);
                    _patchStatus = $"Applied marker written for: {d[0]}";
                }
                else _patchStatus = "No patch packages found.";
            }

            if (_tab == 6)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.O))
                {
                    string path = CrossPlatformSync.ExportSnapshot();
                    _syncStatus = string.IsNullOrWhiteSpace(path) ? "Export failed." : "Exported: " + path;
                }
                if (input.IsPressed(System.Windows.Forms.Keys.I))
                {
                    bool ok = CrossPlatformSync.ImportSnapshot();
                    _syncStatus = ok ? "Imported snapshot into runtime." : "Import failed (no snapshot).";
                }
            }

            if (_tab == 8 && input.IsPressed(System.Windows.Forms.Keys.R))
            {
                if (!ReplaySystemAdvanced.IsRecording)
                {
                    ReplaySystemAdvanced.StartRecording();
                    _replayStatus = "Replay recording started.";
                }
                else
                {
                    string path = ReplaySystemAdvanced.StopAndSave();
                    _replayStatus = "Replay saved: " + path;
                }
            }

            if (_tab == 9)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.N))
                {
                    string id = "client-" + DateTime.Now.ToString("HHmmss");
                    MultiClientSupport.RegisterClient(id);
                }
                if (input.IsPressed(System.Windows.Forms.Keys.M))
                    MultiClientSupport.Broadcast("heartbeat");
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(14, 20, 26)))
                g.FillRectangle(br, 0, 0, W, H);

            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString("PHASE 3 TECH LEAD OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);

            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36)))
                g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawModding(g, body); break;
                case 1: DrawServer(g, body); break;
                case 2: DrawAnalytics(g, body); break;
                case 3: DrawPerf(g, body); break;
                case 4: DrawPatches(g, body); break;
                case 5: DrawAntiCheat(g, body); break;
                case 6: DrawCrossSync(g, body); break;
                case 7: DrawProcGen(g, body); break;
                case 8: DrawReplay(g, body); break;
                default: DrawMultiClient(g, body); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right: Tab   Esc/Enter: Back   E/F  P  O/I  R  N/M",
                    f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 120;
                if (x + w > W - 20) break;
                var r = new Rectangle(x, 52, w, 28);
                using (var br = new SolidBrush(sel ? Color.FromArgb(60, 140, 220) : Color.FromArgb(40, 40, 55)))
                    g.FillRectangle(br, r);
                g.DrawRectangle(sel ? Pens.Cyan : Pens.Gray, r);
                using (var f = new Font("Courier New", 9, FontStyle.Bold))
                    g.DrawString(_tabs[i], f, sel ? Brushes.Cyan : Brushes.LightGray, x + 8, 60);
                x += w + 8;
            }
        }

        private void DrawModding(Graphics g, Rectangle body)
        {
            var mods = ModdingFramework.DiscoverPackages();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Modding Framework", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var m in mods.Take(14))
                {
                    Brush b = m.Valid ? Brushes.LimeGreen : Brushes.OrangeRed;
                    g.DrawString($"• {m.Name} ({m.Id}) v{m.Version}  valid={m.Valid}", f, b, body.X + 12, y);
                    y += 20;
                }
        }

        private void DrawServer(Graphics g, Rectangle body)
        {
            var nodes = ServerArchitecture.GetNodes();
            var best = ServerArchitecture.SelectBest("us-east");

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Server Architecture", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
            {
                foreach (var n in nodes)
                {
                    g.DrawString($"• {n.Name} [{n.Region}] {n.LatencyMs}ms online={n.Online}",
                        f, n.Online ? Brushes.LightGray : Brushes.OrangeRed, body.X + 12, y);
                    y += 20;
                }
                y += 8;
                g.DrawString($"Best node: {(best == null ? "none" : best.Name)}", f, Brushes.Gold, body.X + 12, y);
            }
        }

        private void DrawAnalytics(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Data Analytics Pipeline", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            using (var f = new Font("Courier New", 10))
                g.DrawString(_analyticsStatus, f, Brushes.LightGray, body.X + 12, body.Y + 40);
        }

        private void DrawPerf(Graphics g, Rectangle body)
        {
            var lines = PerformanceOptimizationSuite.Snapshot();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Performance Optimization Suite", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var line in lines)
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 22;
                }
        }

        private void DrawPatches(Graphics g, Rectangle body)
        {
            var discovered = PatchDistributionSystem.Discover();
            var applied = PatchDistributionSystem.GetApplied();

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Patch Distribution System", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            using (var f = new Font("Courier New", 10))
                g.DrawString(_patchStatus, f, Brushes.Gold, body.X + 12, body.Y + 34);

            int y = body.Y + 62;
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Discovered:", f, Brushes.Cyan, body.X + 12, y); y += 20;
                foreach (var p in discovered.Take(8))
                {
                    g.DrawString("• " + p, f, Brushes.LightGray, body.X + 20, y);
                    y += 18;
                }
                y += 8;
                g.DrawString("Applied:", f, Brushes.Cyan, body.X + 12, y); y += 20;
                foreach (var p in applied.Take(8))
                {
                    g.DrawString("• " + p, f, Brushes.LimeGreen, body.X + 20, y);
                    y += 18;
                }
            }
        }

        private void DrawAntiCheat(Graphics g, Rectangle body)
        {
            var lines = AntiCheatFramework.RunChecks();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Anti-Cheat Framework", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var line in lines.Take(14))
                {
                    Brush b = line.StartsWith("No anti-cheat") ? Brushes.LimeGreen : Brushes.OrangeRed;
                    g.DrawString("• " + line, f, b, body.X + 12, y);
                    y += 22;
                }
        }

        private void DrawCrossSync(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Cross-Platform Sync", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString(_syncStatus, f, Brushes.LightGray, body.X + 12, body.Y + 40);
        }

        private void DrawProcGen(Graphics g, Rectangle body)
        {
            var rects = ProceduralGenerationEngine.GeneratePlatforms(1337, body.Width - 24, body.Height - 24, 14);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Procedural Generation Engine", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            var clip = new Rectangle(body.X + 10, body.Y + 40, body.Width - 20, body.Height - 50);
            using (var bg = new SolidBrush(Color.FromArgb(20, 20, 28)))
                g.FillRectangle(bg, clip);

            foreach (var r in rects)
            {
                var draw = new Rectangle(clip.X + r.X, clip.Y + r.Y, r.Width, r.Height);
                using (var br = new SolidBrush(Color.FromArgb(120, 90, 140, 220)))
                    g.FillRectangle(br, draw);
                g.DrawRectangle(Pens.Cyan, draw);
            }
        }

        private void DrawReplay(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Replay System Advanced", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString(_replayStatus, f, Brushes.LightGray, body.X + 12, body.Y + 40);
                g.DrawString("Recording: " + ReplaySystemAdvanced.IsRecording, f,
                    ReplaySystemAdvanced.IsRecording ? Brushes.LimeGreen : Brushes.Gold,
                    body.X + 12, body.Y + 62);
            }
        }

        private void DrawMultiClient(Graphics g, Rectangle body)
        {
            var clients = MultiClientSupport.GetClients();
            var msgs = MultiClientSupport.DrainMessages(10);

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Multi-Client Support", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("N: register client   M: broadcast heartbeat", f, Brushes.Gold, body.X + 12, body.Y + 32);

            int y = body.Y + 58;
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Clients:", f, Brushes.Cyan, body.X + 12, y); y += 20;
                foreach (var c in clients.Take(6)) { g.DrawString("• " + c, f, Brushes.LightGray, body.X + 20, y); y += 18; }
                y += 8;
                g.DrawString("Messages:", f, Brushes.Cyan, body.X + 12, y); y += 20;
                foreach (var m in msgs) { g.DrawString("• " + m, f, Brushes.LightGray, body.X + 20, y); y += 18; }
            }
        }
    }
}
