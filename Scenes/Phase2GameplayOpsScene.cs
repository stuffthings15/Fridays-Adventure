// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 7: Gameplay Programmer
// Feature: Gameplay Ops Scene
// Purpose: In-game validation panel for Team 7 Phase 2 gameplay systems.
// ────────────────────────────────────────────────────────────────────────────

using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 7 Phase 2 gameplay feature verification.
    /// </summary>
    public sealed class Phase2GameplayOpsScene : Scene
    {
        private readonly string[] _tabs = { "WallSlide", "AirDash", "Shield", "Rope", "Magnetic", "SpikeBall", "Conveyor", "Portal", "Slippery", "Rocket" };
        private int _tab;
        private float _charge = 0.5f;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 2)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.A)) ShieldPowerUpSystem.Activate();
                if (input.IsPressed(System.Windows.Forms.Keys.B)) ShieldPowerUpSystem.Absorb(12);
            }
            if (_tab == 9)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.J)) _charge = System.Math.Max(0f, _charge - 0.1f);
                if (input.IsPressed(System.Windows.Forms.Keys.K)) _charge = System.Math.Min(3f, _charge + 0.1f);
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 20, 22))) g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold)) g.DrawString("PHASE 2 GAMEPLAY OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);
            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawLines(g, body, "Wall Slide Mechanic", new[] { $"Apply(vy=5): {WallSlideMechanicSystem.Apply(5f):F2}" }); break;
                case 1: DrawLines(g, body, "Air Dash", new[] { $"Dash left: {AirDashSystem.DashVelocity(-1):F1}", $"Dash right: {AirDashSystem.DashVelocity(1):F1}" }); break;
                case 2: DrawLines(g, body, "Shield Power-Up", new[] { "A: activate  B: absorb 12", $"Shield points: {ShieldPowerUpSystem.Points}" }); break;
                case 3: DrawLines(g, body, "Rope Swing Mechanic", new[] { $"Tangential v(angle=0.8,len=180): {RopeSwingMechanicSystem.TangentialVelocity(0.8f, 180f):F2}" }); break;
                case 4: DrawLines(g, body, "Magnetic Platforms", new[] { $"Pull force(dist=40): {MagneticPlatformsSystem.PullForce(40f):F2}" }); break;
                case 5: DrawLines(g, body, "Spike Ball Enemy", new[] { $"Contact dmg(speed=6): {SpikeBallEnemySystem.ContactDamage(6f)}" }); break;
                case 6: DrawLines(g, body, "Conveyor Belt Sequence", new[] { $"Apply(vx=3,belt=4): {ConveyorBeltSequenceSystem.Apply(3f, 4f):F2}" }); break;
                case 7: DrawLines(g, body, "Portal Mechanic", new[] { $"Transform angle(0.5,+1.0): {PortalMechanicSystem.TransformAngle(0.5f, 1.0f):F2}" }); break;
                case 8: DrawLines(g, body, "Slippery Surface", new[] { $"Apply friction(vx=8): {SlipperySurfaceSystem.ApplyFriction(8f):F2}" }); break;
                default: DrawLines(g, body, "Rocket Launcher Power-Up", new[] { "J/K charge", $"Charge: {_charge:F1}s", $"Damage: {RocketLauncherPowerUpSystem.Damage(_charge)}" }); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right tab   Esc/Enter back   A/B/J/K actions", f, Brushes.DimGray, 14, H - 26);
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

        private static void DrawLines(Graphics g, Rectangle body, string title, string[] lines)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString(title, f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var line in lines)
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                }
        }
    }
}
