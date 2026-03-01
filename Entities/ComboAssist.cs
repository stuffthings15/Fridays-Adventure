using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Entities
{
    // Visual projectile for Zara's interrupt shot
    public sealed class AssistProjectile
    {
        public float X, Y, VX;
        public float Life = 0.6f;
        public bool  IsAlive => Life > 0;

        public AssistProjectile(float x, float y, float vx)
        { X = x; Y = y; VX = vx; }

        public void Update(float dt) { X += VX * dt; Life -= dt; }

        public void Draw(Graphics g)
        {
            float alpha = Math.Max(0, Life / 0.6f);
            using (var br = new SolidBrush(Color.FromArgb((int)(220 * alpha), Color.OrangeRed)))
                g.FillEllipse(br, X - 5, Y - 4, 10, 8);
            using (var pen = new Pen(Color.FromArgb((int)(255 * alpha), Color.Yellow), 1))
                g.DrawEllipse(pen, X - 5, Y - 4, 10, 8);
        }
    }

    // Manages Finn / Zara / Amelia bond-based combat assists
    public sealed class ComboAssist
    {
        private float _zaraCooldown;
        private const float ZaraInterval   = 7f;   // bonds >= 5: fires every 7s
        private const float ZaraDamage     = 22;

        private float _ameliaBoostTimer;
        private float _ameliaCooldown;
        private const float AmeliaInterval = 20f;   // bonds >= 8: boosts every 20s
        private const float AmeliaBoostDuration = 3f;

        private float _zaraFlashTimer;
        public bool   ZaraFlashing => _zaraFlashTimer > 0;
        public bool   AmeliaBoostActive => _ameliaBoostTimer > 0;

        public List<AssistProjectile> Projectiles { get; } = new List<AssistProjectile>();

        public void Update(float dt, Player player, List<Enemy> enemies)
        {
            int bonds = Game.Instance.CrewBonds;

            // ── Zara interrupt shot ────────────────────────────────────────
            if (bonds >= 5)
            {
                _zaraCooldown -= dt;
                if (_zaraCooldown <= 0)
                {
                    FireZara(player, enemies);
                    _zaraCooldown = ZaraInterval;
                }
            }

            // ── Amelia speed/attack boost ──────────────────────────────────
            if (bonds >= 8)
            {
                _ameliaCooldown -= dt;
                if (_ameliaCooldown <= 0)
                {
                    _ameliaBoostTimer = AmeliaBoostDuration;
                    _ameliaCooldown   = AmeliaInterval;
                    player.MoveSpeed  *= 1.4f;
                    player.AttackDamage = (int)(player.AttackDamage * 1.5f);
                }
            }

            if (_ameliaBoostTimer > 0)
            {
                _ameliaBoostTimer -= dt;
                if (_ameliaBoostTimer <= 0)
                {
                    player.MoveSpeed    = 195f;
                    player.AttackDamage = 14;
                }
            }

            if (_zaraFlashTimer > 0) _zaraFlashTimer -= dt;

            // Update projectiles
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                Projectiles[i].Update(dt);
                if (!Projectiles[i].IsAlive) Projectiles.RemoveAt(i);
            }

            // Check projectile hits
            foreach (var proj in Projectiles)
                foreach (var e in enemies)
                    if (e.IsAlive && Math.Abs(proj.X - e.CenterX) < 20 &&
                        Math.Abs(proj.Y - e.CenterY) < 30)
                    {
                        e.TakeDamage((int)ZaraDamage);
                        e.ApplyEffect(StatusEffect.Stunned, 0.6f);
                        proj.Life = 0;
                    }
        }

        private void FireZara(Player player, List<Enemy> enemies)
        {
            // Find nearest enemy
            Enemy nearest = null;
            float minDist = 320f;
            foreach (var e in enemies)
            {
                if (!e.IsAlive) continue;
                float d = player.DistanceTo(e);
                if (d < minDist) { minDist = d; nearest = e; }
            }
            if (nearest == null) return;

            float vx = nearest.X > player.X ? 800f : -800f;
            Projectiles.Add(new AssistProjectile(player.CenterX, player.CenterY - 10, vx));
            _zaraFlashTimer = 0.25f;
            Game.Instance.Audio.BeepAttack();
        }

        public void Draw(Graphics g)
        {
            foreach (var p in Projectiles) p.Draw(g);
        }

        public void DrawHUD(Graphics g, int x, int y)
        {
            int bonds = Game.Instance.CrewBonds;
            using (var f = new Font("Courier New", 8))
            {
                g.DrawString("Bonds:", f, Brushes.LightGray, x, y);
                for (int i = 0; i < 10; i++)
                {
                    var col = i < bonds ? Color.Gold : Color.DimGray;
                    using (var br = new SolidBrush(col))
                        g.FillEllipse(br, x + 44 + i * 14, y + 1, 10, 10);
                }
                if (bonds >= 5)
                {
                    float pct = 1f - (_zaraCooldown / ZaraInterval);
                    string zLabel = $"Zara({(int)Math.Min(100, pct * 100)}%)";
                    g.DrawString(zLabel, f, ZaraFlashing ? Brushes.OrangeRed : Brushes.LightGray, x, y + 14);
                }
                if (AmeliaBoostActive)
                    g.DrawString("AMELIA BOOST!", f, Brushes.Gold, x + 80, y + 14);
            }
        }
    }
}
