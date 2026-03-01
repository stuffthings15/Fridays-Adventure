using System.Drawing;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Hazards
{
    public enum HazardType { WaterPit, SeaStoneZone, FireSource }

    public abstract class Hazard
    {
        public float X      { get; set; }
        public float Y      { get; set; }
        public int   Width  { get; set; }
        public int   Height { get; set; }
        public bool  IsActive { get; set; } = true;
        public HazardType Type { get; protected set; }

        public Rectangle Bounds => new Rectangle((int)X, (int)Y, Width, Height);
        public bool Overlaps(Entity e) => Bounds.IntersectsWith(e.Hitbox);

        protected Hazard(float x, float y, int w, int h)
        { X = x; Y = y; Width = w; Height = h; }

        public virtual void Update(float dt) { }
        public abstract void ApplyEffect(Character c, float dt);
        public abstract void Draw(Graphics g);
    }
}
