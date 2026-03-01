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

namespace Fridays_Adventure.Scenes
{
    public sealed class BossScene : Scene
    {
        // ── State ────────────────────────────────────────────────────────────
        private Player  _player;
        private Enemy   _boss;
        private Bitmap  _bg;
        private Bitmap  _bossSprite;
        private Bitmap  _playerSprite;

        private List<Rectangle>    _platforms;
        private List<Hazard>       _hazards;
        private List<IceWallInstance> _iceWalls;

        // Boss phases
        private int   _phase = 1;
        private bool  _phaseTransition;
        private float _phaseTimer;

        // Attack telegraphing
        private string _telegraph      = "";
        private float  _telegraphTimer;
        private const float TelegraphDuration = 1.6f;

        // Boss pattern timers
        private float _patternTimer;
        private int   _patternStep;

        // SeaStone throws (temporary hazard zones spawned in Phase 2)
        private readonly List<SeaStoneZone> _thrownStones = new List<SeaStoneZone>();
        private float _stoneTimer;

        // Sink / rescue
        private float _sinkMashTimer;
        private bool  _showRescue;
        private float _rescueTimer;

        // Outcome
        private bool  _victory;
        private float _victoryTimer;

        private static readonly Font _hudFont  = new Font("Courier New", 9, FontStyle.Bold);
        private static readonly Font _bigFont  = new Font("Courier New", 18, FontStyle.Bold);
        private static readonly Font _midFont  = new Font("Courier New", 12, FontStyle.Bold);

        // ── Enter / Exit ─────────────────────────────────────────────────────

        public override void OnEnter()
        {
            Build();
            _bg           = SpriteManager.Get("bg_island.png");
            _bossSprite   = SpriteManager.GetScaled("GARP.png",    80, 110);
            _playerSprite = SpriteManager.GetScaled("player_missfriday.png", 40, 60);
            Game.Instance.Audio.PlayBoss();
        }

        public override void OnExit()
        {
            _bossSprite?.Dispose();
            _playerSprite?.Dispose();
        }

        public override void OnResume() => Game.Instance.Audio.PlayBoss();

        private void Build()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            int g = H - 120;

            _iceWalls  = new List<IceWallInstance>();
            _hazards   = new List<Hazard>();
            _platforms = new List<Rectangle>
            {
                new Rectangle(0,       g, W,         120), // main arena floor
                new Rectangle(60,      g - 110, 160, 18),  // left platform
                new Rectangle(W - 220, g - 110, 160, 18),  // right platform
                new Rectangle(W/2-80,  g - 200, 160, 18),  // center high
            };

            // One fire source at each side
            _hazards.Add(new FireSource(20,    g - 48, 38, 48));
            _hazards.Add(new FireSource(W - 58, g - 48, 38, 48));

            _player = new Player(80, g - 60);
            if (_playerSprite != null) _player.Sprite = _playerSprite;

            _boss = new Enemy(W - 160, g - 110, 80, 110, 200,
                              patrolLeft: W * 0.25f, patrolRight: W * 0.85f);
            _boss.EnemyType  = "Boss";
            _boss.MoveSpeed  = 130f;
            _boss.AttackDamage = 18;
            if (_bossSprite != null) _boss.Sprite = _bossSprite;
        }

        // ── Update ────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            if (_victory)      { UpdateVictory(dt);    return; }
            if (_phaseTransition) { UpdateTransition(dt); return; }

            HandleInput(dt);
            _player.Update(dt);
            MoveAndCollide(_player, dt);

            DevilFruitRules.Check(_player, _hazards, dt);
            IceSystem.Update(_iceWalls, _hazards, _player, dt);
            CheckWater(dt);

            UpdateBossAI(dt);
            _boss.Update(dt);
            MoveAndCollide(_boss, dt);

