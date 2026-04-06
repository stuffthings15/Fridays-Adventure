using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  AirshipLevelScene.cs  —  Level Designer: Ideas 5-7 (Airship level)
    //
    //  Idea 5: Airship level — auto-scrolling deck with cannons and moving pipes.
    //  Idea 6: Auto-scroll camera lock — player cannot scroll backward.
    //  Idea 7: Cannonball projectiles — fired from deck turrets on a timer.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Airship level — SMB3-faithful auto-scrolling deck with cannons,
    /// moving pipes, and a locked forward-scroll camera.
    /// Team 5 (Level Designer) — Ideas 5–7.
    /// </summary>
    public sealed class AirshipLevelScene : Scene
    {
        private const float LevelScale = 1.5f;
        private Player _player;
        private List<Rectangle> _platforms = new List<Rectangle>();
        private readonly List<Abilities.IceWallInstance> _iceWalls = new List<Abilities.IceWallInstance>();
        private float _cannonFreezeTimer;

        // ── Auto-scroll (Idea 6) ──────────────────────────────────────────────
        private float _scrollX;           // camera X (world scroll offset)
        private const float ScrollSpeed = 60f;  // px/s

        // ── Cannons (Idea 7) ──────────────────────────────────────────────────
        private struct Cannon
        {
            public float X, Y;
            public float Timer;
            public float FireInterval;
        }
        private List<Cannon>    _cannons    = new List<Cannon>();
        private List<Cannonball> _balls     = new List<Cannonball>();

        private struct Cannonball
        {
            public float X, Y, VX, VY;
            public bool Active;
        }

        // ── Moving pipes (Idea 5) ─────────────────────────────────────────────
        private struct MovingPipe
        {
            public float X, Y, VY, MinY, MaxY;
        }
        private List<MovingPipe> _pipes = new List<MovingPipe>();

        // ── Level state ───────────────────────────────────────────────────────
        private bool  _levelComplete;
        private float _completeTimer;
        private Rectangle _exitFlag;
        private const float WorldWidth = 3600f;

        // ── Background ────────────────────────────────────────────────────────
        private Bitmap _bg;

        // ── Font ─────────────────────────────────────────────────────────────
        private static readonly Font _hud = new Font("Courier New", 9, FontStyle.Bold);

        public override void OnEnter()
        {
            DebugLogger.PushBreadcrumb("AirshipLevelScene.OnEnter");

            // Background — bg_Tempest_Strait.png lives in Assets\Sprites\
            _bg = SpriteManager.Get("bg_Tempest_Strait.png");
            
            BuildLevel();
            Game.Instance.Audio.ContinueOrPlay("combat");
            SMB3Hud.TriggerGetReady();
            SMB3Hud.ShowWorldLabel($"AIRSHIP  {Game.Instance.WorldLevelLabel}");
        }

        public override void OnExit()  { _bg?.Dispose(); _bg = null; }
        public override void OnResume() => Game.Instance.Audio.ContinueOrPlay("combat");

        private void BuildLevel()
        {
            int H = Game.Instance.CanvasHeight;

            // Player archetype is resolved automatically from Game.Instance.SelectedCharacter
            // inside the Player(x, y) constructor — no need to pass extra arguments.
            _player = new Player(120, H - 160);
            _iceWalls.Clear();
            _cannonFreezeTimer = 0f;

            // Deck platforms spanning the world width.
            _platforms.Clear();
            _platforms.Add(new Rectangle(0,    H - 60, (int)WorldWidth, 60));  // deck
            _platforms.Add(new Rectangle(400,  H - 160, 140, 20));
            _platforms.Add(new Rectangle(700,  H - 200, 120, 20));
            _platforms.Add(new Rectangle(1050, H - 160, 160, 20));
            _platforms.Add(new Rectangle(1400, H - 200, 140, 20));
            _platforms.Add(new Rectangle(1800, H - 160, 140, 20));
            _platforms.Add(new Rectangle(2200, H - 200, 120, 20));
            _platforms.Add(new Rectangle(2600, H - 160, 160, 20));
            _platforms.Add(new Rectangle(3000, H - 200, 140, 20));
            _platforms.Add(new Rectangle(3300, H - 160, 200, 20));

            // Cannons.
            _cannons.Clear();
            _cannons.Add(new Cannon { X = 600,  Y = H - 100, Timer = 0, FireInterval = 3.5f });
            _cannons.Add(new Cannon { X = 1200, Y = H - 100, Timer = 0, FireInterval = 2.8f });
            _cannons.Add(new Cannon { X = 1900, Y = H - 100, Timer = 0, FireInterval = 3.2f });
            _cannons.Add(new Cannon { X = 2500, Y = H - 100, Timer = 0, FireInterval = 2.5f });
            _cannons.Add(new Cannon { X = 3100, Y = H - 100, Timer = 0, FireInterval = 2.0f });

            // Moving pipes (Idea 5).
            _pipes.Clear();
            AddPipe(850,  H - 160, H - 260, H - 100);
            AddPipe(1500, H - 160, H - 280, H - 100);
            AddPipe(2300, H - 200, H - 320, H - 100);

            // Exit flagpole.
            _exitFlag = new Rectangle((int)WorldWidth - 100, H - 200, 40, 140);

            ApplyLevelScale();
        }

                private void ApplyLevelScale()
        {
            Rectangle ScaleRect(Rectangle r) => new Rectangle(
                (int)(r.X * LevelScale),
                (int)(r.Y * LevelScale),
                (int)(r.Width * LevelScale),
                (int)(r.Height * LevelScale));

            for (int i = 0; i < _platforms.Count; i++) _platforms[i] = ScaleRect(_platforms[i]);
            _exitFlag = ScaleRect(_exitFlag);

            for (int i = 0; i < _cannons.Count; i++)
            {
                var c = _cannons[i];
                c.X *= LevelScale;
                c.Y *= LevelScale;
                _cannons[i] = c;
            }

            for (int i = 0; i < _pipes.Count; i++)
            {
                var p2 = _pipes[i];
                p2.X *= LevelScale; p2.Y *= LevelScale; p2.MinY *= LevelScale; p2.MaxY *= LevelScale;
                _pipes[i] = p2;
            }

            _player.X *= LevelScale;
            _player.Y *= LevelScale;
            _player.Width = (int)(_player.Width * LevelScale);
            _player.Height = (int)(_player.Height * LevelScale);
            _player.ApplySelectedSprite();
        }
        private void AddPipe(float x, float y, float minY, float maxY)
        {
            _pipes.Add(new MovingPipe { X = x, Y = y, VY = 60f, MinY = minY, MaxY = maxY });
        }

        public override void Update(float dt)
        {
            if (_levelComplete) { _completeTimer -= dt; if (_completeTimer <= 0f) CompleteLevel(); return; }

            var input = Game.Instance.Input;
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Auto-scroll (Idea 6) ───────────────────────────────────────────
            _scrollX += ScrollSpeed * dt;
            // Clamp player to avoid backward scrolling.
            float minPlayerX = _scrollX + 40f;
            if (_player.X < minPlayerX) _player.X = minPlayerX;

            // ── Player movement ───────────────────────────────────────────────
            bool left  = input.IsHeld(System.Windows.Forms.Keys.Left)  || input.IsHeld(System.Windows.Forms.Keys.A);
            bool right = input.IsHeld(System.Windows.Forms.Keys.Right) || input.IsHeld(System.Windows.Forms.Keys.D);
            bool jump  = input.IsPressed(System.Windows.Forms.Keys.Space) || input.IsPressed(System.Windows.Forms.Keys.W);

            if (left)  _player.VelocityX = -_player.MoveSpeed;
            else if (right) _player.VelocityX = _player.MoveSpeed;
            else _player.VelocityX = 0;

            if (jump && _player.IsGrounded) { _player.VelocityY = _player.JumpForce; _player.IsGrounded = false; }

            // Character abilities (Q/E/R) in airship map.
            if (input.Ability1Pressed && _player.UseIceWall(out Abilities.IceWallInstance wall))
            {
                _iceWalls.Add(wall);
                Game.Instance.Audio.BeepIce();
            }
            if (input.Ability2Pressed && _player.UseFlashFreeze())
            {
                _cannonFreezeTimer = _player.GetFlashFreezeDuration(2.0f);
                Game.Instance.Audio.BeepFreeze();
            }
            if (input.Ability3Pressed && _player.UseBreakWall())
            {
                BreakNearbyWallsAndProjectiles();
                Game.Instance.Audio.BeepBreak();
            }
            // Pause and inventory consistent with all other gameplay scenes
            if (input.PausePressed) Game.Instance.Scenes.Push(new PauseScene());
            if (input.InventoryPressed) Game.Instance.Scenes.Push(new InventoryScene(_player));

            _player.Update(dt);
            _player.X += _player.VelocityX * dt;
            _player.Y += _player.VelocityY * dt;

            // ── Platform collisions ───────────────────────────────────────────
            _player.IsGrounded = false;
            foreach (var p in _platforms) ResolvePlayerPlatform(p);
            foreach (var iceWall in _iceWalls)
                if (iceWall.IsAlive) ResolvePlayerPlatform(iceWall.Hitbox);

            // Update temporary ice walls.
            for (int i = _iceWalls.Count - 1; i >= 0; i--)
            {
                _iceWalls[i].Update(dt, nearFire: false);
                if (!_iceWalls[i].IsAlive) _iceWalls.RemoveAt(i);
            }

            if (_cannonFreezeTimer > 0f) _cannonFreezeTimer -= dt;

            // Moving pipes as platforms.
            for (int i = 0; i < _pipes.Count; i++)
            {
                var pipe = _pipes[i];
                pipe.Y += pipe.VY * dt;
                if (pipe.Y <= pipe.MinY) { pipe.Y = pipe.MinY; pipe.VY = Math.Abs(pipe.VY); }
                if (pipe.Y >= pipe.MaxY) { pipe.Y = pipe.MaxY; pipe.VY = -Math.Abs(pipe.VY); }
                _pipes[i] = pipe;

                var pipeRect = new Rectangle((int)pipe.X, (int)pipe.Y, 40, 80);
                ResolvePlayerPlatform(pipeRect);
            }

            // ── Cannon update (Idea 7) ────────────────────────────────────────
            for (int i = 0; i < _cannons.Count; i++)
            {
                var c = _cannons[i];
                if (_cannonFreezeTimer <= 0f)
                {
                    c.Timer += dt;
                    if (c.Timer >= c.FireInterval)
                    {
                        c.Timer = 0;
                        _balls.Add(new Cannonball
                        {
                            X = c.X, Y = c.Y - 10,
                            VX = -200f, VY = 0f,
                            Active = true
                        });
                    }
                }
                _cannons[i] = c;
            }

            // Move and collide cannonballs.
            for (int i = _balls.Count - 1; i >= 0; i--)
            {
                var b = _balls[i];
                b.X += b.VX * dt;
                b.Y += b.VY * dt;
                b.VY += Entities.Character.Gravity * dt * 0.3f;

                var bRect = new Rectangle((int)b.X, (int)b.Y, 12, 12);
                var pRect = new Rectangle((int)_player.X, (int)_player.Y, _player.Width, _player.Height);

                if (bRect.IntersectsWith(pRect)) { _player.TakeDamage(1); b.Active = false; }
                if (b.X < _scrollX - 50) b.Active = false;

                // Cannonballs break on ice walls.
                for (int w = 0; w < _iceWalls.Count; w++)
                {
                    if (_iceWalls[w].IsAlive && bRect.IntersectsWith(_iceWalls[w].Hitbox))
                    {
                        b.Active = false;
                        _iceWalls[w].Health -= 20f;
                        break;
                    }
                }

                _balls[i] = b;
                if (!b.Active) _balls.RemoveAt(i);
            }

            // ── Exit check ────────────────────────────────────────────────────
            var playerRect = new Rectangle((int)_player.X, (int)_player.Y, _player.Width, _player.Height);
            if (_exitFlag.IntersectsWith(playerRect) && !_levelComplete)
            {
                _levelComplete = true;
                _completeTimer = 0.7f;
            }

            // ── Death / fall check ───────────────────────────────────────────
            if (!_player.IsAlive || _player.X < _scrollX - _player.Width)
                HandleDeath();
            else if (_player.Y > H + 100)
            {
                // No fall damage: reset to a safe deck position near the current camera.
                _player.X = _scrollX + 120f;
                _player.Y = H - 160;
                _player.VelocityX = 0f;
                _player.VelocityY = 0f;
                _player.IsGrounded = false;
                _player.GrantInvincibility(0.6f);
            }

            SMB3Hud.Update(dt);
        }

        private void ResolvePlayerPlatform(Rectangle p)
        {
            var pr = new Rectangle((int)_player.X, (int)_player.Y, _player.Width, _player.Height);
            if (!pr.IntersectsWith(p)) return;
            float overBottom = pr.Bottom - p.Top;
            float overTop    = p.Bottom  - pr.Top;
            if (overBottom < overTop && _player.VelocityY > 0)
            {
                _player.Y = p.Top - _player.Height;
                _player.VelocityY = 0; _player.IsGrounded = true;
            }
            else if (overTop < overBottom && _player.VelocityY < 0)
            {
                _player.Y = p.Bottom; _player.VelocityY = 0;
            }
        }

        private void HandleDeath()
        {
            SessionStats.Instance.RecordDeath();
            DebugLogger.LogInfo("AirshipLevelScene", "Player died on airship.");
            Game.Instance.Scenes.Pop();
        }

        private void CompleteLevel()
        {
            DebugLogger.LogInfo("AirshipLevelScene", "Airship cleared!");
            Game.Instance.LevelJustCompleted = true;
            SessionStats.Instance.RecordLevelComplete();
            AchievementSystem.Grant("ach_first_step");
            Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Background ────────────────────────────────────────────────────
            // PHASE 2 - Team 14: Environment Artist - Draw airship background if loaded
            if (_bg != null)
            {
                g.DrawImage(_bg, 0, 0, W, H);
            }
            else
            {
                // Fallback: sky gradient
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                    Color.FromArgb(30, 50, 120), Color.FromArgb(60, 90, 160), 90f))
                    g.FillRectangle(br, 0, 0, W, H);

                // Cloud streaks.
                using (var br = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                {
                    g.FillRectangle(br, 50,  80, 140, 18);
                    g.FillRectangle(br, 300, 50, 200, 20);
                    g.FillRectangle(br, 600, 110, 120, 16);
                    g.FillRectangle(br, 750, 40,  180, 22);
                }
            }

            // Scroll offset.
            float camX = _scrollX;
            g.TranslateTransform(-camX, 0);

            // ── Deck (metal plates) ───────────────────────────────────────────
            foreach (var p in _platforms)
            {
                using (var br = new SolidBrush(Color.FromArgb(100, 95, 80)))
                    g.FillRectangle(br, p);
                // Rivet marks.
                using (var pen = new Pen(Color.FromArgb(70, 65, 55), 1))
                    g.DrawRectangle(pen, p);
            }

            // ── Moving pipes (Idea 5) ─────────────────────────────────────────
            foreach (var pipe in _pipes)
            {
                var pRect = new Rectangle((int)pipe.X, (int)pipe.Y, 40, 80);
                using (var br = new SolidBrush(Color.ForestGreen))
                    g.FillRectangle(br, pRect);
                using (var pen = new Pen(Color.DarkGreen))
                    g.DrawRectangle(pen, pRect);
            }

            // ── Cannons (Idea 7) ──────────────────────────────────────────────
            foreach (var c in _cannons)
            {
                using (var br = new SolidBrush(Color.DarkGray))
                    g.FillRectangle(br, (int)c.X - 14, (int)c.Y - 20, 28, 28);
                using (var pen = new Pen(Color.Black))
                    g.DrawRectangle(pen, (int)c.X - 14, (int)c.Y - 20, 28, 28);
            }

            // ── Cannonballs ───────────────────────────────────────────────────
            foreach (var b in _balls)
            {
                using (var br = new SolidBrush(Color.DimGray))
                    g.FillEllipse(br, (int)b.X, (int)b.Y, 12, 12);
            }

            foreach (var wall in _iceWalls)
                if (wall.IsAlive) wall.Draw(g);

            // ── Exit flagpole ──────────────────────────────────────────────────
            using (var br = new SolidBrush(Color.Gold))
                g.FillRectangle(br, _exitFlag);
            using (var f = new Font("Courier New", 9, FontStyle.Bold))
                g.DrawString("EXIT", f, Brushes.Black, _exitFlag.X + 2, _exitFlag.Y + 10);

            // ── Player ────────────────────────────────────────────────────────
            _player.Draw(g);

            g.ResetTransform();

            // ── Unified HUD (single call) ─────────────────────────────────────
            GameHUD.Draw(g, _player, W, H);
            using (var f  = new Font("Courier New", 9, FontStyle.Bold))
            using (var br = new SolidBrush(Color.OrangeRed))
                g.DrawString("AIRSHIP  < AUTO-SCROLL >", f, br, W / 2 - 80, GameHUD.BandHeight + 4);
            DrawDevMenuButton(g);
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
        }

        private void BreakNearbyWallsAndProjectiles()
        {
            for (int i = _iceWalls.Count - 1; i >= 0; i--)
            {
                var wall = _iceWalls[i];
                if (!wall.IsAlive) continue;
                float dx = _player.CenterX - (wall.X + wall.Width / 2f);
                float dy = _player.CenterY - (wall.Y + wall.Height / 2f);
                if (Math.Sqrt(dx * dx + dy * dy) <= 70f)
                    wall.Health = 0;
            }

            for (int i = _balls.Count - 1; i >= 0; i--)
            {
                var b = _balls[i];
                float dx = _player.CenterX - (b.X + 6f);
                float dy = _player.CenterY - (b.Y + 6f);
                if (Math.Sqrt(dx * dx + dy * dy) <= 80f)
                    _balls.RemoveAt(i);
            }
        }
    }
}



