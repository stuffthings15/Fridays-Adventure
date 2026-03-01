using System.Drawing;
using Fridays_Adventure.AI;

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
            : base(x, y, w, h, maxHp)
        {
            MoveSpeed    = 105f;
            AttackDamage = 10;
            AI           = new EnemyAI(this, patrolLeft, patrolRight);
        }

        public void UpdateWithTarget(float dt, Character target)
        {
            AI.Update(dt, target);
            base.Update(dt);
        }

        protected override void DrawPlaceholder(Graphics g)
        {
            bool isBoss = EnemyType == "Boss";
            Color body  = isBoss ? Color.DarkRed : Color.Navy;
            using (var br = new SolidBrush(body))
                g.FillRectangle(br, X, Y + 14, Width, Height - 14);
            using (var br = new SolidBrush(Color.FromArgb(200, Color.PeachPuff)))
                g.FillEllipse(br, X + 4, Y, Width - 8, 18);
            int ex = FacingRight ? (int)X + Width - 10 : (int)X + 4;
            g.FillEllipse(Brushes.White, ex, (int)Y + 4, 6, 6);
            if (isBoss)
                using (var f = new Font("Arial", 7, FontStyle.Bold))
                    g.DrawString("BOSS", f, Brushes.Gold, X + 2, Y - 14);
            DrawHealthBar(g);
        }
    }
}
