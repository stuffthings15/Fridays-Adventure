// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 11: Build Engineer
// Feature: Build Ops Scene
// Purpose: In-game validation panel for Team 11 Phase 2 build systems.
// ────────────────────────────────────────────────────────────────────────────

using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 11 Phase 2 build feature verification.
    /// </summary>
    public sealed class Phase2BuildOpsScene : Scene
    {
        private readonly string[] _tabs = { "Tests", "Coverage", "Deps", "BuildTime", "Assets", "Style", "Version", "Artifacts", "ReleaseNotes", "Deploy" };
        private int _tab;
        private string _status = "Use tab actions.";

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 2 && input.IsPressed(System.Windows.Forms.Keys.G)) _status = DependencyGraphGeneratorSystem.Generate();
            if (_tab == 7 && input.IsPressed(System.Windows.Forms.Keys.A)) _status = ArtifactArchiverSystem.CreateManifest();
            if (_tab == 8 && input.IsPressed(System.Windows.Forms.Keys.R)) _status = ReleaseNotesGeneratorSystem.Generate();

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 18, 26))) g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold)) g.DrawString("PHASE 2 BUILD OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);
            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawLines(g, body, "Automated Testing Runner", AutomatedTestingRunnerSystem.Run()); break;
                case 1: DrawLines(g, body, "Code Coverage Analyzer", CodeCoverageAnalyzerSystem.Summary()); break;
                case 2: DrawLines(g, body, "Dependency Graph Generator", new[] { "G: generate graph", _status }); break;
                case 3: DrawLines(g, body, "Build Time Analyzer", BuildTimeAnalyzerSystem.Analyze()); break;
                case 4: DrawLines(g, body, "Asset Pipeline", AssetPipelineSystem.Summary()); break;
                case 5: DrawLines(g, body, "Code Style Checker", CodeStyleCheckerSystem.Check()); break;
                case 6: DrawLines(g, body, "Version Bump Automation", new[] { "Next patch version: " + VersionBumpAutomationSystem.NextPatchVersion() }); break;
                case 7: DrawLines(g, body, "Artifact Archiver", new[] { "A: create artifact manifest", _status }); break;
                case 8: DrawLines(g, body, "Release Notes Generator", new[] { "R: generate release notes", _status }); break;
                default: DrawLines(g, body, "Deployment Validator", DeploymentValidatorSystem.Validate()); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right tab   Esc/Enter back   G/A/R actions", f, Brushes.DimGray, 14, H - 26);
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
    }
}
