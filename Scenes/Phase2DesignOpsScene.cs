// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 4: Lead Game Designer
// Feature: Design Ops Scene
// Purpose: In-game validation panel for Team 4 Phase 2 design mechanics.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 4 Phase 2 feature verification.
    /// </summary>
    public sealed class Phase2DesignOpsScene : Scene
    {
        private readonly string[] _tabs = { "Energy", "ComboDecay", "MomentumJump", "Drift", "Power", "Parry", "Grapple", "Stamina", "Knockback", "RiskScore" };
        private int _tab;
        private float _comboStart = 4f;
        private float _elapsed;
        private int _riskTier = 1;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            _elapsed += dt;
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 0)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.C)) EnergyMeterSystem.Consume(20f);
                EnergyMeterSystem.Regen(dt);
            }
            if (_tab == 1 && input.IsPressed(System.Windows.Forms.Keys.R)) _elapsed = 0f;
            if (_tab == 9)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.J)) _riskTier = Math.Max(0, _riskTier - 1);
                if (input.IsPressed(System.Windows.Forms.Keys.K)) _riskTier = Math.Min(3, _riskTier + 1);
            }

            if (input.PausePressed || input.InteractPressed) Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 18, 26))) g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold)) g.DrawString("PHASE 2 DESIGN OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);
            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawLines(g, body, "Energy Meter System", new[] { "C: consume 20", $"Energy: {EnergyMeterSystem.Energy:F1}" }); break;
                case 1: DrawLines(g, body, "Combo Multiplier Decay", new[] { "R: reset elapsed", $"Start: {_comboStart:F1}", $"Elapsed: {_elapsed:F2}s", $"Current: {ComboMultiplierDecaySystem.Decay(_comboStart, _elapsed):F2}" }); break;
                case 2: DrawLines(g, body, "Momentum-Based Jumping", new[] { $"JumpVel(base=12,speed=1.0): {MomentumJumpingSystem.JumpVelocity(12f, 1.0f):F2}" }); break;
                case 3: DrawLines(g, body, "Drift Mechanic", new[] { $"Drift(input=1,vel=3): {DriftMechanicSystem.Drift(1f, 3f):F2}" }); break;
                case 4: DrawLines(g, body, "Power Scaling", new[] { $"Scaled dmg(base=20,lvl=5): {PowerScalingSystem.ScaleDamage(20, 5)}" }); break;
                case 5: DrawLines(g, body, "Parry System", new[] { $"Parry(+0.06): {ParrySystem.IsParry(0.06f)}", $"Parry(+0.20): {ParrySystem.IsParry(0.20f)}" }); break;
                case 6: DrawLines(g, body, "Grapple Hook", new[] { $"Range: {GrappleHookSystem.MaxRange:F0}", $"Cooldown(200): {GrappleHookSystem.Cooldown(200):F2}s" }); break;
                case 7: DrawLines(g, body, "Stamina System", new[] { $"Stamina(70-22): {StaminaSystem.ApplyCost(70f, 22f):F1}" }); break;
                case 8: DrawLines(g, body, "Knockback Multiplier", new[] { $"Force(10*1.3): {KnockbackMultiplierSystem.Apply(10f, 1.3f):F2}" }); break;
                default: DrawLines(g, body, "Risk/Reward Scoring", new[] { "J/K change risk tier", $"Tier: {_riskTier}", $"Score(base=2500): {RiskRewardScoringSystem.Score(2500, _riskTier)}" }); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right tab   Esc/Enter back   C/R/J/K actions", f, Brushes.DimGray, 14, H - 26);
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
