using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Fridays_Adventure.Abilities;
using Fridays_Adventure.AI;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Systems;
using Fridays_Adventure.Hazards;
using Fridays_Adventure.Rules;

namespace Fridays_Adventure.Scenes
{
    public sealed class IslandScene : Scene
    {
        private readonly string _islandId;
        private readonly string _islandName;

        private Player            _player;
        private List<Enemy>       _enemies;
        private List<Rectangle>   _platforms;
        private List<Hazard>      _hazards;
        private List<IceWallInstance> _iceWalls;
        private ComboAssist       _combo = new ComboAssist();

        private float _cameraX;
        private int   _levelWidth = 2800;
        private int   _groundY;

        private float _sinkMashTimer;
        private bool  _showRescue;
        private float _rescueTimer;
        private bool  _levelComplete;
        private float _completeTimer;
        private Rectangle _exitFlag;

        private List<Berries>      _berries;
        private List<HealthPickup>  _healthPickups;
        private int                 _berriesCollected;

        private bool  _introActive;
        private bool  _wasInSeaStone;
        private float _seaStoneFlashAlpha;
        private float _freezeFlashTimer;
        private float _breakShockwaveTimer;
        private float _breakShockwaveWorldX;
        private float _breakShockwaveWorldY;
        private float _iceFlashTimer;

        private Bitmap _bg;
        private static readonly Font _hudFont  = new Font("Courier New", 16, FontStyle.Bold);
        private static readonly Font _infoFont = new Font("Courier New", 12, FontStyle.Bold);
        public IslandScene(string id, string name) { _islandId = id; _islandName = name; }

        public override void OnEnter()
        {
            BuildLevel();
            LoadBackground();
            Game.Instance.Audio.PlayIsland();
        }

        public override void OnExit()
        {
            _bg?.Dispose(); _bg = null;
        }

        public override void OnResume() => Game.Instance.Audio.PlayIsland();

        // ── Level construction ──────────────────────────────────────────────

        private void BuildLevel()
        {
            _groundY   = 440;
            _iceWalls  = new List<IceWallInstance>();
            _platforms = new List<Rectangle>();
            _hazards   = new List<Hazard>();
            _enemies   = new List<Enemy>();

            switch (_islandId)
            {
                case "wano": BuildBladeNation(); break;
                default:     BuildDinoIsland();  break;
            }

            // Player
            _player = new Player(50, _groundY - 56);
            var pSprite = SpriteManager.GetScaled("player_missfriday.png", _player.Width, _player.Height);
            if (pSprite != null) _player.Sprite = pSprite;

            // Berries (SMB3-style collectibles on platforms and over gaps)
            _berries = new List<Berries>();
            SpawnBerries();

            // Health pickups — red cross items scattered through the level
            _healthPickups = new List<HealthPickup>();
            SpawnHealthPickups();

            // Level-entry drop — all characters fall in from above the screen
            _introActive       = true;
            _player.Y          = -120f;
            _player.IsGrounded = false;
            _player.VelocityY  = 0f;
            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemies[i].Y          = -80f - i * 60f;
                _enemies[i].IsGrounded = false;
                _enemies[i].VelocityY  = 0f;
            }
        }

        private void BuildDinoIsland()
        {
            // Jungle survival — rivers (water pits), predator enemies, one SeaStone trap
            _platforms.Add(new Rectangle(0,    _groundY, 700,  160));
            _platforms.Add(new Rectangle(800,  _groundY, 560,  160));
            _platforms.Add(new Rectangle(1440, _groundY, 600,  160));
            _platforms.Add(new Rectangle(2120, _groundY, 680,  160));
            _platforms.Add(new Rectangle(350,  _groundY - 110, 200, 20));
            _platforms.Add(new Rectangle(1050, _groundY - 90,  220, 20));
            _platforms.Add(new Rectangle(1700, _groundY - 120, 180, 20));
            _platforms.Add(new Rectangle(2300, _groundY - 140, 260, 20));

            _hazards.Add(new WaterPit(700,  _groundY, 100, 160));
            _hazards.Add(new WaterPit(1360, _groundY, 80,  160));
            _hazards.Add(new WaterPit(2040, _groundY, 80,  160));
            _hazards.Add(new SeaStoneZone(1500, _groundY - 50, 220, 50));
            _hazards.Add(new FireSource(1780, _groundY - 48, 40, 48));
            _hazards.Add(new FireSource(2250, _groundY - 48, 40, 48));

            SpawnEnemy(200,  _groundY - 42, 0.60f);
            SpawnEnemy(550,  _groundY - 42, 0.60f);
            SpawnEnemy(950,  _groundY - 42, 0.85f);
            SpawnEnemy(1200, _groundY - 42, 0.85f);
            SpawnEnemy(1650, _groundY - 42, 1.00f);
            SpawnEnemy(2200, _groundY - 42, 1.00f, isBoss: true);

            _exitFlag  = new Rectangle(2720, _groundY - 52, 30, 52);
        }

