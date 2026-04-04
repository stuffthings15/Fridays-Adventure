// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 5: Level Designer
// Feature: Level Designer Ops Scene
// Purpose: In-game validation and review interface for all ten Team 5 level
//          concepts, including deterministic geometry previews.
// ────────────────────────────────────────────────────────────────────────────

using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Scene used to inspect Team 5 level design definitions and previews.
    /// </summary>
    public sealed class Phase3LevelDesignerOpsScene : Scene
    {
        private int _index;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left)) _index = (_index + 9) % 10;
            if (input.IsPressed(System.Windows.Forms.Keys.Right)) _index = (_index + 1) % 10;

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            var def = Phase3LevelDesignerSystems.GetByIndex(_index);
            var geo = Phase3LevelDesignerSystems.BuildPreviewGeometry(_index, W - 60, H - 220);

            using (var bg = new SolidBrush(Color.FromArgb(15, 18, 26)))
                g.FillRectangle(bg, 0, 0, W, H);

            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString("PHASE 3 LEVEL DESIGNER OPS", f, Brushes.Gold, 14, 10);

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
            {
                g.DrawString($"[{_index + 1}/10] {def.Name}", f, Brushes.Cyan, 16, 52);
                g.DrawString("Theme: " + def.Theme, f, Brushes.LightGray, 16, 78);
                g.DrawString("Hazard Focus: " + def.HazardFocus, f, Brushes.LightGray, 16, 102);
                g.DrawString("Objective: " + def.ObjectiveHint, f, Brushes.LightGray, 16, 126);
            }

            var preview = new Rectangle(16, 160, W - 32, H - 210);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36)))
                g.FillRectangle(br, preview);
            g.DrawRectangle(Pens.DimGray, preview);

            foreach (var r in geo)
            {
                var draw = new Rectangle(preview.X + 12 + r.X, preview.Y + 10 + r.Y, r.Width, r.Height);
                using (var br = new SolidBrush(Color.FromArgb(120, 90, 150, 230)))
                    g.FillRectangle(br, draw);
                g.DrawRectangle(Pens.Cyan, draw);
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right: cycle levels   Esc/Enter: back", f, Brushes.DimGray, 16, H - 28);
        }
    }
}
