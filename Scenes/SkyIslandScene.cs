using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Fridays_Adventure.Abilities;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Hazards;
using Fridays_Adventure.Rules;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    public sealed class SkyIslandScene : Scene
    {
        private Player             _player;
        private List<Enemy>        _enemies   = new List<Enemy>();
        private List<Rectangle>    _platforms = new List<Rectangle>();
        private List<Hazard>       _hazards   = new List<Hazard>();
        private List<IceWallInstance> _iceWalls = new List<IceWallInstance>();

        private const int LevelWidth  = 900;
        private const int LevelHeight = 3200;

        private float _cameraY;
        private float _windForce;
        private float _windTimer;
        private bool  _windActive;
        private float _windAnnouncTimer;
        private int   _jumpsLeft;
        private float _doubleJumpDelay;
        private const float DoubleJumpCooldown = 0.18f;

        private float _sinkMashTimer;
        private bool  _showRescue;
        private float _rescueTimer;
        private bool  _levelComplete;
        private float _completeTimer;
        private Rectangle _exitZone;

        private Bitmap _bg;
        private static readonly Font       _hud       = new Font("Courier New", 9, FontStyle.Bold);
        private static readonly Font       _hud8      = new Font("Courier New", 8);
        private static readonly SolidBrush _cloudBrush   = new SolidBrush(Color.FromArgb(120, 255, 255, 255));
        private static readonly SolidBrush _platTopBrush = new SolidBrush(Color.FromArgb(200, 200, 255));
        private readonly Random _rng = new Random();

        public override void OnEnter()
        {
            BuildLevel();
            LoadBackground();
            Game.Instance.Audio.ContinueOrPlay("island");
        }

        private void LoadBackground()
        {
            // Background is in Assets\Sprites\ — use the proper named file
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                       "Assets", "Sprites", "bg_Ancient_ruins_island.png");
            if (!File.Exists(path))
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    "Assets", "Sprites", "bg_Volcano_island.png");

            if (File.Exists(path))
            {
                int W = Game.Instance.CanvasWidth;
                int H = Game.Instance.CanvasHeight;
                using (var raw = new Bitmap(path))
                {
                    _bg = new Bitmap(W, H);
                    using (var gr = Graphics.FromImage(_bg))
                        gr.DrawImage(raw, 0, 0, W, H);
                }
            }
        }

        public override void OnExit()  { _bg?.Dispose(); _bg = null; }
        public override void OnResume() => Game.Instance.Audio.ContinueOrPlay("island");

        // ── Level ├───────────────────────────────────────────────────────────

        private void BuildLevel()
        {
            int W = LevelWidth;
            int groundY = LevelHeight - 60;
            _exitZone = new Rectangle(W / 2 - 40, 60, 80, 50);

            // Ground at the bottom
            _platforms.Add(new Rectangle(0, groundY, W, 60));

            // Ascending platform staircase (12 platforms)
            var rng = new System.Random(42);
            int[] xPositions = { 80, 250, 500, 650, 150, 420, 600, 200, 480, 100, 380, 560 };
            int[] widths      = { 180, 160, 200, 170, 190, 175, 185, 200, 165, 180, 195, 210 };

            for (int i = 0; i < 12; i++)
            {
                int py = groundY - 220 - i * 250;
                int px = xPositions[i % xPositions.Length];
                int pw = widths[i % widths.Length];
                _platforms.Add(new Rectangle(px, py, pw, 18));

                // Enemies on alternating platforms
                if (i % 3 == 1)
                {
                    var e = new Enemy(px + 30, py - 48, 32, 48,
                                      maxHp: 30 + i * 5,
                                      patrolLeft: px + 4, patrolRight: px + pw - 36);
                    e.MoveSpeed = 140f;
                    // Sky enemies use enemy_Cloud_Lancer model from Assets\Sprites\
                    var spr = SpriteManager.GetScaled("enemy_Cloud_Lancer.png", 32, 48);
                    if (spr != null) e.Sprite = spr;
                    _enemies.Add(e);
                }
                // Fire on some platforms
                if (i % 4 == 2)
                    _hazards.Add(new FireSource(px + pw - 44, py - 44, 36, 44));
                // SeaStone on one platform
                if (i == 7)
                    _hazards.Add(new SeaStoneZone(px, py - 36, pw, 36));
            }

            // Player at the bottom (selected from Crew screen)
            _player = new Player(W / 2 - 18, groundY - 56);
            _player.MoveSpeed = 290f;
            _player.JumpForce = -520f;
            _player.ApplySelectedSprite();
        }

        // ── Update ────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            if (_levelComplete) { _completeTimer += dt; if (_completeTimer >= 1.0f) { Game.Instance.LevelJustCompleted = true; Game.Instance.Scenes.Pop(); } return; }

            UpdateWind(dt);
            HandleInput(dt);
            _player.Update(dt);
            MoveAndCollide(_player, dt);
            foreach (var hz in _hazards) hz.Update(dt);
            DevilFruitRules.Check(_player, _hazards, dt);
            IceSystem.Update(_iceWalls, _hazards, _player, dt);
            CheckWater(dt);
            UpdateEnemies(dt);
            CheckCombat();
            CheckExit();
            UpdateCamera();
        }

        private void UpdateWind(float dt)
        {
            _windTimer -= dt;
            if (_windTimer <= 0)
            {
                _windActive   = !_windActive;
                _windTimer    = _windActive ? 1.8f : 3.5f;
                if (_windActive)
                {
                    _windForce        = (_rng.Next(0, 2) == 0 ? -1f : 1f) * 160f;
                    _windAnnouncTimer  = 1.0f;
                }
                else _windForce = 0;
            }
            if (_windAnnouncTimer > 0) _windAnnouncTimer -= dt;
        }

        private void HandleInput(float dt)
        {
            var input = Game.Instance.Input;
            if (_player.HasEffect(StatusEffect.Sinking) || _player.HasEffect(StatusEffect.Stunned)) return;

            float moveX = 0;
            if (input.LeftHeld)       { moveX = -_player.MoveSpeed; _player.FacingRight = false; }
            else if (input.RightHeld) { moveX =  _player.MoveSpeed; _player.FacingRight = true; }
            _player.VelocityX = moveX + (_windActive ? _windForce : 0);

            if (_doubleJumpDelay > 0) _doubleJumpDelay -= dt;
            if (_player.IsGrounded) { _jumpsLeft = 2; _doubleJumpDelay = 0; }
            if (input.JumpPressed && _jumpsLeft > 0 && _doubleJumpDelay <= 0)
            {
                bool isSecondJump = _jumpsLeft < 2;
                _player.VelocityY = _player.JumpForce;
                _player.IsGrounded = false;
                _jumpsLeft--;
                if (!isSecondJump) _doubleJumpDelay = DoubleJumpCooldown;
                Game.Instance.Audio.BeepJump();
            }
            // Variable jump height — release early for short hop (SMB3-style)
            if (!input.JumpHeld && _player.VelocityY < -120f)
                _player.VelocityY = -120f;
            if (input.DodgePressed)  _player.TryDodge();
            if (input.AttackPressed && _player.TryAttack()) Game.Instance.Audio.BeepAttack();
            if (input.Ability1Pressed && _player.UseIceWall(out IceWallInstance w))
            { _iceWalls.Add(w); Game.Instance.Audio.BeepIce(); }
            if (input.Ability2Pressed && _player.UseFlashFreeze())
            {
                float freezeSeconds = _player.GetFlashFreezeDuration(2.5f);
                foreach (var e in _enemies)
                    if (_player.DistanceTo(e) <= 130f)
                        e.ApplyEffect(StatusEffect.Frozen, freezeSeconds);
                Game.Instance.Audio.BeepFreeze();
            }
            if (input.Ability3Pressed && _player.UseBreakWall())
            { BreakNearbyWalls(); Game.Instance.Audio.BeepBreak(); }
            if (input.PausePressed) Game.Instance.Scenes.Push(new PauseScene());
            // ── I key — quick-open inventory during gameplay ──────────────────
            if (input.InventoryPressed) Game.Instance.Scenes.Push(new InventoryScene(_player));
        }

        private void MoveAndCollide(Character c, float dt)
        {
            c.X = Math.Max(0, Math.Min(LevelWidth - c.Width, c.X + c.VelocityX * dt));
            ResolveH(c);
            c.Y += c.VelocityY * dt;
            c.IsGrounded = false;
            ResolveV(c);
            if (c.Y > LevelHeight) { if (c == _player) _player.TakeDamage(9999); else c.Health = 0; }
        }

        private void ResolveH(Character c)
        {
            foreach (var p in _platforms) if (c.Hitbox.IntersectsWith(p))
            { c.X = c.VelocityX > 0 ? p.Left - c.Width : p.Right; c.VelocityX = 0; }
            foreach (var wall in _iceWalls)
            {
                if (!wall.IsAlive) continue;
                var wb = wall.Hitbox;
                if (!c.Hitbox.IntersectsWith(wb)) continue;
                c.X = c.VelocityX > 0 ? wb.Left - c.Width : wb.Right;
                c.VelocityX = 0;
            }
        }

        private void ResolveV(Character c)
        {
            foreach (var p in _platforms)
            {
                if (!c.Hitbox.IntersectsWith(p)) continue;
                if (c.VelocityY >= 0) { c.Y = p.Top - c.Height; c.VelocityY = 0; c.IsGrounded = true; }
                else { c.Y = p.Bottom; c.VelocityY = 0; }
            }
            foreach (var wall in _iceWalls)
            {
                if (!wall.IsAlive) continue;
                var wb = wall.Hitbox;
                if (!c.Hitbox.IntersectsWith(wb)) continue;
                if (c.VelocityY >= 0) { c.Y = wb.Top - c.Height; c.VelocityY = 0; c.IsGrounded = true; }
                else { c.Y = wb.Bottom; c.VelocityY = 0; }
            }
        }

        private void CheckWater(float dt)
        {
            if (!_player.HasEffect(StatusEffect.Sinking)) { _sinkMashTimer = 0; _showRescue = false; return; }
            _sinkMashTimer += dt;
            if (Game.Instance.Input.AnyMash) _sinkMashTimer = Math.Max(0, _sinkMashTimer - 0.4f);
            if (_sinkMashTimer >= RescueSystem.GetMashWindow(Game.Instance.CrewBonds))
            { _showRescue = RescueSystem.AutoRescueAvailable(Game.Instance.CrewBonds); _rescueTimer = 0; }
            if (_showRescue) { _rescueTimer += dt; if (_rescueTimer >= 0.8f) RescueSystem.ApplyRescue(_player, ref _sinkMashTimer); }
        }

        private void UpdateEnemies(float dt)
        {
            foreach (var e in _enemies) { if (!e.IsAlive) continue; e.UpdateWithTarget(dt, _player); MoveAndCollide(e, dt); }
        }

        private void CheckCombat()
        {
            var pAtk = _player.AttackHitbox;
            foreach (var e in _enemies)
            {
                if (!e.IsAlive) continue;
                bool stomped = false;

                // Head stomp — eliminate enemy + bounce
                if (_player.VelocityY > 0 && !_player.IsInvincible)
                {
                    float pBot = _player.Y + _player.Height;
                    float overlap = pBot - e.Y;
                    // Use half the enemy's height as the overlap threshold so fast falls still register as stomps
                    if (overlap > 0 && overlap < e.Height * 0.5f &&
                        _player.CenterX > e.X - 8 && _player.CenterX < e.X + e.Width + 8)
                    {
                        e.Health = 0;
                        _player.VelocityY = _player.JumpForce * 0.45f;
                        _player.IsGrounded = false;
                        Game.Instance.Audio.BeepStomp();
                        BountySystem.Award(e.ScoreValue);
                        Game.Instance.TotalBerriesCollected += 10;
                        stomped = true;
                    }
                }

                // Regular attack
                if (pAtk != Rectangle.Empty && pAtk.IntersectsWith(e.Hitbox))
                {
                    bool wasAlive = e.IsAlive;
                    e.TakeDamage(_player.AttackDamage);
                    if (wasAlive && !e.IsAlive)
                    {
                        BountySystem.Award(e.ScoreValue);
                        Game.Instance.TotalBerriesCollected += 10;
                    }
                }

                // Horizontal body contact — skip if the player is clearly descending onto the enemy's head
                bool fallingOnTop = _player.VelocityY > 0 &&
                    (_player.Y + _player.Height) < (e.Y + e.Height * 0.6f);
                if (!stomped && !fallingOnTop && e.IsAlive && !_player.IsInvincible &&
                    _player.Hitbox.IntersectsWith(e.Hitbox))
                {
                    _player.TakeDamage(_player.MaxHealth / 10);
                    Game.Instance.Audio.BeepHurt();
                }
            }
            if (!_player.IsAlive) Game.Instance.Scenes.Replace(new GameOverScene());
        }

        private void CheckExit()
        {
            if (!_levelComplete && _player.Hitbox.IntersectsWith(_exitZone))
            {
                _levelComplete = true;
                Game.Instance.Audio.BeepLevelClear();
                Game.Instance.ThreatLevel  = Math.Max(0, Game.Instance.ThreatLevel - 8);
                Game.Instance.CrewBonds++;
                Game.Instance.PlayerBounty += 600;
                Game.Instance.Save.SetFlag("sky_complete");
                Game.Instance.Save.Save();
            }
        }

        private void BreakNearbyWalls()
        {
            const float range = 200f;
            for (int i = _iceWalls.Count - 1; i >= 0; i--)
            {
                var wall = _iceWalls[i];
                if (!wall.IsAlive) continue;
                float dx = _player.CenterX - (wall.X + wall.Width / 2f);
                float dy = _player.CenterY - (wall.Y + wall.Height / 2f);
                if ((float)Math.Sqrt(dx * dx + dy * dy) <= range)
                {
                    wall.Health = 0;
                    Game.Instance.FloatingText.Spawn(
                        "SMASH!",
                        (int)(wall.X + wall.Width / 2f),
                        (int)wall.Y,
                        Color.OrangeRed, large: true);
                }
            }
            foreach (var e in _enemies)
                if (e.IsAlive && _player.DistanceTo(e) <= 120f)
                    e.TakeDamage(_player.BreakWallShockwaveDamage);
        }

        private void UpdateCamera()
        {
            float targetY = _player.Y - Game.Instance.CanvasHeight * 0.55f;
            _cameraY = Math.Max(0, Math.Min(LevelHeight - Game.Instance.CanvasHeight, targetY));
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            if (_bg != null) g.DrawImage(_bg, 0, 0, W, H);
            else using (var br = new LinearGradientBrush(new Rectangle(0,0,W,H), Color.FromArgb(100,160,230), Color.FromArgb(200,230,255), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            g.TranslateTransform(0, -_cameraY);

            // Clouds
            DrawClouds(g);
            // Platforms
            foreach (var p in _platforms)
            {
                // Fluffy cloud platforms: soft white base with a sky-blue tint strip
                using (var br = new SolidBrush(Color.FromArgb(230, 240, 250, 255)))
                    g.FillRectangle(br, p);
                // Rounded cloud puff bumps along the top edge
                int bumpCount = Math.Max(1, p.Width / 28);
                for (int b = 0; b < bumpCount; b++)
                {
                    int bx = p.Left + 4 + b * (p.Width - 8) / Math.Max(1, bumpCount - 1);
                    using (var br = new SolidBrush(Color.FromArgb(245, 250, 255, 255)))
                        g.FillEllipse(br, bx - 16, p.Top - 10, 32, 20);
                }
                // Sky-blue highlight strip at the very top
                using (var br = new SolidBrush(Color.FromArgb(180, 200, 220, 255)))
                    g.FillRectangle(br, p.X, p.Y, p.Width, 4);
                // Soft shadow on the underside
                using (var br = new SolidBrush(Color.FromArgb(40, 100, 140, 200)))
                    g.FillRectangle(br, p.X + 4, p.Bottom - 5, p.Width - 8, 5);
            }
            // Exit zone
            using (var br = new SolidBrush(Color.FromArgb(120, Color.Gold))) g.FillRectangle(br, _exitZone);
            using (var f = new Font("Arial", 8)) g.DrawString("EXIT", f, Brushes.DarkGoldenrod, _exitZone.X + 20, _exitZone.Y + 16);

            foreach (var hz in _hazards)   hz.Draw(g);
            foreach (var w  in _iceWalls)  w.Draw(g);
            foreach (var e  in _enemies)   if (e.IsAlive) e.Draw(g);
            _player.Draw(g);

            g.ResetTransform();
            // ── Unified HUD (single call) ─────────────────────────────────────
            GameHUD.Draw(g, _player, W, H);
            if (_windAnnouncTimer > 0) DrawWindWarning(g, W, H);
            if (_showRescue) DrawRescuePrompt(g, W, H);
            if (_levelComplete) DrawComplete(g, W, H);
            DrawDevMenuButton(g);
        }

        private void DrawClouds(Graphics g)
        {
            float[] cx = { 100, 350, 600, 200, 500, 750 };
            float[] cy = { 400, 900, 1500, 2000, 2500, 800 };
            for (int i = 0; i < cx.Length; i++)
                g.FillEllipse(_cloudBrush, cx[i], cy[i], 180, 60);
        }

        private void DrawHUD(Graphics g, int W, int H)
        {
            g.FillRectangle(Brushes.Black, 0, 0, W, 60);
            g.DrawString("HP", _hud, Brushes.White, 6, 6);
            g.FillRectangle(Brushes.DarkRed, 30, 8, 130, 12);
            using (var br = new SolidBrush(Color.LimeGreen))
                g.FillRectangle(br, 30, 8, (int)(130 * (float)_player.Health / _player.MaxHealth), 12);
            g.DrawString("ICE", _hud, Brushes.Cyan, 6, 22);
            g.FillRectangle(Brushes.DarkSlateBlue, 30, 24, 100, 10);
            using (var br = new SolidBrush(Color.FromArgb(180,220,255)))
                g.FillRectangle(br, 30, 24, (int)(100 * (float)_player.IceReserve / _player.MaxIceReserve), 10);
            g.DrawString("SCORE", _hud, Brushes.Gold, 6, 36);
            g.DrawString(BountySystem.Formatted(), _hud8, Brushes.Gold, 56, 37);
            g.DrawString($"Berries: {Game.Instance.TotalBerriesCollected}", _hud8, Brushes.Gold, 6, 50);
            {
                int pct = (int)(100f * (1f - _cameraY / Math.Max(1, LevelHeight - H)));
                g.DrawString($"Sky Island   Altitude: {pct}%", _hud8, Brushes.LightBlue, W - 200, 6);
                float iceRem = _player.IceWallCooldownRemaining;
                string wallState = iceRem > 0.05f ? $"Q:Wall[{iceRem:F1}s]" : "Q:Wall[RDY]";
                g.DrawString($"{wallState}  E:Freeze  R:Break  X:Dodge  Z:Attack", _hud8, Brushes.Gray, W - 380, 22);
            }
        }

        private static readonly Font _windFont = new Font("Courier New", 14, FontStyle.Bold);

        private void DrawWindWarning(Graphics g, int W, int H)
        {
            string dir = _windForce < 0 ? "◄ WIND ◄" : "► WIND ►";
            SizeF sz = g.MeasureString(dir, _windFont);
            int alpha = Math.Max(0, Math.Min(255, (int)(180 * _windAnnouncTimer)));
            using (var br = new SolidBrush(Color.FromArgb(alpha, Color.DeepSkyBlue)))
                g.DrawString(dir, _windFont, br, (W - sz.Width) / 2f, H * 0.12f);
        }

        private void DrawRescuePrompt(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, Color.DarkBlue)))
                g.FillRectangle(br, W/2-180, H/2-28, 360, 56);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Zara fires a grapple!\nMash  SPACE / Z / X  to grab!", f, Brushes.Cyan, W/2-168, H/2-22);
        }

        private void DrawComplete(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 22, FontStyle.Bold))
            { SizeF sz = g.MeasureString("SKY ISLAND CLEARED!", f); g.DrawString("SKY ISLAND CLEARED!", f, Brushes.Gold, (W-sz.Width)/2f, H*0.38f); }
            using (var f = new Font("Courier New", 11))
                g.DrawString("+600 Bounty   +1 Crew Bond", f, Brushes.White, W/2f - 120, H*0.38f + 44);
        }
    }
}
