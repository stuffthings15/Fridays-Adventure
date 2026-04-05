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
    public sealed class BossScene : Scene
    {
        // ── State ────────────────────────────────────────────────────────────
        private Player  _player;
        private Enemy   _boss;
        private Bitmap  _bg;
        private Bitmap  _bossSprite;

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

        // Break shockwave
        private float _breakShockwaveTimer;
        private float _breakShockwaveWorldX;
        private float _breakShockwaveWorldY;

        // Outcome
        private bool  _victory;
        private float _victoryTimer;

        private static readonly Font _hudFont  = new Font("Courier New", 9, FontStyle.Bold);
        private static readonly Font _bigFont  = new Font("Courier New", 18, FontStyle.Bold);
        private static readonly Font _midFont  = new Font("Courier New", 12, FontStyle.Bold);

        // ── Enter / Exit ─────────────────────────────────────────────────────

        public override void OnEnter()
        {
            _bossSprite   = SpriteManager.GetScaled("boss_Garp.png", 160, 220);

            // Background — bg_Marine_Blockade.png lives in Assets\Sprites\
            _bg = SpriteManager.Get("bg_Marine_Blockade.png");
            
            Build();
            Game.Instance.Audio.ContinueOrPlay("boss");
        }

        public override void OnExit() { }

        public override void OnResume() => Game.Instance.Audio.ContinueOrPlay("boss");

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
            _player.ApplySelectedSprite();

            _boss = new Enemy(W - 200, g - 220, 160, 220, 200,
                              patrolLeft: W * 0.25f, patrolRight: W * 0.80f);
            _boss.EnemyType    = "Boss";
            _boss.MoveSpeed    = 130f;
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
            MoveAndCollide(_boss, dt, bossMode: true);

            UpdateStonesTimer(dt);
            UpdateTelegraph(dt);
            HudHelper.UpdateBreakShockwaveTimer(ref _breakShockwaveTimer, dt);
            CheckCombat();
            CheckOutcome();
        }

        private void HandleInput(float dt)
        {
            var input = Game.Instance.Input;

            if (!_player.HasEffect(StatusEffect.Sinking) &&
                !_player.HasEffect(StatusEffect.Stunned))
            {
                if (input.LeftHeld)       { _player.VelocityX = -_player.MoveSpeed; _player.FacingRight = false; }
                else if (input.RightHeld) { _player.VelocityX =  _player.MoveSpeed; _player.FacingRight = true; }
                else _player.VelocityX = 0;

                if (input.JumpPressed && _player.JumpsRemaining > 0)
                {
                    _player.VelocityY  = _player.JumpForce;
                    _player.IsGrounded = false;
                    _player.JumpsRemaining--;
                    Game.Instance.Audio.BeepJump();
                }
                // Variable jump height — release early for short hop (SMB3-style)
                if (!input.JumpHeld && _player.VelocityY < -120f)
                    _player.VelocityY = -120f;
                if (input.DodgePressed)  _player.TryDodge();
                if (input.AttackPressed && _player.TryAttack())
                    Game.Instance.Audio.BeepAttack();
            }

            if (input.Ability1Pressed && _player.UseIceWall(out IceWallInstance wall))
            {
                _iceWalls.Add(wall);
                Game.Instance.Audio.BeepIce();
            }
            if (input.Ability2Pressed && _player.UseFlashFreeze())
            {
                if (_player.DistanceTo(_boss) <= 130f)
                    _boss.ApplyEffect(StatusEffect.Frozen, _player.GetFlashFreezeDuration(2f));
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
                    _boss.TakeDamage(_player.BreakWallShockwaveDamage);
                Game.Instance.Audio.BeepBreak();
                _breakShockwaveTimer  = 0.001f;
                _breakShockwaveWorldX = _player.CenterX;
                _breakShockwaveWorldY = _player.CenterY;
            }
            if (input.PausePressed)
                Game.Instance.Scenes.Push(new PauseScene());
            // I key — quick-open inventory during boss fight
            if (input.InventoryPressed)
                Game.Instance.Scenes.Push(new InventoryScene(_player));
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
                // SMB3-style boss defeat SFX.
                Game.Instance.Audio.BeepBossDefeat();
                Game.Instance.ScreenShake.Trigger(0.9f);
                SessionStats.Instance.RecordBossDefeated();
                AchievementSystem.Grant("ach_boss_slayer");
                Game.Instance.FloatingText.Spawn("+2000 BOUNTY!", 400, 260, Color.Gold, large: true);
                Game.Instance.PlayerBounty += 2000;
                Game.Instance.ThreatLevel   = Math.Max(0, Game.Instance.ThreatLevel - 20);
                Game.Instance.CrewBonds    += 2;
                Game.Instance.Save.SetFlag("marine_captain_defeated");
                Game.Instance.Save.Save();
                return;
            }
            if (!_player.IsAlive)
                Game.Instance.Scenes.Replace(new GameOverScene(() => new BossScene()));

            // Phase transition at 50 % boss HP
            if (_phase == 1 && _boss.Health <= _boss.MaxHealth / 2)
            {
                _phase            = 2;
                _phaseTransition  = true;
                _phaseTimer       = 2.5f;
                _boss.MoveSpeed  *= 1.4f;
                _boss.AttackDamage = 24;
                // Phase 2 boss intro SFX.
                Game.Instance.Audio.BeepBossIntro();
                Game.Instance.ScreenShake.Trigger(0.6f);
                ShowTelegraph("CAPTAIN: \"Enough games — Absolute Justice, FINAL FORM!\"");
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
                Game.Instance.Scenes.Replace(new VictoryScene(
                    "CAPTAIN DEFEATED!",
                    "+2000 Bounty   +2 Crew Bonds   Threat -20%",
                    () =>
                    {
                        // Signal level cleared so OverworldScene increments CurrentLevel
                        Game.Instance.LevelJustCompleted = true;
                        Game.Instance.Scenes.Pop();
                    }));
        }

        // ── Collision & Combat ────────────────────────────────────────────────

        private void MoveAndCollide(Character c, float dt, bool bossMode = false)
        {
            c.X += c.VelocityX * dt;
            c.X  = Math.Max(0, Math.Min(Game.Instance.CanvasWidth - c.Width, c.X));
            Resolve(c, horizontal: true,  bossMode: bossMode);
            c.Y += c.VelocityY * dt;
            c.IsGrounded = false;
            Resolve(c, horizontal: false, bossMode: bossMode);
        }

        private void Resolve(Character c, bool horizontal, bool bossMode = false)
        {
            // bossMode: boss is 220px tall and would intersect elevated platforms;
            // restrict it to the main floor (index 0) so it can move freely.
            int count = bossMode ? 1 : _platforms.Count;
            for (int i = 0; i < count; i++)
            {
                var p = _platforms[i];
                if (!c.Hitbox.IntersectsWith(p)) continue;
                if (horizontal)
                {
                    if      (c.VelocityX > 0) c.X = p.Left - c.Width;
                    else if (c.VelocityX < 0) c.X = p.Right;
                    c.VelocityX = 0;
                }
                else if (c.VelocityY >= 0)
                { c.Y = p.Top - c.Height; c.VelocityY = 0; c.IsGrounded = true; }
                else
                { c.Y = p.Bottom; c.VelocityY = 0; }
            }
            foreach (var wall in _iceWalls)
            {
                if (!wall.IsAlive) continue;
                var wb = wall.Hitbox;
                if (!c.Hitbox.IntersectsWith(wb)) continue;
                if (horizontal)
                {
                    if      (c.VelocityX > 0) c.X = wb.Left - c.Width;
                    else if (c.VelocityX < 0) c.X = wb.Right;
                    c.VelocityX = 0;
                }
                else if (c.VelocityY >= 0)
                { c.Y = wb.Top - c.Height; c.VelocityY = 0; c.IsGrounded = true; }
                else
                { c.Y = wb.Bottom; c.VelocityY = 0; }
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
            // ── Head stomp on boss ─────────────────────────────────────────────
            // The player stomps the boss by landing on its head while falling.
            // We test the top 35 % of the boss body so fast falls are still caught.
            // The stomped flag suppresses body-contact damage in the same frame and
            // the VelocityY >= 0 guard prevents damage while the player bounces away.
            bool stomped = false;
            // Stomp should still work while player is blinking from i-frames.
            if (_boss.IsAlive && _player.VelocityY > 0)
            {
                float pBot   = _player.Y + _player.Height;
                float headZone = _boss.Y + _boss.Height * 0.35f;   // top 35 % of boss
                if (pBot > _boss.Y && pBot < headZone &&
                    _player.CenterX > _boss.X - 8 && _player.CenterX < _boss.X + _boss.Width + 8)
                {
                    bool wasAlive = _boss.IsAlive;
                    _boss.TakeDamage(_player.AttackDamage * 2);
                    _player.VelocityY  = _player.JumpForce * 0.45f;
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

            // ── Horizontal body contact — 10 % max HP damage ──────────────────
            // Only fires when the player is falling/stationary (VelocityY >= 0) so
            // the bounce after a stomp never triggers this check.
            if (!stomped && _boss.IsAlive && !_player.IsInvincible &&
                _player.VelocityY >= 0 &&
                _player.Hitbox.IntersectsWith(_boss.Hitbox))
            {
                _player.TakeDamage(_player.MaxHealth / 10);
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
            HudHelper.DrawBreakShockwave(g, _breakShockwaveTimer, _breakShockwaveWorldX, _breakShockwaveWorldY);

            // ── Unified HUD (single call, boss fight) ─────────────────────────
            g.ResetTransform();
            GameHUD.Draw(g, _player, W, H, _boss, "MARINE CAPTAIN");

            if (_telegraphTimer > 0) DrawTelegraph(g, W, H);
            if (_phaseTransition)    DrawPhaseTransition(g, W, H);
            if (_showRescue)         DrawRescuePrompt(g, W, H);
            if (_victory)            DrawVictory(g, W, H);
            DrawDevMenuButton(g);
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
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
                // ── SMB3 brick tile platform ──────────────────────────────────
                using (var br = new SolidBrush(Color.FromArgb(160, 100, 50)))
                    g.FillRectangle(br, p);
                // Lit top edge.
                using (var br = new SolidBrush(Color.FromArgb(200, 130, 70)))
                    g.FillRectangle(br, p.X, p.Y, p.Width, 5);
                // Dark bottom shadow.
                using (var br = new SolidBrush(Color.FromArgb(110, 60, 28)))
                    g.FillRectangle(br, p.X, p.Y + p.Height - 3, p.Width, 3);
                // Brick mortar lines every 32px.
                using (var pen = new Pen(Color.FromArgb(100, 70, 30), 1))
                    for (int bx = p.X; bx < p.X + p.Width; bx += 32)
                        g.DrawLine(pen, bx, p.Y + 5, bx, p.Y + p.Height - 3);
            }
        }

        private void DrawBossHUD(Graphics g, int W, int H)
        {
            // ── Boss health bar — Mega Man segmented style ────────────────────
            const int segCount = 28, segH = 10, segGap = 1, barW = 16;
            string label = _phase == 1 ? "MARINE CAPTAIN" : "MARINE CAPTAIN ─ FINAL FORM";

            // Panel background.
            using (var br = new SolidBrush(Color.FromArgb(210, 10, 10, 30)))
                g.FillRectangle(br, W / 2 - 190, 6, 380, 46);
            using (var pen = new Pen(Color.FromArgb(120, _phase == 1 ? Color.OrangeRed : Color.Gold), 2))
                g.DrawRectangle(pen, W / 2 - 190, 6, 380, 46);

            // Label.
            using (var f = new Font("Courier New", 8, FontStyle.Bold))
            {
                var sz = g.MeasureString(label, f);
                using (var br = new SolidBrush(_phase == 1 ? Color.OrangeRed : Color.Gold))
                    g.DrawString(label, f, br, W / 2f - sz.Width / 2f, 9);
            }

            // Segmented HP bar — fills left to right.
            float pct = _boss.IsAlive ? (float)_boss.Health / _boss.MaxHealth : 0f;
            int filledSegs = Math.Min(segCount, (int)(pct * segCount));
            Color fillColor = _phase == 1 ? Color.OrangeRed : Color.Gold;
            int barStartX = W / 2 - 170;
            int barY = 24;

            for (int i = 0; i < segCount; i++)
            {
                int sx = barStartX + i * (barW + segGap);
                bool filled = i < filledSegs;
                using (var br = new SolidBrush(filled ? fillColor : Color.FromArgb(28, 38, 52)))
                    g.FillRectangle(br, sx, barY, barW, segH);
                if (filled)
                    using (var br = new SolidBrush(Color.FromArgb(80, Color.White)))
                        g.FillRectangle(br, sx, barY, barW, 2);
            }
            // Phase halfway marker.
            int midSeg = barStartX + (segCount / 2) * (barW + segGap) - segGap;
            g.DrawLine(Pens.White, midSeg, barY - 2, midSeg, barY + segH + 2);
        }

        private void DrawPlayerHUD(Graphics g, int W, int H)
        {
            // ── Bottom bar panel ─────────────────────────────────────────────
            g.FillRectangle(Brushes.Black, 0, H - 84, W, 84);
            g.DrawLine(Pens.DimGray, 0, H - 84, W, H - 84);

            // ── HP — segmented (18 segments) ─────────────────────────────────
            const int segCount = 18, segH = 12, segGap = 1, segW = 7;
            float hp  = (float)_player.Health / _player.MaxHealth;
            int hpFill = Math.Min(segCount, (int)(hp * segCount));

            using (var f = new Font("Courier New", 8, FontStyle.Bold))
                g.DrawString("HP", f, Brushes.White, 6, H - 78);

            for (int i = 0; i < segCount; i++)
            {
                int sx = 30 + i * (segW + segGap);
                bool filled = i < hpFill;
                Color segColor = hp > 0.5f ? Color.LimeGreen : (hp > 0.25f ? Color.Yellow : Color.OrangeRed);
                using (var br = new SolidBrush(filled ? segColor : Color.FromArgb(28, 38, 52)))
                    g.FillRectangle(br, sx, H - 76, segW, segH);
                if (filled)
                    using (var br = new SolidBrush(Color.FromArgb(80, Color.White)))
                        g.FillRectangle(br, sx, H - 76, segW, 2);
            }

            // ── ICE — segmented (14 segments) ────────────────────────────────
            float ice = (float)_player.IceReserve / _player.MaxIceReserve;
            int iceFill = Math.Min(14, (int)(ice * 14));

            using (var f = new Font("Courier New", 8, FontStyle.Bold))
                g.DrawString("ICE", f, Brushes.Cyan, 6, H - 60);

            for (int i = 0; i < 14; i++)
            {
                int sx = 30 + i * (segW + segGap);
                bool filled = i < iceFill;
                using (var br = new SolidBrush(filled ? Color.FromArgb(180, 220, 255) : Color.FromArgb(28, 38, 52)))
                    g.FillRectangle(br, sx, H - 58, segW, 10);
                if (filled)
                    using (var br = new SolidBrush(Color.FromArgb(80, Color.White)))
                        g.FillRectangle(br, sx, H - 58, segW, 2);
            }

            // Ability cooldowns.
            HudHelper.DrawAbilityBar(g, _player, 180, H - 82);

            // Status tags.
            HudHelper.DrawStatusTags(g, _player, 470, H - 44);

            // Bounty + berries.
            HudHelper.DrawBountyAndBerries(g, W - 260, H - 78);
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
            // ── Semi-transparent overlay ──────────────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);

            // ── SMB3-style reward card panel ──────────────────────────────────
            int cx = W / 2, cy = H / 2;
            using (var br = new SolidBrush(Color.FromArgb(220, 20, 12, 40)))
                g.FillRectangle(br, cx - 240, cy - 90, 480, 180);
            using (var pen = new Pen(Color.Gold, 3))
                g.DrawRectangle(pen, cx - 240, cy - 90, 480, 180);

            // Gold star decorations on corners (SMB3 star motif).
            foreach (var pt in new[] {
                new Point(cx - 230, cy - 80), new Point(cx + 210, cy - 80),
                new Point(cx - 230, cy + 70), new Point(cx + 210, cy + 70) })
            {
                using (var br = new SolidBrush(Color.Gold))
                    g.DrawString("★", new Font("Courier New", 9), br, pt);
            }

            // Victory text.
            using (var f = new Font("Courier New", 22, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("CAPTAIN DEFEATED!", f);
                g.DrawString("CAPTAIN DEFEATED!", f, Brushes.Gold, cx - sz.Width / 2f, cy - 78);
            }

            // Rewards breakdown.
            using (var f = new Font("Courier New", 11))
            {
                g.DrawString("+2000 Bounty    +2 Crew Bonds    Threat -20%",
                             f, Brushes.White, cx - 190f, cy - 28f);
                g.DrawString("Returning to overworld...",
                             f, Brushes.LightGray, cx - 130f, cy + 4f);
            }

            // Animated score tally bar.
            float pct = Math.Min(1f, _victoryTimer / 2f);
            using (var br = new SolidBrush(Color.FromArgb(60, Color.Gold)))
                g.FillRectangle(br, cx - 220, cy + 50, (int)(440 * pct), 14);
            using (var pen = new Pen(Color.FromArgb(100, Color.Gold)))
                g.DrawRectangle(pen, cx - 220, cy + 50, 440, 14);
        }
    }
}
