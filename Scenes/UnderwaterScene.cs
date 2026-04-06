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

            // Coral platforms.
            _platforms.Add(new Rectangle(0,       H - 40,  W,   40));
            _platforms.Add(new Rectangle(80,      H - 120, 140, 20));
            _platforms.Add(new Rectangle(280,     H - 180, 120, 20));
            _platforms.Add(new Rectangle(480,     H - 240, 100, 20));
            _platforms.Add(new Rectangle(660,     H - 180, 140, 20));
            _platforms.Add(new Rectangle(820,     H - 300, 100, 20));

            // Coral hazards.
            _coralHazards.Add(new Rectangle(200, H - 70,  30, 30));
            _coralHazards.Add(new Rectangle(420, H - 70,  25, 30));
            _coralHazards.Add(new Rectangle(600, H - 70,  28, 30));

            // Upward current zones.
            _currents.Add(new Rectangle(350, H - 250, 50, 180));
            _currents.Add(new Rectangle(700, H - 350, 50, 260));

            // Air bubble refill stations.
            _bubbleStations.Add(new Rectangle(300, H - 160, 20, 20));
            _bubbleStations.Add(new Rectangle(600, H - 260, 20, 20));

            // Jellyfish.
            AddJelly(160, H - 180, 0.6f, 60f);
            AddJelly(500, H - 250, 0.5f, 80f);
            AddJelly(780, H - 200, 0.7f, 50f);

            _exitZone = new Rectangle(W - 80, H - 340, 60, 60);

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

                var jRect = new Rectangle((int)j.X, (int)j.Y, 32, 28);
                if (jRect.IntersectsWith(pr)) _player.TakeDamage(1);
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
            Game.Instance.LevelJustCompleted = true;
            SessionStats.Instance.RecordLevelComplete();
            Game.Instance.Scenes.Pop();
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

            // ── Exit zone ─────────────────────────────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(100, 0, 220, 0)))
                g.FillRectangle(br, _exitZone);
            using (var f = new Font("Courier New", 8, FontStyle.Bold))
                g.DrawString("EXIT", f, Brushes.LimeGreen, _exitZone.X + 8, _exitZone.Y + 22);

            // ── Player ────────────────────────────────────────────────────────
            _player.Draw(g);

            // ── HUD + oxygen bar ──────────────────────────────────────────────
            g.ResetTransform();
            GameHUD.Draw(g, _player, W, H);

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


