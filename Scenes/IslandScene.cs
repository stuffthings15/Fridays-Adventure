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
        private float _sinkSoundTimer;
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

        /// <summary>Running clock that drives animated environmental effects (snow, bubbles, torch flicker).</summary>
        private float _levelAnim;

        private Bitmap _bg;
        private Bitmap _uiPanel;
        private static readonly Font _hudFont  = new Font("Courier New", 16, FontStyle.Bold);
        private static readonly Font _infoFont = new Font("Courier New", 12, FontStyle.Bold);
        public IslandScene(string id, string name) { _islandId = id; _islandName = name; }

        public override void OnEnter()
        {
            BuildLevel();
            LoadBackground();
            _uiPanel = SpriteManager.Get("ui_panel.png");
            
            // PHASE 2 - Team 1: Game Director
            // Apply difficulty modifiers to all spawned enemies
            ApplyDifficultyModifiers();
            
            Game.Instance.Audio.ContinueOrPlay("island");  // Default island music
        }

        public override void OnExit()
        {
            _bg?.Dispose(); _bg = null;
        }

        /// <summary>
        /// PHASE 2 - Team 1: Game Director
        /// Apply difficulty modifiers to all enemies in the level
        /// </summary>
        private void ApplyDifficultyModifiers()
        {
            float healthMult = DifficultyModifiers.GetEnemyHealthMultiplier();
            
            // Only apply if not normal difficulty
            if (Math.Abs(healthMult - 1.0f) > 0.001f)
            {
                foreach (var enemy in _enemies)
                {
                    // Apply health multiplier to enemies
                    int newMaxHp = (int)(enemy.MaxHealth * healthMult);
                    enemy.MaxHealth = newMaxHp;
                    // Reset health to new max
                    enemy.Health = newMaxHp;
                }
            }
        }

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
                case "wano":   BuildBladeNation(); break;
                case "harbor": BuildHarborTown();  break;
                case "coral":  BuildCoralReef();   break;
                case "tundra": BuildTundraPeak();  break;
                default:       BuildDinoIsland();  break;
            }

            // Player (selected from Crew screen)
            _player = new Player(50, _groundY - 56);
            _player.ApplySelectedSprite();

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

        private void BuildHarborTown()
        {
            // Intro hub — lighter opposition, mixed hazards, meets Orca
            _levelWidth = 2200;

            _platforms.Add(new Rectangle(0,    _groundY, 480,  160));
            _platforms.Add(new Rectangle(560,  _groundY, 440,  160));
            _platforms.Add(new Rectangle(1080, _groundY, 500,  160));
            _platforms.Add(new Rectangle(1660, _groundY, 540,  160));
            _platforms.Add(new Rectangle(280,  _groundY - 100, 180, 20));
            _platforms.Add(new Rectangle(750,  _groundY - 120, 200, 20));
            _platforms.Add(new Rectangle(1300, _groundY - 140, 180, 20));

            _hazards.Add(new WaterPit(480,  _groundY, 80,  160));
            _hazards.Add(new WaterPit(1000, _groundY, 80,  160));
            _hazards.Add(new WaterPit(1580, _groundY, 80,  160));
            _hazards.Add(new SeaStoneZone(900,  _groundY - 50, 160, 50));
            _hazards.Add(new SeaStoneZone(1400, _groundY - 50, 140, 50));

            SpawnEnemy(180,  _groundY - 42, 0.7f);
            SpawnEnemy(450,  _groundY - 42, 0.7f);
            SpawnEnemy(850,  _groundY - 42, 0.9f);
            SpawnEnemy(1150, _groundY - 42, 1.0f);
            SpawnEnemy(1500, _groundY - 42, 1.1f);
            SpawnEnemy(1850, _groundY - 48, 1.2f, isBoss: true, hp: 140);

            _exitFlag = new Rectangle(2100, _groundY - 52, 30, 52);
        }

        private void BuildCoralReef()
        {
            // SeaStone-heavy underwater cavern — suppresses ice powers, meets Swan
            _levelWidth = 3000;

            _platforms.Add(new Rectangle(0,    _groundY, 500,  160));
            _platforms.Add(new Rectangle(600,  _groundY, 460,  160));
            _platforms.Add(new Rectangle(1160, _groundY, 480,  160));
            _platforms.Add(new Rectangle(1740, _groundY, 520,  160));
            _platforms.Add(new Rectangle(2360, _groundY, 640,  160));
            _platforms.Add(new Rectangle(320,  _groundY - 100, 180, 20));
            _platforms.Add(new Rectangle(880,  _groundY - 120, 200, 20));
            _platforms.Add(new Rectangle(1460, _groundY - 140, 180, 20));
            _platforms.Add(new Rectangle(2050, _groundY - 160, 200, 20));

            _hazards.Add(new WaterPit(500,  _groundY, 100, 160));
            _hazards.Add(new WaterPit(1060, _groundY, 100, 160));
            _hazards.Add(new WaterPit(1640, _groundY, 100, 160));
            _hazards.Add(new WaterPit(2260, _groundY, 100, 160));
            _hazards.Add(new SeaStoneZone(620,  _groundY - 55, 240, 55));
            _hazards.Add(new SeaStoneZone(1180, _groundY - 55, 220, 55));
            _hazards.Add(new SeaStoneZone(1760, _groundY - 55, 260, 55));
            _hazards.Add(new SeaStoneZone(2380, _groundY - 55, 200, 55));
            _hazards.Add(new FireSource(1000, _groundY - 48, 40, 48));
            _hazards.Add(new FireSource(2100, _groundY - 48, 40, 48));

            SpawnEnemy(200,  _groundY - 42, 1.0f, hp: 70);
            SpawnEnemy(480,  _groundY - 42, 1.0f, hp: 70);
            SpawnEnemy(900,  _groundY - 42, 1.2f, hp: 90);
            SpawnEnemy(1250, _groundY - 42, 1.2f, hp: 90);
            SpawnEnemy(1600, _groundY - 42, 1.4f, hp: 110);
            SpawnEnemy(1950, _groundY - 42, 1.4f, hp: 110);
            SpawnEnemy(2550, _groundY - 48, 1.6f, isBoss: true, hp: 160);

            _exitFlag = new Rectangle(2900, _groundY - 52, 30, 52);
        }

        private void BuildTundraPeak()
        {
            // Volcanic-frost mountain — fire vents erupt through ice, elite marines
            _levelWidth = 2600;

            _platforms.Add(new Rectangle(0,    _groundY, 460,  160));
            _platforms.Add(new Rectangle(540,  _groundY, 500,  160));
            _platforms.Add(new Rectangle(1120, _groundY, 480,  160));
            _platforms.Add(new Rectangle(1700, _groundY, 900,  160));
            _platforms.Add(new Rectangle(260,  _groundY - 140, 180, 20));
            _platforms.Add(new Rectangle(760,  _groundY - 160, 200, 20));
            _platforms.Add(new Rectangle(1320, _groundY - 180, 160, 20));
            _platforms.Add(new Rectangle(1900, _groundY - 200, 260, 20));

            _hazards.Add(new WaterPit(460,  _groundY, 80,  160));
            _hazards.Add(new WaterPit(1040, _groundY, 80,  160));
            _hazards.Add(new SeaStoneZone(1720, _groundY - 60, 240, 60));
            _hazards.Add(new SeaStoneZone(2100, _groundY - 60, 200, 60));
            for (int fx = 120; fx < 2500; fx += 380)
                _hazards.Add(new FireSource(fx, _groundY - 48, 36, 48));

            SpawnEnemy(220,  _groundY - 42, 1.2f, hp: 90);
            SpawnEnemy(600,  _groundY - 42, 1.2f, hp: 90);
            SpawnEnemy(1000, _groundY - 42, 1.4f, hp: 110);
            SpawnEnemy(1300, _groundY - 42, 1.5f, hp: 120);
            SpawnEnemy(1850, _groundY - 42, 1.6f, hp: 130);
            SpawnEnemy(2300, _groundY - 48, 1.8f, isBoss: true, hp: 180);

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

            // Assign island-appropriate enemy model from Assets\Sprites\
            string standardSprite;
            string bossSprite;
            switch (_islandId)
            {
                case "dino":
                    standardSprite = "enemy_Raptor_Marauder.png";
                    bossSprite     = "enemy_Triceratops_Brute.png";
                    break;
                case "wano":
                    standardSprite = "enemy_Ronin_Enforcer.png";
                    bossSprite     = "enemy_Oni_Ashigaru.png";
                    break;
                case "tundra":
                    standardSprite = "enemy_Thunder_Mask_Priest.png";
                    bossSprite     = "boss_Garp.png";
                    break;
                default:
                    standardSprite = "enemy_Garp.png";
                    bossSprite     = "boss_Garp.png";
                    break;
            }

            string spriteFile = isBoss ? bossSprite : standardSprite;
            var loaded = SpriteManager.GetScaled(spriteFile, e.Width, e.Height);
            if (loaded != null) e.Sprite = loaded;

            _enemies.Add(e);
        }

        private void SpawnBerries()
        {
            switch (_islandId)
            {
                case "wano":   SpawnBladeBerries();  break;
                case "harbor": SpawnHarborBerries(); break;
                case "coral":  SpawnCoralBerries();  break;
                case "tundra": SpawnTundraBerries(); break;
                default:       SpawnDinoBerries();   break;
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

        private void SpawnHarborBerries()
        {
            AddBerryRow(300,  _groundY - 122, 3, 40);
            AddBerryRow(780,  _groundY - 142, 3, 50);
            AddBerryRow(1320, _groundY - 162, 3, 40);
            AddBerry(510, _groundY - 60); AddBerry(535, _groundY - 80); AddBerry(560, _groundY - 60);
            AddBerry(1030, _groundY - 70); AddBerry(1055, _groundY - 70);
        }

        private void SpawnCoralBerries()
        {
            AddBerryRow(340,  _groundY - 122, 3, 50);
            AddBerryRow(910,  _groundY - 142, 3, 50);
            AddBerryRow(1490, _groundY - 162, 3, 50);
            AddBerryRow(2080, _groundY - 182, 4, 50);
            AddBerry(550, _groundY - 65); AddBerry(580, _groundY - 80);
            AddBerry(1110, _groundY - 70); AddBerry(1140, _groundY - 70);
        }

        private void SpawnTundraBerries()
        {
            AddBerryRow(280,  _groundY - 162, 3, 50);
            AddBerryRow(790,  _groundY - 182, 3, 50);
            AddBerryRow(1350, _groundY - 202, 3, 40);
            AddBerryRow(1930, _groundY - 222, 4, 50);
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
                case "wano":   SpawnBladeHealthPickups();  break;
                case "harbor": SpawnHarborHealthPickups(); break;
                case "coral":  SpawnCoralHealthPickups();  break;
                case "tundra": SpawnTundraHealthPickups(); break;
                default:       SpawnDinoHealthPickups();   break;
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

        private void SpawnHarborHealthPickups()
        {
            _healthPickups.Add(new HealthPickup(290,  _groundY - 112));
            _healthPickups.Add(new HealthPickup(1310, _groundY - 152));
        }

        private void SpawnCoralHealthPickups()
        {
            _healthPickups.Add(new HealthPickup(340,  _groundY - 112));
            _healthPickups.Add(new HealthPickup(1480, _groundY - 152));
            _healthPickups.Add(new HealthPickup(2080, _groundY - 172));
        }

        private void SpawnTundraHealthPickups()
        {
            _healthPickups.Add(new HealthPickup(280,  _groundY - 152));
            _healthPickups.Add(new HealthPickup(1350, _groundY - 192));
            _healthPickups.Add(new HealthPickup(1940, _groundY - 212));
        }

        private void LoadBackground()
        {
            _bg = LoadBackgroundForIsland(_islandId);
        }

        /// <summary>
        /// Loads the appropriate background sprite for the island ID.
        /// Maps island IDs to their corresponding background PNG files in Assets/Sprites.
        /// PHASE 2 - Team 14: Environment Artist (Level-Specific Backgrounds)
        /// </summary>
        private Bitmap LoadBackgroundForIsland(string islandId)
        {
            // All background images live in Assets\Sprites\ — use their exact filenames
            string bgFileName;
            switch (islandId)
            {
                case "dino":         bgFileName = "bg_Dinosaur_Island.png";      break;
                case "sky":          bgFileName = "bg_Ancient_ruins_island.png"; break;
                case "wano":         bgFileName = "bg_Blade_Nation.png";          break;
                case "harbor":       bgFileName = "bg_Harbor_Town.png";           break;
                case "coral":        bgFileName = "bg_Coral_Reef.png";            break;
                case "tundra":       bgFileName = "bg_Tundra_Peak.png";           break;
                case "dive_gate":    bgFileName = "bg_Dive_Gate.png";             break;
                case "sunken_gate":  bgFileName = "bg_Sunken_Gate.png";           break;
                case "kelp":         bgFileName = "bg_Kelp_Maze.png";             break;
                case "boiling_vent": bgFileName = "bg_Vent_Ruins.png";            break;
                case "abyss":        bgFileName = "bg_Abyss.png";                 break;
                default:             bgFileName = "bg_Dinosaur_Island.png";       break;
            }

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                       "Assets", "Sprites", bgFileName);

            if (File.Exists(path))
            {
                try { return new Bitmap(path); }
                catch { return null; }
            }

            return null;
        }

        // ── Update ──────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            _levelAnim += dt;   // drives environmental animations regardless of game state
            if (_introActive)   { UpdateIntro(dt); return; }
            if (_levelComplete) { UpdateComplete(dt); return; }

            var input = Game.Instance.Input;

            HandleInput(input, dt);
            _player.Update(dt);
            MoveAndCollide(_player, dt);
            foreach (var hz in _hazards) hz.Update(dt);
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
            if (!_player.HasEffect(StatusEffect.Sinking)) { _sinkMashTimer = 0; _showRescue = false; _sinkSoundTimer = 0; return; }
            _sinkMashTimer  += dt;
            _sinkSoundTimer += dt;
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
            if (_sinkSoundTimer >= 1f)
            {
                Game.Instance.Audio.BeepSink();
                _sinkSoundTimer = 0;
            }
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

                // Head stomp — land on enemy to eliminate + bounce
                if (_player.VelocityY > 0 && !_player.IsInvincible)
                {
                    float pBot = _player.Y + _player.Height;
                    float eTop = e.Y;
                    float overlap = pBot - eTop;
                    if (overlap > 0 && overlap < 22 &&
                        _player.CenterX > e.X - 8 && _player.CenterX < e.X + e.Width + 8)
                    {
                        e.Health = 0;
                        _player.VelocityY = _player.JumpForce * 0.45f;
                        _player.IsGrounded = false;
                        Game.Instance.Audio.BeepStomp();
                        BountySystem.Award(e.ScoreValue);
                        Game.Instance.TotalBerriesCollected += 10;
                        stomped = true;
                    }
                }

                // Regular attack
                if (pAtk != Rectangle.Empty && pAtk.IntersectsWith(e.Hitbox))
                {
                    bool wasAlive = e.IsAlive;
                    e.TakeDamage(_player.AttackDamage);
                    if (wasAlive && !e.IsAlive)
                    {
                        BountySystem.Award(e.ScoreValue);
                        Game.Instance.TotalBerriesCollected += 10;
                    }
                }

                // Horizontal body contact
                if (!stomped && e.IsAlive && !_player.IsInvincible &&
                    _player.Hitbox.IntersectsWith(e.Hitbox))
                {
                    int healthBefore = _player.Health;
                    _player.TakeDamage(_player.MaxHealth / 10);
                    if (_player.Health < healthBefore) Game.Instance.Audio.BeepHurt();
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
                    Game.Instance.TotalBerriesCollected++;
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
            float freezeSeconds = _player.GetFlashFreezeDuration(2.5f);
            foreach (var e in _enemies)
                if (e.IsAlive && _player.DistanceTo(e) <= 130f)
                    e.ApplyEffect(StatusEffect.Frozen, freezeSeconds);
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
                    e.TakeDamage(_player.BreakWallShockwaveDamage);
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
                // SMB3-style level-clear chime — fire once on goal touch
                Game.Instance.Audio.BeepLevelClear();
            }
        }

        private void UpdateComplete(float dt)
        {
            _completeTimer += dt;
            if (_completeTimer >= 3.5f)
            {
                // Signal that this level was cleared so the overworld can advance CurrentLevel
                Game.Instance.LevelJustCompleted = true;
                Game.Instance.Scenes.Pop();
            }
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
            // Pass a retry factory so the defeat screen can offer "Try Again"
            string id = _islandId, name = _islandName;
            Game.Instance.Scenes.Replace(new GameOverScene(() => new IslandScene(id, name)));
        }

        public override void HandleClick(System.Drawing.Point p)
        {
            if (HandleDevMenuClick(p)) return;
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
            DrawLevelAtmosphere(g, W, H);   // per-level mood overlay (coral tint, aurora, heat, canopy)

            g.TranslateTransform(-_cameraX, 0);

            DrawPlatforms(g, H);
            DrawLevelDecors(g);             // world-space props on/around platforms
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

            DrawSnowfall(g, W, H);          // tundra: screen-space falling snow
            DrawBubbles(g, W, H);           // coral: screen-space rising bubbles
            DrawScreenFlashes(g, W, H);
            // ── SMB3 HUD: All UI elements (lives, score, coin, abilities, P-meter) ──
            SMB3Hud.DrawAll(g, _player, null, W, H);
            if (_showRescue)    DrawRescuePrompt(g, W, H);
            if (_levelComplete) DrawComplete(g, W, H);
            DrawDevMenuButton(g);
        }

        private void DrawBackground(Graphics g, int W, int H)
        {
            if (_bg != null) { g.DrawImage(_bg, 0, 0, W, H); return; }

            // SMB3-style sky blue gradient fallback — bright and readable
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(92, 148, 252),   // SMB3 sky blue top
                Color.FromArgb(180, 210, 255),  // lighter horizon
                90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Distant cloud bands (SMB3 background clouds are simple white ellipses)
            using (var br = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
            {
                g.FillEllipse(br, W * 0.05f, H * 0.35f, 180, 50);
                g.FillEllipse(br, W * 0.30f, H * 0.25f, 220, 55);
                g.FillEllipse(br, W * 0.58f, H * 0.32f, 200, 48);
                g.FillEllipse(br, W * 0.78f, H * 0.20f, 160, 44);
            }
        }

        private void DrawPlatforms(Graphics g, int H)
        {
            // ── Per-island SMB3 tile palette ─────────────────────────────────
            Color baseCol, topCol, brickCol;
            switch (_islandId)
            {
                case "wano":
                    // Stone/samurai dojo: slate gray blocks with darker mortar lines
                    baseCol  = Color.FromArgb(90,  80,  70);
                    topCol   = Color.FromArgb(60,  70,  80);
                    brickCol = Color.FromArgb(50,  55,  60);
                    break;
                case "sky":
                    // Cloud island: near-white blocks, sky-tinted edges
                    baseCol  = Color.FromArgb(200, 200, 220);
                    topCol   = Color.FromArgb(240, 240, 255);
                    brickCol = Color.FromArgb(180, 185, 210);
                    break;
                case "harbor":
                    // Harbour docks: warm golden-brown wooden planks
                    baseCol  = Color.FromArgb(145,  95,  48);
                    topCol   = Color.FromArgb(188, 138,  68);
                    brickCol = Color.FromArgb(105,  62,  22);
                    break;
                case "coral":
                case "sunken_gate":
                case "kelp":
                case "boiling_vent":
                case "abyss":
                    // Deep ocean: midnight blue with teal-lit highlights
                    baseCol  = Color.FromArgb(18,  52,  96);
                    topCol   = Color.FromArgb(22, 138, 152);
                    brickCol = Color.FromArgb(10,  33,  65);
                    break;
                case "tundra":
                    // Frozen mountain: icy white blocks with crystalline blue edges
                    baseCol  = Color.FromArgb(195, 215, 245);
                    topCol   = Color.FromArgb(235, 248, 255);
                    brickCol = Color.FromArgb(155, 180, 225);
                    break;
                default:
                    // Dino / jungle: warm SMB3 tan-brown earth with bright grass top
                    baseCol  = Color.FromArgb(210, 160,  80);
                    topCol   = Color.FromArgb(60,  170,  50);
                    brickCol = Color.FromArgb(155, 100,  40);
                    break;
            }

            foreach (var p in _platforms)
            {
                // Base fill
                using (var br = new SolidBrush(baseCol))
                    g.FillRectangle(br, p);

                // SMB3 brick grid lines — only on thick ground blocks, not thin air platforms
                if (p.Height > 16)
                {
                    using (var pen = new Pen(Color.FromArgb(55, brickCol), 1))
                    {
                        // Vertical mortar lines every 32 px
                        for (int tx = p.Left + 32; tx < p.Right; tx += 32)
                            g.DrawLine(pen, tx, p.Top, tx, p.Bottom);
                        // Horizontal mortar lines every 16 px
                        for (int ty = p.Top + 16; ty < p.Bottom; ty += 16)
                            g.DrawLine(pen, p.Left, ty, p.Right, ty);
                    }
                }

                // Grass/surface top strip
                using (var br = new SolidBrush(topCol))
                    g.FillRectangle(br, p.X, p.Y, p.Width, 6);
                // Bright highlight at the very top edge (SMB3 crisp top line)
                using (var br = new SolidBrush(Color.FromArgb(90, 255, 255, 255)))
                    g.FillRectangle(br, p.X, p.Y, p.Width, 2);
            }
        }

        private void DrawExitFlag(Graphics g)
        {
            int px = _exitFlag.X + 14;
            int top = _exitFlag.Y;
            int bottom = _exitFlag.Y + _exitFlag.Height;

            // ── SMB3-style goal flagpole ──────────────────────────────────────
            // Pole (silver/gray vertical bar)
            using (var br = new SolidBrush(Color.FromArgb(200, 200, 210)))
                g.FillRectangle(br, px, top, 4, _exitFlag.Height);
            // Pole highlight
            using (var br = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                g.FillRectangle(br, px, top, 2, _exitFlag.Height);
            // Gold ball on top
            using (var br = new SolidBrush(Color.Gold))
                g.FillEllipse(br, px - 4, top - 9, 12, 12);
            using (var pen = new Pen(Color.DarkGoldenrod, 1))
                g.DrawEllipse(pen, px - 4, top - 9, 12, 12);

            // ── Checkered goal flag (SMB3 two-tone green) ────────────────────
            int fw = 24, fh = 16;
            int fx = px + 4;
            using (var br = new SolidBrush(Color.FromArgb(40, 170, 40)))
                g.FillRectangle(br, fx, top, fw, fh);
            // Checker squares
            using (var br = new SolidBrush(Color.FromArgb(28, 120, 28)))
            {
                g.FillRectangle(br, fx,           top,        fw / 2, fh / 2);
                g.FillRectangle(br, fx + fw / 2,  top + fh / 2, fw / 2, fh / 2);
            }
            // "GOAL" label
            using (var f = new Font("Courier New", 7, FontStyle.Bold))
                g.DrawString("GOAL", f, Brushes.White, fx + 2, top + 4);
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
            // ── Solid dark HUD bar ────────────────────────────────────────────
            g.FillRectangle(Brushes.Black, 0, 0, W, 84);
            g.DrawLine(Pens.DimGray, 0, 84, W, 84);

            // ── Mega Man-style segmented HP bar ───────────────────────────────
            g.DrawString("HP", _hudFont, Brushes.White, 8, 6);
            {
                const int segs = 20, segW = 9, segH = 26, gap = 1;
                float hpPct = (float)_player.Health / _player.MaxHealth;
                int filled = Math.Min(segs, (int)(hpPct * segs + 0.99f)); // ceil for partial last seg
                for (int i = 0; i < segs; i++)
                {
                    int sx = 50 + i * (segW + gap);
                    // Color shifts red → yellow → green as HP rises (Mega Man bar convention)
                    Color segFill = i < segs / 4 ? Color.OrangeRed
                                  : i < segs / 2 ? Color.Gold
                                  :                Color.LimeGreen;
                    Color segEmpty = Color.FromArgb(50, 0, 0);
                    using (var br = new SolidBrush(i < filled ? segFill : segEmpty))
                        g.FillRectangle(br, sx, 8, segW, segH);
                    // Thin highlight on top of filled segments
                    if (i < filled)
                        using (var br = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                            g.FillRectangle(br, sx, 8, segW, 3);
                }
                g.DrawRectangle(Pens.White, 50, 8, segs * (segW + gap) - gap, segH);
                g.DrawString($"{_player.Health}/{_player.MaxHealth}", _infoFont, Brushes.White, 52, 38);
            }

            // ── Mega Man-style segmented ICE / weapon energy bar ──────────────
            g.DrawString("ICE", _hudFont, Brushes.Cyan, 264, 6);
            {
                const int segs = 18, segW = 9, segH = 26, gap = 1;
                float icePct = (float)_player.IceReserve / _player.MaxIceReserve;
                int filled = Math.Min(segs, (int)(icePct * segs + 0.99f));
                for (int i = 0; i < segs; i++)
                {
                    int sx = 316 + i * (segW + gap);
                    Color segFill  = Color.FromArgb(110, 200, 255);
                    Color segEmpty = Color.FromArgb(20,  40,  70);
                    using (var br = new SolidBrush(i < filled ? segFill : segEmpty))
                        g.FillRectangle(br, sx, 8, segW, segH);
                    if (i < filled)
                        using (var br = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                            g.FillRectangle(br, sx, 8, segW, 3);
                }
                g.DrawRectangle(Pens.Cyan, 316, 8, segs * (segW + gap) - gap, segH);
                g.DrawString($"{_player.IceReserve}/{_player.MaxIceReserve}", _infoFont, Brushes.Cyan, 318, 38);
            }

            // ── Ability cooldown panels (Mega Man sub-weapon bars) ─────────────
            DrawAbilityIcon(g, 510, 4, "Q:Wall",   _player.IceWallCooldownProgress,      _player.IsSuppressed, _player.IceWallCooldownRemaining);
            DrawAbilityIcon(g, 604, 4, "E:Freeze", _player.FlashFreezeCooldownProgress,  _player.IsSuppressed, _player.FlashFreezeCooldownRemaining);
            DrawAbilityIcon(g, 698, 4, "R:Break",  _player.BreakWallCooldownProgress,    false,                _player.BreakWallCooldownRemaining);

            // ── Status effect tags ────────────────────────────────────────────
            int ex = 800;
            if (_player.HasEffect(StatusEffect.Sinking))    DrawTag(g, ref ex, "SINKING",    Color.Blue);
            if (_player.HasEffect(StatusEffect.Suppressed)) DrawTag(g, ref ex, "SUPPRESSED",  Color.Olive);
            if (_player.HasEffect(StatusEffect.Burning))    DrawTag(g, ref ex, "BURNING",     Color.OrangeRed);
            if (_player.HasEffect(StatusEffect.Frozen))     DrawTag(g, ref ex, "FROZEN",      Color.LightBlue);

            // ── Island name + SMB3 coin + score (top-right) ───────────────────
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            {
                g.DrawString(_islandName, f, Brushes.LightGray, W - 200, 6);

                // SMB3 coin icon before score
                using (var br = new SolidBrush(Color.FromArgb(255, 220, 0)))
                    g.FillEllipse(br, W - 324, 33, 12, 12);
                using (var br = new SolidBrush(Color.FromArgb(200, 150, 0)))
                    g.FillEllipse(br, W - 321, 36, 6, 6);

                g.DrawString($"SCORE: {BountySystem.Formatted()}", f, Brushes.Gold, W - 308, 30);
                g.DrawString($"Berries: {Game.Instance.TotalBerriesCollected}", f, Brushes.Gold, W - 140, 54);
            }
            _combo.DrawHUD(g, W - 400, 56);

            // ── Pause button ──────────────────────────────────────────────────
            var pauseBtn = new System.Drawing.Rectangle(W - 90, 6, 78, 28);
            using (var br = new SolidBrush(Color.FromArgb(60, 200, 200, 255)))
                g.FillRectangle(br, pauseBtn);
            g.DrawRectangle(Pens.LightGray, pauseBtn);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("⏸ PAUSE", f, Brushes.White, W - 86, 11);
        }

        private void DrawAbilityIcon(Graphics g, int x, int y, string label, float progress, bool suppressed, float remainingSecs)
        {
            // Solid background — always visible regardless of charge level
            Color bgColor = suppressed ? Color.FromArgb(90, Color.DarkSlateGray) : Color.FromArgb(90, 0, 50, 70);
            using (var br = new SolidBrush(bgColor))
                g.FillRectangle(br, x, y, 90, 74);

            // Charge fill — opaque so it's readable even at 1%
            if (!suppressed && progress > 0f)
            {
                bool ready = progress >= 1f;
                Color fillColor = ready ? Color.Cyan : Color.SteelBlue;
                using (var br = new SolidBrush(Color.FromArgb(ready ? 180 : 120, fillColor)))
                    g.FillRectangle(br, x, y + 74 - (int)(74 * progress), 90, (int)(74 * progress));
            }

            g.DrawRectangle(suppressed ? Pens.Gray : Pens.Cyan, x, y, 90, 74);

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString(label, f, suppressed ? Brushes.Gray : Brushes.White, x + 4, y + 26);

            // Countdown row
            using (var f = new Font("Courier New", 9, FontStyle.Bold))
            {
                if (suppressed)
                    g.DrawString("--", f, Brushes.DimGray, x + 32, y + 54);
                else if (remainingSecs > 0.05f)
                    g.DrawString($"{remainingSecs:F1}s", f, Brushes.Yellow, x + 24, y + 54);
                else
                    g.DrawString("READY", f, Brushes.LimeGreen, x + 10, y + 54);
            }
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
            // ── SMB3 end-card dark overlay ────────────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);

            // Gold card panel (SMB3 score tally card)
            int cardW = 460, cardH = 140;
            int cx = (W - cardW) / 2, cy = (int)(H * 0.30f);
            using (var br = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                g.FillRectangle(br, cx, cy, cardW, cardH);
            using (var pen = new Pen(Color.Gold, 3))
                g.DrawRectangle(pen, cx, cy, cardW, cardH);

            // ── "ISLAND CLEARED!" heading ─────────────────────────────────────
            using (var f = new Font("Courier New", 22, FontStyle.Bold))
            {
                // Black drop shadow
                SizeF sz = g.MeasureString("ISLAND CLEARED!", f);
                float tx = (W - sz.Width) / 2f, ty = cy + 14;
                g.DrawString("ISLAND CLEARED!", f, Brushes.Black,  tx + 2, ty + 2);
                g.DrawString("ISLAND CLEARED!", f, Brushes.Gold,   tx,     ty);
            }

            // ── SMB3-style star/coin tally below heading ───────────────────────
            using (var f = new Font("Courier New", 11))
            {
                int ry = cy + 68;
                // Draw a small coin icon before the reward line
                using (var br = new SolidBrush(Color.FromArgb(255, 220, 0)))
                    g.FillEllipse(br, cx + 18, ry + 2, 11, 11);
                using (var br = new SolidBrush(Color.FromArgb(200, 150, 0)))
                    g.FillEllipse(br, cx + 21, ry + 5, 5, 5);

                g.DrawString($"+500 Bounty   +1 Crew Bond   Berries: {_berriesCollected}",
                             f, Brushes.White, cx + 34, ry);
                g.DrawString("Returning to overworld...",
                             f, Brushes.LightGray, cx + 90, ry + 26);
            }
        }

        // ── Level differentiation: atmosphere, props, and weather ────────────

        /// <summary>
        /// Draws screen-space atmospheric overlays that set each level's mood.
        /// Called after the background but before the world camera transform.
        /// </summary>
        private void DrawLevelAtmosphere(Graphics g, int W, int H)
        {
            switch (_islandId)
            {
                case "coral":
                case "sunken_gate":
                case "kelp":
                case "boiling_vent":
                case "abyss":
                    // Underwater: semi-transparent blue wash + animated caustic light shafts
                    using (var br = new SolidBrush(Color.FromArgb(50, 0, 55, 145)))
                        g.FillRectangle(br, 0, 0, W, H);
                    for (int i = 0; i < 6; i++)
                    {
                        int sx = (int)(W * (0.08f + i * 0.16f) + Math.Sin(_levelAnim * 0.45f + i * 1.1f) * 38f);
                        using (var br = new SolidBrush(Color.FromArgb(14, 75, 190, 255)))
                            g.FillRectangle(br, sx, 0, 30, H);
                    }
                    break;

                case "tundra":
                    // Aurora borealis: shimmering horizontal ribbons near the top of the screen
                    Color[] auroraColors =
                    {
                        Color.FromArgb(35,   0, 230, 180),
                        Color.FromArgb(30,  60, 110, 255),
                        Color.FromArgb(28, 180,  60, 255),
                        Color.FromArgb(32,   0, 200, 100),
                        Color.FromArgb(25, 100, 200, 255)
                    };
                    for (int band = 0; band < 5; band++)
                    {
                        int bx    = (int)(Math.Sin(_levelAnim * 0.28f + band * 0.9f) * W * 0.18f);
                        int alpha = (int)(auroraColors[band].A + 20 * Math.Sin(_levelAnim * 0.9f + band * 1.5f));
                        alpha = Math.Max(0, Math.Min(60, alpha));
                        using (var br = new SolidBrush(Color.FromArgb(alpha,
                                auroraColors[band].R, auroraColors[band].G, auroraColors[band].B)))
                            g.FillRectangle(br, bx, band * 22, W + 200, 26);
                    }
                    break;

                case "wano":
                    // Heat shimmer: warm orange glow near the ground rising up
                    using (var br = new LinearGradientBrush(
                            new Rectangle(0, H - 150, W, 150),
                            Color.FromArgb(58, 220, 50, 0),
                            Color.FromArgb(0,  220, 50, 0), 90f))
                        g.FillRectangle(br, 0, H - 150, W, 150);
                    break;

                case "dino":
                default:
                    // Jungle canopy: dense green shadow falls from the top
                    using (var br = new LinearGradientBrush(
                            new Rectangle(0, 0, W, 130),
                            Color.FromArgb(80, 10, 85, 0),
                            Color.FromArgb(0,  10, 85, 0), 90f))
                        g.FillRectangle(br, 0, 0, W, 130);
                    break;
            }
        }

        /// <summary>
        /// Draws world-space decorative props that are unique to each level's environment.
        /// Called inside the camera transform so props scroll naturally with the level.
        /// </summary>
        private void DrawLevelDecors(Graphics g)
        {
            switch (_islandId)
            {
                case "tundra":
                    DrawIcicles(g);
                    break;
                case "coral":
                case "sunken_gate":
                case "kelp":
                case "boiling_vent":
                case "abyss":
                    DrawCoralFormations(g);
                    break;
                case "wano":
                    DrawWanoTorches(g);
                    break;
                case "harbor":
                    DrawHarborPosts(g);
                    break;
                default:    // dino / jungle
                    DrawJungleFoliage(g);
                    break;
            }
        }

        /// <summary>
        /// Draws icicle spikes hanging from the underside of thin tundra platforms.
        /// </summary>
        private void DrawIcicles(Graphics g)
        {
            using (var fill = new SolidBrush(Color.FromArgb(205, 215, 245, 255)))
            using (var edge = new Pen(Color.FromArgb(130, 165, 210, 255), 1))
            {
                foreach (var p in _platforms)
                {
                    if (p.Height > 20) continue;    // only on thin floating platforms
                    int count = p.Width / 18;
                    for (int i = 0; i < count; i++)
                    {
                        int ix = p.Left + 9 + i * 18;
                        int ih = 7 + (i % 3) * 6;  // vary icicle length for realism
                        var pts = new PointF[]
                        {
                            new PointF(ix - 4, p.Bottom),
                            new PointF(ix + 4, p.Bottom),
                            new PointF(ix,     p.Bottom + ih)
                        };
                        g.FillPolygon(fill, pts);
                        g.DrawPolygon(edge, pts);
                    }
                }
            }
        }

        /// <summary>
        /// Draws bioluminescent coral clusters at the surface edge of ocean platforms.
        /// </summary>
        private void DrawCoralFormations(Graphics g)
        {
            var rng = new Random(12345);    // fixed seed so positions are stable each frame
            foreach (var p in _platforms)
            {
                if (p.Height <= 20) continue;   // only on thick ground blocks
                for (int cx = p.Left + 12; cx < p.Right - 12; cx += 38 + rng.Next(18))
                {
                    // Pick a coral colour: pink, teal, or pale green
                    int pick = rng.Next(3);
                    Color c = pick == 0 ? Color.FromArgb(210, 255, 70, 130)
                            : pick == 1 ? Color.FromArgb(190,  80, 215, 255)
                            :             Color.FromArgb(175, 120, 255,  80);
                    int ch = 9 + rng.Next(14);
                    int cw = 5 + rng.Next(6);
                    using (var br = new SolidBrush(c))
                        g.FillEllipse(br, cx - cw, p.Top - ch, cw * 2, ch);
                    // Thin stem
                    using (var pen = new Pen(Color.FromArgb(90, 50, 110, 60), 2))
                        g.DrawLine(pen, cx, p.Top, cx, p.Top - ch + 3);
                }
            }
        }

        /// <summary>
        /// Draws animated fire torches on the sides of Blade Nation platforms.
        /// </summary>
        private void DrawWanoTorches(Graphics g)
        {
            foreach (var p in _platforms)
            {
                if (p.Height <= 20) continue;
                // One torch bracket near each end of every thick platform
                DrawTorch(g, p.Left + 14, p.Top - 26);
                DrawTorch(g, p.Right - 22, p.Top - 26);
            }
        }

        /// <summary>
        /// Renders a single animated torch (bracket post + flickering flame).
        /// </summary>
        private void DrawTorch(Graphics g, int x, int y)
        {
            // Iron bracket
            using (var br = new SolidBrush(Color.FromArgb(100, 70, 40)))
                g.FillRectangle(br, x, y, 8, 18);

            // Animated flame — flickers using the level animation clock
            float flicker = (float)Math.Sin(_levelAnim * 8.1f + x * 0.06f);
            int fw = (int)(8 + flicker * 3);
            int fh = (int)(13 + Math.Abs(flicker) * 5);
            using (var outer = new SolidBrush(Color.FromArgb(210, 255, 110,  0)))
                g.FillEllipse(outer, x - fw / 2 + 4, y - fh, fw, fh);
            using (var inner = new SolidBrush(Color.FromArgb(240, 255, 220,  0)))
                g.FillEllipse(inner, x + 1, y - fh + 4, 5, fh - 5);
        }

        /// <summary>
        /// Draws wooden dock posts with rope railings along harbor platforms.
        /// </summary>
        private void DrawHarborPosts(Graphics g)
        {
            using (var postBr = new SolidBrush(Color.FromArgb(110, 65, 25)))
            using (var rope   = new Pen(Color.FromArgb(170, 125, 65), 2))
            {
                int lastX = 0, lastY = 0;
                bool first = true;
                foreach (var p in _platforms)
                {
                    if (p.Height <= 20) continue;
                    for (int px = p.Left + 20; px < p.Right - 10; px += 80)
                    {
                        const int postH = 26;
                        int postTop = p.Top - postH;
                        // Vertical post
                        g.FillRectangle(postBr, px - 5, postTop, 10, postH);
                        // Cap knob
                        g.FillEllipse(postBr, px - 7, postTop - 5, 14, 9);
                        // Connect rope to previous post
                        if (!first)
                            g.DrawLine(rope, lastX, lastY, px, postTop + 5);
                        first = false;
                        lastX = px;
                        lastY = postTop + 5;
                    }
                    first = true;   // reset rope chain between separate platform sections
                }
            }
        }

        /// <summary>
        /// Draws jungle leaf clusters along the tops of dino-island platforms,
        /// giving the sense of a living, overgrown environment.
        /// </summary>
        private void DrawJungleFoliage(Graphics g)
        {
            var rng = new Random(99887);
            using (var dark  = new SolidBrush(Color.FromArgb(130, 18, 105,  0)))
            using (var light = new SolidBrush(Color.FromArgb(165, 38, 185, 28)))
            {
                foreach (var p in _platforms)
                {
                    if (p.Height <= 20) continue;
                    for (int fx = p.Left + 22; fx < p.Right - 10; fx += 58 + rng.Next(18))
                    {
                        int lh = 14 + rng.Next(14);
                        int lw = 18 + rng.Next(14);
                        g.FillEllipse(dark,  fx - lw / 2,     p.Top - lh,     lw,     lh);
                        g.FillEllipse(light, fx - lw / 2 + 3, p.Top - lh + 4, lw - 5, lh - 5);
                    }
                }
            }
        }

        /// <summary>
        /// Draws animated falling snowflakes across the screen for the tundra level.
        /// Screen-space — called after the camera transform is reset.
        /// </summary>
        private void DrawSnowfall(Graphics g, int W, int H)
        {
            if (_islandId != "tundra") return;
            using (var br = new SolidBrush(Color.FromArgb(185, 235, 245, 255)))
            {
                for (int i = 0; i < 55; i++)
                {
                    // Deterministic animated positions: each snowflake has its own drift speed
                    float fx = (float)((i * 139.7f + _levelAnim * 22f * (0.5f + (i % 5) * 0.1f)) % W);
                    float fy = (float)((_levelAnim * 38f * (0.4f + (i % 7) * 0.09f) + i * H / 55f) % H);
                    fx += (float)Math.Sin(_levelAnim * 0.7f + i * 0.8f) * 12f;  // gentle horizontal sway
                    int sz = 2 + (i % 3);
                    g.FillEllipse(br, fx, fy, sz, sz);
                }
            }
        }

        /// <summary>
        /// Draws rising bubble columns for underwater (coral) levels.
        /// Screen-space — called after the camera transform is reset.
        /// </summary>
        private void DrawBubbles(Graphics g, int W, int H)
        {
            if (_islandId != "coral" && _islandId != "sunken_gate" &&
                _islandId != "kelp"  && _islandId != "boiling_vent" && _islandId != "abyss") return;
            using (var pen = new Pen(Color.FromArgb(105, 125, 205, 255), 1))
            {
                for (int i = 0; i < 22; i++)
                {
                    float bx = (W * (i * 0.048f + 0.02f)) % W;
                    float by = (H - ((_levelAnim * 28f * (0.3f + i * 0.04f) + i * H / 22f) % H));
                    float bsz = 3 + (i % 4);
                    g.DrawEllipse(pen, bx - bsz / 2f, by - bsz / 2f, bsz, bsz);
                }
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
