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
        private Bitmap  _uiPanel;

        private List<Rectangle>    _platforms = new List<Rectangle>();
        private List<Hazard>       _hazards   = new List<Hazard>();
        private List<IceWallInstance> _iceWalls = new List<IceWallInstance>();
        private readonly List<PointF> _centipedeSegments = new List<PointF>();
        // Enemy sprites cycled across body segments — one per enemy type so the
        // centipede body is a visible composite of all enemy models.
        private Bitmap[] _segmentSprites;
        private ComboAssist        _combo     = new ComboAssist();

        private const int   CentipedeSegmentCount   = 10;
        private const float CentipedeSegmentSpacing = 34f;

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

        // Break shockwave
        private float _breakShockwaveTimer;
        private float _breakShockwaveWorldX;
        private float _breakShockwaveWorldY;

        // Outcome
        private bool  _victory;
        private float _victoryTimer;

        private static readonly Font _hudFont  = new Font("Courier New", 9, FontStyle.Bold);
        private static readonly Font _hudFont8  = new Font("Courier New", 8, FontStyle.Bold);

        // ── Cached GDI objects — avoids per-frame / per-loop allocations ─────
        private static readonly SolidBrush _segEmptyBr     = new SolidBrush(Color.FromArgb(28, 38, 52));
        private static readonly SolidBrush _segWhiteBr     = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
        private static readonly SolidBrush _segOrangeRedBr = new SolidBrush(Color.OrangeRed);
        private static readonly SolidBrush _segGoldBr      = new SolidBrush(Color.Gold);
        private static readonly SolidBrush _segLimeBr      = new SolidBrush(Color.LimeGreen);
        private static readonly SolidBrush _segYellowBr    = new SolidBrush(Color.Yellow);
        private static readonly SolidBrush _segIceBr       = new SolidBrush(Color.FromArgb(180, 220, 255));
        private static readonly SolidBrush _hudPanelBr     = new SolidBrush(Color.FromArgb(210, 10, 10, 30));
        private static readonly Pen        _hudBorderPen1  = new Pen(Color.FromArgb(120, 255, 69, 0), 2);   // OrangeRed
        private static readonly Pen        _hudBorderPen2  = new Pen(Color.FromArgb(120, 255, 215, 0), 2);  // Gold
        private static readonly SolidBrush _platBaseBr     = new SolidBrush(Color.FromArgb(160, 100, 50));
        private static readonly SolidBrush _platTopBr      = new SolidBrush(Color.FromArgb(200, 130, 70));
        private static readonly SolidBrush _platBotBr      = new SolidBrush(Color.FromArgb(110, 60, 28));
        private static readonly Pen        _platMortarPen  = new Pen(Color.FromArgb(100, 70, 30), 1);
        private static readonly SolidBrush _centBodyBr     = new SolidBrush(Color.FromArgb(220, 80, 170, 80));
        private static readonly Pen        _centBodyPen    = new Pen(Color.FromArgb(230, 35, 90, 35), 2);

        public WarlordBossScene(WarlordConfig config) => _config = config;

        // ── Enter / Exit ─────────────────────────────────────────────────────

        public override void OnEnter()
        {
            Build();

            // Each warlord gets a themed background — all live in Assets\Sprites\ with bg_ prefix
            string bgFile;
            switch (_config.Type)
            {
                case WarlordType.FireLord:      bgFile = "bg_Warlord_Sudo.png";          break;
                case WarlordType.SeaStoneLord:  bgFile = "bg_Marine_Blockade.png";       break;
                case WarlordType.StormLord:     bgFile = "bg_Warlord_Vanta.png";         break;
                case WarlordType.CentipedeLord: bgFile = "bg_Centipede_of_the_Deep.png"; break;
                default:                        bgFile = "bg_island.png";                break;
            }
            _bg = SpriteManager.Get(bgFile);

            _uiPanel = SpriteManager.Get("ui_panel.png");
            Game.Instance.Audio.ContinueOrPlay("boss");
        }

        // _bg comes from SpriteManager cache — do NOT dispose it
        public override void OnExit()   { _bg = null; }
        public override void OnResume() => Game.Instance.Audio.ContinueOrPlay("boss");

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
            _player.ApplySelectedSprite();

            // Enemy constructor scales w/h by 1.5× internally:
            //   passed (80,110) → actual (120,165).
            // Spawn Y must use the ACTUAL height so the boss stands on
            // the ground surface rather than clipping 55px below it.
            // The old Y = gY-110 caused the boss to overlap the floor
            // platform on frame 1, and horizontal collision pushed it
            // to X = platformRight = W — completely off-screen.
            int bossW = 80, bossH = 110;
            int actualW = (int)(bossW * 1.5f);
            int actualH = (int)(bossH * 1.5f);
            _boss = new Enemy(W - actualW - 60, gY - actualH, bossW, bossH,
                              (int)(_config.MaxHp * ThreatSystem.EnemyHpMultiplier()),
                              patrolLeft: W * 0.15f, patrolRight: W * 0.85f);
            _boss.EnemyType    = "Boss";
            _boss.MoveSpeed    = _config.MoveSpeed;
            _boss.AttackDamage = _config.BaseDamage;
            // Use dedicated boss sprite art scaled to match actual hitbox size
            var bs = SpriteManager.GetScaled("enemy_boss.png", actualW, actualH);
            if (bs != null) _boss.Sprite = bs;

            if (_config.Type == WarlordType.CentipedeLord)
            {
                // Load every named enemy sprite so each segment of the centipede
                // body displays a different enemy model — a chimera of all foes.
                string[] enemyNames =
                {
                    "enemy_Garp.png",
                    "enemy_Raptor_Marauder.png",
                    "enemy_Ronin_Enforcer.png",
                    "enemy_Thunder_Mask_Priest.png",
                    "enemy_hammer_bro.png",
                    "enemy_Oni_Ashigaru.png",
                    "enemy_boss.png",
                };
                int segPx = 30; // matches GetCentipedeSegmentRect size
                _segmentSprites = new Bitmap[enemyNames.Length];
                for (int si = 0; si < enemyNames.Length; si++)
                    _segmentSprites[si] = SpriteManager.GetScaled(enemyNames[si], segPx, segPx);

                _centipedeSegments.Clear();
                for (int i = 0; i < CentipedeSegmentCount; i++)
                    _centipedeSegments.Add(new PointF(_boss.CenterX - i * CentipedeSegmentSpacing, _boss.CenterY));
            }
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
                case WarlordType.CentipedeLord:
                    // Gauntlet arena — every hazard type present
                    _hazards.Add(new WaterPit(W / 2 - 50, gY, 100, 120));
                    _hazards.Add(new SeaStoneZone(W / 2 - 100, gY - 50, 200, 50));
                    for (int fx = 60; fx < W; fx += W / 4)
                        _hazards.Add(new FireSource(fx, gY - 44, 28, 44));
                    break;
            }
        }

        // ── Update ────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            if (_victory)         { _victoryTimer += dt; if (_victoryTimer >= 4f) { var cfg = _config; Game.Instance.Scenes.Replace(new VictoryScene($"{cfg.Name.ToUpper()} DEFEATED!", "+3500 Bounty   +3 Crew Bonds   Threat -20%", () => { SessionStats.Instance.RecordLevelComplete(); Game.Instance.LevelJustCompleted = true; Game.Instance.Scenes.Replace(new CreditsScene()); })); } return; }
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
            UpdateCentipedeSegments(dt);
            ApplyTypeSpecial(dt);
            _combo.Update(dt, _player, new List<Enemy> { _boss });
            if (_telegraphTimer > 0) _telegraphTimer -= dt;
            HudHelper.UpdateBreakShockwaveTimer(ref _breakShockwaveTimer, dt);
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
            if (input.JumpPressed && _player.JumpsRemaining > 0)
            { _player.VelocityY = _player.JumpForce; _player.IsGrounded = false; _player.JumpsRemaining--; Game.Instance.Audio.BeepJump(); }
            // Variable jump height — release early for short hop (SMB3-style)
            if (!input.JumpHeld && _player.VelocityY < -120f)
                _player.VelocityY = -120f;
            if (input.DodgePressed)  _player.TryDodge();
            if (input.AttackPressed && _player.TryAttack()) Game.Instance.Audio.BeepAttack();
            if (input.Ability1Pressed && _player.UseIceWall(out IceWallInstance w))
            { _iceWalls.Add(w); Game.Instance.Audio.BeepIce(); }
            if (input.Ability2Pressed && _player.UseFlashFreeze())
            {
                if (_player.DistanceTo(_boss) <= 130f)
                    _boss.ApplyEffect(StatusEffect.Frozen, _player.GetFlashFreezeDuration(1.8f));
                Game.Instance.Audio.BeepFreeze();
            }
            if (input.Ability3Pressed && _player.UseBreakWall())
            {
                for (int i = _iceWalls.Count - 1; i >= 0; i--)
                {
                    var iw = _iceWalls[i];
                    if (iw.IsAlive && Math.Abs(_player.CenterX - (iw.X + iw.Width / 2f)) < 70f) iw.Health = 0;
                }
                if (_boss.IsAlive && _player.DistanceTo(_boss) <= 80f) _boss.TakeDamage(_player.BreakWallShockwaveDamage);
                Game.Instance.Audio.BeepBreak();
                _breakShockwaveTimer  = 0.001f;
                _breakShockwaveWorldX = _player.CenterX;
                _breakShockwaveWorldY = _player.CenterY;
            }
            // Pause and inventory consistent with all other gameplay scenes
            if (input.PausePressed) Game.Instance.Scenes.Push(new PauseScene());
            // C key — Quick Dash (works grounded or airborne, Team 7 Idea 7)
            if (input.AirDashPressed && _player.TryDash())
                Game.Instance.Audio.BeepJump();
            if (input.InventoryPressed) Game.Instance.Scenes.Push(new InventoryScene(_player));
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
                case WarlordType.StormLord:
                    // Twin lightning bolts bracket the player
                    float lx = Math.Max(10, _player.X - 40 + rng.Next(20));
                    _hazards.Add(new FireSource(lx,                          gY - 44, 24, 44));
                    _hazards.Add(new FireSource(lx + rng.Next(80, 120),      gY - 44, 24, 44));
                    ShowTelegraph("VANTA: \"Lightning falls — nowhere is safe!\"");
                    break;
                case WarlordType.CentipedeLord:
                    // Unleashes all hazard types at once
                    float cx = Math.Max(30, _player.X - 30 + rng.Next(60));
                    _hazards.Add(new FireSource(cx,                      gY - 44, 24, 44));
                    _hazards.Add(new SeaStoneZone(cx + rng.Next(50, 90), gY - 40, 80, 40));
                    ShowTelegraph("CENTIPEDE: \"Every weakness — all at once!\"");
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
            foreach (var wall in _iceWalls)
            {
                if (!wall.IsAlive) continue;
                var wb = wall.Hitbox;
                if (!c.Hitbox.IntersectsWith(wb)) continue;
                c.X = c.VelocityX > 0 ? wb.Left - c.Width : wb.Right;
                c.VelocityX = 0;
            }
            c.Y += c.VelocityY * dt;
            c.IsGrounded = false;
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

        private void CheckCombat()
        {
            bool stomped = false;
            // Keep stomp interactions enabled while blinking from recent damage.
            if (_boss.IsAlive && _player.VelocityY > 0)
            {
                float pBot = _player.Y + _player.Height;
                float overlap = pBot - _boss.Y;
                if (overlap > 0 && overlap < 22 &&
                    _player.CenterX > _boss.X - 8 && _player.CenterX < _boss.X + _boss.Width + 8)
                {
                    bool wasAlive = _boss.IsAlive;
                    _boss.TakeDamage(_player.AttackDamage * 2);
                    _player.VelocityY = _player.JumpForce * 0.45f;
                    _player.IsGrounded = false;
                    Game.Instance.Audio.BeepStomp();
                    if (wasAlive && !_boss.IsAlive)
                    {
                        BountySystem.Award(_boss.ScoreValue);
                        Game.Instance.TotalBerriesCollected += 50;
                    }
                    stomped = true;
                }
            }

            var pAtk = _player.AttackHitbox;
            if (_boss.IsAlive && pAtk != Rectangle.Empty &&
                pAtk.IntersectsWith(_boss.Hitbox))
            {
                bool wasAlive = _boss.IsAlive;
                _boss.TakeDamage(_player.AttackDamage);
                if (wasAlive && !_boss.IsAlive)
                {
                    BountySystem.Award(_boss.ScoreValue);
                    Game.Instance.TotalBerriesCollected += 50;
                }
            }

            // Horizontal body contact — 10 % max-HP damage with brief invincibility.
            if (!stomped && _boss.IsAlive && !_player.IsInvincible &&
                _player.Hitbox.IntersectsWith(_boss.Hitbox))
            {
                _player.TakeDamage(_player.MaxHealth / 10);
                Game.Instance.Audio.BeepHurt();
            }

            if (_config.Type == WarlordType.CentipedeLord && _boss.IsAlive)
            {
                // Segment body collisions (centipede train).
                for (int i = 0; i < _centipedeSegments.Count; i++)
                {
                    Rectangle segRect = GetCentipedeSegmentRect(i);
                    if (!_player.IsInvincible && _player.Hitbox.IntersectsWith(segRect))
                    {
                        _player.TakeDamage(_player.MaxHealth / 10);
                        Game.Instance.Audio.BeepHurt();
                        break;
                    }
                }

                // Any segment can be struck, applying shared boss damage.
                if (pAtk != Rectangle.Empty)
                {
                    for (int i = 0; i < _centipedeSegments.Count; i++)
                    {
                        if (!pAtk.IntersectsWith(GetCentipedeSegmentRect(i))) continue;
                        bool wasAlive = _boss.IsAlive;
                        _boss.TakeDamage(_player.AttackDamage);
                        if (wasAlive && !_boss.IsAlive)
                        {
                            BountySystem.Award(_boss.ScoreValue);
                            Game.Instance.TotalBerriesCollected += 50;
                        }
                        break;
                    }
                }
            }
        }

        private void CheckOutcome()
        {
            if (!_boss.IsAlive && !_victory)
            {
                _victory      = true;
                _victoryTimer = 0f;
                Game.Instance.Audio.BeepBossDefeat();
                Game.Instance.ScreenShake.Trigger(0.9f);
                SessionStats.Instance.RecordBossDefeated();
                AchievementSystem.Grant("ach_boss_slayer");
                Game.Instance.FloatingText.Spawn(
                    $"+{_config.BountyReward} BOUNTY!", 400, 260, Color.Gold, large: true);
                Game.Instance.PlayerBounty += _config.BountyReward;
                Game.Instance.ThreatLevel   = Math.Max(0, Game.Instance.ThreatLevel - 20);
                Game.Instance.CrewBonds    += 3;
                Game.Instance.Save.SetFlag(_config.DefeatFlag);
                Game.Instance.Save.Save();
                return;
            }

            if (!_player.IsAlive)
            {
                SessionStats.Instance.RecordDeath();
                Game.Instance.Scenes.Replace(new GameOverScene(() => new WarlordBossScene(_config)));
            }

            // Phase transition at 50 % boss HP.
            if (_phase == 1 && _boss.Health <= _boss.MaxHealth / 2)
            {
                _phase            = 2;
                _phaseTransition  = true;
                _phaseTimer       = 2.5f;
                _boss.MoveSpeed  *= 1.4f;
                _boss.AttackDamage = (int)(_config.BaseDamage * 1.4f);
                Game.Instance.Audio.BeepBossIntro();
                Game.Instance.ScreenShake.Trigger(0.6f);
                ShowTelegraph(_config.TauntP2);
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
            DrawCentipedeSegments(g);
            if (_boss.IsAlive) _boss.Draw(g);
            _player.Draw(g);
            if (_player.IsAttacking)
                using (var br = new SolidBrush(Color.FromArgb(70, Color.Cyan)))
                    g.FillRectangle(br, _player.AttackHitbox);
            HudHelper.DrawBreakShockwave(g, _breakShockwaveTimer, _breakShockwaveWorldX, _breakShockwaveWorldY);

            // ── Unified HUD (single call, warlord fight) ──────────────────────
            g.ResetTransform();
            GameHUD.Draw(g, _player, W, H, _boss, _config.Name);

            if (_telegraphTimer > 0)  DrawTelegraph(g, W, H);
            if (_phaseTransition)     DrawPhaseTransition(g, W, H);
            if (_showRescue)          DrawRescuePrompt(g, W, H);
            if (_victory)             DrawVictory(g, W, H);
            DrawDevMenuButton(g);
        }

        /// <summary>
        /// Updates centipede body segments so they trail the boss head in a connected chain.
        /// </summary>
        /// <remarks>PHASE 3 - Team 5/6 crossover: Centipede boss body rig.</remarks>
        private void UpdateCentipedeSegments(float dt)
        {
            if (_config.Type != WarlordType.CentipedeLord || _centipedeSegments.Count == 0) return;

            PointF prev = new PointF(_boss.CenterX, _boss.CenterY);
            for (int i = 0; i < _centipedeSegments.Count; i++)
            {
                PointF cur = _centipedeSegments[i];
                float dx = cur.X - prev.X;
                float dy = cur.Y - prev.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                if (dist > CentipedeSegmentSpacing)
                {
                    float k = CentipedeSegmentSpacing / dist;
                    cur = new PointF(prev.X + dx * k, prev.Y + dy * k);
                }

                // Smooth follow so the chain feels alive.
                cur = new PointF(
                    cur.X + (prev.X - cur.X) * Math.Min(1f, dt * 10f),
                    cur.Y + (prev.Y - cur.Y) * Math.Min(1f, dt * 10f));

                _centipedeSegments[i] = cur;
                prev = cur;
            }
        }

        /// <summary>Returns rectangle for one centipede body segment.</summary>
        private Rectangle GetCentipedeSegmentRect(int index)
        {
            PointF p = _centipedeSegments[index];
            int size = 30;
            return new Rectangle((int)(p.X - size / 2f), (int)(p.Y - size / 2f), size, size);
        }

        /// <summary>Draws the centipede body chain for the centipede boss fight.
        /// Each segment renders a different enemy sprite so the body is a
        /// composite of all enemy models in the game.</summary>
        private void DrawCentipedeSegments(Graphics g)
        {
            if (_config.Type != WarlordType.CentipedeLord || _centipedeSegments.Count == 0 || !_boss.IsAlive) return;

            // Draw connecting chain lines between segments.
            using (var linkPen = new Pen(Color.FromArgb(180, 90, 200, 90), 6))
            {
                PointF prev = new PointF(_boss.CenterX, _boss.CenterY);
                for (int i = 0; i < _centipedeSegments.Count; i++)
                {
                    PointF s = _centipedeSegments[i];
                    g.DrawLine(linkPen, prev, s);
                    prev = s;
                }
            }

            // Draw each segment: cycle through all enemy sprites so the body
            // is a visible combination of every enemy model in the game.
            bool hasSprites = _segmentSprites != null && _segmentSprites.Length > 0;
            for (int i = 0; i < _centipedeSegments.Count; i++)
            {
                var r = GetCentipedeSegmentRect(i);

                Bitmap sprite = hasSprites ? _segmentSprites[i % _segmentSprites.Length] : null;
                if (sprite != null)
                {
                    // Draw enemy model sprite fitted to the segment rect.
                    g.DrawImage(sprite, r);
                    // Thin green outline keeps the centipede chain readable.
                    g.DrawEllipse(_centBodyPen, r);
                }
                else
                {
                    // Fallback coloured circle when a sprite file is missing.
                    g.FillEllipse(_centBodyBr, r);
                    g.DrawEllipse(_centBodyPen, r);
                }
            }
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
        }

        private void DrawBackground(Graphics g, int W, int H)
        {
            if (_bg != null) { g.DrawImage(_bg, 0, 0, W, H); return; }
            // Fallback gradient by warlord type (visible, not near-black).
            Color top, bot;
            switch (_config.Type)
            {
                case WarlordType.FireLord:      top = Color.FromArgb(60, 20, 10);  bot = Color.FromArgb(140, 50, 15);  break;
                case WarlordType.SeaStoneLord:  top = Color.FromArgb(10, 40, 70);  bot = Color.FromArgb(20, 80, 110);  break;
                case WarlordType.StormLord:     top = Color.FromArgb(30, 20, 60);  bot = Color.FromArgb(70, 40, 110);  break;
                case WarlordType.CentipedeLord: top = Color.FromArgb(15, 40, 20);  bot = Color.FromArgb(40, 90, 50);   break;
                default:                        top = Color.FromArgb(30, 20, 50);  bot = Color.FromArgb(70, 40, 100);  break;
            }
            using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, W, H), top, bot, 90f))
                g.FillRectangle(br, 0, 0, W, H);
        }

        private void DrawPlatforms(Graphics g)
        {
            // Hoist sprite lookup outside loop — avoids a dictionary hit per platform per frame
            Bitmap stoneTile = Data.SpriteManager.GetScaled("tile_stone_block.png", 18, 18);
            foreach (var p in _platforms)
            {
                // SMB3 brick tile platform base fill
                g.FillRectangle(_platBaseBr, p);

                // Tile stone sprites if available
                if (stoneTile != null)
                {
                    for (int tx = p.X; tx < p.X + p.Width; tx += 18)
                    {
                        for (int ty = p.Y; ty < p.Y + p.Height; ty += 18)
                        {
                            int dw = Math.Min(18, p.X + p.Width - tx);
                            int dh = Math.Min(18, p.Y + p.Height - ty);
                            g.DrawImage(stoneTile, tx, ty, dw, dh);
                        }
                    }
                }
                else
                {
                    // Fallback: GDI highlight/shadow/mortar
                    g.FillRectangle(_platTopBr, p.X, p.Y, p.Width, 5);
                    g.FillRectangle(_platBotBr, p.X, p.Y + p.Height - 3, p.Width, 3);
                    for (int bx = p.X; bx < p.X + p.Width; bx += 32)
                        g.DrawLine(_platMortarPen, bx, p.Y + 5, bx, p.Y + p.Height - 3);
                }
            }
        }

        private void DrawBossHUD(Graphics g, int W, int H)
        {
            // Mega Man segmented boss health bar
            const int segCount = 28, segH = 10, segGap = 1, barW = 16;
            string label = _phase == 1
                ? _config.Name.ToUpper()
                : _config.Name.ToUpper() + " — FINAL FORM";

            using (var br = new SolidBrush(Color.FromArgb(210, 10, 10, 30)))
                g.FillRectangle(br, W / 2 - 190, 6, 380, 46);
            using (var pen = new Pen(Color.FromArgb(120, _phase == 1 ? Color.OrangeRed : Color.Gold), 2))
                g.DrawRectangle(pen, W / 2 - 190, 6, 380, 46);

            using (var f = new Font("Courier New", 8, FontStyle.Bold))
            {
                var sz = g.MeasureString(label, f);
                using (var br = new SolidBrush(_phase == 1 ? Color.OrangeRed : Color.Gold))
                    g.DrawString(label, f, br, W / 2f - sz.Width / 2f, 9);
            }

            float pct = _boss.IsAlive ? (float)_boss.Health / _boss.MaxHealth : 0f;
            int filledSegs = Math.Min(segCount, (int)(pct * segCount));
            SolidBrush fillBr = _phase == 1 ? _segOrangeRedBr : _segGoldBr;
            int barStartX = W / 2 - 170;
            int barY = 24;

            for (int i = 0; i < segCount; i++)
            {
                int sx = barStartX + i * (barW + segGap);
                bool filled = i < filledSegs;
                g.FillRectangle(filled ? fillBr : _segEmptyBr, sx, barY, barW, segH);
                if (filled)
                    g.FillRectangle(_segWhiteBr, sx, barY, barW, 2);
            }
            int midSeg = barStartX + (segCount / 2) * (barW + segGap) - segGap;
            g.DrawLine(Pens.White, midSeg, barY - 2, midSeg, barY + segH + 2);
        }

        private void DrawPlayerHUD(Graphics g, int W, int H)
        {
            g.FillRectangle(Brushes.Black, 0, H - 84, W, 84);
            g.DrawLine(Pens.DimGray, 0, H - 84, W, H - 84);

            const int segCount = 18, segH = 12, segGap = 1, segW = 7;
            float hp  = (float)_player.Health / _player.MaxHealth;
            int hpFill = Math.Min(segCount, (int)(hp * segCount));

            using (var f = new Font("Courier New", 8, FontStyle.Bold))
                g.DrawString("HP", f, Brushes.White, 6, H - 78);

            for (int i = 0; i < segCount; i++)
            {
                int sx = 30 + i * (segW + segGap);
                bool filled = i < hpFill;
                SolidBrush segBr = filled ? (hp > 0.5f ? _segLimeBr : (hp > 0.25f ? _segYellowBr : _segOrangeRedBr)) : _segEmptyBr;
                g.FillRectangle(segBr, sx, H - 76, segW, segH);
                if (filled)
                    g.FillRectangle(_segWhiteBr, sx, H - 76, segW, 2);
            }

            float ice = (float)_player.IceReserve / _player.MaxIceReserve;
            int iceFill = Math.Min(14, (int)(ice * 14));
            using (var f = new Font("Courier New", 8, FontStyle.Bold))
                g.DrawString("ICE", f, Brushes.Cyan, 6, H - 60);
            for (int i = 0; i < 14; i++)
            {
                int sx = 30 + i * (segW + segGap);
                bool filled = i < iceFill;
                g.FillRectangle(filled ? _segIceBr : _segEmptyBr, sx, H - 58, segW, 10);
                if (filled)
                    g.FillRectangle(_segWhiteBr, sx, H - 58, segW, 2);
            }

            HudHelper.DrawAbilityBar(g, _player, 180, H - 82);
            HudHelper.DrawStatusTags(g, _player, 470, H - 44);
            HudHelper.DrawBountyAndBerries(g, W - 260, H - 78);
        }

        private void DrawTelegraph(Graphics g, int W, int H)
        {
            float alpha = Math.Min(1f, _telegraphTimer / TelegraphDur);
            using (var br = new SolidBrush(Color.FromArgb((int)(160 * alpha), 0, 0, 0)))
                g.FillRectangle(br, 0, H * 0.55f, W, 44);
            // Use fully-qualified FontStyle to avoid any namespace-shadow compile error.
            using (var f = new Font("Courier New", 10, System.Drawing.FontStyle.Italic))
            {
                SizeF sz = g.MeasureString(_telegraph ?? "", f);
                using (var br = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Yellow)))
                    g.DrawString(_telegraph ?? "", f, br, (W - sz.Width) / 2f, H * 0.55f + 12);
            }
        }

        private void DrawPhaseTransition(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(120, Color.DarkRed)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("— FINAL FORM —", f);
                g.DrawString("— FINAL FORM —", f, Brushes.Gold, (W - sz.Width) / 2f, H * 0.42f);
            }
        }

        private void DrawRescuePrompt(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, Color.DarkBlue)))
                g.FillRectangle(br, W / 2 - 180, H / 2 - 28, 360, 56);
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
                g.DrawString("Mash  SPACE / Z / X  to survive!",
                             f, Brushes.Cyan, W / 2 - 150f, H / 2 - 10f);
        }

        private void DrawVictory(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);

            int cx = W / 2, cy = H / 2;
            using (var br = new SolidBrush(Color.FromArgb(220, 20, 12, 40)))
                g.FillRectangle(br, cx - 240, cy - 90, 480, 180);
            using (var pen = new Pen(Color.Gold, 3))
                g.DrawRectangle(pen, cx - 240, cy - 90, 480, 180);

            // Gold star corner decorations (SMB3 card style).
            foreach (var pt in new[] {
                new Point(cx - 230, cy - 80), new Point(cx + 210, cy - 80),
                new Point(cx - 230, cy + 70), new Point(cx + 210, cy + 70) })
                g.DrawString("★", new Font("Courier New", 9), Brushes.Gold, pt);

            using (var f = new Font("Courier New", 18, FontStyle.Bold))
            {
                string title = _config.Name.ToUpper() + " DEFEATED!";
                SizeF sz = g.MeasureString(title, f);
                g.DrawString(title, f, Brushes.Gold, cx - sz.Width / 2f, cy - 78);
            }
            using (var f = new Font("Courier New", 11))
            {
                g.DrawString($"+{_config.BountyReward} Bounty    +3 Crew Bonds    Threat -20%",
                             f, Brushes.White, cx - 190f, cy - 28f);
                g.DrawString("Returning to overworld...",
                             f, Brushes.LightGray, cx - 130f, cy + 4f);
            }

            float pct = Math.Min(1f, _victoryTimer / 2f);
            using (var br = new SolidBrush(Color.FromArgb(60, Color.Gold)))
                g.FillRectangle(br, cx - 220, cy + 50, (int)(440 * pct), 14);
            using (var pen = new Pen(Color.FromArgb(100, Color.Gold)))
                g.DrawRectangle(pen, cx - 220, cy + 50, 440, 14);
        }
    }
}
