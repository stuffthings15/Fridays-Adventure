// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 10: Engine Programmer
// Feature: Engine Ops Scene
// Purpose: Interactive validation UI for Team 10 engine systems.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Linq;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime validation scene for Team 10 Phase 3 engine features.
    /// </summary>
    public sealed class Phase3EngineOpsScene : Scene
    {
        private readonly string[] _tabs =
        {
            "ProcGen",
            "Pooling",
            "PhysicsReplay",
            "DynDiff",
            "Waypoint",
            "Checkpoint+",
            "Camera",
            "DialogueFx",
            "Weather+",
            "Shaders"
        };

        private int _tab;
        private int _seed = 1337;
        private string _status = "Use per-tab keys shown below.";

        public override void OnEnter()
        {
            WaypointSystem.Set(new[]
            {
                new WaypointSystem.Waypoint { Id = "w1", Position = new PointF(80, 220) },
                new WaypointSystem.Waypoint { Id = "w2", Position = new PointF(240, 170) },
                new WaypointSystem.Waypoint { Id = "w3", Position = new PointF(420, 210) },
            });
            CheckpointSystemExtended.Set("alpha", new PointF(120, 300));
        }

        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 0)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.N)) _seed++;
                if (input.IsPressed(System.Windows.Forms.Keys.B)) _seed = Math.Max(1, _seed - 1);
            }
            else if (_tab == 1 && input.IsPressed(System.Windows.Forms.Keys.P))
            {
                var pt = AdvancedPoolingSystem.RentPoint("engine");
                AdvancedPoolingSystem.ReturnPoint("engine", pt);
                _status = "Pooled point rent/return complete.";
            }
            else if (_tab == 2 && input.IsPressed(System.Windows.Forms.Keys.R))
            {
                if (!ReplaySystemAdvanced.IsRecording)
                {
                    ReplaySystemAdvanced.StartRecording();
                    _status = "Replay recording started.";
                }
                else
                {
                    string path = ReplaySystemAdvanced.StopAndSave();
                    var lines = PhysicsReplaySystem.LoadReplay(path);
                    _status = PhysicsReplaySystem.Summarize(lines);
                }
            }
            else if (_tab == 5 && input.IsPressed(System.Windows.Forms.Keys.C))
            {
                CheckpointSystemExtended.Set("alpha", new PointF(160 + DateTime.Now.Second, 300));
                _status = "Checkpoint alpha moved.";
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            using (var br = new SolidBrush(Color.FromArgb(16, 18, 24)))
                g.FillRectangle(br, 0, 0, W, H);

            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString("PHASE 3 ENGINE OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);

            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36)))
                g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawProcGen(g, body); break;
                case 1: DrawPooling(g, body); break;
                case 2: DrawPhysicsReplay(g, body); break;
                case 3: DrawDynDiff(g, body); break;
                case 4: DrawWaypoints(g, body); break;
                case 5: DrawCheckpoints(g, body); break;
                case 6: DrawCamera(g, body); break;
                case 7: DrawDialogueFx(g, body); break;
                case 8: DrawWeather(g, body); break;
                default: DrawShaders(g, body); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right: Tab   Esc/Enter: Back   N/B/P/R/C per tab", f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 102;
                if (x + w > W - 20) break;
                var r = new Rectangle(x, 52, w, 28);
                using (var br = new SolidBrush(sel ? Color.FromArgb(70, 130, 220) : Color.FromArgb(40, 40, 55)))
                    g.FillRectangle(br, r);
                g.DrawRectangle(sel ? Pens.Cyan : Pens.Gray, r);
                using (var f = new Font("Courier New", 8, FontStyle.Bold))
                    g.DrawString(_tabs[i], f, sel ? Brushes.Cyan : Brushes.LightGray, x + 6, 60);
                x += w + 6;
            }
        }

        private void DrawProcGen(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Procedural Level Generator", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString($"Seed: {_seed}   N/B to adjust", f, Brushes.Gold, body.X + 12, body.Y + 34);

            var rects = ProceduralLevelGenerator.Generate(_seed, body.Width - 24, body.Height - 60, 18);
            var clip = new Rectangle(body.X + 10, body.Y + 60, body.Width - 20, body.Height - 70);
            using (var bg = new SolidBrush(Color.FromArgb(20, 20, 28))) g.FillRectangle(bg, clip);
            foreach (var r in rects)
            {
                var draw = new Rectangle(clip.X + r.X, clip.Y + r.Y, r.Width, r.Height);
                using (var br = new SolidBrush(Color.FromArgb(120, 80, 150, 230))) g.FillRectangle(br, draw);
                g.DrawRectangle(Pens.Cyan, draw);
            }
        }

        private void DrawPooling(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Advanced Pooling System", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Press P to rent/return pooled point.", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString(_status, f, Brushes.LightGray, body.X + 12, body.Y + 60);
            }
        }

        private void DrawPhysicsReplay(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Physics Replay System", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Press R to start/stop replay capture.", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString("Recording: " + ReplaySystemAdvanced.IsRecording, f,
                    ReplaySystemAdvanced.IsRecording ? Brushes.LimeGreen : Brushes.LightGray,
                    body.X + 12, body.Y + 60);
                g.DrawString(_status, f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawDynDiff(Graphics g, Rectangle body)
        {
            int deaths = SessionStats.Instance.DeathCount;
            float mins = SessionStats.Instance.PlaySeconds / 60f;
            float scale = DynamicDifficultyScaling.GetScale(deaths, mins);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Dynamic Difficulty Scaling", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString($"Deaths: {deaths}   Play minutes: {mins:F1}", f, Brushes.LightGray, body.X + 12, body.Y + 34);
                g.DrawString($"Scale: {scale:F2}", f, Brushes.Gold, body.X + 12, body.Y + 60);
            }
        }

        private void DrawWaypoints(Graphics g, Rectangle body)
        {
            var wps = WaypointSystem.Get();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Waypoint System", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var wp in wps)
                {
                    g.DrawString($"• {wp.Id}: ({wp.Position.X:F1}, {wp.Position.Y:F1})", f, Brushes.LightGray, body.X + 12, y);
                    y += 22;
                }
        }

        private void DrawCheckpoints(Graphics g, Rectangle body)
        {
            CheckpointSystemExtended.TryGet("alpha", out PointF p);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Checkpoint System Extended", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Press C to move checkpoint alpha.", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"alpha: ({p.X:F1}, {p.Y:F1})", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString(_status, f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawCamera(Graphics g, Rectangle body)
        {
            var keys = new[]
            {
                new CinematicCameraSystem.Keyframe { Time = 0f, Target = new PointF(0, 0), Zoom = 1.0f },
                new CinematicCameraSystem.Keyframe { Time = 1f, Target = new PointF(120, 60), Zoom = 1.2f },
                new CinematicCameraSystem.Keyframe { Time = 2f, Target = new PointF(250, 90), Zoom = 1.4f },
            };
            float t = (float)(DateTime.Now.TimeOfDay.TotalSeconds % 2.0);
            var k = CinematicCameraSystem.Evaluate(keys, t);

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Cinematic Camera System", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString($"t={t:F2}s", f, Brushes.LightGray, body.X + 12, body.Y + 34);
                g.DrawString($"Target=({k.Target.X:F1},{k.Target.Y:F1})", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString($"Zoom={k.Zoom:F2}", f, Brushes.Gold, body.X + 12, body.Y + 84);
            }
        }

        private void DrawDialogueFx(Graphics g, Rectangle body)
        {
            const string line = "The Grand Line calls the bold...";
            float e = (float)(DateTime.Now.TimeOfDay.TotalSeconds % 4.0);
            int vis = DialogueAnimation.VisibleChars(line, e, 12f);
            string shown = line.Substring(0, Math.Max(0, Math.Min(vis, line.Length)));

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Dialogue Animation", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Typewriter preview:", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString(shown, f, Brushes.LightGray, body.X + 12, body.Y + 60);
            }
        }

        private void DrawWeather(Graphics g, Rectangle body)
        {
            var clear = WeatherSystemAdvanced.Get("clear");
            var storm = WeatherSystemAdvanced.Get("storm");
            var mist = WeatherSystemAdvanced.Get("mist");
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Weather System Advanced", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
            {
                foreach (var p in new[] { clear, storm, mist })
                {
                    g.DrawString($"• {p.Name,-6} wind={p.Wind:F1} rain={p.Rain:F1} fog={p.Fog:F1}", f, Brushes.LightGray, body.X + 12, y);
                    y += 22;
                }
            }
        }

        private void DrawShaders(Graphics g, Rectangle body)
        {
            var presets = ShaderLibrary.GetPresets();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Shader Library", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var p in presets)
                {
                    g.DrawString("• " + p, f, Brushes.LightGray, body.X + 12, y);
                    y += 22;
                }
        }
    }
}
