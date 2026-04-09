// ────────────────────────────────────────────
// PHASE 2 - Team 9: UI Programmer
// Feature: Neon Survivor Integration
// Purpose: Ports the Neon Survivor Android game into a
//          GDI+ Scene playable from the main menu.
//          Original: SkiaSharp / .NET 9 Android.
//          This version: System.Drawing / .NET Framework 4.7.2.
// ────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    // ───────────────────────────────────────────────────────────
    //  Data types (mirrors NeonSurvivor.Android)
    // ───────────────────────────────────────────────────────────

    /// <summary>Entity types in the Neon Survivor arena.</summary>
    internal enum NSEntityType { Enemy, Coin, PowerUp }

    /// <summary>Enemy sub-types with different movement behaviours.</summary>
    internal enum NSSubType
    {
        Normal, Tracker, Splitter, Orbiter, Boss, Teleporter, Ricochet
    }

    /// <summary>A game entity — enemy, coin, or power-up.</summary>
    internal sealed class NSEntity
    {
        public float X, Y, VX, VY, Radius;
        public bool Alive = true;
        public NSEntityType EntityType;
        public NSSubType SubType;
        public int HitPoints = 1;
        public int LifeTimer;
        public Color EntityColor = Color.White;
    }

    /// <summary>A visual particle for neon explosions and trails.</summary>
    internal sealed class NSParticle
    {
        public float X, Y, VX, VY, Radius;
        public int Life, MaxLife;
        public Color ParticleColor;
    }

    /// <summary>A floating score/text popup.</summary>
    internal sealed class NSFloatingText
    {
        public float X, Y;
        public string Text = "";
        public int Life;
        public Color TextColor;
    }

    /// <summary>Level definition — name, colours, difficulty scaling.</summary>
    internal sealed class NSLevel
    {
        public string Name;
        public int ScoreThreshold;
        public Color Primary, Accent, BgTop, BgBottom;
        public float SpeedMul, SpawnMul;
        public bool HasTrackers, HasSplitters, HasOrbiters, HasBoss;
    }

    // ───────────────────────────────────────────────────────────
    //  NeonSurvivorScene — the playable Scene
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Self-contained Neon Survivor mini-game rendered with GDI+.
    /// Keyboard-controlled (WASD + Space to dash + Esc to quit).
    /// Ported from the Android/SkiaSharp original.
    /// </summary>
    /// <remarks>PHASE 2 - Team 9: Neon Survivor Integration</remarks>
    public sealed class NeonSurvivorScene : Scene
    {
        // ── Design constants ──────────────────────────────────────────
        private const int ArenaW = 800, ArenaH = 600;
        private const float PlayerRadius  = 10f;
        private const float PlayerSpeed   = 4.5f;
        private const int   DashDuration  = 12;
        private const int   DashCooldown  = 50;
        private const float DashSpeed     = 14f;
        private const int   InvincOnHit   = 90;
        private const int   StartLives    = 3;
        private const int   ComboTimeout  = 120; // ticks before combo resets
        private const int   SpawnInterval = 45;  // ticks between enemy waves

        // ── Game state ────────────────────────────────────────────────
        private float _px, _py;           // player position
        private int   _lives;
        private int   _score, _highScore;
        private int   _combo, _comboTimer;
        private int   _tick;
        private int   _dashActive, _dashCooldown;
        private float _dashDX, _dashDY;   // dash direction
        private int   _invincible;
        private int   _screenShake;
        private bool  _gameStarted, _gameOver;
        private int   _levelIndex;
        private int   _killStreak, _bestStreak;

        // ── Collections ───────────────────────────────────────────────
        private readonly List<NSEntity>       _entities = new List<NSEntity>();
        private readonly List<NSParticle>     _particles = new List<NSParticle>();
        private readonly List<NSFloatingText> _floats   = new List<NSFloatingText>();
        private readonly Random _rng = new Random();

        // ── Levels (mirrors NeonSurvivor LevelData) ───────────────────
        private static readonly NSLevel[] Levels =
        {
            new NSLevel { Name="NEON GRID",       ScoreThreshold=400,  Primary=Color.Cyan,     Accent=Color.FromArgb(0,255,180),   BgTop=Color.FromArgb(5,5,30),    BgBottom=Color.FromArgb(0,20,40),   SpeedMul=1.0f, SpawnMul=1.0f },
            new NSLevel { Name="CRIMSON MAZE",    ScoreThreshold=800,  Primary=Color.Red,      Accent=Color.FromArgb(255,120,50),  BgTop=Color.FromArgb(15,0,0),    BgBottom=Color.FromArgb(40,5,5),    SpeedMul=1.1f, SpawnMul=0.95f, HasTrackers=true },
            new NSLevel { Name="GOLDEN HIVE",     ScoreThreshold=1200, Primary=Color.Gold,     Accent=Color.FromArgb(255,200,0),   BgTop=Color.Black,               BgBottom=Color.Black,               SpeedMul=1.3f, SpawnMul=0.85f, HasTrackers=true, HasSplitters=true },
            new NSLevel { Name="VOID BREACH",     ScoreThreshold=1800, Primary=Color.FromArgb(100,0,255), Accent=Color.FromArgb(180,50,255), BgTop=Color.Black, BgBottom=Color.Black, SpeedMul=1.4f, SpawnMul=0.80f, HasTrackers=true, HasOrbiters=true },
            new NSLevel { Name="INFERNO CORE",    ScoreThreshold=2500, Primary=Color.OrangeRed,Accent=Color.Gold,                   BgTop=Color.Black,               BgBottom=Color.Black,               SpeedMul=1.5f, SpawnMul=0.75f, HasTrackers=true, HasSplitters=true, HasBoss=true },
            new NSLevel { Name="OMEGA SINGULARITY",ScoreThreshold=int.MaxValue, Primary=Color.White, Accent=Color.FromArgb(255,0,100), BgTop=Color.Black, BgBottom=Color.Black, SpeedMul=2.0f, SpawnMul=0.5f, HasTrackers=true, HasSplitters=true, HasBoss=true },
        };

        // ── Rendering helpers ─────────────────────────────────────────
        private float _scaleX, _scaleY;   // screen-to-arena scale factors
        private float _animTime;

        // ═══════════════════════════════════════════════════════════════
        //  Scene lifecycle
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Initialise the scene and present the title card.</summary>
        public override void OnEnter()
        {
            _gameStarted = false;
            _gameOver    = false;
            _tick        = 0;
            _animTime    = 0f;
        }

        public override void OnExit() { }

        public override void HandleClick(Point p)
        {
            if (HandleMainMenuClick(p)) return;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Update (runs every frame ≈ 60 Hz)
        // ═══════════════════════════════════════════════════════════════

        public override void Update(float dt)
        {
            _animTime += dt;
            var inp = Game.Instance.Input;

            // Escape always returns to the previous scene
            if (inp.PausePressed)
            {
                Game.Instance.Scenes.Pop();
                return;
            }

            // ── Title screen — tap / press to start ──
            if (!_gameStarted)
            {
                if (inp.JumpPressed || inp.AttackPressed || inp.InteractPressed)
                    StartNewGame();
                return;
            }

            // ── Game-over screen ──
            if (_gameOver)
            {
                if (inp.JumpPressed || inp.AttackPressed || inp.InteractPressed)
                    StartNewGame();
                return;
            }

            _tick++;

            // ── Player movement ──
            float speed = PlayerSpeed;
            float dx = 0, dy = 0;
            if (inp.LeftHeld)  dx -= 1;
            if (inp.RightHeld) dx += 1;
            if (inp.UpHeld)    dy -= 1;
            if (inp.DownHeld)  dy += 1;
            // Normalise diagonal movement
            if (dx != 0 && dy != 0) { dx *= 0.707f; dy *= 0.707f; }

            // ── Dash (Space) ──
            if (inp.JumpPressed && _dashCooldown <= 0 && _dashActive <= 0)
            {
                _dashActive   = DashDuration;
                _dashCooldown = DashCooldown;
                // Dash in movement direction, or forward if idle
                if (dx != 0 || dy != 0) { _dashDX = dx; _dashDY = dy; }
                else { _dashDX = 0; _dashDY = -1; }
                _screenShake = 4;
                Game.Instance.Audio.BeepJump();
            }

            if (_dashActive > 0)
            {
                _dashActive--;
                _px += _dashDX * DashSpeed;
                _py += _dashDY * DashSpeed;
                // Dash trail particles
                SpawnParticles(_px, _py, Color.FromArgb(255, 255, 100), 2, 1.5f);
            }
            else
            {
                _px += dx * speed;
                _py += dy * speed;
            }

            if (_dashCooldown > 0) _dashCooldown--;
            if (_invincible  > 0) _invincible--;

            // Clamp to arena
            _px = Math.Max(PlayerRadius, Math.Min(ArenaW - PlayerRadius, _px));
            _py = Math.Max(PlayerRadius, Math.Min(ArenaH - PlayerRadius, _py));

            // ── Combo timer ──
            if (_comboTimer > 0) { _comboTimer--; if (_comboTimer <= 0) { _combo = 0; _killStreak = 0; } }

            // ── Spawn enemies ──
            NSLevel lvl = Levels[Math.Min(_levelIndex, Levels.Length - 1)];
            int interval = (int)(SpawnInterval * lvl.SpawnMul);
            if (interval < 10) interval = 10;
            if (_tick % interval == 0)
                SpawnWave(lvl);

            // ── Level progression ──
            if (_levelIndex < Levels.Length - 1 && _score >= lvl.ScoreThreshold)
            {
                _levelIndex++;
                _screenShake = 12;
                AddFloat(_px, _py - 30, $">> {Levels[_levelIndex].Name} <<", Color.White);
            }

            // ── Update entities ──
            for (int i = _entities.Count - 1; i >= 0; i--)
            {
                var e = _entities[i];
                if (!e.Alive) { _entities.RemoveAt(i); continue; }

                // Move
                e.X += e.VX * lvl.SpeedMul;
                e.Y += e.VY * lvl.SpeedMul;

                // Tracker AI: steer toward player
                if (e.SubType == NSSubType.Tracker)
                {
                    float tdx = _px - e.X, tdy = _py - e.Y;
                    float dist = (float)Math.Sqrt(tdx * tdx + tdy * tdy);
                    if (dist > 1) { e.VX += (tdx / dist) * 0.06f; e.VY += (tdy / dist) * 0.06f; }
                    float cap = 2.5f * lvl.SpeedMul;
                    e.VX = Math.Max(-cap, Math.Min(cap, e.VX));
                    e.VY = Math.Max(-cap, Math.Min(cap, e.VY));
                }

                // Bounce off arena walls (enemies)
                if (e.EntityType == NSEntityType.Enemy)
                {
                    if (e.X < e.Radius || e.X > ArenaW - e.Radius) e.VX = -e.VX;
                    if (e.Y < e.Radius || e.Y > ArenaH - e.Radius) e.VY = -e.VY;
                    e.X = Math.Max(e.Radius, Math.Min(ArenaW - e.Radius, e.X));
                    e.Y = Math.Max(e.Radius, Math.Min(ArenaH - e.Radius, e.Y));
                }

                // Remove off-screen coins / power-ups
                if (e.EntityType != NSEntityType.Enemy)
                {
                    e.LifeTimer--;
                    if (e.LifeTimer <= 0) { e.Alive = false; continue; }
                }

                // ── Collision with player ──
                float cdx = _px - e.X, cdy = _py - e.Y;
                float cdist = (float)Math.Sqrt(cdx * cdx + cdy * cdy);
                float colR  = PlayerRadius + e.Radius;

                if (cdist < colR)
                {
                    if (e.EntityType == NSEntityType.Coin)
                    {
                        // Collect coin
                        e.Alive = false;
                        int pts = 10 * (1 + _combo / 5);
                        _score += pts;
                        AddFloat(e.X, e.Y - 10, $"+{pts}", Color.Gold);
                        SpawnParticles(e.X, e.Y, Color.Gold, 6, 2f);
                        Game.Instance.Audio.BeepCoin();
                    }
                    else if (e.EntityType == NSEntityType.PowerUp)
                    {
                        // Collect power-up: +1 life
                        e.Alive = false;
                        _lives = Math.Min(_lives + 1, 9);
                        AddFloat(e.X, e.Y - 10, "+1 LIFE", Color.Lime);
                        SpawnParticles(e.X, e.Y, Color.Lime, 10, 3f);
                        Game.Instance.Audio.BeepPowerup();
                    }
                    else if (e.EntityType == NSEntityType.Enemy)
                    {
                        if (_dashActive > 0)
                        {
                            // Dash-kill: destroy enemy
                            KillEnemy(e);
                        }
                        else if (_invincible <= 0)
                        {
                            // Take damage
                            _lives--;
                            _invincible = InvincOnHit;
                            _screenShake = 10;
                            SpawnParticles(_px, _py, Color.Red, 15, 3f);
                            Game.Instance.Audio.BeepHurt();

                            if (_lives <= 0)
                            {
                                _gameOver = true;
                                if (_score > _highScore) _highScore = _score;
                                SpawnParticles(_px, _py, Color.OrangeRed, 40, 4f);
                            }
                        }
                    }
                }
            }

            // ── Update particles ──
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.X += p.VX; p.Y += p.VY;
                p.VX *= 0.96f; p.VY *= 0.96f;
                p.Life--;
                if (p.Life <= 0) _particles.RemoveAt(i);
            }

            // ── Update floating texts ──
            for (int i = _floats.Count - 1; i >= 0; i--)
            {
                var f = _floats[i];
                f.Y -= 0.8f;
                f.Life--;
                if (f.Life <= 0) _floats.RemoveAt(i);
            }

            // ── Survival score ──
            if (_tick % 60 == 0)
                _score += 5;

            // ── Periodic coin drops ──
            if (_tick % 90 == 0)
                SpawnCoin();

            // ── Rare power-up drop ──
            if (_tick % 600 == 0)
                SpawnPowerUp();

            // Screen shake decay
            if (_screenShake > 0) _screenShake--;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Draw (GDI+ rendering)
        // ═══════════════════════════════════════════════════════════════

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            _scaleX = W / (float)ArenaW;
            _scaleY = H / (float)ArenaH;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (!_gameStarted)  { DrawTitleCard(g, W, H); DrawMainMenuReturnButton(g); return; }
            if (_gameOver)      { DrawGameOver(g, W, H);  DrawMainMenuReturnButton(g); return; }

            // ── Apply screen shake ──
            float shakeOX = 0, shakeOY = 0;
            if (_screenShake > 0)
            {
                shakeOX = (_rng.Next(-3, 4)) * (_screenShake / 4f);
                shakeOY = (_rng.Next(-3, 4)) * (_screenShake / 4f);
                g.TranslateTransform(shakeOX, shakeOY);
            }

            // ── Background ──
            NSLevel lvl = Levels[Math.Min(_levelIndex, Levels.Length - 1)];
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                lvl.BgTop, lvl.BgBottom, 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Scrolling grid lines (neon aesthetic)
            float gridScroll = (_tick * 0.5f) % 40f;
            using (var pen = new Pen(Color.FromArgb(18, lvl.Primary), 1))
            {
                for (float gy = -40 + gridScroll; gy < H + 40; gy += 40)
                    g.DrawLine(pen, 0, gy, W, gy);
                for (float gx = -40 + gridScroll; gx < W + 40; gx += 40)
                    g.DrawLine(pen, gx, 0, gx, H);
            }

            // ── Draw entities ──
            foreach (var e in _entities)
            {
                if (!e.Alive) continue;
                float ex = e.X * _scaleX, ey = e.Y * _scaleY;
                float er = e.Radius * Math.Min(_scaleX, _scaleY);
                using (var br = new SolidBrush(e.EntityColor))
                    g.FillEllipse(br, ex - er, ey - er, er * 2, er * 2);
            }

            // ── Draw particles ──
            foreach (var p in _particles)
            {
                if (p.Life <= 0) continue;
                float alpha = Math.Min(255, p.Life / (float)p.MaxLife * 255);
                float px = p.X * _scaleX, py = p.Y * _scaleY;
                float pr = p.Radius * Math.Min(_scaleX, _scaleY);
                using (var br = new SolidBrush(Color.FromArgb((int)alpha, p.ParticleColor)))
                    g.FillEllipse(br, px - pr, py - pr, pr * 2, pr * 2);
            }

            // ── Draw player ──
            {
                float ppx = _px * _scaleX, ppy = _py * _scaleY;
                float pr  = PlayerRadius * Math.Min(_scaleX, _scaleY);

                // Glow halo
                using (var br = new SolidBrush(Color.FromArgb(30, 0, 255, 180)))
                    g.FillEllipse(br, ppx - pr - 8, ppy - pr - 8, (pr + 8) * 2, (pr + 8) * 2);

                // Body colour — yellow during dash, cyan normally
                Color bodyCol = _dashActive > 0 ? Color.FromArgb(255, 255, 100) : Color.FromArgb(0, 255, 180);
                using (var br = new SolidBrush(bodyCol))
                    g.FillEllipse(br, ppx - pr, ppy - pr, pr * 2, pr * 2);

                // Invincibility flash
                if (_invincible > 0 && _tick % 4 < 2)
                    using (var br = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                        g.FillEllipse(br, ppx - pr - 3, ppy - pr - 3, (pr + 3) * 2, (pr + 3) * 2);
            }

            // ── Draw floating texts ──
            using (var font = new Font("Courier New", 11, FontStyle.Bold))
            {
                foreach (var ft in _floats)
                {
                    if (ft.Life <= 0) continue;
                    int alpha = Math.Min(255, ft.Life * 6);
                    using (var br = new SolidBrush(Color.FromArgb(alpha, ft.TextColor)))
                        g.DrawString(ft.Text, font, br, ft.X * _scaleX, ft.Y * _scaleY);
                }
            }

            // Reset shake transform
            if (_screenShake > 0)
                g.TranslateTransform(-shakeOX, -shakeOY);

            // ── HUD ──
            DrawHud(g, W, H, lvl);
            DrawMainMenuReturnButton(g);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Drawing helpers
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Draws the pre-game title card with neon styling.</summary>
        private void DrawTitleCard(Graphics g, int W, int H)
        {
            // Black background with scrolling grid
            g.Clear(Color.Black);
            float gridScroll = (_animTime * 20f) % 40f;
            using (var pen = new Pen(Color.FromArgb(15, 0, 255, 140), 1))
            {
                for (float gy = -40 + gridScroll; gy < H + 40; gy += 40)
                    g.DrawLine(pen, 0, gy, W, gy);
                for (float gx = -40 + gridScroll; gx < W + 40; gx += 40)
                    g.DrawLine(pen, gx, 0, gx, H);
            }

            // Title text with glow
            using (var f = new Font("Courier New", 42, FontStyle.Bold))
            {
                const string title = "NEON SURVIVOR";
                SizeF sz = g.MeasureString(title, f);
                float tx = (W - sz.Width) / 2f;
                float ty = H / 3f;
                // Shadow / glow
                using (var br = new SolidBrush(Color.FromArgb(100, 0, 180, 80)))
                    g.DrawString(title, f, br, tx + 3, ty + 3);
                using (var br = new SolidBrush(Color.FromArgb(0, 255, 140)))
                    g.DrawString(title, f, br, tx, ty);
            }

            // Blink prompt
            bool blink = ((int)(_animTime * 2f)) % 2 == 0;
            if (blink)
            {
                using (var f = new Font("Courier New", 18, FontStyle.Bold))
                {
                    const string prompt = "PRESS SPACE TO START";
                    SizeF sz = g.MeasureString(prompt, f);
                    g.DrawString(prompt, f, Brushes.White, (W - sz.Width) / 2f, H / 2f + 20);
                }
            }

            // Controls hint
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
            {
                const string hint = "WASD = Move    SPACE = Dash    ESC = Quit";
                SizeF sz = g.MeasureString(hint, f);
                g.DrawString(hint, f, Brushes.DimGray, (W - sz.Width) / 2f, H * 0.72f);
            }

            // High score
            if (_highScore > 0)
                using (var f = new Font("Courier New", 13, FontStyle.Bold))
                {
                    string hs = $"HIGH SCORE: {_highScore:N0}";
                    SizeF sz = g.MeasureString(hs, f);
                    g.DrawString(hs, f, Brushes.Gold, (W - sz.Width) / 2f, H - 60);
                }
        }

        /// <summary>Draws the game-over screen with final stats.</summary>
        private void DrawGameOver(Graphics g, int W, int H)
        {
            g.Clear(Color.Black);

            // Scrolling grid
            float gridScroll = (_animTime * 10f) % 40f;
            using (var pen = new Pen(Color.FromArgb(10, 200, 50, 50), 1))
            {
                for (float gy = -40 + gridScroll; gy < H + 40; gy += 40)
                    g.DrawLine(pen, 0, gy, W, gy);
                for (float gx = -40 + gridScroll; gx < W + 40; gx += 40)
                    g.DrawLine(pen, gx, 0, gx, H);
            }

            using (var f = new Font("Courier New", 36, FontStyle.Bold))
            {
                const string go = "GAME OVER";
                SizeF sz = g.MeasureString(go, f);
                using (var br = new SolidBrush(Color.FromArgb(255, 60, 60)))
                    g.DrawString(go, f, br, (W - sz.Width) / 2f, H / 4f);
            }

            // Stats
            using (var f = new Font("Courier New", 16, FontStyle.Bold))
            {
                float cy = H / 2.5f;
                string[] lines =
                {
                    $"SCORE:  {_score:N0}",
                    $"BEST:   {_highScore:N0}",
                    $"STAGE:  {Levels[Math.Min(_levelIndex, Levels.Length-1)].Name}",
                    $"BEST STREAK:  x{_bestStreak}",
                    $"SURVIVED:  {_tick / 60}s",
                };
                foreach (string line in lines)
                {
                    SizeF sz = g.MeasureString(line, f);
                    g.DrawString(line, f, Brushes.White, (W - sz.Width) / 2f, cy);
                    cy += 28;
                }
            }

            // Prompt
            bool blink = ((int)(_animTime * 2f)) % 2 == 0;
            if (blink)
                using (var f = new Font("Courier New", 14, FontStyle.Bold))
                {
                    const string prompt = "PRESS SPACE TO RETRY   |   ESC TO QUIT";
                    SizeF sz = g.MeasureString(prompt, f);
                    g.DrawString(prompt, f, Brushes.LightGray, (W - sz.Width) / 2f, H * 0.82f);
                }
        }

        /// <summary>Draws the in-game HUD (score, lives, combo, dash, stage).</summary>
        private void DrawHud(Graphics g, int W, int H, NSLevel lvl)
        {
            // Semi-transparent top bar
            using (var br = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, 34);

            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            {
                g.DrawString($"Score: {_score:N0}", f, Brushes.White, 10, 8);
                g.DrawString($"Lives: {_lives}", f, Brushes.White, 180, 8);

                // Combo indicator (colour intensity scales with combo)
                if (_combo > 0)
                {
                    int r = Math.Min(255, 150 + _combo * 10);
                    using (var br = new SolidBrush(Color.FromArgb(r, 255, 100)))
                        g.DrawString($"Combo x{_combo}", f, br, 300, 8);
                }

                // Dash cooldown bar
                float dashPct = _dashCooldown > 0 ? 1f - (_dashCooldown / (float)DashCooldown) : 1f;
                int barX = W - 170, barW = 120;
                g.FillRectangle(Brushes.DimGray, barX, 12, barW, 10);
                Color barCol = dashPct >= 1f ? Color.Cyan : Color.FromArgb(80, 180, 255);
                using (var br = new SolidBrush(barCol))
                    g.FillRectangle(br, barX, 12, (int)(barW * dashPct), 10);
                g.DrawString("DASH", f, Brushes.White, barX - 46, 8);

                // Stage name — right-aligned
                string stage = lvl.Name;
                SizeF sz = g.MeasureString(stage, f);
                using (var br = new SolidBrush(lvl.Primary))
                    g.DrawString(stage, f, br, W - sz.Width - 10, H - 28);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Game logic helpers
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Resets all state for a fresh run.</summary>
        private void StartNewGame()
        {
            _px = ArenaW / 2f; _py = ArenaH / 2f;
            _lives = StartLives; _score = 0; _combo = 0; _comboTimer = 0;
            _tick = 0; _dashActive = 0; _dashCooldown = 0; _invincible = 60;
            _levelIndex = 0; _killStreak = 0; _bestStreak = 0;
            _screenShake = 0;
            _entities.Clear(); _particles.Clear(); _floats.Clear();
            _gameStarted = true; _gameOver = false;
        }

        /// <summary>Spawns a wave of enemies appropriate for the current level.</summary>
        private void SpawnWave(NSLevel lvl)
        {
            // Base count scales with game time
            int count = 1 + _tick / 600;
            if (count > 8) count = 8;

            for (int i = 0; i < count; i++)
            {
                NSSubType sub = NSSubType.Normal;
                Color col = lvl.Primary;

                // Pick sub-type based on level flags and random chance
                double roll = _rng.NextDouble();
                if (lvl.HasTrackers && roll < 0.25)
                { sub = NSSubType.Tracker; col = Color.OrangeRed; }
                else if (lvl.HasSplitters && roll < 0.40)
                { sub = NSSubType.Splitter; col = Color.FromArgb(255, 200, 0); }
                else if (lvl.HasOrbiters && roll < 0.50)
                { sub = NSSubType.Orbiter; col = Color.FromArgb(180, 50, 255); }

                // Spawn from a random edge
                float sx, sy, svx, svy;
                int edge = _rng.Next(4);
                switch (edge)
                {
                    case 0: sx = _rng.Next(ArenaW); sy = -10; break;   // top
                    case 1: sx = _rng.Next(ArenaW); sy = ArenaH + 10; break; // bottom
                    case 2: sx = -10; sy = _rng.Next(ArenaH); break;   // left
                    default: sx = ArenaW + 10; sy = _rng.Next(ArenaH); break; // right
                }

                // Aim roughly toward the centre with some randomness
                float tdx = (ArenaW / 2f + _rng.Next(-100, 100)) - sx;
                float tdy = (ArenaH / 2f + _rng.Next(-100, 100)) - sy;
                float tdist = (float)Math.Sqrt(tdx * tdx + tdy * tdy);
                float baseSpeed = 1.2f + (float)_rng.NextDouble() * 1.0f;
                svx = (tdx / tdist) * baseSpeed;
                svy = (tdy / tdist) * baseSpeed;

                float radius = sub == NSSubType.Splitter ? 12 : 8;

                _entities.Add(new NSEntity
                {
                    X = sx, Y = sy, VX = svx, VY = svy,
                    Radius = radius, EntityType = NSEntityType.Enemy,
                    SubType = sub, EntityColor = col,
                    HitPoints = sub == NSSubType.Splitter ? 2 : 1
                });
            }

            // Boss spawn at level entry if flagged
            if (lvl.HasBoss && _score >= lvl.ScoreThreshold - 100 && !BossAlive())
                SpawnBoss(lvl);
        }

        /// <summary>Spawns a large boss enemy at a random edge.</summary>
        private void SpawnBoss(NSLevel lvl)
        {
            _entities.Add(new NSEntity
            {
                X = _rng.Next(100, ArenaW - 100),
                Y = -30,
                VX = 0.5f * (_rng.Next(2) == 0 ? 1 : -1),
                VY = 0.8f,
                Radius = 28,
                EntityType = NSEntityType.Enemy,
                SubType = NSSubType.Boss,
                HitPoints = 10,
                EntityColor = Color.FromArgb(255, 60, 0)
            });
            _screenShake = 15;
            AddFloat(ArenaW / 2f, ArenaH / 3f, "!! BOSS !!", Color.OrangeRed);
        }

        /// <summary>Returns true if a boss enemy is currently alive.</summary>
        private bool BossAlive()
        {
            foreach (var e in _entities)
                if (e.Alive && e.SubType == NSSubType.Boss) return true;
            return false;
        }

        /// <summary>Handles killing an enemy via dash or other means.</summary>
        private void KillEnemy(NSEntity e)
        {
            e.HitPoints--;
            if (e.HitPoints > 0)
            {
                // Multi-hit enemy: flash and bounce back
                SpawnParticles(e.X, e.Y, e.EntityColor, 4, 1.5f);
                return;
            }

            e.Alive = false;
            _combo++;
            _comboTimer = ComboTimeout;
            _killStreak++;
            if (_killStreak > _bestStreak) _bestStreak = _killStreak;

            int pts = 25 * (1 + _combo / 3);
            if (e.SubType == NSSubType.Boss) pts = 500;
            _score += pts;
            AddFloat(e.X, e.Y - 10, $"+{pts}", Color.White);
            SpawnParticles(e.X, e.Y, e.EntityColor, 12, 2.5f);
            Game.Instance.Audio.BeepStomp();

            // Splitters break into two smaller normals
            if (e.SubType == NSSubType.Splitter)
            {
                for (int s = 0; s < 2; s++)
                {
                    float ang = (float)(_rng.NextDouble() * Math.PI * 2);
                    _entities.Add(new NSEntity
                    {
                        X = e.X, Y = e.Y,
                        VX = (float)Math.Cos(ang) * 2f,
                        VY = (float)Math.Sin(ang) * 2f,
                        Radius = 5, EntityType = NSEntityType.Enemy,
                        SubType = NSSubType.Normal, HitPoints = 1,
                        EntityColor = Color.FromArgb(255, 220, 80)
                    });
                }
            }

            // Boss death: big explosion + coin shower
            if (e.SubType == NSSubType.Boss)
            {
                SpawnParticles(e.X, e.Y, Color.OrangeRed, 40, 4f);
                _screenShake = 20;
                for (int c = 0; c < 8; c++)
                    SpawnCoinAt(e.X + _rng.Next(-40, 40), e.Y + _rng.Next(-40, 40));
            }
        }

        /// <summary>Spawns a coin at a random position.</summary>
        private void SpawnCoin()
        {
            SpawnCoinAt(_rng.Next(40, ArenaW - 40), _rng.Next(40, ArenaH - 40));
        }

        /// <summary>Spawns a coin at a specific position.</summary>
        private void SpawnCoinAt(float x, float y)
        {
            _entities.Add(new NSEntity
            {
                X = x, Y = y, Radius = 5,
                EntityType = NSEntityType.Coin,
                EntityColor = Color.Gold,
                LifeTimer = 300
            });
        }

        /// <summary>Spawns a life power-up at a random position.</summary>
        private void SpawnPowerUp()
        {
            _entities.Add(new NSEntity
            {
                X = _rng.Next(40, ArenaW - 40),
                Y = _rng.Next(40, ArenaH - 40),
                Radius = 7,
                EntityType = NSEntityType.PowerUp,
                EntityColor = Color.Lime,
                LifeTimer = 450
            });
        }

        /// <summary>Spawns a burst of neon particles at the given position.</summary>
        private void SpawnParticles(float x, float y, Color col, int count, float speedScale)
        {
            for (int i = 0; i < count; i++)
            {
                float ang = (float)(_rng.NextDouble() * Math.PI * 2);
                float spd = (1 + (float)_rng.NextDouble() * 3) * speedScale;
                _particles.Add(new NSParticle
                {
                    X = x, Y = y,
                    VX = (float)Math.Cos(ang) * spd,
                    VY = (float)Math.Sin(ang) * spd,
                    Radius = 1.5f + (float)_rng.NextDouble() * 2.5f,
                    Life = 15 + _rng.Next(20),
                    MaxLife = 35,
                    ParticleColor = col
                });
            }
        }

        /// <summary>Adds a floating text popup at the given arena position.</summary>
        private void AddFloat(float x, float y, string text, Color col)
        {
            _floats.Add(new NSFloatingText { X = x, Y = y, Text = text, Life = 60, TextColor = col });
        }
    }
}
