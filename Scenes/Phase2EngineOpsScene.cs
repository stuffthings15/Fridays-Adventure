// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 10: Engine Programmer
// Feature: Engine Ops Scene
// Purpose: In-game validation panel for Team 10 Phase 2 engine systems.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 10 Phase 2 engine feature verification.
    /// </summary>
    public sealed class Phase2EngineOpsScene : Scene
    {
        private readonly string[] _tabs = { "Streaming", "ParticlePool", "Predict", "Shake", "Blur", "Vignette", "Zoom", "Culling", "Lighting", "PostFX" };
        private int _tab;
        private float _zoom = 1.0f;
        private float _t;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            _t += dt;
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 0 && input.IsPressed(System.Windows.Forms.Keys.S))
            {
                LevelStreamingSystem.LoadChunk("chunk_a");
                LevelStreamingSystem.LoadChunk("chunk_b");
            }
            if (_tab == 1 && input.IsPressed(System.Windows.Forms.Keys.P))
            {
                var p = ParticleEffectPoolingSystem.Get();
                p.Lifetime = 0.8f;
                ParticleEffectPoolingSystem.Return(p);
            }
            if (_tab == 6)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.J)) _zoom = ZoomMechanicSystem.Clamp(_zoom - 0.1f);
                if (input.IsPressed(System.Windows.Forms.Keys.K)) _zoom = ZoomMechanicSystem.Clamp(_zoom + 0.1f);
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 18, 26))) g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold)) g.DrawString("PHASE 2 ENGINE OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);
            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawLines(g, body, "Level Streaming System", Merge(new[] { "S: load sample chunks" }, LevelStreamingSystem.Loaded())); break;
                case 1: DrawLines(g, body, "Particle Effect Pooling", new[] { "P: get+return packet", "Available=" + ParticleEffectPoolingSystem.Available }); break;
                case 2:
                    var pred = PhysicsPredictionSystem.Predict(new PointF(10, 20), new PointF(3, -1), 0.5f);
                    DrawLines(g, body, "Physics Prediction", new[] { $"Predicted=({pred.X:F1},{pred.Y:F1})" });
                    break;
                case 3:
                    var off = CameraShakeSequencer.Offset(_t, 5f);
                    DrawLines(g, body, "Camera Shake Sequencer", new[] { $"Offset=({off.X:F2},{off.Y:F2})" });
                    break;
                case 4: DrawLines(g, body, "Blur Effect System", new[] { $"Blur radius(speed=30): {BlurEffectSystem.Radius(30f):F2}" }); break;
                case 5: DrawLines(g, body, "Vignette Renderer", new[] { $"Opacity(threat=2.5): {VignetteRendererSystem.Opacity(2.5f)}" }); break;
                case 6: DrawLines(g, body, "Zoom Mechanic", new[] { "J/K adjust zoom", $"Zoom={_zoom:F2}" }); break;
                case 7:
                    bool vis = CullingSystem.Visible(new Rectangle(20, 20, 64, 64), new Rectangle(0, 0, 48, 48));
                    DrawLines(g, body, "Culling System", new[] { "Visible(sample rect): " + vis });
                    break;
                case 8: DrawLines(g, body, "Lighting System", new[] { $"Ambient(dayPhase=0.4): {LightingSystem.Ambient(0.4f):F2}" }); break;
                default: DrawLines(g, body, "Post-Processing Pipeline", PostProcessingPipelineSystem.Stages()); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right tab   Esc/Enter back   S/P/J/K actions", f, Brushes.DimGray, 14, H - 26);
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