        private void BuildBladeNation()
        {
            // Samurai honour trial — fire torches everywhere, no rivers, armoured enemies, SeaStone at the end
            _levelWidth = 2600;

            _platforms.Add(new Rectangle(0,    _groundY, 500,  160));
            _platforms.Add(new Rectangle(560,  _groundY, 480,  160));
            _platforms.Add(new Rectangle(1100, _groundY, 520,  160));
            _platforms.Add(new Rectangle(1680, _groundY, 920,  160));
            _platforms.Add(new Rectangle(300,  _groundY - 130, 180, 20));
            _platforms.Add(new Rectangle(800,  _groundY - 110, 200, 20));
            _platforms.Add(new Rectangle(1350, _groundY - 130, 160, 20));
            _platforms.Add(new Rectangle(2000, _groundY - 150, 240, 20));

            // Torch-lined corridors — fire sources instead of water
            for (int fx = 100; fx < 2500; fx += 350)
                _hazards.Add(new FireSource(fx, _groundY - 48, 36, 48));

            _hazards.Add(new SeaStoneZone(1680, _groundY - 60, 280, 60));
            _hazards.Add(new SeaStoneZone(2100, _groundY - 60, 200, 60));

            // Armoured samurai — higher HP, slower, but hurt more
            SpawnEnemy(250,  _groundY - 48, 1.0f,  hp: 70);
            SpawnEnemy(700,  _groundY - 48, 1.0f,  hp: 70);
            SpawnEnemy(1200, _groundY - 48, 1.2f,  hp: 90);
            SpawnEnemy(1500, _groundY - 48, 1.2f,  hp: 90);
            SpawnEnemy(1900, _groundY - 48, 1.4f,  hp: 110);
            SpawnEnemy(2300, _groundY - 48, 1.5f,  hp: 150, isBoss: true);

            _exitFlag = new Rectangle(2520, _groundY - 52, 30, 52);
        }


        private void SpawnEnemy(float x, float groundTop, float difficulty,
                               bool isBoss = false, int hp = -1)
        {
            int finalHp = hp > 0 ? hp : (isBoss ? 120 : (int)(40 * difficulty));
            var e = new Enemy(x, groundTop, 32, 48, finalHp,
                              patrolLeft: x - 140, patrolRight: x + 140);
            e.MoveSpeed    = 90f + 40f * difficulty;
            e.EnemyType    = isBoss ? "Boss" : "Marine";
            e.ScoreValue   = isBoss ? 100 : 15;
            e.AttackDamage = isBoss ? 16 : (int)(8 * difficulty);
            string spriteFile = isBoss ? "GARP.png" : "GARP.png";
            var eSprite = SpriteManager.GetScaled(spriteFile, e.Width, e.Height);
            if (eSprite != null) e.Sprite = eSprite;
            _enemies.Add(e);
        }

        private void SpawnBerries()
        {
            switch (_islandId)
            {
                case "wano": SpawnBladeBerries(); break;
                default:     SpawnDinoBerries();  break;
            }
        }

