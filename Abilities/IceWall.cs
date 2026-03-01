using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fridays_Adventure.Abilities
{
    public sealed class IceWall : Ability
    {
        public IceWall() : base("Ice Wall", 4f) { }
        protected override void OnUse(Entities.Character caster) { }
    }

    public sealed class IceWallInstance
    {
        public float X      { get; set; }
        public float Y      { get; set; }
        public int   Width  { get; } = 20;
        public int   Height { get; } = 88;
        public float Health { get; set; } = 100f;
        public float Age    { get; private set; }
        private const float MaxAge = 14f;
        public bool IsAlive => Health > 0 && Age < MaxAge;
        public Rectangle Hitbox => new Rectangle((int)X, (int)Y, Width, Height);

        public IceWallInstance(float x, float y) { X = x; Y = y; }

        public void Update(float dt, bool nearFire)
        {
            Age += dt * (nearFire ? 4f : 1f);
        }

        public void Draw(Graphics g)
        {
            float alpha = Math.Max(60, 200 - (Age / MaxAge) * 140);
            using (var br = new SolidBrush(Color.FromArgb((int)alpha, 160, 220, 255)))
                g.FillRectangle(br, X, Y, Width, Height);
            using (var pen = new Pen(Color.FromArgb((int)alpha, 220, 240, 255), 2))
                g.DrawRectangle(pen, X, Y, Width, Height);
        }
    }
}
