using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    public sealed class StormScene : Scene
    {
        // Ship deck — oscillates with waves
        private float _deckBaseY;
        private float _deckOscillation;
        private float _waveTimer;

        private Player         _player;
        private List<LightningStrike> _strikes = new List<LightningStrike>();
        private float _strikeSpawnTimer;
        private const float StrikeInterval = 0.5f;

        private float              _lightningFlash;
        private List<HealthPickup> _healthPickups = new List<HealthPickup>();

        private float _survivalTimer;
        private const float SurvivalGoal = 25f;
        private bool  _complete;
        private float _completeTimer;
        private bool  _failed;

        private float _stormAnim;
        private readonly Random _rng = new Random();

        // SMB3-style feel helpers:
        // - Coyote time lets jump work briefly after leaving platform.
        // - Jump buffer stores early jump input shortly before landing.
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private const float CoyoteWindow = 0.11f;
        private const float JumpBufferWindow = 0.12f;

        // Momentum tuning for less abrupt horizontal movement.
        private const float GroundAccel = 14f;
        private const float AirAccel = 8f;

        private static readonly Font _hudFont = new Font("Courier New", 11, FontStyle.Bold);

        // Cached warning count for HUD readability (how many bolts are currently telegraphed).
        private int _warningStrikeCount;

        // Rain seed cache for draw-time performance (no per-frame Random allocation).
        private float[] _rainSeedX = new float[80];
        private float[] _rainSeedY = new float[80];

        // ── Inner classes ─────────────────────────────────────────────────────

        private sealed class LightningStrike
        {
            public float X;
            public float Intensity;
            private float _warnTimer  = 0.6f;
            private float _strikeTimer;
            private const float StrikeDur = 0.32f;
            public bool IsStriking => _warnTimer <= 0 && _strikeTimer > 0;
            public bool IsDone     => _warnTimer <= 0 && _strikeTimer <= 0;
            private bool _damaged;

            public LightningStrike(float x, float intensity = 0.5f)
            {
                X            = x;
                Intensity    = intensity;
                _strikeTimer = StrikeDur;
            }

            /// <summary>Returns true the moment the bolt fires (triggers screen flash).</summary>
            public bool Update(float dt, Player player, int canvasHeight)
            {
                if (_warnTimer > 0) { _warnTimer -= dt; return false; }
                _strikeTimer -= dt;
                if (!_damaged && IsStriking)
                {
                    _damaged = true;
                    Game.Instance.Audio.BeepBreak();
                    var hit = new System.Drawing.Rectangle((int)X - 24, 0, 48, canvasHeight);
                    if (hit.IntersectsWith(player.Hitbox))
                    { player.TakeDamage((int)(18 + 14 * Intensity)); Game.Instance.Audio.BeepHurt(); }
                    return true;
                }
                return false;
            }

            public void Draw(Graphics g, int canvasHeight)
            {
                if (IsStriking)
                {
                    // Outer corona — wider and more orange at peak intensity
                    float glowW = 55 + 55 * Intensity;
                    using (var br = new SolidBrush(Color.FromArgb((int)(55 + 40 * Intensity),
                        Intensity > 0.6f ? Color.OrangeRed : Color.Yellow)))
                        g.FillRectangle(br, X - glowW, 0, glowW * 2, canvasHeight);
                    // Inner hot core
                    using (var br = new SolidBrush(Color.FromArgb(90, Color.White)))
                        g.FillRectangle(br, X - 20, 0, 40, canvasHeight);

                    // Zigzag bolt — seeded so the shape is stable during the strike
                    var rng  = new System.Random((int)(X * 1009));
                    int segs = 20 + (int)(Intensity * 8);
                    var pts  = new PointF[segs + 2];
                    pts[0]   = new PointF(X, 0);
                    float spread = 34 + 24 * Intensity;
                    for (int i = 1; i <= segs; i++)
                        pts[i] = new PointF(X + (float)(rng.NextDouble() - 0.5) * spread,
                                            canvasHeight * i / (float)(segs + 1));
                    pts[segs + 1] = new PointF(X, canvasHeight);

                    float boltW = 5 + 5 * Intensity;
                    using (var pen = new Pen(Intensity > 0.6f ? Color.OrangeRed : Color.White, boltW + 4))
                        g.DrawLines(pen, pts);
                    using (var pen = new Pen(Color.White, boltW))  g.DrawLines(pen, pts);
                    using (var pen = new Pen(Color.Yellow, boltW - 2)) g.DrawLines(pen, pts);

                    // Multiple branches — more at higher intensity
                    DrawBranch(g, rng, pts, segs / 3,      canvasHeight, 60, 200);
                    DrawBranch(g, rng, pts, segs / 2,      canvasHeight, 70, 180);
                    if (Intensity > 0.5f)
                        DrawBranch(g, rng, pts, segs * 2 / 3, canvasHeight, 55, 150);
                    if (Intensity > 0.8f)
                        DrawBranch(g, rng, pts, segs / 4,     canvasHeight, 45, 120);
                }
                else if (_warnTimer > 0)
                {
                    float a = 1f - _warnTimer / 0.6f;
                    using (var pen = new Pen(Color.FromArgb((int)(220 * a), Color.Yellow), 2)
                           { DashStyle = DashStyle.Dash })
                        g.DrawLine(pen, X, 0, X, canvasHeight);
                    using (var br = new SolidBrush(Color.FromArgb((int)(220 * a), Color.Yellow)))
                        g.FillPolygon(br, new[]
                        {
                            new PointF(X - 14, 28), new PointF(X + 14, 28), new PointF(X, 4)
                        });
                    if (Intensity > 0.5f)
                        using (var pen = new Pen(Color.FromArgb((int)(180 * a), Color.OrangeRed), 2))
                            g.DrawEllipse(pen, X - 18, 6, 36, 36);
                }
            }

            private static void DrawBranch(Graphics g, System.Random rng, PointF[] pts,
                                           int pivotIdx, int canvasH, float spread, int alpha)
            {
                if (pivotIdx < 0 || pivotIdx >= pts.Length) return;
                var branch = new PointF[5];
                branch[0]  = pts[pivotIdx];
                for (int b = 1; b < 5; b++)
                    branch[b] = new PointF(
                        branch[0].X + (float)(rng.NextDouble() - 0.35) * spread * b,
                        branch[0].Y + canvasH * b / 10f);
                using (var pen = new Pen(Color.FromArgb(alpha, Color.White), 2))
                    g.DrawLines(pen, branch);
                using (var pen = new Pen(Color.FromArgb(Math.Max(0, alpha - 40), Color.Yellow), 1))
                    g.DrawLines(pen, branch);
            }
        }

        private sealed class HealthPickup
        {
            public float X, Y;
            public bool  Active = true;
            private float _respawnTimer;
            private const float RespawnDelay = 12f;

            public HealthPickup(float x, float y) { X = x; Y = y; }

            public void Update(float dt)
            {
                if (!Active) { _respawnTimer -= dt; if (_respawnTimer <= 0) Active = true; }
            }

            public bool TryCollect(Player player)
            {
                if (!Active) return false;
                var area = new System.Drawing.Rectangle((int)X - 14, (int)Y - 14, 28, 28);
                if (!area.IntersectsWith(player.Hitbox)) return false;
                Active = false;
                _respawnTimer = RespawnDelay;
                return true;
            }

            public void Draw(Graphics g)
            {
                if (!Active) return;
                using (var br = new SolidBrush(Color.FromArgb(220, 30, 200, 30)))
                    g.FillEllipse(br, X - 13, Y - 13, 26, 26);
                using (var pen = new Pen(Color.White, 2))
                    g.DrawEllipse(pen, X - 13, Y - 13, 26, 26);
                using (var pen = new Pen(Color.White, 3))
                {
                    g.DrawLine(pen, X - 7, Y, X + 7, Y);
                    g.DrawLine(pen, X, Y - 7, X, Y + 7);
                }
            }
        }

        // ── Scene lifecycle ──────────────────────────────────────────────────

        public override void OnEnter()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            _deckBaseY = H - 100;
            _player    = new Player(W / 2f - 18, _deckBaseY - 56);
            _player.MoveSpeed *= 1.3f;
            _player.ApplySelectedSprite();
            Game.Instance.Audio.ContinueOrPlay("combat");
            SpawnPickups();

            // Initialize rain seeds once (optimization phase).
            for (int i = 0; i < _rainSeedX.Length; i++)
            {
                _rainSeedX[i] = (float)_rng.NextDouble();
                _rainSeedY[i] = (float)_rng.NextDouble();
            }
        }

        public override void OnExit()  { }
        public override void OnResume() => Game.Instance.Audio.ContinueOrPlay("combat");

        // ── Update ────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            ThreatSystem.Tick(dt);
            _stormAnim += dt;

            if (_complete) { _completeTimer += dt; if (_completeTimer >= 1.0f) { SessionStats.Instance.RecordLevelComplete(); Game.Instance.LevelJustCompleted = true; Game.Instance.Scenes.Pop(); } return; }
            if (_failed)   { Game.Instance.Scenes.Replace(new GameOverScene(() => new StormScene())); return; }

            UpdateDeck(dt);
            HandleInput(dt);
            _player.Update(dt);
            _player.X += _player.VelocityX * dt;   // was missing — caused cannot-move bug
            _player.Y += _player.VelocityY * dt;
            ResolveDeck(dt);
            UpdateStrikes(dt);
            UpdatePickups(dt);

            // Cache telegraph count for HUD and readability.
            _warningStrikeCount = 0;
            for (int i = 0; i < _strikes.Count; i++)
                if (!_strikes[i].IsStriking && !_strikes[i].IsDone) _warningStrikeCount++;

            _survivalTimer += dt;
            if (_survivalTimer >= SurvivalGoal) OnComplete();
            if (!_player.IsAlive) { SessionStats.Instance.RecordDeath(); _failed = true; }
        }

        private void UpdateDeck(float dt)
        {
            _waveTimer   += dt;
            _deckOscillation = (float)Math.Sin(_waveTimer * 1.8) * 28f;
        }

        private float CurrentDeckY => _deckBaseY + _deckOscillation;

        private void HandleInput(float dt)
        {
            var input = Game.Instance.Input;
            if (_player.HasEffect(StatusEffect.Sinking) ||
                _player.HasEffect(StatusEffect.Stunned)) return;

            // Track jump buffer each frame (press slightly before landing still jumps).
            if (input.JumpPressed)
                _jumpBufferTimer = JumpBufferWindow;
            else if (_jumpBufferTimer > 0f)
                _jumpBufferTimer = Math.Max(0f, _jumpBufferTimer - dt);

            // Horizontal target speed from input.
            float targetX = 0f;
            if (input.LeftHeld)
            {
                targetX = -_player.MoveSpeed;
                _player.FacingRight = false;
            }
            else if (input.RightHeld)
            {
                targetX = _player.MoveSpeed;
                _player.FacingRight = true;
            }

            // SMB3-style momentum (accelerate instead of instantly snapping).
            float accel = _player.IsGrounded ? GroundAccel : AirAccel;
            _player.VelocityX += (targetX - _player.VelocityX) * Math.Min(1f, accel * dt);

            // Buffered jump + coyote time jump + double jump.
            bool canJump = _player.JumpsRemaining > 0 || _coyoteTimer > 0f;
            if (_jumpBufferTimer > 0f && canJump)
            {
                _player.VelocityY = _player.JumpForce;
                _player.IsGrounded = false;
                if (_player.JumpsRemaining > 0) _player.JumpsRemaining--;
                _coyoteTimer = 0f;
                _jumpBufferTimer = 0f;
                Game.Instance.Audio.BeepJump();
            }

            // Variable jump height — release early for short hop (SMB3-style)
            if (!input.JumpHeld && _player.VelocityY < -120f)
                _player.VelocityY = -120f;

            if (input.DodgePressed) _player.TryDodge();
            if (input.PausePressed) Game.Instance.Scenes.Push(new PauseScene());
            // I key — quick-open inventory during storm
            if (input.InventoryPressed) Game.Instance.Scenes.Push(new InventoryScene(_player));
        }

        private void ResolveDeck(float dt)
        {
            int W = Game.Instance.CanvasWidth;
            var deck = new System.Drawing.Rectangle(0, (int)CurrentDeckY, W, 100);
            _player.X = Math.Max(0, Math.Min(W - _player.Width, _player.X));
            if (_player.Hitbox.IntersectsWith(deck) && _player.VelocityY >= 0)
            {
                _player.Y = CurrentDeckY - _player.Height;
                _player.VelocityY = 0;
                _player.IsGrounded = true;

                // Refresh coyote timer while standing on deck.
                _coyoteTimer = CoyoteWindow;
            }
            else
            {
                _player.IsGrounded = false;
                if (_coyoteTimer > 0f)
                    _coyoteTimer = Math.Max(0f, _coyoteTimer - dt);
            }

            // No fall damage: recover to deck when the player drops out of bounds.
            if (_player.Y > Game.Instance.CanvasHeight + 100)
            {
                _player.X = W / 2f - _player.Width / 2f;
                _player.Y = CurrentDeckY - _player.Height;
                _player.VelocityX = 0f;
                _player.VelocityY = 0f;
                _player.IsGrounded = true;
                _player.GrantInvincibility(0.6f);
            }
        }

        private void UpdateStrikes(float dt)
        {
            _strikeSpawnTimer -= dt;
            if (_strikeSpawnTimer <= 0)
            {
                float intensity  = Math.Min(1f, _survivalTimer / SurvivalGoal) * 0.6f;
                float interval   = StrikeInterval * Math.Max(0.5f, 1f - 0.7f * intensity);
                int   spawnCount = 1 + (int)(intensity * 2.0f);  // 1 → 2 bolts per burst
                for (int s = 0; s < spawnCount; s++)
                {
                    float x = 40 + (float)_rng.NextDouble() * (Game.Instance.CanvasWidth - 80);
                    _strikes.Add(new LightningStrike(x, intensity));
                }
                _strikeSpawnTimer = interval * (0.4f + (float)_rng.NextDouble() * 0.5f);
            }
            int H = Game.Instance.CanvasHeight;
            for (int i = _strikes.Count - 1; i >= 0; i--)
            {
                if (_strikes[i].Update(dt, _player, H)) _lightningFlash = 0.38f;
                if (_strikes[i].IsDone) _strikes.RemoveAt(i);
            }
            if (_lightningFlash > 0f)
                _lightningFlash = Math.Max(0f, _lightningFlash - dt * 4.5f);
        }

        private void OnComplete()
        {
            _complete = true;
            ThreatSystem.OnStealthRoute();
            Game.Instance.PlayerBounty += 300;
        }

        private void SpawnPickups()
        {
            int   W        = Game.Instance.CanvasWidth;
            float deckTop  = _deckBaseY - 28f;
            _healthPickups.Clear();
            _healthPickups.Add(new HealthPickup(W * 0.25f, deckTop));
            _healthPickups.Add(new HealthPickup(W * 0.75f, deckTop));
        }

        private void UpdatePickups(float dt)
        {
            float deckTop = CurrentDeckY - 28f;
            foreach (var p in _healthPickups)
            {
                p.Y = deckTop;
                p.Update(dt);
                if (p.TryCollect(_player))
                {
                    PowerUpInventory.AddHealthItem(1);
                    Game.Instance.FloatingText.Spawn("+1 MEDKIT", p.X, p.Y - 16, Color.LimeGreen, large: false);
                    Game.Instance.Audio.BeepHeal();
                }
            }
        }

        // ── Draw

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            DrawStormBackground(g, W, H);
            DrawSea(g, W, H);
            DrawShipDeck(g, W);
            foreach (var p in _healthPickups) p.Draw(g);
            foreach (var s in _strikes) s.Draw(g, H);
            _player.Draw(g);
            if (_lightningFlash > 0f)
                using (var br = new SolidBrush(Color.FromArgb((int)(_lightningFlash * 180), Color.White)))
                    g.FillRectangle(br, 0, 0, W, H);
            // ── Unified HUD (single call, screen space) ───────────────────────
            g.ResetTransform();
            GameHUD.Draw(g, _player, W, H);
            // ── Storm-specific survival timer + progress bar ─────────────────
            DrawHUD(g, W, H);
            if (_complete) DrawComplete(g, W, H);
            DrawDevMenuButton(g);
        }

        public override void HandleClick(Point p)
        {
            // Allow the global dev-menu overlay button to work in this scene.
            if (HandleDevMenuClick(p)) return;
        }

        private void DrawStormBackground(Graphics g, int W, int H)
        {
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(15, 15, 40), Color.FromArgb(40, 40, 80), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Rain streaks (cached-seed mode to avoid per-frame allocations).
            using (var pen = new Pen(Color.FromArgb(60, Color.LightBlue), 1))
            {
                for (int i = 0; i < _rainSeedX.Length; i++)
                {
                    float rx = (float)((_rainSeedX[i] * W + _stormAnim * 200) % W);
                    float ry = (float)((_rainSeedY[i] * H + _stormAnim * 300) % H);
                    g.DrawLine(pen, rx, ry, rx - 4, ry + 18);
                }
            }
        }

        private void DrawSea(Graphics g, int W, int H)
        {
            int deckY = (int)CurrentDeckY + 100;
            using (var br = new SolidBrush(Color.FromArgb(180, 20, 60, 160)))
                g.FillRectangle(br, 0, deckY, W, H - deckY);
            using (var pen = new Pen(Color.FromArgb(120, Color.LightBlue), 2))
                for (int wx = 0; wx < W; wx += 60)
                    g.DrawArc(pen, wx + (int)(_stormAnim * 30) % 60 - 30, deckY - 8, 50, 16, 0, 180);
        }

        private void DrawShipDeck(Graphics g, int W)
        {
            int dy = (int)CurrentDeckY;
            using (var br = new SolidBrush(Color.SaddleBrown))
                g.FillRectangle(br, 0, dy, W, 100);
            // Planks
            using (var pen = new Pen(Color.FromArgb(80, Color.Black), 1))
                for (int bx = 0; bx < W; bx += 40)
                    g.DrawLine(pen, bx, dy, bx, dy + 100);
            // Railing
            using (var br = new SolidBrush(Color.FromArgb(180, Color.Sienna)))
                g.FillRectangle(br, 0, dy, W, 8);
        }

        private void DrawHUD(Graphics g, int W, int H)
        {
            // ── Storm-specific survival overlay (below GameHUD band) ─────────
            int topY = GameHUD.BandHeight + 4;

            // Semi-transparent panel behind storm info for readability.
            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(br, 6, topY, 330, 32);

            // Primary objective line: countdown timer.
            float remaining = Math.Max(0, SurvivalGoal - _survivalTimer);
            g.DrawString($"Survive: {remaining:F1}s", _hudFont, Brushes.Cyan, 10, topY + 2);

            // Progress bar shifts color as the player gets closer to clearing.
            float pct = _survivalTimer / SurvivalGoal;
            int barY = topY + 18;
            g.FillRectangle(Brushes.DarkSlateBlue, 10, barY, 300, 8);
            Color progressColor = pct < 0.5f ? Color.DeepSkyBlue : (pct < 0.85f ? Color.Cyan : Color.Lime);
            using (var br = new SolidBrush(Color.FromArgb(180, progressColor)))
                g.FillRectangle(br, 10, barY, (int)(300 * pct), 8);

            // Live danger telemetry.
            Brush dangerBrush = _warningStrikeCount > 0 ? Brushes.Orange : Brushes.DarkGray;
            g.DrawString($"Warnings: {_warningStrikeCount}", _hudFont, dangerBrush, W - 180, topY + 2);
        }

        private void DrawComplete(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 22, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("STORM SURVIVED!", f);
                g.DrawString("STORM SURVIVED!", f, Brushes.Cyan, (W - sz.Width) / 2f, H * 0.38f);
            }
            using (var f = new Font("Courier New", 11))
                g.DrawString("+300 Bounty  Threat -5%  Returning to overworld...",
                             f, Brushes.White, W / 2f - 220, H * 0.38f + 44);
        }
    }
}