            UpdateStonesTimer(dt);
            UpdateTelegraph(dt);
            CheckCombat();
            CheckOutcome();
        }

        private void HandleInput(float dt)
        {
            var input = Game.Instance.Input;
            if (_player.HasEffect(StatusEffect.Sinking) ||
                _player.HasEffect(StatusEffect.Stunned)) return;

            if (input.LeftHeld)       { _player.VelocityX = -_player.MoveSpeed; _player.FacingRight = false; }
            else if (input.RightHeld) { _player.VelocityX =  _player.MoveSpeed; _player.FacingRight = true; }
            else _player.VelocityX = 0;

            if (input.JumpPressed && _player.IsGrounded)
            {
                _player.VelocityY  = _player.JumpForce;
                _player.IsGrounded = false;
                Game.Instance.Audio.BeepJump();
            }
            // Variable jump height — release early for short hop (SMB3-style)
            if (!input.JumpHeld && _player.VelocityY < -120f)
                _player.VelocityY = -120f;
            if (input.DodgePressed)  _player.TryDodge();
            if (input.AttackPressed && _player.TryAttack())
                Game.Instance.Audio.BeepAttack();

            if (input.Ability1Pressed && _player.UseIceWall(out IceWallInstance wall))
            {
                _iceWalls.Add(wall);
                Game.Instance.Audio.BeepIce();
            }
            if (input.Ability2Pressed && _player.UseFlashFreeze())
            {
                if (_player.DistanceTo(_boss) <= 130f)
                    _boss.ApplyEffect(StatusEffect.Frozen, 2f);
                Game.Instance.Audio.BeepFreeze();
            }
            if (input.Ability3Pressed && _player.UseBreakWall())
            {
                for (int i = _iceWalls.Count - 1; i >= 0; i--)
                {
                    var w = _iceWalls[i];
                    if (w.IsAlive && _player.DistanceTo(new Entities.Entity(w.X, w.Y, w.Width, w.Height)) <= 70f)
                        w.Health = 0;
                }
                if (_boss.IsAlive && _player.DistanceTo(_boss) <= 80f)
                    _boss.TakeDamage(8);
                Game.Instance.Audio.BeepBreak();
            }
        }

        // ── Boss AI Pattern ───────────────────────────────────────────────────

        private void UpdateBossAI(float dt)
        {
            if (_boss.HasEffect(StatusEffect.Frozen))
            { _boss.VelocityX = 0; return; }

            _patternTimer -= dt;
            if (_patternTimer > 0) return;

            if (_phase == 1)
                RunPhase1Pattern();
            else
                RunPhase2Pattern();
        }

        private void RunPhase1Pattern()
        {
            switch (_patternStep % 4)
            {
                case 0: ShowTelegraph("MARINE CAPTAIN: \"You're outmatched, pirate!\"");
                        _boss.VelocityX = _boss.FacingRight ? _boss.MoveSpeed * 1.2f : -_boss.MoveSpeed * 1.2f;
                        _patternTimer = 0.9f; break;
                case 1: _boss.VelocityX = 0; _boss.TryAttack(); _patternTimer = 0.6f; break;
                case 2: ShowTelegraph("CAPTAIN: \"Justice always prevails!\"");
                        _patternTimer = 1.2f; break;
                case 3: ChargeAtPlayer(); _patternTimer = 1.0f; break;
            }
            _patternStep++;
        }

        private void RunPhase2Pattern()
        {
            switch (_patternStep % 5)
            {
                case 0: ShowTelegraph("CAPTAIN: \"SeaStone — your powers are NOTHING!\"");
                        _patternTimer = 1.4f; break;
                case 1: ThrowSeaStone(); _patternTimer = 0.8f; break;
                case 2: ChargeAtPlayer(); _patternTimer = 0.7f; break;
                case 3: _boss.TryAttack(); _patternTimer = 0.5f; break;
                case 4: ShowTelegraph("CAPTAIN: \"You can't freeze the sea!\"");
                        ThrowSeaStone(); _patternTimer = 1.0f; break;
            }
            _patternStep++;
        }

        private void ChargeAtPlayer()
        {
            float dir = _player.X > _boss.X ? 1f : -1f;
            _boss.VelocityX   = dir * _boss.MoveSpeed * 2.2f;
            _boss.FacingRight = dir > 0;
        }

        private void ThrowSeaStone()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            int gY = H - 120;
            float rx = (float)(new Random().NextDouble() * (W - 100) + 50);
            var zone = new SeaStoneZone(rx, gY - 36, 80, 36);
            _thrownStones.Add(zone);
            _hazards.Add(zone);
        }

        private void UpdateStonesTimer(float dt)
        {
            _stoneTimer += dt;
            // Remove stones after 5 seconds
            if (_stoneTimer >= 5f)
            {
                foreach (var s in _thrownStones) _hazards.Remove(s);
                _thrownStones.Clear();
                _stoneTimer = 0;
            }
            foreach (var hz in _hazards) hz.Update(dt);
        }

        private void ShowTelegraph(string text)
        {
            _telegraph      = text;
            _telegraphTimer = TelegraphDuration;
        }

        private void UpdateTelegraph(float dt)
        {
            if (_telegraphTimer > 0) _telegraphTimer -= dt;
        }

        // ── Phase Transition ──────────────────────────────────────────────────

        private void CheckOutcome()
        {
            if (!_boss.IsAlive && !_victory)
            {
                _victory      = true;
                _victoryTimer = 0;
                Game.Instance.Audio.StopMusic();
                Game.Instance.PlayerBounty += 2000;
                Game.Instance.ThreatLevel   = Math.Max(0, Game.Instance.ThreatLevel - 20);
                Game.Instance.CrewBonds    += 2;
                Game.Instance.Save.SetFlag("marine_captain_defeated");
                Game.Instance.Save.Save();
                return;
            }
            if (!_player.IsAlive)
                Game.Instance.Scenes.Replace(new GameOverScene());

            // Phase transition at 50 % boss HP
            if (_phase == 1 && _boss.Health <= _boss.MaxHealth / 2)
            {
                _phase            = 2;
                _phaseTransition  = true;
                _phaseTimer       = 2.5f;
                _boss.MoveSpeed  *= 1.4f;
                _boss.AttackDamage = 24;
                ShowTelegraph("CAPTAIN: \"Enough games — Absolute Justice, FINAL FORM!\"");
                Game.Instance.Audio.BeepHurt();
            }
        }

        private void UpdateTransition(float dt)
        {
            _phaseTimer -= dt;
            if (_phaseTimer <= 0) _phaseTransition = false;
        }

        private void UpdateVictory(float dt)
        {
            _victoryTimer += dt;
            if (_victoryTimer >= 4f)
                Game.Instance.Scenes.Pop(); // Return to overworld
        }

        // ── Collision & Combat ────────────────────────────────────────────────

        private void MoveAndCollide(Character c, float dt)
        {
            c.X += c.VelocityX * dt;
            c.X  = Math.Max(0, Math.Min(Game.Instance.CanvasWidth - c.Width, c.X));
            Resolve(c, horizontal: true);
            c.Y += c.VelocityY * dt;
            c.IsGrounded = false;
            Resolve(c, horizontal: false);
        }

        private void Resolve(Character c, bool horizontal)
        {
            foreach (var p in _platforms)
            {
                if (!c.Hitbox.IntersectsWith(p)) continue;
                if (horizontal)
                {
                    c.X         = c.VelocityX > 0 ? p.Left - c.Width : p.Right;
                    c.VelocityX = 0;
                }
                else if (c.VelocityY >= 0)
                {
                    c.Y = p.Top - c.Height; c.VelocityY = 0; c.IsGrounded = true;
                }
                else
                { c.Y = p.Bottom; c.VelocityY = 0; }
            }
        }

        private void CheckWater(float dt)
        {
            if (!_player.HasEffect(StatusEffect.Sinking)) { _sinkMashTimer = 0; _showRescue = false; return; }
            _sinkMashTimer += dt;
            if (Game.Instance.Input.AnyMash) _sinkMashTimer = Math.Max(0, _sinkMashTimer - 0.5f);
            if (_sinkMashTimer >= RescueSystem.GetMashWindow(Game.Instance.CrewBonds))
            {
                _showRescue  = RescueSystem.AutoRescueAvailable(Game.Instance.CrewBonds);
                _rescueTimer = 0;
            }
            if (_showRescue)
            {
                _rescueTimer += dt;
                if (_rescueTimer >= 0.8f)
                    RescueSystem.ApplyRescue(_player, ref _sinkMashTimer);
            }
        }

        private void CheckCombat()
        {
            // SMB3 Head Stomp on boss
            bool stomped = false;
            if (_boss.IsAlive && _player.VelocityY > 0 && !_player.IsInvincible)
            {
                float pBot = _player.Y + _player.Height;
                float overlap = pBot - _boss.Y;
                if (overlap > 0 && overlap < 22 &&
                    _player.CenterX > _boss.X - 8 && _player.CenterX < _boss.X + _boss.Width + 8)
                {
                    _boss.TakeDamage(_player.AttackDamage * 2);
                    _player.VelocityY = _player.JumpForce * 0.45f;
                    _player.IsGrounded = false;
                    Game.Instance.Audio.BeepStomp();
                    stomped = true;
                }
            }

            var pAtk = _player.AttackHitbox;
            if (_boss.IsAlive && pAtk != Rectangle.Empty &&
                pAtk.IntersectsWith(_boss.Hitbox))
                _boss.TakeDamage(_player.AttackDamage);

            if (!stomped && _boss.AttackHitbox != Rectangle.Empty &&
                _boss.AttackHitbox.IntersectsWith(_player.Hitbox))
            {
                _player.TakeDamage(_boss.AttackDamage);
                Game.Instance.Audio.BeepHurt();
            }
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            DrawBackground(g, W, H);
            DrawPlatforms(g);
            foreach (var hz in _hazards)  hz.Draw(g);
            foreach (var w  in _iceWalls) w.Draw(g);
            if (_boss.IsAlive)  _boss.Draw(g);
            _player.Draw(g);
            if (_player.IsAttacking)
                using (var br = new SolidBrush(Color.FromArgb(70, Color.Cyan)))
                    g.FillRectangle(br, _player.AttackHitbox);

            DrawBossHUD(g, W, H);
            DrawPlayerHUD(g, W, H);
            if (_telegraphTimer > 0) DrawTelegraph(g, W, H);
            if (_phaseTransition)    DrawPhaseTransition(g, W, H);
            if (_showRescue)         DrawRescuePrompt(g, W, H);
            if (_victory)            DrawVictory(g, W, H);
        }

        private void DrawBackground(Graphics g, int W, int H)
        {
            if (_bg != null) { g.DrawImage(_bg, 0, 0, W, H); return; }
            using (var br = new LinearGradientBrush(new Rectangle(0,0,W,H),
                Color.FromArgb(30,10,10), Color.FromArgb(80,30,10), 90f))
                g.FillRectangle(br, 0, 0, W, H);
        }

        private void DrawPlatforms(Graphics g)
        {
            foreach (var p in _platforms)
            {
                using (var br = new SolidBrush(Color.FromArgb(120, 80, 50)))
                    g.FillRectangle(br, p);
                using (var br = new SolidBrush(Color.FromArgb(60, 140, 50)))
                    g.FillRectangle(br, p.X, p.Y, p.Width, 5);
            }
        }

        private void DrawBossHUD(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(br, W/2 - 180, 8, 360, 36);
            using (var f = new Font("Courier New", 8, FontStyle.Bold))
            {
                string label = _phase == 1 ? "MARINE CAPTAIN" : "MARINE CAPTAIN — FINAL FORM";
                SizeF  sz    = g.MeasureString(label, f);
                g.DrawString(label, f, _phase == 1 ? Brushes.OrangeRed : Brushes.Gold,
                             W/2f - sz.Width/2f, 10);
            }
            int bx = W/2 - 160;
            g.FillRectangle(Brushes.DarkRed, bx, 22, 320, 14);
            float pct = _boss.IsAlive ? (float)_boss.Health / _boss.MaxHealth : 0f;
            Color barColor = _phase == 1 ? Color.OrangeRed : Color.Gold;
            using (var br = new SolidBrush(barColor))
                g.FillRectangle(br, bx, 22, (int)(320 * pct), 14);
            // Phase divider marker at 50%
            g.DrawLine(Pens.White, bx + 160, 22, bx + 160, 36);
        }

        private void DrawPlayerHUD(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(br, 0, H - 44, 260, 44);
            g.DrawString("HP", _hudFont, Brushes.White, 6, H - 38);
            g.FillRectangle(Brushes.DarkRed, 30, H - 36, 140, 12);
            float hp = (float)_player.Health / _player.MaxHealth;
            using (var br = new SolidBrush(Color.LimeGreen))
                g.FillRectangle(br, 30, H - 36, (int)(140 * hp), 12);
            g.DrawString("ICE", _hudFont, Brushes.Cyan, 6, H - 22);
            g.FillRectangle(Brushes.DarkSlateBlue, 30, H - 20, 110, 10);
            float ice = (float)_player.IceReserve / _player.MaxIceReserve;
            using (var br = new SolidBrush(Color.FromArgb(180, 220, 255)))
                g.FillRectangle(br, 30, H - 20, (int)(110 * ice), 10);
            using (var f = new Font("Courier New", 8))
                g.DrawString($"Phase {_phase}  Bonds:{Game.Instance.CrewBonds}", f,
                             Brushes.LightGray, 180, H - 36);
        }

        private void DrawTelegraph(Graphics g, int W, int H)
        {
            float alpha = Math.Min(1f, _telegraphTimer / TelegraphDuration);
            using (var br = new SolidBrush(Color.FromArgb((int)(160 * alpha), 0, 0, 0)))
                g.FillRectangle(br, 0, H * 0.55f, W, 44);
            using (var f = new Font("Courier New", 10, FontStyle.Italic))
            {
                SizeF sz = g.MeasureString(_telegraph, f);
                using (var br = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Yellow)))
                    g.DrawString(_telegraph, f, br, (W - sz.Width) / 2f, H * 0.55f + 12);
            }
        }

        private void DrawPhaseTransition(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(120, Color.DarkRed)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("— PHASE 2 —", f);
                g.DrawString("— PHASE 2 —", f, Brushes.Gold, (W - sz.Width) / 2f, H * 0.42f);
            }
        }

        private void DrawRescuePrompt(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, Color.DarkBlue)))
                g.FillRectangle(br, W/2-180, H/2-28, 360, 56);
            g.DrawString("Finn reaches out!\nMash  SPACE / Z / X  to survive!",
                         _midFont, Brushes.Cyan, W/2 - 168, H/2 - 22);
        }

        private void DrawVictory(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 24, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("CAPTAIN DEFEATED!", f);
                g.DrawString("CAPTAIN DEFEATED!", f, Brushes.Gold, (W - sz.Width)/2f, H * 0.34f);
            }
            using (var f = new Font("Courier New", 11))
            {
                g.DrawString("+2000 Bounty    +2 Crew Bonds    Threat -20%",
                             f, Brushes.White, W/2f - 190, H * 0.34f + 48);
                g.DrawString("Returning to overworld...",
                             f, Brushes.LightGray, W/2f - 130, H * 0.34f + 72);
            }
        }
    }
}
