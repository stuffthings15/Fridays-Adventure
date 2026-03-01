using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Hazards;
using Fridays_Adventure.Abilities;
using Fridays_Adventure.Rules;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    public sealed class WarlordBossScene : Scene
    {
        private readonly WarlordConfig _config;

        private Player  _player;
        private Enemy   _boss;
        private Bitmap  _bg;

        private List<Rectangle>    _platforms = new List<Rectangle>();
        private List<Hazard>       _hazards   = new List<Hazard>();
        private List<IceWallInstance> _iceWalls = new List<IceWallInstance>();
        private ComboAssist        _combo     = new ComboAssist();

        // Phase tracking
        private int   _phase = 1;
        private bool  _phaseTransition;
        private float _phaseTimer;

        // Pattern
        private float  _patternTimer;
        private int    _patternStep;
        private string _telegraph;
        private float  _telegraphTimer;
        private const float TelegraphDur = 1.8f;

        // Type-specific timers
        private float _specialTimer;
        private const float SpecialInterval = 4f;

        // Sink / rescue
        private float _sinkTimer;
        private bool  _showRescue;
        private float _rescueTimer;

        // Outcome
        private bool  _victory;
        private float _victoryTimer;

        private static readonly Font _hudFont = new Font("Courier New", 9, FontStyle.Bold);

        public WarlordBossScene(WarlordConfig config) => _config = config;

        // ── Enter / Exit ─────────────────────────────────────────────────────

        public override void OnEnter()
        {
            Build();
            _bg = SpriteManager.GetScaled("bg_island.png",
                  Game.Instance.CanvasWidth, Game.Instance.CanvasHeight);
            Game.Instance.Audio.PlayBoss();
        }

        public override void OnExit()   { _bg?.Dispose(); }
        public override void OnResume() => Game.Instance.Audio.PlayBoss();

        private void Build()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            int gY = H - 120;

            _platforms.Add(new Rectangle(0,       gY, W,        120));
            _platforms.Add(new Rectangle(60,      gY - 110, 140, 18));
            _platforms.Add(new Rectangle(W-200,   gY - 110, 140, 18));
            _platforms.Add(new Rectangle(W/2-70,  gY - 200, 140, 18));

            AddTypeHazards(W, gY);

            _player = new Player(80, gY - 60);
            var ps  = SpriteManager.GetScaled("player_missfriday.png",
                                              _player.Width, _player.Height);
            if (ps != null) _player.Sprite = ps;

            _boss = new Enemy(W - 180, gY - 110,  80, 110,
                              (int)(_config.MaxHp * ThreatSystem.EnemyHpMultiplier()),
                              patrolLeft: W * 0.15f, patrolRight: W * 0.85f);
            _boss.EnemyType    = "Boss";
            _boss.MoveSpeed    = _config.MoveSpeed;
            _boss.AttackDamage = _config.BaseDamage;
            var bs = SpriteManager.GetScaled("GARP.png", 80, 110);
            if (bs != null) _boss.Sprite = bs;
        }

        private void AddTypeHazards(int W, int gY)
        {
            switch (_config.Type)
            {
                case WarlordType.FireLord:
                    for (int fx = 40; fx < W; fx += 220)
                        _hazards.Add(new FireSource(fx, gY - 44, 36, 44));
                    break;
                case WarlordType.SeaStoneLord:
                    _hazards.Add(new SeaStoneZone(W / 2 - 120, gY - 50, 240, 50));
                    break;
                case WarlordType.StormLord:
                    _hazards.Add(new WaterPit(W / 2 - 50, gY, 100, 120));
                    break;
            }
        }

        // ── Update ────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            if (_victory)         { _victoryTimer += dt; if (_victoryTimer >= 4f) Game.Instance.Scenes.Pop(); return; }
            if (_phaseTransition) { _phaseTimer   -= dt; if (_phaseTimer   <= 0) _phaseTransition = false; return; }

            ThreatSystem.Tick(dt);
            HandleInput(dt);
            _player.Update(dt);
            MoveAndCollide(_player, dt);
            DevilFruitRules.Check(_player, _hazards, dt);
            IceSystem.Update(_iceWalls, _hazards, _player, dt);
            CheckWater(dt);
            UpdateBossAI(dt);
            _boss.Update(dt);
            MoveAndCollide(_boss, dt);
            ApplyTypeSpecial(dt);
            _combo.Update(dt, _player, new List<Enemy> { _boss });
            if (_telegraphTimer > 0) _telegraphTimer -= dt;
            foreach (var hz in _hazards) hz.Update(dt);
            CheckCombat();
            CheckOutcome();
        }

        private void HandleInput(float dt)
        {
            var input = Game.Instance.Input;
            if (_player.HasEffect(StatusEffect.Sinking) || _player.HasEffect(StatusEffect.Stunned)) return;
            if (input.LeftHeld)       { _player.VelocityX = -_player.MoveSpeed; _player.FacingRight = false; }
            else if (input.RightHeld) { _player.VelocityX =  _player.MoveSpeed; _player.FacingRight = true; }
            else _player.VelocityX = 0;
            if (input.JumpPressed && _player.IsGrounded)
            { _player.VelocityY = _player.JumpForce; _player.IsGrounded = false; Game.Instance.Audio.BeepJump(); }
            // Variable jump height — release early for short hop (SMB3-style)
            if (!input.JumpHeld && _player.VelocityY < -120f)
                _player.VelocityY = -120f;
            if (input.DodgePressed)  _player.TryDodge();
            if (input.AttackPressed && _player.TryAttack()) Game.Instance.Audio.BeepAttack();
            if (input.Ability1Pressed && _player.UseIceWall(out IceWallInstance w))
            { _iceWalls.Add(w); Game.Instance.Audio.BeepIce(); }
            if (input.Ability2Pressed && _player.UseFlashFreeze())
            { if (_player.DistanceTo(_boss) <= 130f) _boss.ApplyEffect(StatusEffect.Frozen, 1.8f); Game.Instance.Audio.BeepFreeze(); }
            if (input.Ability3Pressed && _player.UseBreakWall())
            {
                for (int i = _iceWalls.Count - 1; i >= 0; i--)
                {
                    var iw = _iceWalls[i];
                    if (iw.IsAlive && Math.Abs(_player.CenterX - (iw.X + iw.Width / 2f)) < 70f) iw.Health = 0;
                }
                if (_boss.IsAlive && _player.DistanceTo(_boss) <= 80f) _boss.TakeDamage(8);
                Game.Instance.Audio.BeepBreak();
            }
        }

        private void UpdateBossAI(float dt)
        {
            if (_boss.HasEffect(StatusEffect.Frozen)) { _boss.VelocityX = 0; return; }
            _patternTimer -= dt;
            if (_patternTimer > 0) return;
            bool p2 = _phase == 2;
            switch (_patternStep % (p2 ? 5 : 4))
            {
                case 0: ShowTelegraph(_phase == 1 ? _config.TauntP1 : _config.TauntP2); _patternTimer = 1.4f; break;
                case 1: _boss.VelocityX = _boss.FacingRight ? _boss.MoveSpeed * 1.3f : -_boss.MoveSpeed * 1.3f; _patternTimer = 0.8f; break;
                case 2: _boss.VelocityX = 0; _boss.TryAttack(); _patternTimer = 0.6f; break;
                case 3: Charge(); _patternTimer = 1.0f; break;
                case 4: TriggerTypeSpecial(); _patternTimer = 0.5f; break;
            }
            _patternStep++;
        }

        private void Charge()
        {
            float dir = _player.X > _boss.X ? 1f : -1f;
            _boss.VelocityX = dir * _boss.MoveSpeed * 2f;
            _boss.FacingRight = dir > 0;
        }

        private void ApplyTypeSpecial(float dt)
        {
            _specialTimer -= dt;
            if (_specialTimer > 0) return;
            _specialTimer = SpecialInterval;
            TriggerTypeSpecial();
        }

        private void TriggerTypeSpecial()
        {
            int W  = Game.Instance.CanvasWidth;
            int H  = Game.Instance.CanvasHeight;
            int gY = H - 120;
            var rng = new Random();

            switch (_config.Type)
            {
                case WarlordType.FireLord:
                    // Spawn extra fire source near player
                    _hazards.Add(new FireSource(_player.X - 20 + rng.Next(40),
                                               gY - 44, 36, 44));
                    break;
                case WarlordType.SeaStoneLord:
                    // Throw a SeaStone fragment near player
                    _hazards.Add(new SeaStoneZone(_player.X - 40,
                                                  gY - 40, 100, 40));
                    break;
            }
        }

        private void ShowTelegraph(string text) { _telegraph = text; _telegraphTimer = TelegraphDur; }

        private void CheckWater(float dt)
        {
            if (!_player.HasEffect(StatusEffect.Sinking)) { _sinkTimer = 0; _showRescue = false; return; }
            _sinkTimer += dt;
            if (Game.Instance.Input.AnyMash) _sinkTimer = Math.Max(0, _sinkTimer - 0.4f);
            if (_sinkTimer >= RescueSystem.GetMashWindow(Game.Instance.CrewBonds))
            { _showRescue = RescueSystem.AutoRescueAvailable(Game.Instance.CrewBonds); _rescueTimer = 0; }
            if (_showRescue) { _rescueTimer += dt; if (_rescueTimer >= 0.8f) RescueSystem.ApplyRescue(_player, ref _sinkTimer); }
        }

        private void MoveAndCollide(Character c, float dt)
        {
            c.X = Math.Max(0, Math.Min(Game.Instance.CanvasWidth - c.Width, c.X + c.VelocityX * dt));
            foreach (var p in _platforms) if (c.Hitbox.IntersectsWith(p))
            { c.X = c.VelocityX > 0 ? p.Left - c.Width : p.Right; c.VelocityX = 0; }
            c.Y += c.VelocityY * dt;
            c.IsGrounded = false;
            foreach (var p in _platforms)
            {
                if (!c.Hitbox.IntersectsWith(p)) continue;
                if (c.VelocityY >= 0) { c.Y = p.Top - c.Height; c.VelocityY = 0; c.IsGrounded = true; }
                else { c.Y = p.Bottom; c.VelocityY = 0; }
            }
        }

        private void CheckCombat()
        {
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
            if (pAtk != Rectangle.Empty && pAtk.IntersectsWith(_boss.Hitbox))
                _boss.TakeDamage(_player.AttackDamage);
            if (!stomped && _boss.AttackHitbox != Rectangle.Empty &&
                _boss.AttackHitbox.IntersectsWith(_player.Hitbox))
            { _player.TakeDamage(_boss.AttackDamage); Game.Instance.Audio.BeepHurt(); }
        }

        private void CheckOutcome()
        {
            if (!_boss.IsAlive && !_victory)
            {
                _victory = true;
                BountySystem.Award(3500);
                ThreatSystem.OnBossDefeated();
                Game.Instance.CrewBonds += 3;
                Game.Instance.Save.SetFlag("warlord_defeated");
                Game.Instance.Save.Save();
                Game.Instance.Audio.StopMusic();
            }
            if (!_player.IsAlive)
                Game.Instance.Scenes.Replace(new GameOverScene());
            if (_phase == 1 && _boss.Health <= _config.Phase2Hp)
            {
                _phase = 2; _phaseTransition = true; _phaseTimer = 2.2f;
                _boss.MoveSpeed    *= 1.35f;
                _boss.AttackDamage  = (int)(_config.BaseDamage * 1.5f);
                ShowTelegraph(_config.TauntP2);
            }
        }

        // ── Draw ─────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            if (_bg != null) g.DrawImage(_bg, 0, 0, W, H);
            else g.Clear(Color.FromArgb(30, 10, 10));

            // Type-specific overlay tint
            Color tint;
            switch (_config.Type)
            {
                case WarlordType.FireLord:      tint = Color.FromArgb(40, 200, 50, 0); break;
                case WarlordType.SeaStoneLord:  tint = Color.FromArgb(40, 180, 180, 0); break;
                default:                        tint = Color.FromArgb(30, 0, 50, 200); break;
            }
            using (var br = new SolidBrush(tint)) g.FillRectangle(br, 0, 0, W, H);

            foreach (var p in _platforms)
            { using (var br = new SolidBrush(Color.FromArgb(120,80,50))) g.FillRectangle(br, p); }
            foreach (var hz in _hazards)   hz.Draw(g);
            foreach (var w  in _iceWalls)  w.Draw(g);
            _combo.Draw(g);
            if (_boss.IsAlive) _boss.Draw(g);
            _player.Draw(g);
            if (_player.IsAttacking)
                using (var br = new SolidBrush(Color.FromArgb(70, Color.Cyan)))
                    g.FillRectangle(br, _player.AttackHitbox);

            DrawBossHUD(g, W, H);
            DrawPlayerHUD(g, W, H);
            if (_telegraphTimer > 0) DrawTelegraph(g, W, H);
            if (_phaseTransition)   DrawPhase2Banner(g, W, H);
            if (_showRescue)        DrawRescue(g, W, H);
            if (_victory)           DrawVictory(g, W, H);
        }

        private void DrawBossHUD(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(br, W/2-200, 8, 400, 36);
            string label = $"{_config.Epithet}  ·  {_config.Name}  [Phase {_phase}]";
            using (var f = new Font("Courier New", 8, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(label, f);
                g.DrawString(label, f, Brushes.Gold, W/2f - sz.Width/2f, 10);
            }
            g.FillRectangle(Brushes.DarkRed, W/2-180, 24, 360, 12);
            float pct = _boss.IsAlive ? (float)_boss.Health / _boss.MaxHealth : 0f;
            using (var br = new SolidBrush(_phase == 2 ? Color.Gold : Color.OrangeRed))
                g.FillRectangle(br, W/2-180, 24, (int)(360*pct), 12);
            g.DrawLine(Pens.White, W/2, 24, W/2, 36);
        }

        private void DrawPlayerHUD(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(br, 0, H-54, 300, 54);
            g.DrawString("HP", _hudFont, Brushes.White, 6, H-50);
            g.FillRectangle(Brushes.DarkRed, 30, H-48, 140, 12);
            using (var br = new SolidBrush(Color.LimeGreen))
                g.FillRectangle(br, 30, H-48, (int)(140*(float)_player.Health/_player.MaxHealth), 12);
            g.DrawString("ICE", _hudFont, Brushes.Cyan, 6, H-32);
            g.FillRectangle(Brushes.DarkSlateBlue, 30, H-30, 110, 10);
            using (var br = new SolidBrush(Color.FromArgb(180,220,255)))
                g.FillRectangle(br, 30, H-30, (int)(110*(float)_player.IceReserve/_player.MaxIceReserve), 10);
            _combo.DrawHUD(g, 180, H-50);
        }

        private void DrawTelegraph(Graphics g, int W, int H)
        {
            float a = Math.Min(1f, _telegraphTimer / TelegraphDur);
            using (var br = new SolidBrush(Color.FromArgb((int)(160*a), 0, 0, 0)))
                g.FillRectangle(br, 0, H*0.55f, W, 42);
            using (var f = new Font("Courier New", 10, FontStyle.Italic))
            {
                SizeF sz = g.MeasureString(_telegraph, f);
                using (var br = new SolidBrush(Color.FromArgb((int)(255*a), Color.Yellow)))
                    g.DrawString(_telegraph, f, br, (W-sz.Width)/2f, H*0.55f+10);
            }
        }

        private void DrawPhase2Banner(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(120, Color.DarkRed)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("— WARLORD UNLEASHED —", f);
                g.DrawString("— WARLORD UNLEASHED —", f, Brushes.Gold, (W-sz.Width)/2f, H*0.42f);
            }
        }

        private void DrawRescue(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, Color.DarkBlue)))
                g.FillRectangle(br, W/2-180, H/2-28, 360, 56);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Finn reaches out!\nMash  SPACE / Z / X!", f, Brushes.Cyan, W/2-168, H/2-22);
        }

        private void DrawVictory(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 22, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString($"{_config.Name.ToUpper()} DEFEATED!", f);
                g.DrawString($"{_config.Name.ToUpper()} DEFEATED!", f, Brushes.Gold, (W-sz.Width)/2f, H*0.32f);
            }
            using (var f = new Font("Courier New", 11))
            {
                g.DrawString("+3500 Bounty    +3 Crew Bonds    Threat -20%", f, Brushes.White, W/2f-200, H*0.32f+48);
                g.DrawString($"\"{_config.Epithet}\" has been added to your legend.", f, Brushes.LightGray, W/2f-210, H*0.32f+68);
            }
        }
    }
}