        private void SpawnDinoBerries()
        {
            // Elevated platforms — reward exploration
            AddBerryRow(380, _groundY - 132, 3, 40);
            AddBerryRow(1080, _groundY - 112, 3, 60);
            AddBerryRow(1720, _groundY - 142, 3, 50);
            AddBerryRow(2330, _groundY - 162, 4, 50);
            // Arcs over water pits — risk/reward
            AddBerry(720, _groundY - 60); AddBerry(750, _groundY - 80); AddBerry(780, _groundY - 60);
            AddBerry(1380, _groundY - 70); AddBerry(1410, _groundY - 70);
            AddBerry(2060, _groundY - 70); AddBerry(2090, _groundY - 80);
        }

        private void SpawnBladeBerries()
        {
            AddBerryRow(320, _groundY - 152, 3, 50);
            AddBerryRow(830, _groundY - 132, 3, 50);
            AddBerryRow(1370, _groundY - 152, 3, 40);
            AddBerryRow(2020, _groundY - 172, 4, 50);
        }

        private void AddBerryRow(float x, float y, int count, float spacing)
        {
            for (int i = 0; i < count; i++)
                _berries.Add(new Berries(x + i * spacing, y));
        }

        private void AddBerry(float x, float y) => _berries.Add(new Berries(x, y));

        private void SpawnHealthPickups()
        {
            switch (_islandId)
            {
                case "wano": SpawnBladeHealthPickups(); break;
                default:     SpawnDinoHealthPickups();  break;
            }
        }

        private void SpawnDinoHealthPickups()
        {
            _healthPickups.Add(new HealthPickup(410,  _groundY - 120));
            _healthPickups.Add(new HealthPickup(1510, _groundY - 130));
            _healthPickups.Add(new HealthPickup(2340, _groundY - 155));
        }

        private void SpawnBladeHealthPickups()
        {
            _healthPickups.Add(new HealthPickup(330,  _groundY - 142));
            _healthPickups.Add(new HealthPickup(1380, _groundY - 142));
            _healthPickups.Add(new HealthPickup(2040, _groundY - 165));
        }

        private void LoadBackground()
        {
            string file;
            switch (_islandId)
            {
                case "dino":    file = "level_1.png";        break;
                case "sky":     file = "bg_skyisland.png";   break;
                case "wano":    file = "bg_bladenation.png"; break;
                default:        file = "bg_island.png";      break;
            }
            string base_ = AppDomain.CurrentDomain.BaseDirectory;
            string spritesPath = Path.Combine(base_, "Assets", "Sprites", file);
            string assetsPath  = Path.Combine(base_, "Assets", file);
            string path = File.Exists(spritesPath) ? spritesPath
                        : File.Exists(assetsPath)  ? assetsPath
                        : null;
            if (path != null) _bg = new Bitmap(path);
        }

        // ── Update ──────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            if (_introActive)   { UpdateIntro(dt); return; }
            if (_levelComplete) { UpdateComplete(dt); return; }

            var input = Game.Instance.Input;

            HandleInput(input, dt);
            _player.Update(dt);
            MoveAndCollide(_player, dt);
            DevilFruitRules.Check(_player, _hazards, dt);

            bool nowInSeaStone = _player.HasEffect(StatusEffect.Suppressed);
            if (nowInSeaStone && !_wasInSeaStone)
            {
                Game.Instance.Audio.BeepSeaStone();
                _seaStoneFlashAlpha = 180f;
            }
            _wasInSeaStone = nowInSeaStone;
            if (_seaStoneFlashAlpha  > 0f) _seaStoneFlashAlpha  = Math.Max(0f, _seaStoneFlashAlpha - 300f * dt);
            if (_freezeFlashTimer    > 0f) { _freezeFlashTimer    += dt; if (_freezeFlashTimer    >= 0.45f) _freezeFlashTimer    = 0f; }
            if (_breakShockwaveTimer > 0f) { _breakShockwaveTimer += dt; if (_breakShockwaveTimer >= 0.40f) _breakShockwaveTimer = 0f; }
            if (_iceFlashTimer       > 0f) { _iceFlashTimer       += dt; if (_iceFlashTimer       >= 0.30f) _iceFlashTimer       = 0f; }

