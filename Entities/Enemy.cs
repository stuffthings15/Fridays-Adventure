using System;
using System.Drawing;
using Fridays_Adventure.AI;
using Fridays_Adventure.Data;

namespace Fridays_Adventure.Entities
{
    public sealed class Enemy : Character
    {
        public EnemyAI AI        { get; }
        public string  EnemyType { get; set; } = "Marine";
        public int     ScoreValue { get; set; } = 10;

        public Rectangle AttackHitbox
        {
            get
            {
                if (!IsAttacking) return Rectangle.Empty;
                return FacingRight
                    ? new Rectangle((int)X + Width, (int)Y + 8, 36, Height - 16)
                    : new Rectangle((int)X - 36,    (int)Y + 8, 36, Height - 16);
            }
        }

        public Enemy(float x, float y, int w, int h, int maxHp, float patrolLeft, float patrolRight)
            : base(x, y, Math.Max(1, (int)(w * 1.5f)), Math.Max(1, (int)(h * 1.5f)), maxHp)
        {
            MoveSpeed    = 105f;
            AttackDamage = 10;
            AI           = new EnemyAI(this, patrolLeft, patrolRight);

            // Default enemy visual — enemy_Garp.png (renamed from GARP.png)
            var garp = SpriteManager.GetScaled("enemy_Garp.png", Width, Height);
            if (garp != null) Sprite = garp;
        }

        public void UpdateWithTarget(float dt, Character target)
        {
            AI.Update(dt, target);
            base.Update(dt);
        }

        protected override void DrawPlaceholder(Graphics g)
        {
            // PHASE 2 - Team 14: Environment Artist
            // Draw sprite if loaded (enemy models), otherwise use placeholder
            if (Sprite != null)
            {
                g.DrawImage(Sprite, (int)X, (int)Y, Width, Height);
                return;
            }

            // ── Mega Man-style invincibility blink on damage ─────────────────
            // Skip every other frame while the hit-flash window is active.
            if (IsInvincible && (int)(InvincibilityTimer * 14) % 2 == 0) return;

            bool isBoss    = EnemyType == "Boss";
            bool isArmored = EnemyType == "Armored";

            // ── Type-based Mega Man robot palette ────────────────────────────
            // Marine (standard) = dark navy; Armored = slate gray; Boss = deep crimson
            Color body   = isBoss    ? Color.FromArgb(180, 20,  20)
                         : isArmored ? Color.FromArgb(80,  90, 110)
                         :             Color.FromArgb(20,  50, 120);
            Color accent = isBoss    ? Color.OrangeRed
                         : isArmored ? Color.CornflowerBlue
                         :             Color.DeepSkyBlue;

            // Body torso (robot chassis)
            using (var br = new SolidBrush(body))
                g.FillRectangle(br, X, Y + 14, Width, Height - 14);

            // Chest armor stripe (Mega Man-style color band)
            using (var br = new SolidBrush(Color.FromArgb(90, accent)))
                g.FillRectangle(br, X, Y + Height / 2 - 2, Width, 8);

            // Head / helmet
            using (var br = new SolidBrush(Color.FromArgb(220, Color.PeachPuff)))
                g.FillEllipse(br, X + 4, Y, Width - 8, 18);

            // Helmet visor strip (Mega Man robot head styling)
            using (var br = new SolidBrush(Color.FromArgb(160, body)))
                g.FillRectangle(br, (int)X + 4, (int)Y, Width - 8, 6);

            // Glowing robot eye (white sclera + colored iris/LED)
            int ex = FacingRight ? (int)X + Width - 12 : (int)X + 4;
            g.FillEllipse(Brushes.White, ex, (int)Y + 5, 7, 7);
            using (var br = new SolidBrush(accent))
                g.FillEllipse(br, ex + 1, (int)Y + 6, 5, 5);
            // Pupil/LED center — small bright dot
            using (var br = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                g.FillEllipse(br, ex + 2, (int)Y + 7, 2, 2);

            // Boss label tag above the sprite
            if (isBoss)
                using (var f = new Font("Courier New", 7, FontStyle.Bold))
                    g.DrawString("BOSS", f, Brushes.Gold, X + 2, Y - 14);

            // HP bar (always visible on enemies so the player can track damage)
            DrawHealthBar(g);
        }
    }
}
