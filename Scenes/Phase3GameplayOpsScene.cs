// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 7: Gameplay Programmer
// Feature: Gameplay Ops Scene
// Purpose: In-game validation interface for Team 7 gameplay systems.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 7 gameplay feature verification.
    /// </summary>
    public sealed class Phase3GameplayOpsScene : Scene
    {
        private readonly string[] _tabs =
        {
            "Skins",
            "Weapons",
            "Finishers",
            "Shield+",
            "Bomb",
            "BoostPads",
            "TimeSlow",
            "IFrames+",
            "DoubleDmg",
            "KnockRes"
        };

        private int _tab;
        private int _combo = 0;
        private float _charge = 0.0f;
        private bool _timeSlow;
        private bool _doubleDamage;
        private float _resistance = 0.2f;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 0 && input.IsPressed(System.Windows.Forms.Keys.E))
                CharacterSkinsSystem.Equip("Miss Friday", CosmeticInventorySystem.GetEquipped());

            if (_tab == 2)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.J)) _combo = Math.Max(0, _combo - 1);
                if (input.IsPressed(System.Windows.Forms.Keys.K)) _combo = Math.Min(40, _combo + 1);
            }

            if (_tab == 3)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.B)) ShieldMechanicsAdvancedSystem.BlockDamage(15);
                if (input.IsPressed(System.Windows.Forms.Keys.R)) ShieldMechanicsAdvancedSystem.Refill();
            }

            if (_tab == 4)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.J)) _charge = Math.Max(0f, _charge - 0.1f);
                if (input.IsPressed(System.Windows.Forms.Keys.K)) _charge = Math.Min(2.5f, _charge + 0.1f);
            }

            if (_tab == 6 && input.IsPressed(System.Windows.Forms.Keys.T)) _timeSlow = !_timeSlow;
            if (_tab == 8 && input.IsPressed(System.Windows.Forms.Keys.D)) _doubleDamage = !_doubleDamage;

            if (_tab == 9)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.J)) _resistance = Math.Max(0f, _resistance - 0.05f);
                if (input.IsPressed(System.Windows.Forms.Keys.K)) _resistance = Math.Min(0.9f, _resistance + 0.05f);
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 20, 22)))
                g.FillRectangle(br, 0, 0, W, H);

            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString("PHASE 3 GAMEPLAY OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);

            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawSkins(g, body); break;
                case 1: DrawWeapons(g, body); break;
                case 2: DrawFinishers(g, body); break;
                case 3: DrawShield(g, body); break;
                case 4: DrawBomb(g, body); break;
                case 5: DrawBoostPads(g, body); break;
                case 6: DrawTimeSlow(g, body); break;
                case 7: DrawIFrames(g, body); break;
                case 8: DrawDoubleDamage(g, body); break;
                default: DrawKnockRes(g, body); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right: tab   Esc/Enter: back   J/K/B/R/T/D/E keys per tab", f, Brushes.DimGray, 14, H - 26);
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

        private void DrawSkins(Graphics g, Rectangle body)
        {
            string eq = CharacterSkinsSystem.GetEquipped("Miss Friday");
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Character Skins", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("E: equip current inventory skin", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString("Equipped (Miss Friday): " + eq, f, Brushes.LightGray, body.X + 12, body.Y + 60);
            }
        }

        private void DrawWeapons(Graphics g, Rectangle body)
        {
            var list = WeaponSystem.GetAll();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Weapon System", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10)) foreach (var w in list)
            {
                g.DrawString($"• {w.Name} dmg={w.Damage} spd={w.Speed:F1}", f, Brushes.LightGray, body.X + 12, y);
                y += 22;
            }
        }

        private void DrawFinishers(Graphics g, Rectangle body)
        {
            string finisher = ComboFinisherMovesSystem.ResolveFinisher(_combo);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Combo Finisher Moves", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("J/K adjust combo", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Combo: {_combo}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString("Finisher: " + finisher, f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawShield(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Shield Mechanics Advanced", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("B: block 15 dmg   R: refill", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Durability: {ShieldMechanicsAdvancedSystem.Durability}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
            }
        }

        private void DrawBomb(Graphics g, Rectangle body)
        {
            int dmg = BombThrowableSystem.DamageFromCharge(_charge);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Bomb Throwable", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("J/K charge seconds", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Charge: {_charge:F1}s", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString($"Explosion damage: {dmg}", f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawBoostPads(Graphics g, Rectangle body)
        {
            float vy = JumpBoostPadsSystem.ApplyBoost(currentVy: 2.5f, padStrength: 12f);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Jump Boost Pads", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10)) g.DrawString("Boosted VY: " + vy.ToString("F2"), f, Brushes.LightGray, body.X + 12, body.Y + 40);
        }

        private void DrawTimeSlow(Graphics g, Rectangle body)
        {
            float scale = TimeSlowPowerUpSystem.GetTimeScale(_timeSlow);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Time Slow Power-Up", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("T: toggle active", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Active: {_timeSlow}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString($"Time scale: {scale:F2}", f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawIFrames(Graphics g, Rectangle body)
        {
            float d = InvulnerabilityFramesAdvancedSystem.GetDuration(1.1f);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Invulnerability Frames Advanced", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10)) g.DrawString($"I-Frame duration (scale 1.1): {d:F2}s", f, Brushes.LightGray, body.X + 12, body.Y + 40);
        }

        private void DrawDoubleDamage(Graphics g, Rectangle body)
        {
            int dmg = DoubleDamageModifierSystem.Apply(22, _doubleDamage);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Double Damage Modifier", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("D: toggle", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Enabled: {_doubleDamage}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString($"Final damage from 22: {dmg}", f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawKnockRes(Graphics g, Rectangle body)
        {
            float outKb = KnockbackResistanceSystem.Apply(10f, _resistance);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Knockback Resistance", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("J/K adjust resistance", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Resistance: {_resistance:F2}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString($"Output knockback from 10: {outKb:F2}", f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }
    }
}