            IceSystem.Update(_iceWalls, _hazards, _player, dt);
            ThreatSystem.Tick(dt);
            CheckWaterFall(dt, input);
            UpdateEnemies(dt);
            _combo.Update(dt, _player, _enemies);
            CheckCombat();
            UpdateBerries(dt);
            UpdateHealthPickups(dt);
            CheckExit();
            UpdateCamera();
        }

        private void HandleInput(Engine.InputManager input, float dt)
        {
            if (!_player.HasEffect(StatusEffect.Sinking) &&
                !_player.HasEffect(StatusEffect.Stunned))
            {
                if (input.LeftHeld)  { _player.VelocityX = -_player.MoveSpeed; _player.FacingRight = false; }
                else if (input.RightHeld) { _player.VelocityX = _player.MoveSpeed; _player.FacingRight = true; }
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
                if (input.DodgePressed) _player.TryDodge();
                if (input.AttackPressed)
                {
                    if (_player.TryAttack()) Game.Instance.Audio.BeepAttack();
                }
            }

            if (input.Ability1Pressed)
            {
                if (_player.UseIceWall(out IceWallInstance wall))
                {
                    _iceWalls.Add(wall);
                    Game.Instance.Audio.BeepIce();
                    _iceFlashTimer = 0.001f;
                }
            }
            if (input.Ability2Pressed)
            {
                if (_player.UseFlashFreeze())
                {
                    FreezeNearbyEnemies();
                    Game.Instance.Audio.BeepFreeze();
                    _freezeFlashTimer = 0.001f;
                }
            }
            if (input.Ability3Pressed)
            {
                if (_player.UseBreakWall())
                {
                    BreakNearbyWalls();
                    Game.Instance.Audio.BeepBreak();
                    _breakShockwaveTimer  = 0.001f;
                    _breakShockwaveWorldX = _player.CenterX;
                    _breakShockwaveWorldY = _player.CenterY;
                }
            }
            if (input.PausePressed)
                Game.Instance.Scenes.Push(new PauseScene());
        }

        private void MoveAndCollide(Character c, float dt)
        {
            c.X += c.VelocityX * dt;
            c.X  = Math.Max(0, Math.Min(_levelWidth - c.Width, c.X));
            ResolveHorizontal(c);

            c.Y += c.VelocityY * dt;
            c.IsGrounded = false;
            ResolveVertical(c);
        }

        private void ResolveHorizontal(Character c)
        {
            foreach (var plat in _platforms)
                if (c.Hitbox.IntersectsWith(plat))
                {
                    if (c.VelocityX > 0) c.X = plat.Left - c.Width;
                    else if (c.VelocityX < 0) c.X = plat.Right;
                    c.VelocityX = 0;
                }
            foreach (var wall in _iceWalls)
            {
                if (!wall.IsAlive) continue;
                var wb = wall.Hitbox;
                if (!c.Hitbox.IntersectsWith(wb)) continue;
                if (c.VelocityX > 0) c.X = wb.Left - c.Width;
                else if (c.VelocityX < 0) c.X = wb.Right;
                c.VelocityX = 0;
            }
        }

        private void ResolveVertical(Character c)
        {
            foreach (var plat in _platforms)
            {
                if (!c.Hitbox.IntersectsWith(plat)) continue;
                if (c.VelocityY >= 0)
                {
                    c.Y          = plat.Top - c.Height;
                    c.VelocityY  = 0;
                    c.IsGrounded = true;
                }
                else
                {
                    c.Y         = plat.Bottom;
                    c.VelocityY = 0;
                }
            }
            foreach (var wall in _iceWalls)
            {
                if (!wall.IsAlive) continue;
                var wb = wall.Hitbox;
                if (!c.Hitbox.IntersectsWith(wb)) continue;
                if (c.VelocityY >= 0)
                {
                    c.Y          = wb.Top - c.Height;
                    c.VelocityY  = 0;
                    c.IsGrounded = true;
                }
                else
                {
                    c.Y         = wb.Bottom;
                    c.VelocityY = 0;
                }
            }
            // Death floor
            if (c.Y > Game.Instance.CanvasHeight + 200)
            {
                if (c == _player) _player.TakeDamage(9999);
                else c.Health = 0;
            }
        }

        private void CheckHazards(float dt)
        {
            foreach (var hz in _hazards)
            {
                hz.Update(dt);
                if (hz.Overlaps(_player)) hz.ApplyEffect(_player, dt);
                if (hz.Type == HazardType.FireSource)
                {
                    var fire = (FireSource)hz;
                    foreach (var wall in _iceWalls)
                        if (fire.IsNear(wall.X + wall.Width / 2f, wall.Y + wall.Height / 2f))
                            wall.Update(dt, nearFire: true);
                }
            }
        }

        private void CheckWaterFall(float dt, Engine.InputManager input)
        {
            if (!_player.HasEffect(StatusEffect.Sinking)) { _sinkMashTimer = 0; _showRescue = false; return; }
            _sinkMashTimer += dt;
            if (input.AnyMash) _sinkMashTimer = Math.Max(0, _sinkMashTimer - 0.4f);
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
            Game.Instance.Audio.BeepSink();
        }

        private void UpdateEnemies(float dt)
        {
            foreach (var e in _enemies)
            {
                if (!e.IsAlive) continue;
                e.UpdateWithTarget(dt, _player);
                MoveAndCollide(e, dt);
            }
        }

        private void CheckCombat()
        {
            var pAtk = _player.AttackHitbox;
            foreach (var e in _enemies)
            {
                if (!e.IsAlive) continue;

                bool stomped = false;

                // SMB3 Head Stomp — land on enemy to deal 2x damage + bounce
                if (_player.VelocityY > 0 && !_player.IsInvincible)
                {
                    float pBot = _player.Y + _player.Height;
                    float eTop = e.Y;
                    float overlap = pBot - eTop;
                    if (overlap > 0 && overlap < 22 &&
                        _player.CenterX > e.X - 8 && _player.CenterX < e.X + e.Width + 8)
                    {
                        e.TakeDamage(_player.AttackDamage * 2);
                        _player.VelocityY = _player.JumpForce * 0.45f;
                        _player.IsGrounded = false;
                        Game.Instance.Audio.BeepStomp();
                        stomped = true;
                    }
                }

                // Regular attack
                if (pAtk != Rectangle.Empty && pAtk.IntersectsWith(e.Hitbox))
                    e.TakeDamage(_player.AttackDamage);

                // Enemy attacks player (skip if stomp this frame)
                if (!stomped && e.AttackHitbox != Rectangle.Empty &&
                    e.AttackHitbox.IntersectsWith(_player.Hitbox))
                {
                    _player.TakeDamage(e.AttackDamage);
                    if (_player.Health <= 0) Game.Instance.Audio.BeepHurt();
                }
            }
            if (!_player.IsAlive) GameOver();
        }

        private void UpdateBerries(float dt)
        {
            foreach (var b in _berries)
            {
                b.Update(dt);
                if (!b.Collected && _player.Hitbox.IntersectsWith(b.Hitbox))
                {
                    b.Collected = true;
                    _berriesCollected++;
                    BountySystem.Award(b.Value);
                    Game.Instance.Audio.BeepBerry();
                }
            }
        }

        private void UpdateHealthPickups(float dt)
        {
            foreach (var hp in _healthPickups)
            {
                hp.Update(dt);
                if (hp.TryCollect(_player))
                {
                    _player.Health = Math.Min(_player.MaxHealth, _player.Health + 30);
                    Game.Instance.Audio.BeepHeal();
                }
            }
        }

        private void FreezeNearbyEnemies()
        {
            foreach (var e in _enemies)
                if (e.IsAlive && _player.DistanceTo(e) <= 130f)
                    e.ApplyEffect(StatusEffect.Frozen, 2.5f);
        }

        private void BreakNearbyWalls()
        {
            float range = 70f;
            for (int i = _iceWalls.Count - 1; i >= 0; i--)
            {
                var wall = _iceWalls[i];
                if (!wall.IsAlive) continue;
                float dx = _player.CenterX - (wall.X + wall.Width / 2f);
                float dy = _player.CenterY - (wall.Y + wall.Height / 2f);
                if (Math.Sqrt(dx * dx + dy * dy) <= range)
                    wall.Health = 0;
            }
            foreach (var e in _enemies)
                if (e.IsAlive && _player.DistanceTo(e) <= 80f)
                    e.TakeDamage(8);
        }

        private void CheckExit()
        {
            if (_player.Hitbox.IntersectsWith(_exitFlag) && !_levelComplete)
            {
                _levelComplete = true;
                _completeTimer = 0;
                ThreatSystem.OnIslandCleared();
                BountySystem.Award(500);
                Game.Instance.CrewBonds++;
                Game.Instance.Save.SetFlag(_islandId + "_complete");
                Game.Instance.Save.Save();
            }
        }

        private void UpdateComplete(float dt)
        {
            _completeTimer += dt;
            if (_completeTimer >= 3.5f)
                Game.Instance.Scenes.Pop();
        }

        private void UpdateIntro(float dt)
        {
            _player.Update(dt);
            MoveAndCollide(_player, dt);
            foreach (var e in _enemies)
            {
                e.Update(dt);
                MoveAndCollide(e, dt);
            }
            UpdateCamera();
            bool allLanded = _player.IsGrounded;
            if (allLanded)
                foreach (var e in _enemies)
                    if (!e.IsGrounded) { allLanded = false; break; }
            if (allLanded)
                _introActive = false;
        }

        private void GameOver()
        {
            Game.Instance.Scenes.Replace(new GameOverScene());
        }

        public override void HandleClick(System.Drawing.Point p)
        {
            if (_levelComplete || _introActive) return;
            int W = Game.Instance.CanvasWidth;
            if (new System.Drawing.Rectangle(W - 90, 6, 78, 28).Contains(p))
                Game.Instance.Scenes.Push(new PauseScene());
        }

        private void UpdateCamera()
        {
            float target = _player.CenterX - Game.Instance.CanvasWidth * 0.4f;
            _cameraX = Math.Max(0, Math.Min(_levelWidth - Game.Instance.CanvasWidth, target));
        }

        // ── Draw ────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            DrawBackground(g, W, H);

            g.TranslateTransform(-_cameraX, 0);

            DrawPlatforms(g, H);
            foreach (var hz in _hazards)   hz.Draw(g);
            foreach (var w  in _iceWalls)  w.Draw(g);
            foreach (var e  in _enemies)   if (e.IsAlive) e.Draw(g);
            foreach (var b  in _berries)   b.Draw(g);
            foreach (var hp in _healthPickups) hp.Draw(g);
            _combo.Draw(g);
            DrawExitFlag(g);
            _player.Draw(g);
            if (_player.IsAttacking) DrawAttackArc(g);
            if (_breakShockwaveTimer > 0f) DrawBreakShockwave(g);

            g.ResetTransform();

            DrawScreenFlashes(g, W, H);
            DrawHUD(g, W, H);
            if (_showRescue)   DrawRescuePrompt(g, W, H);
            if (_levelComplete) DrawComplete(g, W, H);
        }

        private void DrawBackground(Graphics g, int W, int H)
        {
            if (_bg != null) { g.DrawImage(_bg, 0, 0, W, H); return; }
            using (var br = new LinearGradientBrush(new Rectangle(0,0,W,H),
                Color.FromArgb(60,90,140), Color.FromArgb(140,110,70), 90f))
                g.FillRectangle(br, 0, 0, W, H);
        }

        private void DrawPlatforms(Graphics g, int H)
        {
            foreach (var p in _platforms)
            {
                using (var br = new SolidBrush(Color.FromArgb(160, 100, 60)))
                    g.FillRectangle(br, p);
                using (var br = new SolidBrush(Color.FromArgb(80, 160, 60)))
                    g.FillRectangle(br, p.X, p.Y, p.Width, 6);
            }
        }

        private void DrawExitFlag(Graphics g)
        {
            g.FillRectangle(Brushes.Sienna, _exitFlag.X + 12, _exitFlag.Y, 4, _exitFlag.Height);
            g.FillRectangle(Brushes.Gold, _exitFlag.X + 16, _exitFlag.Y, 20, 14);
            using (var f = new Font("Arial", 7))
                g.DrawString("EXIT", f, Brushes.Black, _exitFlag.X + 17, _exitFlag.Y + 2);
        }

        private void DrawAttackArc(Graphics g)
        {
            var h = _player.AttackHitbox;
            using (var br = new SolidBrush(Color.FromArgb(80, Color.Cyan)))
                g.FillRectangle(br, h);
        }

        private void DrawBreakShockwave(Graphics g)
        {
            float prog   = _breakShockwaveTimer / 0.4f;
            float radius = prog * 100f;
            int   alpha  = (int)(200 * (1f - prog));
            using (var pen = new Pen(Color.FromArgb(alpha, Color.OrangeRed), 3))
                g.DrawEllipse(pen,
                    _breakShockwaveWorldX - radius, _breakShockwaveWorldY - radius,
                    radius * 2f, radius * 2f);
        }

        private void DrawScreenFlashes(Graphics g, int W, int H)
        {
            if (_seaStoneFlashAlpha > 0f)
                using (var br = new SolidBrush(Color.FromArgb((int)_seaStoneFlashAlpha, 180, 160, 0)))
                    g.FillRectangle(br, 0, 0, W, H);

            if (_player.HasEffect(StatusEffect.Suppressed))
                using (var br = new SolidBrush(Color.FromArgb(28, 100, 90, 0)))
                    g.FillRectangle(br, 0, 0, W, H);

            if (_iceFlashTimer > 0f)
            {
                int alpha = (int)(120 * (1f - _iceFlashTimer / 0.3f));
                using (var br = new SolidBrush(Color.FromArgb(alpha, 180, 220, 255)))
                    g.FillRectangle(br, 0, 0, W, H);
            }

            if (_freezeFlashTimer > 0f)
            {
                int alpha = (int)(180 * (1f - _freezeFlashTimer / 0.45f));
                using (var br = new SolidBrush(Color.FromArgb(alpha, 200, 240, 255)))
                    g.FillRectangle(br, 0, 0, W, H);
            }
        }

        private void DrawHUD(Graphics g, int W, int H)
        {
            // Taller panel
            using (var br = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, 84);
            g.DrawLine(Pens.DimGray, 0, 84, W, 84);

            // Health bar
            g.DrawString("HP", _hudFont, Brushes.White, 8, 6);
            g.FillRectangle(Brushes.DarkRed, 50, 8, 200, 26);
            float hpPct = (float)_player.Health / _player.MaxHealth;
            using (var br = new SolidBrush(Color.LimeGreen))
                g.FillRectangle(br, 50, 8, (int)(200 * hpPct), 26);
            g.DrawRectangle(Pens.White, 50, 8, 200, 26);
            g.DrawString($"{_player.Health}/{_player.MaxHealth}", _infoFont, Brushes.White, 52, 38);

            // Ice reserve
            g.DrawString("ICE", _hudFont, Brushes.Cyan, 264, 6);
            g.FillRectangle(Brushes.DarkSlateBlue, 316, 8, 180, 26);
            float icePct = (float)_player.IceReserve / _player.MaxIceReserve;
            using (var br = new SolidBrush(Color.FromArgb(180, 220, 255)))
                g.FillRectangle(br, 316, 8, (int)(180 * icePct), 26);
            g.DrawRectangle(Pens.Cyan, 316, 8, 180, 26);
            g.DrawString($"{_player.IceReserve}/{_player.MaxIceReserve}", _infoFont, Brushes.Cyan, 318, 38);

            // Ability cooldowns
            DrawAbilityIcon(g, 510, 4, "Q:Wall",    _player.IceWallCooldownProgress,     _player.IsSuppressed);
            DrawAbilityIcon(g, 604, 4, "E:Freeze",  _player.FlashFreezeCooldownProgress,  _player.IsSuppressed);
            DrawAbilityIcon(g, 698, 4, "R:Break",   _player.BreakWallCooldownProgress,    false);

            // Status effect tags
            int ex = 800;
            if (_player.HasEffect(StatusEffect.Sinking))    DrawTag(g, ref ex, "SINKING",    Color.Blue);
            if (_player.HasEffect(StatusEffect.Suppressed)) DrawTag(g, ref ex, "SUPPRESSED",  Color.Olive);
            if (_player.HasEffect(StatusEffect.Burning))    DrawTag(g, ref ex, "BURNING",     Color.OrangeRed);
            if (_player.HasEffect(StatusEffect.Frozen))     DrawTag(g, ref ex, "FROZEN",      Color.LightBlue);

            // Island name + bounty (top-right)
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            {
                g.DrawString(_islandName,          f, Brushes.LightGray, W - 200, 6);
                g.DrawString(BountySystem.Formatted(), f, Brushes.Gold,   W - 260, 30);
                g.DrawString($"Berries: {_berriesCollected}", f, Brushes.Gold, W - 140, 54);
            }
            _combo.DrawHUD(g, W - 400, 56);

            // Pause button
            var pauseBtn = new System.Drawing.Rectangle(W - 90, 6, 78, 28);
            using (var br = new SolidBrush(Color.FromArgb(60, 200, 200, 255)))
                g.FillRectangle(br, pauseBtn);
            g.DrawRectangle(Pens.LightGray, pauseBtn);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("⏸ PAUSE", f, Brushes.White, W - 86, 11);
        }

        private void DrawAbilityIcon(Graphics g, int x, int y, string label, float progress, bool suppressed)
        {
            Color fill = suppressed ? Color.Gray : Color.FromArgb(50, Color.Cyan);
            using (var br = new SolidBrush(fill))
                g.FillRectangle(br, x, y, 90, 74);
            using (var br = new SolidBrush(Color.FromArgb(suppressed ? 40 : (int)(200 * progress), Color.Cyan)))
                g.FillRectangle(br, x, y + 74 - (int)(74 * progress), 90, (int)(74 * progress));
            g.DrawRectangle(Pens.Cyan, x, y, 90, 74);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString(label, f, suppressed ? Brushes.Gray : Brushes.White, x + 4, y + 26);
        }

        private void DrawTag(Graphics g, ref int x, string text, Color color)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, color)))
                g.FillRectangle(br, x, 8, 100, 20);
            using (var f = new Font("Courier New", 9, FontStyle.Bold))
                g.DrawString(text, f, Brushes.White, x + 3, 10);
            x += 104;
        }

        private void DrawRescuePrompt(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, Color.DarkBlue)))
                g.FillRectangle(br, W/2 - 160, H/2 - 30, 320, 60);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Finn is reaching for you!\nMash  SPACE/Z/X  to survive!", f, Brushes.Cyan, W/2 - 150, H/2 - 24);
        }

        private void DrawComplete(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 22, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("ISLAND CLEARED!", f);
                g.DrawString("ISLAND CLEARED!", f, Brushes.Gold, (W-sz.Width)/2f, H*0.38f);
            }
            using (var f = new Font("Courier New", 11))
            {
                g.DrawString($"+500 Bounty   +1 Crew Bond   Berries: {_berriesCollected}", f, Brushes.White,
                             (W - 220f)/2f, H*0.38f + 44);
                g.DrawString("Returning to overworld...", f, Brushes.LightGray,
                             (W - 220f)/2f, H*0.38f + 68);
            }
        }

        private sealed class HealthPickup
        {
            public float X, Y;
            public bool  Active = true;
            private float _bob;

            public HealthPickup(float x, float y) { X = x; Y = y; }

            public void Update(float dt) { _bob += dt; }

            public bool TryCollect(Player player)
            {
                if (!Active) return false;
                var area = new Rectangle((int)X - 12, (int)Y - 12, 24, 24);
                if (!area.IntersectsWith(player.Hitbox)) return false;
                Active = false;
                return true;
            }

            public void Draw(Graphics g)
            {
                if (!Active) return;
                float yOff = (float)Math.Sin(_bob * 3.5) * 2f;
                using (var br = new SolidBrush(Color.FromArgb(220, 220, 55, 55)))
                    g.FillEllipse(br, X - 11, Y - 11 + yOff, 22, 22);
                using (var pen = new Pen(Color.White, 1.5f))
                    g.DrawEllipse(pen, X - 11, Y - 11 + yOff, 22, 22);
                using (var pen = new Pen(Color.White, 3))
                {
                    g.DrawLine(pen, X - 6, Y + yOff, X + 6, Y + yOff);
                    g.DrawLine(pen, X, Y - 6 + yOff, X, Y + 6 + yOff);
                }
            }
        }
    }
}
