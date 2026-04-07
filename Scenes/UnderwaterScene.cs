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
    //  UnderwaterScene.cs  —  Level Designer: Idea 8 (full underwater level)
    //
    //  Idea 8: Underwater level — buoyancy physics, coral hazards, air bubble
    //          oxygen timer, jellyfish enemies, upward current zones.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Full underwater level with buoyancy physics, an oxygen timer,
    /// coral reef platforms, jellyfish enemies, and upward current zones.
    /// Team 5 (Level Designer) — Idea 8.
    /// </summary>
    public sealed class UnderwaterScene : Scene
    {
        private const float LevelScale = 1.5f;
        private Player _player;
        private List<Rectangle>  _platforms   = new List<Rectangle>();
        private List<Rectangle>  _coralHazards = new List<Rectangle>();
        private List<JellyFish>  _jellies     = new List<JellyFish>();
        private List<Rectangle>  _currents    = new List<Rectangle>();  // upward current zones
        private readonly List<Abilities.IceWallInstance> _iceWalls = new List<Abilities.IceWallInstance>();
        private float _jellyFreezeTimer;

        // ── Oxygen timer ──────────────────────────────────────────────────────
        private float _oxygenTimer    = 30f;
        private const float MaxOxygen = 30f;

        // ── Air bubbles (refill stations) ─────────────────────────────────────
        private List<Rectangle> _bubbleStations = new List<Rectangle>();

        // ── Level state ───────────────────────────────────────────────────────
        private bool  _levelComplete;
        private float _completeTimer;
        private Rectangle _exitZone;

        // ── Jelly fish ────────────────────────────────────────────────────────
        private struct JellyFish
        {
            public float X, Y, VY, Range, BaseY;
        }

        private static readonly Font _hud = new Font("Courier New", 9, FontStyle.Bold);

        // ── Background ────────────────────────────────────────────────────────
        private Bitmap _bg;

        public override void OnEnter()
        {
            DebugLogger.PushBreadcrumb("UnderwaterScene.OnEnter");

            // Load underwater background — all files in Assets\Sprites\ with bg_ prefix
            string bgFileName;
            switch (Game.Instance.Save.CurrentNodeId)
            {
                case "dive_gate":   bgFileName = "bg_Dive_Gate.png";   break;
                case "sunken_gate": bgFileName = "bg_Sunken_Gate.png"; break;
                case "kelp":        bgFileName = "bg_Kelp_Maze.png";   break;
                case "boiling_vent":bgFileName = "bg_Vent_Ruins.png";  break;
                case "abyss":       bgFileName = "bg_Abyss.png";       break;
                default:            bgFileName = "bg_Dive_Gate.png";   break;
            }
            _bg = SpriteManager.Get(bgFileName);
            
            BuildLevel();
            Game.Instance.Audio.ContinueOrPlay("exploration");
            SMB3Hud.TriggerGetReady();
            SMB3Hud.ShowWorldLabel($"DEEP SEA  {Game.Instance.WorldLevelLabel}");
        }

        public override void OnExit()  { _bg?.Dispose(); _bg = null; }
        public override void OnResume() => Game.Instance.Audio.ContinueOrPlay("exploration");

        /// <summary>
        /// Builds the underwater level layout.  Each of the 6 underwater
        /// levels gets a unique arrangement of platforms, hazards, currents,
        /// jellyfish, and exit position for gameplay variety.
        /// </summary>
        /// <remarks>PHASE 2 - Session 112: per-level layout variety</remarks>
        private void BuildLevel()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Player archetype is resolved automatically from Game.Instance.SelectedCharacter
            // inside the Player(x, y) constructor — no need to pass extra arguments.
            // MoveSpeed is reduced to simulate underwater movement resistance.
            _player = new Player(80, H / 2f - 20);
            _player.MoveSpeed = 100f;
            _iceWalls.Clear();
            _jellyFreezeTimer = 0f;

            // Shared floor — every underwater level has a seabed
            _platforms.Add(new Rectangle(0, H - 40, W, 40));

            // Per-level layouts give each underwater stage a distinct feel
            string nodeId = Game.Instance.Save.CurrentNodeId ?? "coral";
            switch (nodeId)
            {
                case "dive_gate":
                    // Dive Gate — wide coral shelves, strong upward current, few jellyfish
                    _platforms.Add(new Rectangle(60,  H - 140, 200, 20));
                    _platforms.Add(new Rectangle(340, H - 200, 180, 20));
                    _platforms.Add(new Rectangle(620, H - 260, 160, 20));
                    _coralHazards.Add(new Rectangle(250, H - 70, 30, 30));
                    _currents.Add(new Rectangle(450, H - 300, 60, 220));
                    _bubbleStations.Add(new Rectangle(200, H - 170, 20, 20));
                    _bubbleStations.Add(new Rectangle(650, H - 290, 20, 20));
                    AddJelly(400, H - 180, 0.5f, 70f);
                    AddJelly(720, H - 200, 0.6f, 50f);
                    _exitZone = new Rectangle(W - 100, H - 320, 80, 80);
                    break;

                case "sunken_gate":
                    // Sunken Gate — maze-like shelves, more coral hazards
                    _platforms.Add(new Rectangle(100, H - 120, 140, 20));
                    _platforms.Add(new Rectangle(300, H - 200, 100, 20));
                    _platforms.Add(new Rectangle(200, H - 300, 160, 20));
                    _platforms.Add(new Rectangle(500, H - 160, 120, 20));
                    _platforms.Add(new Rectangle(700, H - 250, 110, 20));
                    _coralHazards.Add(new Rectangle(150, H - 70,  28, 30));
                    _coralHazards.Add(new Rectangle(450, H - 70,  32, 30));
                    _coralHazards.Add(new Rectangle(680, H - 70,  26, 30));
                    _coralHazards.Add(new Rectangle(350, H - 170, 25, 25));
                    _currents.Add(new Rectangle(600, H - 350, 50, 250));
                    _bubbleStations.Add(new Rectangle(250, H - 230, 20, 20));
                    _bubbleStations.Add(new Rectangle(550, H - 190, 20, 20));
                    AddJelly(180, H - 200, 0.7f, 60f);
                    AddJelly(520, H - 250, 0.5f, 80f);
                    AddJelly(750, H - 160, 0.6f, 40f);
                    _exitZone = new Rectangle(W - 110, H - 380, 80, 80);
                    break;

                case "kelp":
                    // Kelp Maze — many narrow platforms like kelp forest, upward currents
                    _platforms.Add(new Rectangle(80,  H - 100, 80, 20));
                    _platforms.Add(new Rectangle(200, H - 160, 80, 20));
                    _platforms.Add(new Rectangle(340, H - 220, 80, 20));
                    _platforms.Add(new Rectangle(460, H - 280, 80, 20));
                    _platforms.Add(new Rectangle(580, H - 200, 80, 20));
                    _platforms.Add(new Rectangle(700, H - 320, 100, 20));
                    _platforms.Add(new Rectangle(820, H - 250, 60, 20));
                    _coralHazards.Add(new Rectangle(300, H - 70, 25, 30));
                    _coralHazards.Add(new Rectangle(550, H - 70, 28, 30));
                    _currents.Add(new Rectangle(150, H - 280, 40, 200));
                    _currents.Add(new Rectangle(500, H - 350, 40, 200));
                    _currents.Add(new Rectangle(750, H - 400, 40, 250));
                    _bubbleStations.Add(new Rectangle(380, H - 250, 20, 20));
                    _bubbleStations.Add(new Rectangle(720, H - 350, 20, 20));
                    AddJelly(260, H - 200, 0.5f, 50f);
                    AddJelly(620, H - 260, 0.6f, 70f);
                    _exitZone = new Rectangle(W - 90, H - 400, 80, 80);
                    break;

                case "boiling_vent":
                    // Vent Ruins — hazardous floor, strong upward currents from vents
                    _platforms.Add(new Rectangle(120, H - 150, 160, 20));
                    _platforms.Add(new Rectangle(400, H - 200, 140, 20));
                    _platforms.Add(new Rectangle(650, H - 280, 130, 20));
                    _coralHazards.Add(new Rectangle(100, H - 70,  35, 30));
                    _coralHazards.Add(new Rectangle(300, H - 70,  35, 30));
                    _coralHazards.Add(new Rectangle(500, H - 70,  35, 30));
                    _coralHazards.Add(new Rectangle(700, H - 70,  35, 30));
                    // Multiple vent currents (the "boiling" part)
                    _currents.Add(new Rectangle(200, H - 320, 60, 250));
                    _currents.Add(new Rectangle(480, H - 350, 60, 280));
                    _currents.Add(new Rectangle(760, H - 380, 60, 300));
                    _bubbleStations.Add(new Rectangle(350, H - 230, 20, 20));
                    _bubbleStations.Add(new Rectangle(600, H - 310, 20, 20));
                    AddJelly(250, H - 200, 0.8f, 90f);
                    AddJelly(550, H - 280, 0.7f, 60f);
                    AddJelly(800, H - 180, 0.6f, 50f);
                    _exitZone = new Rectangle(W - 100, H - 420, 80, 80);
                    break;

                case "abyss":
                    // Abyss — deep, dark, exit at the very top, lots of jellyfish
                    _platforms.Add(new Rectangle(60,  H - 100, 120, 20));
                    _platforms.Add(new Rectangle(250, H - 180, 100, 20));
                    _platforms.Add(new Rectangle(450, H - 260, 120, 20));
                    _platforms.Add(new Rectangle(650, H - 340, 100, 20));
                    _platforms.Add(new Rectangle(350, H - 400, 140, 20));
                    _coralHazards.Add(new Rectangle(180, H - 70, 30, 30));
                    _coralHazards.Add(new Rectangle(550, H - 70, 30, 30));
                    _currents.Add(new Rectangle(300, H - 350, 50, 280));
                    _currents.Add(new Rectangle(700, H - 420, 50, 330));
                    _bubbleStations.Add(new Rectangle(100, H - 130, 20, 20));
                    _bubbleStations.Add(new Rectangle(500, H - 290, 20, 20));
                    _bubbleStations.Add(new Rectangle(700, H - 370, 20, 20));
                    AddJelly(150, H - 160, 0.6f, 60f);
                    AddJelly(380, H - 220, 0.5f, 80f);
                    AddJelly(600, H - 300, 0.7f, 70f);
                    AddJelly(750, H - 180, 0.8f, 50f);
                    _exitZone = new Rectangle(W / 2 - 40, 40, 80, 80);
                    break;

                default: // "coral" and any other
                    // Coral Reef — the original default layout
                    _platforms.Add(new Rectangle(80,  H - 120, 140, 20));
                    _platforms.Add(new Rectangle(280, H - 180, 120, 20));
                    _platforms.Add(new Rectangle(480, H - 240, 100, 20));
                    _platforms.Add(new Rectangle(660, H - 180, 140, 20));
                    _platforms.Add(new Rectangle(820, H - 300, 100, 20));
                    _coralHazards.Add(new Rectangle(200, H - 70,  30, 30));
                    _coralHazards.Add(new Rectangle(420, H - 70,  25, 30));
                    _coralHazards.Add(new Rectangle(600, H - 70,  28, 30));
                    _currents.Add(new Rectangle(350, H - 250, 50, 180));
                    _currents.Add(new Rectangle(700, H - 350, 50, 260));
                    _bubbleStations.Add(new Rectangle(300, H - 160, 20, 20));
                    _bubbleStations.Add(new Rectangle(600, H - 260, 20, 20));
                    AddJelly(160, H - 180, 0.6f, 60f);
                    AddJelly(500, H - 250, 0.5f, 80f);
                    AddJelly(780, H - 200, 0.7f, 50f);
                    _exitZone = new Rectangle(W - 100, H - 360, 80, 80);
                    break;
            }

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
            for (int i = 0; i < _coralHazards.Count; i++) _coralHazards[i] = ScaleRect(_coralHazards[i]);
            for (int i = 0; i < _currents.Count; i++) _currents[i] = ScaleRect(_currents[i]);
            for (int i = 0; i < _bubbleStations.Count; i++) _bubbleStations[i] = ScaleRect(_bubbleStations[i]);
            _exitZone = ScaleRect(_exitZone);

            for (int i = 0; i < _jellies.Count; i++)
            {
                var j = _jellies[i];
                j.X *= LevelScale; j.Y *= LevelScale; j.BaseY *= LevelScale; j.Range *= LevelScale;
                _jellies[i] = j;
            }

            _player.X *= LevelScale;
            _player.Y *= LevelScale;
            _player.Width = (int)(_player.Width * LevelScale);
            _player.Height = (int)(_player.Height * LevelScale);
            _player.ApplySelectedSprite();
        }
        private void AddJelly(float x, float y, float speed, float range)
        {
            _jellies.Add(new JellyFish { X = x, Y = y, VY = speed, Range = range, BaseY = y });
        }

        public override void Update(float dt)
        {
            if (_levelComplete) { _completeTimer -= dt; if (_completeTimer <= 0f) CompleteLevel(); return; }

            var input = Game.Instance.Input;
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Buoyancy movement (all 8 directions underwater) ───────────────
            bool left  = input.IsHeld(System.Windows.Forms.Keys.Left)  || input.IsHeld(System.Windows.Forms.Keys.A);
            bool right = input.IsHeld(System.Windows.Forms.Keys.Right) || input.IsHeld(System.Windows.Forms.Keys.D);
            bool up    = input.IsHeld(System.Windows.Forms.Keys.Up)    || input.IsHeld(System.Windows.Forms.Keys.W) ||
                         input.IsHeld(System.Windows.Forms.Keys.Space);
            bool down  = input.IsHeld(System.Windows.Forms.Keys.Down)  || input.IsHeld(System.Windows.Forms.Keys.S);

            const float swimSpeed = 110f;
            _player.VelocityX = left ? -swimSpeed : right ? swimSpeed : 0f;
            _player.VelocityY = up   ? -swimSpeed : down  ? swimSpeed : 0f;

            // Character abilities (Q/E/R) in underwater map.
            if (input.Ability1Pressed && _player.UseIceWall(out Abilities.IceWallInstance wall))
            {
                _iceWalls.Add(wall);
                Game.Instance.Audio.BeepIce();
            }
            if (input.Ability2Pressed && _player.UseFlashFreeze())
            {
                _jellyFreezeTimer = _player.GetFlashFreezeDuration(2.2f);
                Game.Instance.Audio.BeepFreeze();
            }
            if (input.Ability3Pressed && _player.UseBreakWall())
            {
                BreakNearbyObjects();
                Game.Instance.Audio.BeepBreak();
            }

            // Upward current zones.
            var pr = new Rectangle((int)_player.X, (int)_player.Y, _player.Width, _player.Height);
            foreach (var cur in _currents)
                if (cur.IntersectsWith(pr)) _player.VelocityY -= 80f;  // push up

            _player.X += _player.VelocityX * dt;
            _player.Y += _player.VelocityY * dt;
            _player.X = Math.Max(0, Math.Min(W - _player.Width, _player.X));
            _player.Y = Math.Max(0, Math.Min(H - _player.Height, _player.Y));
            _player.IsGrounded = false;

            // Tick player internals (status effects, blink timer, ability cooldowns,
            // auto-health, animation). Without this call the player entity is inert.
            _player.Update(dt);

            // ── Platform collisions ───────────────────────────────────────────
            foreach (var p in _platforms) ResolvePlayerPlatform(p);
            foreach (var iceWall in _iceWalls)
                if (iceWall.IsAlive) ResolvePlayerPlatform(iceWall.Hitbox);

            // Update temporary ice walls.
            for (int i = _iceWalls.Count - 1; i >= 0; i--)
            {
                _iceWalls[i].Update(dt, nearFire: false);
                if (!_iceWalls[i].IsAlive) _iceWalls.RemoveAt(i);
            }

            if (_jellyFreezeTimer > 0f) _jellyFreezeTimer -= dt;

            // ── Oxygen countdown ──────────────────────────────────────────────
            _oxygenTimer -= dt;
            if (_oxygenTimer <= 0f)
            {
                _oxygenTimer = 0f;
                _player.TakeDamage(99);  // drown
            }

            // ── Bubble stations (refill oxygen) ──────────────────────────────
            foreach (var b in _bubbleStations)
                if (b.IntersectsWith(pr)) { _oxygenTimer = MaxOxygen; }

            // ── Coral hazards ─────────────────────────────────────────────────
            foreach (var coral in _coralHazards)
                if (coral.IntersectsWith(pr)) _player.TakeDamage(1);

            // ── Jellyfish update ──────────────────────────────────────────────
            for (int i = 0; i < _jellies.Count; i++)
            {
                var j = _jellies[i];
                if (_jellyFreezeTimer <= 0f)
                {
                    j.Y += j.VY * dt;
                    if (j.Y > j.BaseY + j.Range)  j.VY = -Math.Abs(j.VY);
                    if (j.Y < j.BaseY - j.Range)  j.VY =  Math.Abs(j.VY);
                }
                _jellies[i] = j;

                // Frozen jellyfish are harmless — only deal damage when active
                if (_jellyFreezeTimer <= 0f)
                {
                    var jRect = new Rectangle((int)j.X, (int)j.Y, 32, 28);
                    if (jRect.IntersectsWith(pr)) _player.TakeDamage(1);
                }
            }

            // ── Exit ──────────────────────────────────────────────────────────
            if (_exitZone.IntersectsWith(pr) && !_levelComplete)
            {
                _levelComplete = true; _completeTimer = 2f;
            }

            // ── Death check ───────────────────────────────────────────────────
            if (!_player.IsAlive) HandleDeath();

            // Pause and inventory consistent with all other gameplay scenes
            if (Game.Instance.Input.PausePressed) Game.Instance.Scenes.Push(new PauseScene());
            // C key — Quick Dash (works underwater for a burst of speed)
            if (Game.Instance.Input.AirDashPressed && _player.TryDash())
                Game.Instance.Audio.BeepJump();
            if (Game.Instance.Input.InventoryPressed) Game.Instance.Scenes.Push(new InventoryScene(_player));

            SMB3Hud.Update(dt);
        }

        private void ResolvePlayerPlatform(Rectangle p)
        {
            var pr = new Rectangle((int)_player.X, (int)_player.Y, _player.Width, _player.Height);
            if (!pr.IntersectsWith(p)) return;
            float ob = pr.Bottom - p.Top;
            float ot = p.Bottom  - pr.Top;
            if (ob < ot && _player.VelocityY > 0)
            {
                _player.Y = p.Top - _player.Height;
                _player.VelocityY = 0; _player.IsGrounded = true;
                _player.JumpsRemaining = _player.MaxJumps;  // reset for double jump
            }
            else if (ot < ob && _player.VelocityY < 0)
            {
                _player.Y = p.Bottom; _player.VelocityY = 0;
            }
        }

        private void HandleDeath()
        {
            SessionStats.Instance.RecordDeath();
            DebugLogger.LogInfo("UnderwaterScene", "Player drowned or died.");
            Game.Instance.Scenes.Pop();
        }

        private void CompleteLevel()
        {
            DebugLogger.LogInfo("UnderwaterScene", "Underwater level cleared!");
            // Push CourseClearScene for the SMB3-style fanfare, then
            // double-pop back to the Overworld map screen.
            Game.Instance.Scenes.Push(new CardRouletteScene(() =>
            {
                Game.Instance.Scenes.Pop(); // pop CardRoulette
                Game.Instance.Scenes.Push(new CourseClearScene(
                    "Underwater", 0, 0,
                    onContinue: () =>
                    {
                        SessionStats.Instance.RecordLevelComplete();
                        Game.Instance.LevelJustCompleted = true;
                        Game.Instance.Scenes.Pop(); // pop CourseClear
                        Game.Instance.Scenes.Pop(); // pop UnderwaterScene → Overworld
                    }));
            }));
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Background ────────────────────────────────────────────────────
            // PHASE 2 - Team 14: Environment Artist - Draw underwater background if loaded
            if (_bg != null)
            {
                g.DrawImage(_bg, 0, 0, W, H);
            }
            else
            {
                // Fallback: underwater gradient with light rays
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                    Color.FromArgb(0, 40, 100), Color.FromArgb(0, 20, 60), 90f))
                    g.FillRectangle(br, 0, 0, W, H);

                // Caustic light rays.
                using (var br = new SolidBrush(Color.FromArgb(15, 180, 220, 255)))
                {
                    g.FillRectangle(br, 100, 0, 30, H);
                    g.FillRectangle(br, 320, 0, 20, H);
                    g.FillRectangle(br, 580, 0, 28, H);
                    g.FillRectangle(br, 780, 0, 22, H);
                }
            }

            // ── Platforms (coral) ─────────────────────────────────────────────
            foreach (var p in _platforms)
            {
                using (var br = new SolidBrush(Color.FromArgb(180, 80, 40)))
                    g.FillRectangle(br, p);
                using (var pen = new Pen(Color.IndianRed))
                    g.DrawRectangle(pen, p);
            }

            // ── Coral hazards (spiky) ─────────────────────────────────────────
            foreach (var c in _coralHazards)
            {
                using (var br = new SolidBrush(Color.DarkRed))
                    g.FillRectangle(br, c);
                using (var f = new Font("Courier New", 9))
                    g.DrawString("▲▲", f, Brushes.OrangeRed, c.X, c.Y - 12);
            }

            // ── Current zones ─────────────────────────────────────────────────
            foreach (var cur in _currents)
            {
                using (var br = new SolidBrush(Color.FromArgb(30, 100, 200, 255)))
                    g.FillRectangle(br, cur);
                using (var f = new Font("Courier New", 8))
                    g.DrawString("▲\n▲\n▲", f, Brushes.CornflowerBlue, cur.X + 16, cur.Y + 10);
            }

            // ── Air bubble stations ───────────────────────────────────────────
            foreach (var b in _bubbleStations)
            {
                using (var br = new SolidBrush(Color.FromArgb(120, 200, 240, 255)))
                    g.FillEllipse(br, b);
                using (var f = new Font("Courier New", 7))
                    g.DrawString("O2", f, Brushes.Cyan, b.X - 2, b.Y + 2);
            }

            // ── Jellyfish ─────────────────────────────────────────────────────
            foreach (var j in _jellies)
            {
                using (var br = new SolidBrush(Color.FromArgb(160, 180, 100, 200)))
                    g.FillEllipse(br, (int)j.X, (int)j.Y, 32, 22);
                using (var pen = new Pen(Color.MediumOrchid))
                    g.DrawEllipse(pen, (int)j.X, (int)j.Y, 32, 22);
                // Tentacles.
                using (var pen = new Pen(Color.FromArgb(140, 200, 100, 200), 1))
                {
                    g.DrawLine(pen, j.X + 8,  j.Y + 22, j.X + 8,  j.Y + 36);
                    g.DrawLine(pen, j.X + 16, j.Y + 22, j.X + 16, j.Y + 40);
                    g.DrawLine(pen, j.X + 24, j.Y + 22, j.X + 24, j.Y + 36);
                }
            }

            foreach (var wall in _iceWalls)
                if (wall.IsAlive) wall.Draw(g);

            // ── Exit zone — animated underwater beacon ──────────────────────
            DrawExitBeacon(g);

            // ── Player ────────────────────────────────────────────────────────
            _player.Draw(g);

            // ── HUD + oxygen bar ──────────────────────────────────────────────
            g.ResetTransform();
            GameHUD.Draw(g, _player, W, H);

            // Directional arrow toward exit when exit is off-screen
            DrawExitDirectionArrow(g, W, H);

            // Oxygen bar drawn below HUD band
            float oxyPct = Math.Max(0f, _oxygenTimer / MaxOxygen);
            int oxyBarW = 200;
            int oxyBarX = W / 2 - oxyBarW / 2;
            int oxyBarY = H - 34;
            using (var br = new SolidBrush(Color.FromArgb(60, 0, 100, 200)))
                g.FillRectangle(br, oxyBarX, oxyBarY, oxyBarW, 12);
            Color oxyColor = oxyPct < 0.3f ? Color.OrangeRed : Color.DeepSkyBlue;
            using (var br = new SolidBrush(oxyColor))
                g.FillRectangle(br, oxyBarX, oxyBarY, (int)(oxyBarW * oxyPct), 12);
            using (var f = new Font("Courier New", 8))
                g.DrawString($"O2  {_oxygenTimer:F0}s", f, Brushes.Cyan, oxyBarX - 36, oxyBarY - 1);
            DrawDevMenuButton(g);
        }

        /// <summary>
        /// Draws an animated underwater exit beacon — pulsing glow, rotating
        /// light ring, "EXIT" label, and animated arrows.  Much more visible
        /// than the old dim green square.
        /// </summary>
        /// <remarks>PHASE 2 - Session 111: clear underwater goal</remarks>
        private void DrawExitBeacon(Graphics g)
        {
            int tick = Environment.TickCount;
            float pulse = (float)(Math.Sin(tick / 300.0) * 0.5 + 0.5); // 0..1 pulse

            int cx = _exitZone.X + _exitZone.Width / 2;
            int cy = _exitZone.Y + _exitZone.Height / 2;

            // Outer glow halo (pulsing radius 50-70)
            int glowR = 50 + (int)(20f * pulse);
            int glowAlpha = 40 + (int)(60f * pulse);
            using (var br = new SolidBrush(Color.FromArgb(glowAlpha, 0, 255, 120)))
                g.FillEllipse(br, cx - glowR, cy - glowR, glowR * 2, glowR * 2);

            // Inner bright zone
            using (var br = new SolidBrush(Color.FromArgb(140, 0, 220, 80)))
                g.FillRectangle(br, _exitZone);
            using (var pen = new Pen(Color.FromArgb(200, 0, 255, 100), 2))
                g.DrawRectangle(pen, _exitZone);

            // Animated arrows pointing into the exit zone (3 arrows descending)
            for (int a = 0; a < 3; a++)
            {
                int aOff = (int)((tick / 4 + a * 18) % 54) - 54;
                int aAlpha = Math.Max(50, 220 - Math.Abs(aOff) * 4);
                using (var br = new SolidBrush(Color.FromArgb(aAlpha, 100, 255, 150)))
                {
                    // Down-arrow shapes pointing at the exit zone
                    g.FillPolygon(br, new Point[] {
                        new Point(cx - 8, _exitZone.Top + aOff),
                        new Point(cx + 8, _exitZone.Top + aOff),
                        new Point(cx,     _exitZone.Top + aOff + 12)
                    });
                }
            }

            // "EXIT" text — bold, bright, larger
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
            {
                var sz = g.MeasureString("EXIT", f);
                g.DrawString("EXIT", f, Brushes.White, cx - sz.Width / 2, cy - sz.Height / 2);
            }

            // ">>> GOAL <<<" label above the beacon
            using (var f = new Font("Courier New", 9, FontStyle.Bold))
            {
                string lbl = ">>> GOAL <<<";
                var sz = g.MeasureString(lbl, f);
                int lblAlpha = 140 + (int)(80f * pulse);
                using (var br = new SolidBrush(Color.FromArgb(lblAlpha, 100, 255, 180)))
                    g.DrawString(lbl, f, br, cx - sz.Width / 2, _exitZone.Top - 22);
            }
        }

        /// <summary>
        /// Draws a directional arrow on the HUD pointing toward the exit
        /// zone when it is off-screen or far from the player.  Helps the
        /// player know which direction to swim.
        /// </summary>
        /// <remarks>PHASE 2 - Session 111: underwater navigation aid</remarks>
        private void DrawExitDirectionArrow(Graphics g, int W, int H)
        {
            // Distance from player to exit center
            float ex = _exitZone.X + _exitZone.Width / 2f;
            float ey = _exitZone.Y + _exitZone.Height / 2f;
            float dx = ex - (_player.X + _player.Width / 2f);
            float dy = ey - (_player.Y + _player.Height / 2f);
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            // Only show arrow if far enough away (>150 px)
            if (dist < 150f) return;

            // Determine direction label
            string dir = "";
            if (Math.Abs(dx) > Math.Abs(dy))
                dir = dx > 0 ? "EXIT  >>>" : "<<<  EXIT";
            else
                dir = dy > 0 ? "EXIT  vvv" : "^^^  EXIT";

            int tick = Environment.TickCount;
            int alpha = 160 + (int)(60f * Math.Sin(tick / 250.0));

            // Draw at top of screen below HUD
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            using (var br = new SolidBrush(Color.FromArgb(alpha, 100, 255, 180)))
            {
                var sz = g.MeasureString(dir, f);
                int arrowX = W / 2 - (int)(sz.Width / 2);
                int arrowY = GameHUD.BandHeight + 6;
                // Semi-transparent background for readability
                using (var bgBr = new SolidBrush(Color.FromArgb(100, 0, 30, 60)))
                    g.FillRectangle(bgBr, arrowX - 6, arrowY - 2, (int)sz.Width + 12, (int)sz.Height + 4);
                g.DrawString(dir, f, br, arrowX, arrowY);
            }

            // Also draw distance indicator
            using (var f = new Font("Courier New", 8))
            using (var br = new SolidBrush(Color.FromArgb(180, 180, 220, 255)))
            {
                string distStr = $"{(int)dist}px away";
                var sz = g.MeasureString(distStr, f);
                g.DrawString(distStr, f, br, W / 2 - sz.Width / 2, GameHUD.BandHeight + 22);
            }
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
        }

        private void BreakNearbyObjects()
        {
            // Break nearby ice walls.
            for (int i = _iceWalls.Count - 1; i >= 0; i--)
            {
                var wall = _iceWalls[i];
                if (!wall.IsAlive) continue;
                float dx = _player.CenterX - (wall.X + wall.Width / 2f);
                float dy = _player.CenterY - (wall.Y + wall.Height / 2f);
                if (Math.Sqrt(dx * dx + dy * dy) <= 70f)
                    wall.Health = 0;
            }

            // Clear nearby coral hazards.
            for (int i = _coralHazards.Count - 1; i >= 0; i--)
            {
                var c = _coralHazards[i];
                float cx = c.X + c.Width / 2f;
                float cy = c.Y + c.Height / 2f;
                float dx = _player.CenterX - cx;
                float dy = _player.CenterY - cy;
                if (Math.Sqrt(dx * dx + dy * dy) <= 80f)
                    _coralHazards.RemoveAt(i);
            }
        }
    }
}


