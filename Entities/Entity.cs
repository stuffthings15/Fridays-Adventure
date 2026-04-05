using System;
using System.Drawing;

namespace Fridays_Adventure.Entities
{
    public class Entity
    {
        public float X         { get; set; }
        public float Y         { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public int   Width     { get; set; }
        public int   Height    { get; set; }
        public bool  IsActive  { get; set; } = true;
        public bool  FacingRight { get; set; } = true;
        public Bitmap Sprite   { get; set; }

        public Rectangle Hitbox    => new Rectangle((int)X, (int)Y, Width, Height);
        public float     CenterX   => X + Width  * 0.5f;
        public float     CenterY   => Y + Height * 0.5f;

        public Entity(float x, float y, int w, int h)
        {
            X = x; Y = y; Width = w; Height = h;
        }

        public bool Overlaps(Entity other) => Hitbox.IntersectsWith(other.Hitbox);
        public bool Overlaps(Rectangle r)  => Hitbox.IntersectsWith(r);

        public float DistanceTo(Entity other)
        {
            float dx = CenterX - other.CenterX;
            float dy = CenterY - other.CenterY;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public virtual void Update(float dt) { }

        public virtual void Draw(Graphics g)
        {
            if (Sprite != null)
            {
                // Use high-quality interpolation for sprite rendering
                // so character art stays crisp at any scale.
                var prevMode = g.InterpolationMode;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(Sprite, X, Y, Width, Height);
                g.InterpolationMode = prevMode;
            }
            else
                DrawPlaceholder(g);
        }

        protected virtual void DrawPlaceholder(Graphics g)
        {
            g.FillRectangle(Brushes.Magenta, X, Y, Width, Height);
        }
    }
}
