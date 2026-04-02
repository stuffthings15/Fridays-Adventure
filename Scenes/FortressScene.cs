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
    //  FortressScene.cs  —  Level Designer: Ideas 1-4 (Fortress level)
    //
    //  Idea 1: Fortress level — dark brick interior, torches, boss key gate.
    //  Idea 2: Boss key gate — player must carry the BossKey SuitType to pass.
    //  Idea 3: Thwomp-style falling stone blocks on timed paths.
    //  Idea 4: Moving lava tide — camera-lock bottom scrolls upward as lava rises.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fortress level — SMB3-style castle interior with dark bricks, torches,
    /// a boss key gate, Thwomp stone blocks, and a rising lava tide.
    /// Team 5 (Level Designer) — Ideas 1–4.
    /// </summary>
    public sealed class FortressScene : Scene
    {
        // ── Player + entities ─────────────────────────────────────────────────
        private Player            _player;
        private List<Rectangle>   _platforms  = new List<Rectangle>();
        private List<Rectangle>   _thwomps    = new List<Rectangle>();
        private List<float>       _thwompY     = new List<float>();   // current Y per Thwomp
        private List<bool>        _thwompFalling = new List<bool>();
        private readonly List<Abilities.IceWallInstance> _iceWalls = new List<Abilities.IceWallInstance>();
        private float _thwompFreezeTimer;

        // ── Lava tide (Idea 4) ────────────────────────────────────────────────
        private float _lavaY;           // current Y top of the lava (rises toward 0)
        private const float LavaRiseRate = 18f;  // px/s

        // ── Boss key gate (Idea 2) ────────────────────────────────────────────
        private Rectangle _gateRect;
        private bool      _gateOpen;

        // ── Level state ───────────────────────────────────────────────────────
        private bool  _levelComplete;
        private float _completeTimer;
        private Rectangle _exitDoor;

        // ── Camera ────────────────────────────────────────────────────────────
        private float _cameraY;

        // ── Background ────────────────────────────────────────────────────────
        private Bitmap _bg;

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font _hud = new Font("Courier New", 9, FontStyle.Bold);

        public override void OnEnter()
        {
            DebugLogger.PushBreadcrumb("FortressScene.OnEnter");

            // Background — bg_Marine_Blockade.png lives in Assets\Sprites\
            _bg = SpriteManager.Get("bg_Marine_Blockade.png");
            
            BuildLevel();
            Game.Instance.Audio.ContinueOrPlay("boss");
            SMB3Hud.TriggerGetReady();
            SMB3Hud.ShowWorldLabel($"FORTRESS  {Game.Instance.WorldLevelLabel}");
        }

        public override void OnExit()  { _bg?.Dispose(); _bg = null; }
        public override void OnResume() => Game.Instance.Audio.ContinueOrPlay("boss");

        private void BuildLevel()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Player archetype is resolved automatically from Game.Instance.SelectedCharacter
            // inside the Player(x, y) constructor — no need to pass extra arguments.
            _player = new Player(W / 2f - 16, H - 200);
            _iceWalls.Clear();
            _thwompFreezeTimer = 0f;

            // ── Platforms (brick layout) ──────────────────────────────────────
            _platforms.Clear();
            _platforms.Add(new Rectangle(0,       H - 40,  W, 40));      // floor
            _platforms.Add(new Rectangle(60,      H - 140, 200, 20));
            _platforms.Add(new Rectangle(300,     H - 200, 180, 20));
            _platforms.Add(new Rectangle(550,     H - 160, 200, 20));
            _platforms.Add(new Rectangle(200,     H - 280, 160, 20));
            _platforms.Add(new Rectangle(420,     H - 320, 200, 20));
            _platforms.Add(new Rectangle(100,     H - 400, 140, 20));
            _platforms.Add(new Rectangle(350,     H - 440, 220, 20));
            _platforms.Add(new Rectangle(580,     H - 400, 180, 20));

            // ── Thwomps (Idea 3) ──────────────────────────────────────────────
            _thwomps.Clear(); _thwompY.Clear(); _thwompFalling.Clear();
            AddThwomp(180, H - 200, H - 120);
            AddThwomp(460, H - 360, H - 280);
            AddThwomp(650, H - 320, H - 240);

            // ── Boss key gate (Idea 2) ────────────────────────────────────────
            _gateRect = new Rectangle(W - 100, H - 440 - 80, 40, 80);
            _gateOpen = PowerUpInventory.ReserveItem == SuitType.BossKey;

            // ── Exit door ─────────────────────────────────────────────────────
            _exitDoor = new Rectangle(W - 80, H - 440 - 60, 36, 60);

            // ── Lava tide ─────────────────────────────────────────────────────
            _lavaY = H + 40;  // starts below screen
        }

        private void AddThwomp(int x, int restY, int dropY)
        {
            _thwomps.Add(new Rectangle(x, (int)restY, 40, 40));
            _thwompY.Add(restY);
            _thwompFalling.Add(false);
        }

        public override void Update(float dt)
        {
            if (_levelComplete) { _completeTimer -= dt; if (_completeTimer <= 0f) CompleteLevel(); return; }

            var input = Game.Instance.Input;
            int   W   = Game.Instance.CanvasWidth;
            int   H   = Game.Instance.CanvasHeight;

            // ── Player movement ───────────────────────────────────────────────
            bool left  = input.IsHeld(System.Windows.Forms.Keys.Left)  || input.IsHeld(System.Windows.Forms.Keys.A);
            bool right = input.IsHeld(System.Windows.Forms.Keys.Right) || input.IsHeld(System.Windows.Forms.Keys.D);
            bool jump  = input.IsPressed(System.Windows.Forms.Keys.Space) || input.IsPressed(System.Windows.Forms.Keys.W);

            if (left)  _player.VelocityX = -_player.MoveSpeed;
            else if (right) _player.VelocityX = _player.MoveSpeed;
            else _player.VelocityX = 0;

            if (jump && _player.IsGrounded) { _player.VelocityY = _player.JumpForce; _player.IsGrounded = false; }

            // Character abilities (Q/E/R) in fortress map.
            if (input.Ability1Pressed && _player.UseIceWall(out Abilities.IceWallInstance wall))
            {
                _iceWalls.Add(wall);
                Game.Instance.Audio.BeepIce();
            }
            if (input.Ability2Pressed && _player.UseFlashFreeze())
            {
                _thwompFreezeTimer = _player.GetFlashFreezeDuration(2.0f);
                Game.Instance.Audio.BeepFreeze();
            }
            if (input.Ability3Pressed && _player.UseBreakWall())
            {
                BreakNearbyWalls();
                Game.Instance.Audio.BeepBreak();
            }

            _player.Update(dt);

            // Apply velocity.
            _player.X += _player.VelocityX * dt;
            _player.Y += _player.VelocityY * dt;
            _player.X = Math.Max(0, Math.Min(W - _player.Width, _player.X));

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

            if (_thwompFreezeTimer > 0f) _thwompFreezeTimer -= dt;

            // ── Thwomp update (Idea 3) ────────────────────────────────────────
            for (int i = 0; i < _thwomps.Count; i++)
            {
                var t = _thwomps[i];
                float px = _player.X + _player.Width / 2f;
                float dist = Math.Abs(px - (t.X + t.Width / 2f));

                if (_thwompFreezeTimer <= 0f)
                {
                    if (!_thwompFalling[i] && dist < 60f)
                        _thwompFalling[i] = true;

                    if (_thwompFalling[i])
                    {
                        _thwompY[i] += 300f * dt;
                        if (_thwompY[i] >= Game.Instance.CanvasHeight - 80)
                        { _thwompY[i] = Game.Instance.CanvasHeight - 80; _thwompFalling[i] = false; }
                    }
                }

                _thwomps[i] = new Rectangle(t.X, (int)_thwompY[i], t.Width, t.Height);

                // Damage player if crushed.
                if (_thwomps[i].IntersectsWith(new Rectangle(
                    (int)_player.X, (int)_player.Y, _player.Width, _player.Height)))
                    _player.TakeDamage(1);
            }

            // ── Lava tide (Idea 4) ────────────────────────────────────────────
            _lavaY -= LavaRiseRate * dt;
            if (_player.Y + _player.Height > _lavaY)
            {
                _player.TakeDamage(99);  // instant death in lava
                DebugLogger.LogInfo("FortressScene", "Player fell into lava.");
            }

            // ── Boss key gate check (Idea 2) ──────────────────────────────────
            if (!_gateOpen && PowerUpInventory.ReserveItem == SuitType.BossKey)
            {
                _gateOpen = true;
                Game.Instance.FloatingText.Spawn("GATE OPEN!", W / 2, H / 2, Color.Gold, large: true);
            }

            // ── Exit ──────────────────────────────────────────────────────────
            var playerRect = new Rectangle((int)_player.X, (int)_player.Y, _player.Width, _player.Height);
            if (_gateOpen && _exitDoor.IntersectsWith(playerRect))
            {
                _levelComplete = true;
                _completeTimer = 1.8f;
                SMB3Hud.TriggerGetReady();
            }

            // ── Death check ───────────────────────────────────────────────────
            if (!_player.IsAlive || _player.Y > H + 100)
                HandleDeath();

            // ── Camera ────────────────────────────────────────────────────────
            _cameraY = _player.Y - H / 2f;
            _cameraY = Math.Max(0, Math.Min(_cameraY, 0));

            SMB3Hud.Update(dt);
        }

        private void ResolvePlayerPlatform(Rectangle p)
        {
            var pr = new Rectangle((int)_player.X, (int)_player.Y, _player.Width, _player.Height);
            if (!pr.IntersectsWith(p)) return;

            float overlapBottom = (pr.Bottom - p.Top);
            float overlapTop    = (p.Bottom - pr.Top);

            if (overlapBottom < overlapTop && _player.VelocityY > 0)
            {
                _player.Y       = p.Top - _player.Height;
                _player.VelocityY  = 0f;
                _player.IsGrounded = true;
            }
            else if (overlapTop < overlapBottom && _player.VelocityY < 0)
            {
                _player.Y         = p.Bottom;
                _player.VelocityY = 0f;
            }
        }

        private void HandleDeath()
        {
            SessionStats.Instance.RecordDeath();
            DebugLogger.LogInfo("FortressScene", "Player died.");
            Game.Instance.Scenes.Pop();
        }

        private void CompleteLevel()
        {
            DebugLogger.LogInfo("FortressScene", "Fortress cleared!");
            Game.Instance.LevelJustCompleted = true;
            SessionStats.Instance.RecordLevelComplete();
            AchievementSystem.Grant("ach_boss_slayer");
            Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Background ────────────────────────────────────────────────────
            // PHASE 2 - Team 14: Environment Artist - Draw fortress background if loaded
            if (_bg != null)
            {
                g.DrawImage(_bg, 0, 0, W, H);
            }
            else
            {
                // Fallback: dark fortress brick
                using (var br = new SolidBrush(Color.FromArgb(20, 12, 8)))
                    g.FillRectangle(br, 0, 0, W, H);

                // Draw brick pattern for walls.
                DrawBrickBackground(g, W, H);
            }

            // ── Torches ───────────────────────────────────────────────────────
            DrawTorch(g, 80,  H - 200);
            DrawTorch(g, 360, H - 340);
            DrawTorch(g, 640, H - 300);

            // ── Platforms ─────────────────────────────────────────────────────
            foreach (var p in _platforms)
            {
                using (var br = new SolidBrush(Color.FromArgb(80, 60, 50)))
                    g.FillRectangle(br, p);
                using (var pen = new Pen(Color.FromArgb(100, 80, 60)))
                    g.DrawRectangle(pen, p);
            }

            // ── Thwomps (Idea 3) ──────────────────────────────────────────────
            foreach (var t in _thwomps)
            {
                using (var br = new SolidBrush(Color.FromArgb(100, 90, 90)))
                    g.FillRectangle(br, t);
                using (var pen = new Pen(Color.DarkGray))
                    g.DrawRectangle(pen, t);
                using (var f  = new Font("Courier New", 9, FontStyle.Bold))
                    g.DrawString("▼", f, Brushes.OrangeRed, t.X + 10, t.Y + 10);
            }

            // ── Boss key gate (Idea 2) ────────────────────────────────────────
            if (!_gateOpen)
            {
                using (var br = new SolidBrush(Color.FromArgb(120, 80, 30)))
                    g.FillRectangle(br, _gateRect);
                using (var f = new Font("Courier New", 8, FontStyle.Bold))
                    g.DrawString("KEY\nGATE", f, Brushes.Gold, _gateRect.X + 2, _gateRect.Y + 10);
            }

            // ── Exit door ─────────────────────────────────────────────────────
            using (var br = new SolidBrush(_gateOpen ? Color.LimeGreen : Color.DimGray))
                g.FillRectangle(br, _exitDoor);

            // ── Lava tide (Idea 4) ────────────────────────────────────────────
            int lavaTop = Math.Max(0, (int)_lavaY);
            using (var lavaBr = new LinearGradientBrush(
                new Rectangle(0, lavaTop, W, H - lavaTop + 1),
                Color.FromArgb(255, 80, 0), Color.FromArgb(200, 30, 0), 90f))
                g.FillRectangle(lavaBr, 0, lavaTop, W, H - lavaTop);

            // Lava warning pulsing text.
            if (_lavaY < H * 0.7f)
            {
                using (var f  = new Font("Courier New", 10, FontStyle.Bold))
                using (var br = new SolidBrush(Color.OrangeRed))
                    g.DrawString("⚠ LAVA RISING!", f, br, W / 2f - 70, lavaTop - 24);
            }

            // ── Player ────────────────────────────────────────────────────────
            _player.Draw(g);
            foreach (var wall in _iceWalls)
                if (wall.IsAlive) wall.Draw(g);

            // ── HUD ───────────────────────────────────────────────────────────
            SMB3Hud.DrawAll(g, _player, null, W, H);
        }

        private static void DrawBrickBackground(Graphics g, int W, int H)
        {
            int bw = 48, bh = 24;
            using (var pen = new Pen(Color.FromArgb(35, 25, 20), 1))
            {
                for (int row = 0; row < H / bh + 2; row++)
                {
                    int offset = (row % 2 == 0) ? 0 : bw / 2;
                    for (int col = -1; col < W / bw + 2; col++)
                        g.DrawRectangle(pen, col * bw + offset, row * bh, bw, bh);
                }
            }
        }

        private static void DrawTorch(Graphics g, int x, int y)
        {
            // Torch base.
            using (var br = new SolidBrush(Color.SaddleBrown))
                g.FillRectangle(br, x, y, 8, 20);

            // Flame glow.
            using (var br = new SolidBrush(Color.FromArgb(180, 255, 140, 0)))
                g.FillEllipse(br, x - 6, y - 16, 20, 22);
            using (var br = new SolidBrush(Color.FromArgb(120, 255, 220, 0)))
                g.FillEllipse(br, x - 2, y - 10, 12, 14);
        }

        private void BreakNearbyWalls()
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
        }
    }
}
