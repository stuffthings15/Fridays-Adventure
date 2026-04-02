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
        private float _preBoostMoveSpeed;
        private int   _preBoostAttackDamage;
        private const float AmeliaInterval     = 20f;   // bonds >= 8: boosts every 20s
        private const float AmeliaBoostDuration = 3f;

        private float _orcaCooldown;
        private float _orcaFlashTimer;
        private const float OrcaInterval = 12f;   // bonds >= 6: area slam every 12s
        private const float OrcaDamage   = 28f;
        private const float OrcaRadius   = 120f;

        private float _swanCooldown;
        private float _swanBoostTimer;
        private float _preSwanMoveSpeed;
        private const float SwanInterval      = 18f;  // bonds >= 9: speed burst every 18s
        private const float SwanBoostDuration =  2.5f;

        private float _zaraFlashTimer;
        public bool   ZaraFlashing      => _zaraFlashTimer > 0;
        public bool   AmeliaBoostActive => _ameliaBoostTimer > 0;
        public bool   OrcaFlashing      => _orcaFlashTimer > 0;
        public bool   SwanBoostActive   => _swanBoostTimer > 0;

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
                    _preBoostMoveSpeed   = player.MoveSpeed;
                    _preBoostAttackDamage = player.AttackDamage;
                    _ameliaBoostTimer    = AmeliaBoostDuration;
                    _ameliaCooldown      = AmeliaInterval;
                    player.MoveSpeed     *= 1.4f;
                    player.AttackDamage  = (int)(player.AttackDamage * 1.5f);
                }
            }

            if (_ameliaBoostTimer > 0)
            {
                _ameliaBoostTimer -= dt;
                if (_ameliaBoostTimer <= 0)
                {
                    player.MoveSpeed    = _preBoostMoveSpeed;
                    player.AttackDamage = _preBoostAttackDamage;
                }
            }

            if (_zaraFlashTimer > 0) _zaraFlashTimer -= dt;

            // ── Orca area slam ──────────────────────────────────────────
            if (bonds >= 6)
            {
                _orcaCooldown -= dt;
                if (_orcaCooldown <= 0)
                {
                    FireOrca(player, enemies);
                    _orcaCooldown = OrcaInterval;
                }
            }
            if (_orcaFlashTimer > 0) _orcaFlashTimer -= dt;

            // ── Swan speed burst ─────────────────────────────────────────
            if (bonds >= 9)
            {
                _swanCooldown -= dt;
                if (_swanCooldown <= 0)
                {
                    TriggerSwan(player);
                    _swanCooldown = SwanInterval;
                }
            }
            if (_swanBoostTimer > 0)
            {
                _swanBoostTimer -= dt;
                if (_swanBoostTimer <= 0)
                    player.MoveSpeed = _preSwanMoveSpeed;
            }

            // Update projectiles
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                Projectiles[i].Update(dt);
                if (!Projectiles[i].IsAlive) Projectiles.RemoveAt(i);
            }

            // Check projectile hits
            foreach (var proj in Projectiles)
            {
                if (!proj.IsAlive) continue;
                foreach (var e in enemies)
                {
                    if (!e.IsAlive || !proj.IsAlive) continue;
                    if (Math.Abs(proj.X - e.CenterX) < 20 &&
                        Math.Abs(proj.Y - e.CenterY) < 30)
                    {
                        e.TakeDamage((int)ZaraDamage);
                        e.ApplyEffect(StatusEffect.Stunned, 0.6f);
                        proj.Life = 0;
                    }
                }
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

        private void FireOrca(Player player, List<Enemy> enemies)
        {
            bool hit = false;
            foreach (var e in enemies)
            {
                if (!e.IsAlive) continue;
                if (player.DistanceTo(e) <= OrcaRadius)
                {
                    e.TakeDamage((int)OrcaDamage);
                    e.ApplyEffect(StatusEffect.Stunned, 0.8f);
                    hit = true;
                }
            }
            if (hit)
            {
                _orcaFlashTimer = 0.3f;
                Game.Instance.Audio.BeepStomp();
            }
        }

        private void TriggerSwan(Player player)
        {
            _preSwanMoveSpeed = player.MoveSpeed;
            _swanBoostTimer   = SwanBoostDuration;
            player.MoveSpeed  = _preSwanMoveSpeed * 1.35f;
            Game.Instance.Audio.BeepJump();
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
                if (bonds >= 6)
                {
                    float orcaPct = 1f - (_orcaCooldown / OrcaInterval);
                    string oLabel = $"Orca({(int)Math.Min(100, orcaPct * 100)}%)";
                    g.DrawString(oLabel, f, OrcaFlashing ? Brushes.DeepSkyBlue : Brushes.LightGray, x, y + 28);
                }
                if (bonds >= 9)
                {
                    float swanPct = 1f - (_swanCooldown / SwanInterval);
                    string sLabel = $"Swan({(int)Math.Min(100, swanPct * 100)}%)";
                    g.DrawString(sLabel, f, SwanBoostActive ? Brushes.Gold : Brushes.LightGray, x + 80, y + 28);
                }
            }
        }
    }
}
