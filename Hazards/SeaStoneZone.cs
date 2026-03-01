using System.Drawing;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Hazards
{
    public sealed class SeaStoneZone : Hazard
    {
        private float _pulseTimer;
        public SeaStoneZone(float x, float y, int w, int h) : base(x, y, w, h)
            => Type = HazardType.SeaStoneZone;

        public override void Update(float dt) => _pulseTimer += dt;

        public override void ApplyEffect(Character c, float dt)
        {
            c.ApplyEffect(StatusEffect.Suppressed, 0.2f);
            foreach (var a in c.Abilities)
                a.TickCooldown(-dt); // pause cooldown regen while suppressed
        }

        public override void Draw(Graphics g)
        {
            using (var br = new SolidBrush(Color.FromArgb(160, 140, 150, 60)))
                g.FillRectangle(br, X, Y, Width, Height);
            using (var pen = new Pen(Color.FromArgb(200, Color.Olive), 2))
                g.DrawRectangle(pen, X, Y, Width - 1, Height - 1);
            using (var f = new Font("Arial", 8, FontStyle.Bold))
                g.DrawString("⚓ SEA STONE", f, Brushes.DarkOliveGreen, X + 4, Y + 4);
        }
    }
}
