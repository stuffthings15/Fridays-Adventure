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
using Fridays_Adventure.Tests;

namespace Fridays_Adventure.Scenes
{
    public sealed class IslandScene : Scene
    {
        private const float LevelScale = 1.5f;
        private readonly string _islandId;
        private readonly string _islandName;

        private Player            _player;
        private List<Enemy>       _enemies;
        private List<Rectangle>   _platforms;
        private List<Hazard>      _hazards;
        private List<IceWallInstance> _iceWalls;
        private List<MovingPlatform>  _movingPlatforms;
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

        // ── PHASE 3: SMB3 interactive entities ────────────────────────────────
        // Team 4 (Lead Game Designer) — question blocks, brick blocks, new enemies
        private List<Fireball>         _fireballs        = new List<Fireball>();
        private List<FrostBallProjectile> _frostBalls      = new List<FrostBallProjectile>();
        private List<GoombaEnemy>      _goombas          = new List<GoombaEnemy>();
        private List<KoopaEnemy>       _koopas           = new List<KoopaEnemy>();
        private List<PiranhaPlant>     _piranhaPlants    = new List<PiranhaPlant>();
        private List<Thwomp>           _thwomps          = new List<Thwomp>();
        private List<HammerBroEnemy>   _hammerBros       = new List<HammerBroEnemy>();
        // Star coins — 3 per level (Team 4 — Phase 3)
        private List<StarCoinPickup>   _starCoins        = new List<StarCoinPickup>();
        // Power-up drops — enemies randomly drop items (Team 4 — Phase 2)
        private List<PowerUp>          _powerUps         = new List<PowerUp>();
        private static readonly Random _dropRng          = new Random();

        // SMB3 Fire Flower projectile cadence.
        private float _fireballShotCooldown;

        private bool  _introActive;
        private bool  _wasInSeaStone;
        private float _seaStoneFlashAlpha;
        private float _freezeFlashTimer;
        private float _breakShockwaveTimer;
        private float _breakShockwaveWorldX;
        private float _breakShockwaveWorldY;
        private float _iceFlashTimer;

        // ── Speed-run timer (Team 1 — Phase 2 Idea 3) ─────────────────────────
        /// <summary>Elapsed seconds since level start — displayed as a speedrun clock.</summary>
        private float _speedRunTimer;

        // ── Death counter (used by CourseClearScene grade) ────────────────────
        private int _deathCount;

        /// <summary>Running clock that drives animated environmental effects (snow, bubbles, torch flicker).</summary>
        private float _levelAnim;

        // ── Parallax background (Phase 2 — Team 14: Environment Artist) ──────
        /// <summary>Multi-layer parallax system built per island in LoadBackground().</summary>
        private ParallaxBackground _parallax;

        private Bitmap _bg;
        private Bitmap _uiPanel;
        /// <summary>
        /// Pre-rendered terrain bitmap baked once in BuildLevel so DrawPlatforms
        /// is a single BitBlt instead of dozens of GDI+ allocations per frame.
        /// This eliminates the GDI pressure freeze when entities first hit the ground.
        /// </summary>
        private Bitmap _terrainCache;
        private static readonly Font _hudFont  = new Font("Courier New", 16, FontStyle.Bold);
        private static readonly Font _infoFont = new Font("Courier New", 12, FontStyle.Bold);
        public IslandScene(string id, string name) { _islandId = id; _islandName = name; }

        public override void OnEnter()
        {
            BlockManager.Reset();   // Phase 3: clear any previous level's blocks
            Game.Instance.StarCoinsThisLevel = 0;  // Phase 3: reset star coin counter
            BuildLevel();
            LoadBackground();
            _uiPanel = SpriteManager.Get("ui_panel.png");

            // Apply difficulty modifiers to all spawned enemies.
            ApplyDifficultyModifiers();

            // PHASE 2 — Team 14: Wire WeatherSystem based on island theme.
            WeatherSystem.Set(GetIslandWeather(_islandId));

            // SMB3-style level-start overlays: GET READY! banner + world/level label.
            SMB3Hud.TriggerGetReady();
            SMB3Hud.ShowWorldLabel(
                $"WORLD {Game.Instance.WorldNumber}-{Game.Instance.LevelNumber}  {_islandName.ToUpper()}");

            Game.Instance.Audio.PlayIsland();
        }

        public override void OnExit()
        {
            WeatherSystem.Set(WeatherSystem.Mode.None);  // clear weather on exit
            _bg?.Dispose();           _bg           = null;
            _terrainCache?.Dispose(); _terrainCache = null;
        }

        // ════════════════════════════════════════════════════════════════════════
        // BOT AI DETECTION METHODS (BATCH 2 - Game Scene Integration)
        // These methods scan the level for hazards, enemies, and pickups
        // and provide that information to SmartBotAI each frame.
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Detect all hazards near the bot (lightning, spikes, obstacles).
        /// Returns list of DetectedHazard objects with positions and distances.
        /// Called by BotPlayLevelScene each frame for smart bot decisions.
        /// </summary>
        public List<Tests.DetectedHazard> DetectHazardsNearBot(Player bot)
        {
            var detected = new List<Tests.DetectedHazard>();
            if (bot == null) return detected;

            const float DETECTION_RANGE = 300f;

            // Scan hazard list for spikes, fire, etc.
            foreach (var h in _hazards)
            {
                if (h == null || !h.IsActive) continue;
                float dist = Math.Abs(h.X - bot.X);
                if (dist > DETECTION_RANGE) continue;

                detected.Add(new Tests.DetectedHazard
                {
                    X = h.X,
                    Y = h.Y,
                    Width = h.Width,
                    Height = h.Height,
                    Type = h.GetType().Name.ToLower(),
                    Distance = dist,
                    IsImmediate = dist < 150f
                });
            }

            // Scan moving platforms as obstacles
            foreach (var mp in _movingPlatforms)
            {
                float dist = Math.Abs(mp.X - bot.X);
                if (dist > DETECTION_RANGE) continue;
                if (Math.Abs(mp.VelocityX) > 100f || Math.Abs(mp.VelocityY) > 100f)
                {
                    detected.Add(new Tests.DetectedHazard
                    {
                        X = mp.X,
                        Y = mp.Y,
                        Width = mp.Width,
                        Height = mp.Height,
                        Type = "moving_platform",
                        Distance = dist,
                        IsImmediate = dist < 150f
                    });
                }
            }

            return detected;
        }

        /// <summary>
        /// Detect all enemies near the bot.
        /// Returns list of DetectedEnemy objects with positions and threat levels.
        /// Bot uses this to decide whether to attack or dodge.
        /// </summary>
        public List<Tests.DetectedEnemy> DetectEnemiesNearBot(Player bot)
        {
            var detected = new List<Tests.DetectedEnemy>();
            if (bot == null) return detected;

            const float DETECTION_RANGE = 400f;

            // Scan all generic enemies
            foreach (var e in _enemies)
            {
                if (!e.IsAlive) continue;
                float dist = Math.Abs(e.X - bot.X);
                if (dist > DETECTION_RANGE) continue;

                bool isAggressive = e.VelocityX != 0 || (e.X < bot.X && e.VelocityX < 0) || (e.X > bot.X && e.VelocityX > 0);

                detected.Add(new Tests.DetectedEnemy
                {
                    X = e.X,
                    Y = e.Y,
                    Width = e.Width,
                    Height = e.Height,
                    Type = e.GetType().Name,
                    Distance = dist,
                    IsAggressive = isAggressive,
                    Health = e.Health
                });
            }

            return detected;
        }

        /// <summary>
        /// Detect all pickups near the bot (berries, health items, powerups, star coins).
        /// Returns list of DetectedPickup objects with positions and values.
        /// Bot prioritizes health pickups when hurt, currency otherwise.
        /// </summary>
        public List<Tests.DetectedPickup> DetectPickupsNearBot(Player bot)
        {
            var detected = new List<Tests.DetectedPickup>();
            if (bot == null) return detected;

            const float DETECTION_RANGE = 250f;

            // Berries (currency)
            foreach (var b in _berries)
            {
                if (b.Collected) continue;
                float dist = Math.Abs(b.X - bot.X);
                if (dist > DETECTION_RANGE) continue;

                detected.Add(new Tests.DetectedPickup
                {
                    X = b.X,
                    Y = b.Y,
                    Type = "berry",
                    Value = b.Value,
                    Distance = dist
                });
            }

            // Health pickups (CRITICAL - top priority when hurt)
            foreach (var hp in _healthPickups)
            {
                float dist = Math.Abs(hp.X - bot.X);
                if (dist > DETECTION_RANGE) continue;

                detected.Add(new Tests.DetectedPickup
                {
                    X = hp.X,
                    Y = hp.Y,
                    Type = "health",
                    Value = 25,
                    Distance = dist
                });
            }

            // Power-ups
            foreach (var pu in _powerUps)
            {
                if (pu.IsCollected) continue;
                float dist = Math.Abs(pu.X - bot.X);
                if (dist > DETECTION_RANGE) continue;

                detected.Add(new Tests.DetectedPickup
                {
                    X = pu.X,
                    Y = pu.Y,
                    Type = "powerup",
                    Value = 100,
                    Distance = dist
                });
            }

            // Star coins
            foreach (var sc in _starCoins)
            {
                float dist = Math.Abs(sc.X - bot.X);
                if (dist > DETECTION_RANGE) continue;

                detected.Add(new Tests.DetectedPickup
                {
                    X = sc.X,
                    Y = sc.Y,
                    Type = "starcoin",
                    Value = 50,
                    Distance = dist
                });
            }

            return detected;
        }

        /// <summary>
        /// Helper: Get player health for bot decision making.
        /// </summary>
        public int GetBotPlayerHealth() => _player?.Health ?? 0;

        /// <summary>
        /// Helper: Check if level is still playable.
        /// </summary>
        public bool IsBotLevelActive() => !_levelComplete && _player.IsAlive;

        // ════════════════════════════════════════════════════════════════════════
        // END BOT AI DETECTION (See BotPlayLevelScene for integration)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// PHASE 2 — Team 14: Maps an island ID to its weather mode.
        /// </summary>
        private static WeatherSystem.Mode GetIslandWeather(string id)
        {
            switch (id)
            {
                case "tundra":      return WeatherSystem.Mode.Snow;
                case "harbor":      return WeatherSystem.Mode.Rain;
                case "coral":       return WeatherSystem.Mode.Underwater;
                case "dive_gate":
                case "sunken_gate":
                case "kelp":        return WeatherSystem.Mode.Underwater;
                case "boiling_vent":return WeatherSystem.Mode.Embers;
                case "abyss":       return WeatherSystem.Mode.Embers;
                default:            return WeatherSystem.Mode.None;
            }
        }

        /// <summary>
        /// PHASE 2 - Team 1: Game Director
        /// Apply difficulty modifiers to all enemies and the player on level entry.
        /// Hard mode: enemies have 2× HP.
        /// Challenge mode: enemies have normal HP but player is reduced to 30 HP.
        /// </summary>
        private void ApplyDifficultyModifiers()
        {
            float healthMult = DifficultyModifiers.GetEnemyHealthMultiplier();

            // Scale enemy HP (Hard mode: 2×; Challenge/Normal: 1×)
            if (Math.Abs(healthMult - 1.0f) > 0.001f)
            {
                foreach (var enemy in _enemies)
                {
                    int newMaxHp = (int)(enemy.MaxHealth * healthMult);
                    enemy.MaxHealth = newMaxHp;
                    enemy.Health    = newMaxHp;
                }
            }

            // Challenge mode — cap player HP to 30 for near-1-hit-KO tension
            int playerMaxHp = DifficultyModifiers.GetPlayerMaxHealth();
            if (playerMaxHp != _player.MaxHealth)
            {
                _player.MaxHealth = playerMaxHp;
                _player.Health    = Math.Min(_player.Health, playerMaxHp);
            }

            // New Game+ — additional 1.5× enemy HP and +20 % speed bonus
            if (Game.Instance.NewGamePlus)
            {
                foreach (var enemy in _enemies)
                {
                    int ngpHp = (int)(enemy.MaxHealth * 1.5f);
                    enemy.MaxHealth = ngpHp;
                    enemy.Health    = ngpHp;
                    enemy.MoveSpeed *= 1.2f;
                }
                SMB3Hud.ShowToast("⚔ NEW GAME+ ACTIVE");
            }
        }

        // ── Level construction ──────────────────────────────────────────────

        /// <summary>
        /// PHASE 2 — Team 14: Applies island-specific enemy archetypes after the base level is built.
        /// Each island has a themed enemy type with unique HP, speed, and score value.
        /// Keeps level layout code clean while giving every island a distinct feel.
        /// </summary>
        private void ApplyIslandEnemyVariants()
        {
            foreach (var e in _enemies)
            {
                switch (_islandId)
                {
                    case "wano":
                        // Blade Nation — armored samurai marines, high HP, slow
                        e.EnemyType  = "Armored";
                        e.MaxHealth  = e.Health = 60;
                        e.MoveSpeed  = 80f;
                        e.ScoreValue = 25;
                        e.AttackDamage = 14;
                        break;

                    case "sky":
                        // Sky Island — winged scout marines, fast and fragile
                        e.EnemyType  = "Marine";
                        e.MaxHealth  = e.Health = 28;
                        e.MoveSpeed  = 160f;
                        e.ScoreValue = 18;
                        e.AttackDamage = 8;
                        break;

                    case "coral":
                    case "dive_gate":
                    case "sunken_gate":
                        // Underwater — diver marines, normal HP but poison on contact
                        e.EnemyType  = "Marine";
                        e.MaxHealth  = e.Health = 40;
                        e.MoveSpeed  = 95f;
                        e.ScoreValue = 20;
                        e.AttackDamage = 12;
                        break;

                    case "kelp":
                    case "boiling_vent":
                        // Deep-sea — heavy armoured divers, very high HP
                        e.EnemyType  = "Armored";
                        e.MaxHealth  = e.Health = 80;
                        e.MoveSpeed  = 70f;
                        e.ScoreValue = 35;
                        e.AttackDamage = 18;
                        break;

                    case "abyss":
                        // Abyss — boss-tier grunt, extremely tough
                        e.EnemyType  = "Boss";
                        e.MaxHealth  = e.Health = 110;
                        e.MoveSpeed  = 65f;
                        e.ScoreValue = 50;
                        e.AttackDamage = 22;
                        break;

                    case "tundra":
                        // Tundra — cold-weather marines, normal stats
                        e.EnemyType  = "Marine";
                        e.MaxHealth  = e.Health = 35;
                        e.MoveSpeed  = 100f;
                        e.ScoreValue = 15;
                        e.AttackDamage = 10;
                        break;

                    case "harbor":
                        // Harbor — naval soldiers, slightly tougher than basics
                        e.EnemyType  = "Marine";
                        e.MaxHealth  = e.Health = 45;
                        e.MoveSpeed  = 110f;
                        e.ScoreValue = 20;
                        e.AttackDamage = 12;
                        break;

                    // dino + default: baseline patrol marine (values from Enemy ctor)
                }
            }
        }

        private void BuildLevel()
        {
            _groundY         = 440;
            _iceWalls        = new List<IceWallInstance>();
            _movingPlatforms = new List<MovingPlatform>();
            _platforms       = new List<Rectangle>();
            _hazards         = new List<Hazard>();
            _enemies         = new List<Enemy>();

            // Phase 3 entities are per-level; always reset on build.
            _fireballs     = new List<Fireball>();
            _frostBalls    = new List<FrostBallProjectile>();
            _starCoins     = new List<StarCoinPickup>();
            _goombas       = new List<GoombaEnemy>();
            _koopas        = new List<KoopaEnemy>();
            _piranhaPlants = new List<PiranhaPlant>();
            _thwomps       = new List<Thwomp>();
            _hammerBros    = new List<HammerBroEnemy>();

            switch (_islandId)
            {
                case "wano":        BuildBladeNation(); break;
                case "harbor":      BuildHarborTown();  break;
                case "coral":       BuildCoralReef();   break;
                case "tundra":      BuildTundraPeak();  break;
                case "sky":         BuildDinoIsland();  break;  // sky uses dino layout, different enemies
                case "sunken_gate": BuildDinoIsland();  break;
                case "kelp":        BuildDinoIsland();  break;
                case "boiling_vent":BuildDinoIsland();  break;
                case "abyss":       BuildDinoIsland();  break;
                case "dive_gate":   BuildDinoIsland();  break;
                default:            BuildDinoIsland();  break;
            }

            // Overlay island-specific enemy types after the base layout is built
            ApplyIslandEnemyVariants();

            // Player (selected from Crew screen)
            _player = new Player(50, _groundY - 56);
            _player.ApplySelectedSprite();

            // Berries (SMB3-style collectibles on platforms and over gaps)
            _berries = new List<Berries>();
            SpawnBerries();

            // Health pickups — red cross items scattered through the level
            _healthPickups = new List<HealthPickup>();
            SpawnHealthPickups();

            // Phase 3 — hidden star coins (3 per level like SMB3)
            SpawnStarCoins();

            ApplyLevelScale();

            // Bake terrain into a single bitmap so DrawPlatforms is one BitBlt per frame.
            BakeTerrainCache();
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

                private void ApplyLevelScale()
        {
            Rectangle ScaleRect(Rectangle r) => new Rectangle(
                (int)(r.X * LevelScale),
                (int)(r.Y * LevelScale),
                (int)(r.Width * LevelScale),
                (int)(r.Height * LevelScale));

            _groundY = (int)(_groundY * LevelScale);
            _levelWidth = (int)(_levelWidth * LevelScale);

            for (int i = 0; i < _platforms.Count; i++) _platforms[i] = ScaleRect(_platforms[i]);
            _exitFlag = ScaleRect(_exitFlag);

            foreach (var hz in _hazards)
            {
                hz.X *= LevelScale;
                hz.Y *= LevelScale;
                hz.Width = (int)(hz.Width * LevelScale);
                hz.Height = (int)(hz.Height * LevelScale);
            }

            foreach (var e in _enemies)
            {
                e.X *= LevelScale;
                e.Y *= LevelScale;
                e.Width = (int)(e.Width * LevelScale);
                e.Height = (int)(e.Height * LevelScale);
            }

            foreach (var b in _berries)
            {
                b.X *= LevelScale;
                b.Y *= LevelScale;
                b.Width  = (int)(b.Width  * LevelScale);
                b.Height = (int)(b.Height * LevelScale);
                // Re-anchor the bob origin to the scaled Y so the
                // berry's bobbing animation starts from the correct position.
                b.SyncBaseY();
            }

            foreach (var sc in _starCoins)
            {
                sc.ApplyLevelScale(LevelScale);
            }
            // Scale moving platform extents
            foreach (var mp in _movingPlatforms)
                mp.ApplyScale(LevelScale);

            _player.X *= LevelScale;
            _player.Y *= LevelScale;
            _player.Width = (int)(_player.Width * LevelScale);
            _player.Height = (int)(_player.Height * LevelScale);
            _player.ApplySelectedSprite();
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

            // Moving platforms — SMB3-style traversal challenge.
            _movingPlatforms.Add(new MovingPlatform(350, _groundY - 130, 80, 350, 550, 70f));
            _movingPlatforms.Add(new MovingPlatform(1050, _groundY - 110, 80, 1050, 1200, 60f));

            // ── PHASE 3: SMB3 interactive blocks & new enemy types ─────────────
            // Team 4 (Lead Game Designer) — Question blocks with coin/item contents
            BlockManager.QuestionBlocks.Add(new QuestionBlock(380, _groundY - 148, BlockContent.Coin));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(420, _groundY - 148, BlockContent.Mushroom));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(460, _groundY - 148, BlockContent.Coin));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(1080, _groundY - 120, BlockContent.FireFlower));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(1120, _groundY - 120, BlockContent.MultiCoin));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(1720, _groundY - 152, BlockContent.Leaf));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(2320, _groundY - 174, BlockContent.Star));

            // Brick blocks (Team 4 — Idea 2)
            BlockManager.BrickBlocks.Add(new BrickBlock(400, _groundY - 148));
            BlockManager.BrickBlocks.Add(new BrickBlock(440, _groundY - 148));
            BlockManager.BrickBlocks.Add(new BrickBlock(1100, _groundY - 120));
            BlockManager.BrickBlocks.Add(new BrickBlock(1740, _groundY - 152));
            BlockManager.BrickBlocks.Add(new BrickBlock(2340, _groundY - 174));

            // Goomba enemies (Team 4 — Idea 9)
            _goombas.Add(new GoombaEnemy(160,  _groundY - 28, 100,  400));
            _goombas.Add(new GoombaEnemy(860,  _groundY - 28, 800,  1100));
            _goombas.Add(new GoombaEnemy(1520, _groundY - 28, 1440, 1720));

            // Koopa enemies (Team 4 — Idea 10)
            _koopas.Add(new KoopaEnemy(480, _groundY - 40, 350, 700));
            _koopas.Add(new KoopaEnemy(1300, _groundY - 40, 1100, 1440));

            // Piranha Plant in a pipe (Team 4 — Idea 11)
            _piranhaPlants.Add(new PiranhaPlant(860,  _groundY - 10));
            _piranhaPlants.Add(new PiranhaPlant(1960, _groundY - 10));

            // Thwomp (Team 4 — Idea 12)
            _thwomps.Add(new Thwomp(1580, _groundY - 260));

            // Hammer Bro encounter (Team 4 — Idea 13)
            _hammerBros.Add(new HammerBroEnemy(2100, _groundY - 44, 2050, 2300));

            // Star coins — 3 hidden collectibles per level (Team 4 — Idea 6)
            _starCoins.Add(new StarCoinPickup(350 + 50, _groundY - 80));
            _starCoins.Add(new StarCoinPickup(1050 + 70, _groundY - 80));
            _starCoins.Add(new StarCoinPickup(2300 + 80, _groundY - 80));
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

            _movingPlatforms.Add(new MovingPlatform(300, _groundY - 155, 80, 300, 480, 65f));

            // ── Phase 3: SMB3 blocks & enemies — Blade Nation ─────────────────
            BlockManager.QuestionBlocks.Add(new QuestionBlock(320, _groundY - 162, BlockContent.Coin));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(360, _groundY - 162, BlockContent.FireFlower));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(820, _groundY - 142, BlockContent.MultiCoin));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(1370, _groundY - 162, BlockContent.Mushroom));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(2020, _groundY - 182, BlockContent.Star));

            BlockManager.BrickBlocks.Add(new BrickBlock(340, _groundY - 162));
            BlockManager.BrickBlocks.Add(new BrickBlock(840, _groundY - 142));
            BlockManager.BrickBlocks.Add(new BrickBlock(1390, _groundY - 162));

            _goombas.Add(new GoombaEnemy(200, _groundY - 28, 100, 500));
            _goombas.Add(new GoombaEnemy(650, _groundY - 28, 560, 900));
            _koopas.Add(new KoopaEnemy(1150, _groundY - 40, 1100, 1400));
            _hammerBros.Add(new HammerBroEnemy(1850, _groundY - 44, 1800, 2050));
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

            _movingPlatforms.Add(new MovingPlatform(280, _groundY - 120, 80, 280, 460, 55f));

            // ── Phase 3: SMB3 blocks & enemies — Harbor Town ──────────────────
            BlockManager.QuestionBlocks.Add(new QuestionBlock(300, _groundY - 132, BlockContent.Coin));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(340, _groundY - 132, BlockContent.Mushroom));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(770, _groundY - 152, BlockContent.FireFlower));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(1320, _groundY - 172, BlockContent.MultiCoin));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(1360, _groundY - 172, BlockContent.Coin));

            BlockManager.BrickBlocks.Add(new BrickBlock(320, _groundY - 132));
            BlockManager.BrickBlocks.Add(new BrickBlock(790, _groundY - 152));
            BlockManager.BrickBlocks.Add(new BrickBlock(1340, _groundY - 172));

            _goombas.Add(new GoombaEnemy(100, _groundY - 28, 0, 480));
            _goombas.Add(new GoombaEnemy(650, _groundY - 28, 560, 1000));
            _koopas.Add(new KoopaEnemy(900, _groundY - 40, 800, 1080));
            _piranhaPlants.Add(new PiranhaPlant(570, _groundY - 10));
            _thwomps.Add(new Thwomp(1100, _groundY - 240));
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

            _movingPlatforms.Add(new MovingPlatform(320, _groundY - 120, 80, 320, 500, 58f));
            _movingPlatforms.Add(new MovingPlatform(1460, _groundY - 162, 80, 1460, 1620, 72f));

            // ── Phase 3: SMB3 blocks & enemies — Coral Reef ───────────────────
            BlockManager.QuestionBlocks.Add(new QuestionBlock(340, _groundY - 132, BlockContent.Mushroom));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(900, _groundY - 152, BlockContent.MultiCoin));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(1480, _groundY - 172, BlockContent.FireFlower));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(2070, _groundY - 192, BlockContent.Leaf));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(2110, _groundY - 192, BlockContent.Coin));

            BlockManager.BrickBlocks.Add(new BrickBlock(360, _groundY - 132));
            BlockManager.BrickBlocks.Add(new BrickBlock(920, _groundY - 152));
            BlockManager.BrickBlocks.Add(new BrickBlock(1500, _groundY - 172));

            _goombas.Add(new GoombaEnemy(150, _groundY - 28, 0,    600));
            _goombas.Add(new GoombaEnemy(720, _groundY - 28, 600,  1060));
            _koopas.Add(new KoopaEnemy(1000, _groundY - 40, 900,   1160));
            _koopas.Add(new KoopaEnemy(1850, _groundY - 40, 1740,  2100));
            _piranhaPlants.Add(new PiranhaPlant(610, _groundY - 10));
            _piranhaPlants.Add(new PiranhaPlant(1670, _groundY - 10));
            _thwomps.Add(new Thwomp(1260, _groundY - 260));
            _hammerBros.Add(new HammerBroEnemy(2450, _groundY - 44, 2360, 2700));
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

            _movingPlatforms.Add(new MovingPlatform(260, _groundY - 160, 80, 260, 440, 60f));

            // ── Phase 3: SMB3 blocks & enemies — Tundra Peak ─────────────────
            BlockManager.QuestionBlocks.Add(new QuestionBlock(280, _groundY - 172, BlockContent.Coin));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(320, _groundY - 172, BlockContent.Leaf));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(780, _groundY - 192, BlockContent.MultiCoin));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(1340, _groundY - 212, BlockContent.FireFlower));
            BlockManager.QuestionBlocks.Add(new QuestionBlock(1920, _groundY - 232, BlockContent.Star));

            BlockManager.BrickBlocks.Add(new BrickBlock(300, _groundY - 172));
            BlockManager.BrickBlocks.Add(new BrickBlock(800, _groundY - 192));
            BlockManager.BrickBlocks.Add(new BrickBlock(1360, _groundY - 212));

            _goombas.Add(new GoombaEnemy(100, _groundY - 28, 0,    460));
            _goombas.Add(new GoombaEnemy(700, _groundY - 28, 540,  1040));
            _koopas.Add(new KoopaEnemy(1050, _groundY - 40, 980,   1200));
            _koopas.Add(new KoopaEnemy(1780, _groundY - 40, 1700,  2100));
            _piranhaPlants.Add(new PiranhaPlant(550, _groundY - 10));
            _thwomps.Add(new Thwomp(1350, _groundY - 280));
            _hammerBros.Add(new HammerBroEnemy(2100, _groundY - 44, 2000, 2380));
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
            // Rows placed at max-jump height so the bot can always reach them
            AddBerryRow(380,  _groundY - 80, 3, 40);
            AddBerryRow(1080, _groundY - 80, 3, 60);
            AddBerryRow(1720, _groundY - 80, 3, 50);
            AddBerryRow(2330, _groundY - 80, 4, 50);
            // Arcs over water pits — risk/reward (already within jump height)
            AddBerry(720, _groundY - 60); AddBerry(750, _groundY - 80); AddBerry(780, _groundY - 60);
            AddBerry(1380, _groundY - 70); AddBerry(1410, _groundY - 70);
            AddBerry(2060, _groundY - 70); AddBerry(2090, _groundY - 80);
        }

        private void SpawnBladeBerries()
        {
            AddBerryRow(320,  _groundY - 80, 3, 50);
            AddBerryRow(830,  _groundY - 80, 3, 50);
            AddBerryRow(1370, _groundY - 80, 3, 40);
            AddBerryRow(2020, _groundY - 80, 4, 50);
        }

        private void SpawnHarborBerries()
        {
            AddBerryRow(300,  _groundY - 80, 3, 40);
            AddBerryRow(780,  _groundY - 80, 3, 50);
            AddBerryRow(1320, _groundY - 80, 3, 40);
            AddBerry(510, _groundY - 60); AddBerry(535, _groundY - 80); AddBerry(560, _groundY - 60);
            AddBerry(1030, _groundY - 70); AddBerry(1055, _groundY - 70);
        }

        private void SpawnCoralBerries()
        {
            AddBerryRow(340,  _groundY - 80, 3, 50);
            AddBerryRow(910,  _groundY - 80, 3, 50);
            AddBerryRow(1490, _groundY - 80, 3, 50);
            AddBerryRow(2080, _groundY - 80, 4, 50);
            AddBerry(550, _groundY - 65); AddBerry(580, _groundY - 80);
            AddBerry(1110, _groundY - 70); AddBerry(1140, _groundY - 70);
        }

        private void SpawnTundraBerries()
        {
            AddBerryRow(280,  _groundY - 80, 3, 50);
            AddBerryRow(790,  _groundY - 80, 3, 50);
            AddBerryRow(1350, _groundY - 80, 3, 40);
            AddBerryRow(1930, _groundY - 80, 4, 50);
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
            _healthPickups.Add(new HealthPickup(410,  _groundY - 80));
            _healthPickups.Add(new HealthPickup(1510, _groundY - 80));
            _healthPickups.Add(new HealthPickup(2340, _groundY - 80));
        }

        private void SpawnBladeHealthPickups()
        {
            _healthPickups.Add(new HealthPickup(330,  _groundY - 80));
            _healthPickups.Add(new HealthPickup(1380, _groundY - 80));
            _healthPickups.Add(new HealthPickup(2040, _groundY - 80));
        }

        private void SpawnHarborHealthPickups()
        {
            _healthPickups.Add(new HealthPickup(290,  _groundY - 80));
            _healthPickups.Add(new HealthPickup(1310, _groundY - 80));
        }

        private void SpawnCoralHealthPickups()
        {
            _healthPickups.Add(new HealthPickup(340,  _groundY - 80));
            _healthPickups.Add(new HealthPickup(1480, _groundY - 80));
            _healthPickups.Add(new HealthPickup(2080, _groundY - 80));
        }

        private void SpawnTundraHealthPickups()
        {
            _healthPickups.Add(new HealthPickup(280,  _groundY - 80));
            _healthPickups.Add(new HealthPickup(1350, _groundY - 80));
            _healthPickups.Add(new HealthPickup(1940, _groundY - 80));
        }

        /// <summary>
        /// Phase 3 (SMB3 style): place 3 hidden star coins per level.
        /// </summary>
        private void SpawnStarCoins()
        {
            switch (_islandId)
            {
                case "wano":
                    _starCoins.Add(new StarCoinPickup(360,  _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(1440, _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(2220, _groundY - 80));
                    break;

                case "harbor":
                    _starCoins.Add(new StarCoinPickup(320,  _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(920,  _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(1820, _groundY - 80));
                    break;

                case "coral":
                case "dive_gate":
                case "sunken_gate":
                case "kelp":
                case "boiling_vent":
                case "abyss":
                    _starCoins.Add(new StarCoinPickup(420,  _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(1540, _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(2440, _groundY - 80));
                    break;

                case "tundra":
                    _starCoins.Add(new StarCoinPickup(300,  _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(980,  _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(2060, _groundY - 80));
                    break;

                case "sky":
                    _starCoins.Add(new StarCoinPickup(420,  _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(1200, _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(2200, _groundY - 80));
                    break;

                default:
                    _starCoins.Add(new StarCoinPickup(420,  _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(1320, _groundY - 80));
                    _starCoins.Add(new StarCoinPickup(2320, _groundY - 80));
                    break;
            }
        }

        private void LoadBackground()
        {
            var raw = LoadBackgroundForIsland(_islandId);
            if (raw != null)
            {
                // Pre-scale background to screen size using fast interpolation.
                // HighQualityBicubic is VERY expensive (~10s freeze) — use NearestNeighbor instead.
                // This trades imperceptible visual quality for instant level load times.
                int sw = Game.Instance.CanvasWidth;
                int sh = Game.Instance.CanvasHeight;
                if (sw > 0 && sh > 0 && (raw.Width != sw || raw.Height != sh))
                {
                    var scaled = new Bitmap(sw, sh);
                    using (var sg = Graphics.FromImage(scaled))
                    {
                        // Use fast NearestNeighbor interpolation to avoid 10-second freeze
                        sg.InterpolationMode  = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        sg.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.AssumeLinear;
                        sg.DrawImage(raw, 0, 0, sw, sh);
                    }
                    raw.Dispose();
                    _bg = scaled;
                }
                else
                {
                    _bg = raw;
                }
            }
            BuildParallax();
        }

        /// <summary>
        /// PHASE 2 — Team 14: Environment Artist.
        /// Builds a multi-layer parallax stack tuned to the current island's theme.
        /// Sky + mountain + cloud layers are always present; underwater islands use
        /// a dark deep-sea colour scheme instead.
        /// </summary>
        private void BuildParallax()
        {
            _parallax = new ParallaxBackground();

            bool isUnderwater = _islandId == "dive_gate" || _islandId == "sunken_gate" ||
                                _islandId == "kelp"      || _islandId == "boiling_vent" ||
                                _islandId == "abyss";

            if (isUnderwater)
            {
                // Deep-sea: dark teal sky, no clouds
                _parallax.AddLayer(new ParallaxBackground.SkyLayer
                {
                    TopColor    = Color.FromArgb(8,  28,  60),
                    BottomColor = Color.FromArgb(12, 55, 100)
                });
                _parallax.AddLayer(new ParallaxBackground.StarLayer()); // bioluminescence
            }
            else if (_islandId == "tundra")
            {
                // Ice world: pale sky, snowy mountains
                _parallax.AddLayer(new ParallaxBackground.SkyLayer
                {
                    TopColor    = Color.FromArgb(180, 210, 240),
                    BottomColor = Color.FromArgb(220, 235, 255)
                });
                _parallax.AddLayer(new ParallaxBackground.MountainLayer());
                _parallax.AddLayer(new ParallaxBackground.CloudLayer());
            }
            else
            {
                // Default tropical / adventure sky
                _parallax.AddLayer(new ParallaxBackground.SkyLayer());
                _parallax.AddLayer(new ParallaxBackground.MountainLayer());
                _parallax.AddLayer(new ParallaxBackground.CloudLayer());
            }
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

            // Advance speed-run timer while playing
            _speedRunTimer += dt;
            Game.Instance.LevelElapsedSeconds = _speedRunTimer;

            // Music heartbeat — restart island track if the audio device stalled or
            // was never started (e.g. first frame audio was not yet ready on entry).
            // ContinueOrPlay is a no-op while music is already playing.
            if (_speedRunTimer % 5f < dt)
                Game.Instance.Audio.ContinueOrPlay("island");

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
            if (_fireballShotCooldown > 0f) _fireballShotCooldown = Math.Max(0f, _fireballShotCooldown - dt);

            IceSystem.Update(_iceWalls, _hazards, _player, dt);
            ThreatSystem.Tick(dt);

            // Update moving platforms and carry the player when riding.
            foreach (var mp in _movingPlatforms)
                mp.Update(dt, _player);

            CheckWaterFall(dt, input);
            UpdateEnemies(dt);

            // ── PHASE 3: Update new SMB3 entity types ─────────────────────────
            BlockManager.Update(dt, _player);
            // SMB3 enemies handled in UpdateEnemies

            _combo.Update(dt, _player, _enemies);
            CheckCombat();
            UpdateBerries(dt);
            UpdateHealthPickups(dt);
            UpdatePowerUps(dt);
            // Update star coins — checks player hitbox and awards score on collection.
            // Previously missing from the update loop, making star coins uncollectible.
            foreach (var sc in _starCoins) sc.Update(dt, _player);

            // ── Update projectiles ──────────────────────────────────────────
            // Update and cull fireballs (Fire Flower projectiles)
            for (int i = _fireballs.Count - 1; i >= 0; i--)
            {
                _fireballs[i].UpdateProjectile(dt, _groundY, _levelWidth, _platforms);
                if (!_fireballs[i].IsActive) _fireballs.RemoveAt(i);
            }

            // Update and cull frost balls (X-key ability projectiles)
            for (int i = _frostBalls.Count - 1; i >= 0; i--)
            {
                _frostBalls[i].UpdateProjectile(dt, _groundY, _levelWidth, _platforms);
                if (!_frostBalls[i].IsActive) _frostBalls.RemoveAt(i);
            }

            WeatherSystem.Update(dt);  // Phase 2 — Team 14: weather particle simulation
            CheckExit();
            UpdateCamera();
        }

        private void HandleInput(Engine.InputManager input, float dt)
        {
            if (_player.HasEffect(StatusEffect.Sinking) || _player.HasEffect(StatusEffect.Stunned))
                goto abilities;   // skip movement but still allow ability input below

            // ── Horizontal movement + P-Meter + stamina sprint ───────────────────
            bool movingHoriz = input.LeftHeld || input.RightHeld;
            bool wantsSprint = input.SprintHeld && movingHoriz && _player.IsGrounded;
            _player.TickStamina(wantsSprint, dt);

            float sprintMult = _player.IsSprinting ? 1.35f : 1.0f;
            float moveSpd = _player.MoveSpeed * (_player.PMeterActive ? 1.4f : 1.0f) * sprintMult;

            // Determine if currently dashing
            bool isDashing = _player.IsDashing || _player.HasEffect(StatusEffect.Dodging);

            if (!isDashing)
            {
                // ── NORMAL MOVEMENT - Apply input normally
                if (input.LeftHeld)
                {
                    if (!_player.IsWallJumping) { _player.VelocityX = -moveSpd; _player.FacingRight = false; }
                }
                else if (input.RightHeld)
                {
                    if (!_player.IsWallJumping) { _player.VelocityX = moveSpd; _player.FacingRight = true; }
                }
                else
                {
                    if (!_player.IsWallJumping) _player.VelocityX = 0;
                }
            }
            else
            {
                // ── DASH/DODGE ACTIVE - Preserve velocity momentum
                // Only update facing direction, don't override velocity
                if (input.LeftHeld)
                    _player.FacingRight = false;
                else if (input.RightHeld)
                    _player.FacingRight = true;
                // Do NOT set VelocityX - let the dash velocity persist
            }

            // ── Phase 2 T7 #1: Wall Slide ──────────────────────────────────────
            // When airborne and pressing into a wall, cap fall speed to simulate slide.
            bool pressingIntoWall = (_player.IsOnRightWall && input.RightHeld) ||
                                    (_player.IsOnLeftWall  && input.LeftHeld);
            _player.IsWallSliding = !_player.IsGrounded && pressingIntoWall &&
                                    _player.VelocityY > 0f;
            if (_player.IsWallSliding)
                _player.VelocityY = Math.Min(_player.VelocityY, Player.WallSlideSpeed);

            // ── Jump logic: regular, coyote-time, wall-jump ───────────────────
            if (input.JumpPressed)
            {
                // Wall jump (Team 7) — takes priority; player must be against a wall
                if (!_player.IsGrounded && (_player.IsOnLeftWall || _player.IsOnRightWall))
                {
                    _player.DoWallJump(wallOnRight: _player.IsOnRightWall);
                    Game.Instance.Audio.BeepJump();
                    AchievementSystem.Grant("ach_wall_jump");  // achievement trigger
                }
                // Standard / coyote jump
                else if (_player.JumpsRemaining > 0 ||
                         _player.CoyoteTimeRemaining > 0f)
                {
                    // Phase 2 T4 #3: Momentum-Based Jumping — P-Meter full run boosts jump 12 %.
                    float momentumMult = _player.PMeterActive ? 1.12f : 1.0f;
                    _player.VelocityY      = _player.JumpForce * momentumMult;
                    _player.IsGrounded     = false;
                    _player.JumpsRemaining = Math.Max(0, _player.JumpsRemaining - 1);
                    Game.Instance.Audio.BeepJump();
                }
            }

            // Variable jump height — release early for short hop (SMB3-style)
            if (!input.JumpHeld && _player.VelocityY < -120f)
                _player.VelocityY = -120f;

            // ── Ground pound (Team 7) — Down + Attack while airborne ──────────
            if (!_player.IsGrounded && input.DownHeld && input.AttackPressed)
                _player.StartGroundPound();

            // ── Swan glide (Team 7 / Gameplay Programmer — Idea 2) ───────────
            // Press jump while airborne as Swan to start glide (gentle fall).
            if (_player.Archetype == PlayableCharacter.Swan && !_player.IsGrounded && input.JumpPressed)
            {
                _player.IsGliding = true;
            }
            if (_player.IsGliding && !_player.IsGrounded)
            {
                // Dampen downward velocity to create a float/glide feel
                _player.VelocityY = Math.Min(_player.VelocityY, 80f);
            }
            else if (_player.IsGrounded)
            {
                _player.IsGliding = false;
            }

            // ── Dodge ─────────────────────────────────────────────────────────
            if (input.DodgePressed)
            {
                bool frostSuccess = _player.TryShootFrostBall();
                if (frostSuccess)
                {
                    float fx = _player.FacingRight ? _player.X + _player.Width + 4 : _player.X - 18;
                    float fy = _player.Y + _player.Height * 0.55f;
                    _frostBalls.Add(new FrostBallProjectile(fx, fy, _player.FacingRight));
                    Game.Instance.Audio.BeepFireball();
                    System.Diagnostics.Debug.WriteLine("[FROST_BALL] Ice projectile fired");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[FROST_BALL] Ice projectile REJECTED - cooldown active");
                }
            }

            // ── Melee attack / Fire Flower projectile ─────────────────────────
            if (input.AttackPressed && !input.DownHeld)
            {
                bool fired = false;

                // SMB3 style: Fire Flower converts attack button into fireball shot.
                if (PowerUpInventory.ActiveSuit == SuitType.FireFlower && _fireballShotCooldown <= 0f)
                {
                    float fx = _player.FacingRight ? _player.X + _player.Width + 4 : _player.X - 18;
                    float fy = _player.Y + _player.Height * 0.55f;
                    _fireballs.Add(new Fireball(fx, fy, _player.FacingRight));
                    _fireballShotCooldown = 0.28f;
                    Game.Instance.Audio.BeepFireball();
                    fired = true;
                    System.Diagnostics.Debug.WriteLine("[ATTACK] FireFlower fireball fired");
                }

                if (!fired)
                {
                    bool attackSuccess = _player.TryAttack();
                    if (attackSuccess)
                    {
                        Game.Instance.Audio.BeepAttack();
                        System.Diagnostics.Debug.WriteLine($"[ATTACK] Melee attack fired. Cooldown set to {_player.AttackCooldown:F2}s");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ATTACK] Melee attack REJECTED. Cooldown={_player.AttackCooldown:F2}s, IsAttacking={_player.IsAttacking}");
                    }
                }
            }

            abilities:
            // ── Q — Ice Wall (all characters, Orca wider) ────────────────────
            if (input.Ability1Pressed)
            {
                if (_player.UseIceWall(out IceWallInstance wall))
                {
                    _iceWalls.Add(wall);
                    Game.Instance.Audio.BeepIce();
                    _iceFlashTimer = 0.001f;
                }
            }

            // ── E — Character-unique ability ──────────────────────────────────
            // Orca → Tidal Slam (AOE shockwave), Swan → Wing Dash, Friday → Freeze Flash
            if (input.Ability2Pressed)
            {
                if (_player.UseCharacterAbility())
                {
                    switch (_player.Archetype)
                    {
                        case PlayableCharacter.Orca:
                            // Apply AOE slam damage to all nearby enemies
                            TidalSlamAttack();
                            Game.Instance.Audio.BeepAttack();
                            Game.Instance.ScreenShake.Trigger(0.6f);
                            break;
                        case PlayableCharacter.Swan:
                            // Wing Dash already applied velocity inside WingDash.OnUse
                            Game.Instance.Audio.BeepJump();
                            break;
                        default:
                            // MissFriday — Flash Freeze
                            FreezeNearbyEnemies();
                            Game.Instance.Audio.BeepFreeze();
                            _freezeFlashTimer = 0.001f;
                            break;
                    }
                }
            }

            // ── R — Break Wall ────────────────────────────────────────────────
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

            // ── B — Frost Ball ────────────────────────────────────────────────
            if (input.FrostBallPressed)
            {
                if (_player.TryShootFrostBall())
                {
                    float fx = _player.FacingRight ? _player.X + _player.Width + 4 : _player.X - 18;
                    float fy = _player.Y + _player.Height * 0.55f;
                    _frostBalls.Add(new FrostBallProjectile(fx, fy, _player.FacingRight));
                    Game.Instance.Audio.BeepFireball(); // reuse fireball sound
                }
            }

            // ── C — Quick Dash (works grounded or airborne) ───────────────────
            // Team 7 (Gameplay Programmer) — Idea 7: horizontal burst dash.
            // Grants i-frames and moves the player rapidly in the facing direction.
            if (input.AirDashPressed)
            {
                if (_player.TryDash())
                    Game.Instance.Audio.BeepJump();
            }
        }

        /// <summary>
        /// PHASE 2 — Team 4: Lead Game Designer.
        /// 15 % chance to drop a random power-up at the given world position.
        /// Heavier weight toward Mushroom to keep pacing fair.
        /// </summary>
        private void TryDropPowerUp(float cx, float cy)
        {
            if (_dropRng.NextDouble() > 0.15) return;
            PowerUp.PowerUpType[] pool =
            {
                PowerUp.PowerUpType.Mushroom,
                PowerUp.PowerUpType.Mushroom,
                PowerUp.PowerUpType.FireFlower,
                PowerUp.PowerUpType.SeaStar,
                PowerUp.PowerUpType.Star,
            };
            var type = pool[_dropRng.Next(pool.Length)];
            // PowerUp(x, y, type) — item pops up from drop position
            _powerUps.Add(new PowerUp(cx - 14, cy - 16, type));
        }

        /// <summary>Updates active power-ups and handles player collection.</summary>
        private void UpdatePowerUps(float dt)
        {
            for (int i = _powerUps.Count - 1; i >= 0; i--)
            {
                var pu = _powerUps[i];
                pu.Update(dt);
                if (pu.TryCollect(_player))
                {
                    // Map PowerUp.PowerUpType → SuitType and apply via PowerUpInventory
                    SuitType suit;
                    if (pu.Type == PowerUp.PowerUpType.FireFlower) suit = SuitType.FireFlower;
                    else if (pu.Type == PowerUp.PowerUpType.Star)  suit = SuitType.Star;
                    else                                           suit = SuitType.Mushroom;
                    PowerUpInventory.ApplySuit(suit);
                    // Mushroom / SeaStar also restore 25% HP
                    if (pu.Type == PowerUp.PowerUpType.Mushroom ||
                        pu.Type == PowerUp.PowerUpType.SeaStar)
                        _player.Health = Math.Min(_player.MaxHealth,
                                                  _player.Health + _player.MaxHealth / 4);
                    Game.Instance.Audio.BeepBerry();
                    _powerUps.RemoveAt(i);
                }
                else if (pu.IsExpired || pu.IsCollected)
                {
                    _powerUps.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// PHASE 2 — Orca Tidal Slam: damages all enemies within TidalSlamRadius.
        /// Triggered by the E-key ability when playing as Orca.
        /// </summary>
        private void TidalSlamAttack()
        {
            float radius = _player.TidalSlamRadius;
            int   damage = _player.TidalSlamDamage;
            ParticleSystem.SpawnBurst(_player.CenterX, _player.CenterY, 16,
                Color.DeepSkyBlue, 40f, 200f, 0.5f, 1.2f);
            foreach (var e in _enemies)
            {
                if (!e.IsAlive) continue;
                float dx = e.CenterX - _player.CenterX;
                float dy = e.CenterY - _player.CenterY;
                if ((float)Math.Sqrt(dx * dx + dy * dy) <= radius)
                {
                    e.TakeDamage(damage);
                    // Knock enemies outward from the slam epicentre
                    e.VelocityX = dx > 0 ? 220f : -220f;
                    e.VelocityY = -180f;
                }
            }
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
            // Reset wall-contact flags on the player each horizontal-resolve pass
            var player = c as Player;
            if (player != null)
            {
                player.IsOnLeftWall  = false;
                player.IsOnRightWall = false;
            }

            foreach (var plat in _platforms)
                if (c.Hitbox.IntersectsWith(plat))
                {
                    if (c.VelocityX > 0)
                    {
                        c.X = plat.Left - c.Width;
                        if (player != null && !c.IsGrounded) player.IsOnRightWall = true;
                    }
                    else if (c.VelocityX < 0)
                    {
                        c.X = plat.Right;
                        if (player != null && !c.IsGrounded) player.IsOnLeftWall = true;
                    }
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

            // Hazards (fire, water pits, sea-stone, etc.) are NOT solid — characters walk and
            // fall through them freely. Damage is applied via DevilFruitRules.Check() each frame.
            // DO NOT add hazard collision geometry here; it blocks enemies, stops the player at
            // water edges, and prevents falls into pits.
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

            // Hazards are NOT solid vertically — the player and enemies must fall through water
            // pits and fire zones. Standing on water or hovering over holes was caused by this
            // block pushing Y upward; it has been removed. Damage is via DevilFruitRules.Check().

            // Fall recovery (no fall damage): falling below the level resets the player.
            if (c.Y > Game.Instance.CanvasHeight + 200)
            {
                if (c == _player)
                {
                    c.X = 48;
                    c.Y = _groundY - c.Height;
                    c.VelocityX = 0f;
                    c.VelocityY = 0f;
                    c.IsGrounded = true;
                }
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
                // Allow stomps even during i-frames (blinking) so bounce combat remains reliable.
                if (_player.VelocityY > 0)
                {
                    float pBot = _player.Y + _player.Height;
                    float eTop = e.Y;
                    float overlap = pBot - eTop;
                    // Use half the enemy's height as the overlap threshold so fast falls still register as stomps
                    if (overlap > 0 && overlap < e.Height * 0.5f &&
                        _player.CenterX > e.X - 8 && _player.CenterX < e.X + e.Width + 8)
                    {
                        e.Health = 0;
                        _player.VelocityY = _player.JumpForce * 0.45f;
                        _player.IsGrounded = false;
                        Game.Instance.Audio.BeepStomp();

                        // Phase 2 T4 #10: Risk/Reward Scoring — double score near hazards.
                        int stompScore = e.ScoreValue;
                        if (IsPlayerNearHazard())
                        {
                            stompScore *= 2;
                            Game.Instance.FloatingText.Spawn("RISKY! ×2", (int)_player.CenterX,
                                (int)_player.Y - 28, Color.OrangeRed, large: true);
                        }
                        BountySystem.Award(stompScore);
                        Game.Instance.TotalBerriesCollected += 10;
                        _player.RegisterStompChain();
                        AchievementSystem.CheckCombo(_player.StompChain);
                        if (_player.IsGroundPounding)
                            AchievementSystem.Grant("ach_ground_pound");
                        stomped = true;
                    }
                }

                // ── DASH/DODGE CONTACT DAMAGE ──────────────────────────────────────
                // Swan's WingDash deals 18 damage on contact with enemies
                if (!stomped && e.IsAlive && _player.HasEffect(StatusEffect.Dodging) &&
                    _player.Hitbox.IntersectsWith(e.Hitbox))
                {
                    bool wasAlive = e.IsAlive;
                    e.TakeDamage(18);  // WingDash contact damage
                    if (wasAlive && !e.IsAlive)
                    {
                        BountySystem.Award(e.ScoreValue);
                        Game.Instance.TotalBerriesCollected += 10;
                        TryDropPowerUp(e.CenterX, e.Y);
                        Game.Instance.Audio.BeepAttack();
                    }
                    // Knockback from dash
                    float kbDir = _player.FacingRight ? 1f : -1f;
                    e.VelocityX = kbDir * 200f;
                    e.VelocityY = -100f;
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
                        // 15 % chance to drop a power-up on melee kill
                        TryDropPowerUp(e.CenterX, e.Y);
                    }
                    // Phase 2 T4 #9: Knockback Multiplier — scale by run speed.
                    float kbDir   = _player.FacingRight ? 1f : -1f;
                    float kbForce = 180f + Math.Abs(_player.VelocityX) * 0.4f;
                    e.VelocityX = kbDir * kbForce;
                    e.VelocityY = -80f;
                }

                // Horizontal body contact — skip if the player is clearly descending onto the enemy's head
                bool fallingOnTop = _player.VelocityY > 0 &&
                    (_player.Y + _player.Height) < (e.Y + e.Height * 0.6f);
                if (!stomped && !fallingOnTop && e.IsAlive && !_player.IsInvincible &&
                    _player.Hitbox.IntersectsWith(e.Hitbox))
                {
                    // Phase 2 T4 #6: Parry — open window deflects the hit
                    if (_player.TryParry())
                    {
                        // Parried — stun the enemy briefly and push them back
                        e.TakeDamage(_player.AttackDamage / 2);
                        e.VelocityX = _player.FacingRight ? 220f : -220f;
                        e.VelocityY = -180f;
                        Game.Instance.FloatingText.Spawn("PARRY!", (int)_player.CenterX,
                            (int)_player.Y - 20, Color.Gold, large: true);
                        Game.Instance.Audio.BeepAttack();
                        ParticleSystem.SpawnBurst(_player.CenterX, _player.CenterY,
                            10, Color.Gold, 80f, 200f, 0.3f, 0.6f);
                        AchievementSystem.Grant("ach_parry");
                    }
                    else
                    {
                        int healthBefore = _player.Health;
                        _player.TakeDamage(_player.MaxHealth / 10);
                        if (_player.Health < healthBefore)
                        {
                            Game.Instance.Audio.BeepHurt();
                            _player.ResetStompChain();
                        }
                    }
                }
            }

            // ── PROJECTILE vs ENEMY COLLISION ──────────────────────────────────
            // Frost Ball projectiles damage enemies on contact
            foreach (var fb in _frostBalls)
            {
                if (fb.IsActive) fb.CheckEnemyHit(_enemies);
            }

            // Fireball (Fire Flower) projectiles damage enemies on contact
            foreach (var fireball in _fireballs)
            {
                if (fireball.IsActive) fireball.CheckEnemyHit(_enemies, 10);
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

                    // Log pickup collection for diagnostics
                    SessionStats.Instance.RecordBerry(b.Value);
                    System.Diagnostics.Debug.WriteLine($"[PICKUP] Berry collected. Value: {b.Value}, Total this level: {_berriesCollected}, Total overall: {Game.Instance.TotalBerriesCollected}");
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
                    PowerUpInventory.AddHealthItem(1);
                    Game.Instance.FloatingText.Spawn("+1 MEDKIT", hp.X, hp.Y - 16, Color.LimeGreen, large: false);
                    Game.Instance.Audio.BeepHeal();

                    // Log health pickup for diagnostics
                    System.Diagnostics.Debug.WriteLine($"[PICKUP] Health pickup collected at ({hp.X:F0}, {hp.Y:F0})");
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
            // Generous range: any wall within 200 px of the player's centre is destroyed.
            // This ensures a freshly placed wall always breaks regardless of movement.
            const float range = 200f;
            int broke = 0;
            for (int i = _iceWalls.Count - 1; i >= 0; i--)
            {
                var wall = _iceWalls[i];
                if (!wall.IsAlive) continue;
                float dx = _player.CenterX - (wall.X + wall.Width / 2f);
                float dy = _player.CenterY - (wall.Y + wall.Height / 2f);
                if ((float)Math.Sqrt(dx * dx + dy * dy) <= range)
                {
                    wall.Health = 0;
                    // Spawn a SMASH! text at the wall's world position so the player
                    // gets clear visual feedback that the wall was destroyed.
                    Game.Instance.FloatingText.Spawn(
                        "SMASH!",
                        (int)(wall.X + wall.Width / 2f),
                        (int)wall.Y,
                        Color.OrangeRed, large: true);
                    broke++;
                }
            }
            // Shockwave damages nearby enemies regardless of wall count
            foreach (var e in _enemies)
                if (e.IsAlive && _player.DistanceTo(e) <= 120f)
                    e.TakeDamage(_player.BreakWallShockwaveDamage);
        }

        /// <summary>
        /// Phase 2 T4 #10: Risk/Reward Scoring — true when the player is within
        /// 80 px of any active hazard (water pit, fire source, or sea-stone zone).
        /// Used to award a ×2 score multiplier on stomps and kills near danger.
        /// </summary>
        private bool IsPlayerNearHazard()
        {
            const float dangerRadius = 80f;
            foreach (var hz in _hazards)
            {
                float dx = _player.CenterX - (hz.X + hz.Width  * 0.5f);
                float dy = _player.CenterY - (hz.Y + hz.Height * 0.5f);
                if (dx * dx + dy * dy < dangerRadius * dangerRadius)
                    return true;
            }
            return false;
        }

        private void CheckExit()
        {
            // Only complete the level when the player actually reaches the goal flag.
            if (!_levelComplete && _player.Hitbox.IntersectsWith(_exitFlag))
            {
                _levelComplete = true;
                _completeTimer = 0;
                ThreatSystem.OnIslandCleared();
                BountySystem.Award(500);
                Game.Instance.CrewBonds++;
                Game.Instance.Save.SetFlag(_islandId + "_complete");
                Game.Instance.Save.Save();
                Game.Instance.Audio.BeepLevelClear();

                // Achievement: no-death run
                if (_deathCount == 0)
                    AchievementSystem.Grant("ach_no_death");

                // Achievement: berry milestone (session berries vs total)
                AchievementSystem.CheckBerryMilestones(
                    _berriesCollected, Game.Instance.TotalBerriesCollected);
            }
        }

        private void UpdateComplete(float dt)
        {
            _completeTimer += dt;
            // Short completion pause, then push the SMB3 card-roulette mini-game.
            // Flow: IslandScene → CardRouletteScene → CourseClearScene → Overworld
            if (_completeTimer >= 0.35f && _completeTimer - dt < 0.35f)
            {
                string  id   = _islandId;
                string  name = _islandName;
                int     time = (int)_speedRunTimer;
                int     dead = _deathCount;

                // The card roulette runs first; when it finishes it pushes CourseClearScene.
                Game.Instance.Scenes.Push(new CardRouletteScene(() =>
                {
                    // Pop CardRoulette, then push CourseClear on top of IslandScene
                    Game.Instance.Scenes.Pop();
                    Game.Instance.Scenes.Push(new CourseClearScene(
                        name, time, dead,
                        onContinue: () =>
                        {
                            SessionStats.Instance.RecordLevelComplete();
                            Game.Instance.LevelJustCompleted = true;
                            Game.Instance.Scenes.Pop();  // pop CourseClear
                            Game.Instance.Scenes.Pop();  // pop IslandScene → Overworld
                        }));
                }));
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
            _deathCount++;  // track deaths for CourseClearScene grade
            SessionStats.Instance.RecordDeath();
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
            foreach (var mp in _movingPlatforms) mp.Draw(g);
            foreach (var hz in _hazards)   hz.Draw(g);
            foreach (var w  in _iceWalls)  w.Draw(g);
            foreach (var e  in _enemies)   if (e.IsAlive) e.Draw(g);
            foreach (var b  in _berries)   b.Draw(g);
            foreach (var hp in _healthPickups) hp.Draw(g);
            foreach (var pu in _powerUps)  pu.Draw(g);
            foreach (var sc in _starCoins) sc.Draw(g, _cameraX);
            foreach (var fb in _fireballs) fb.Draw(g);
            foreach (var fb in _frostBalls) fb.Draw(g);
            _combo.Draw(g);
            _player.Draw(g);
            if (_player.IsAttacking) DrawAttackArc(g);
            if (_breakShockwaveTimer > 0f) DrawBreakShockwave(g);

            // Draw exit flag with animated indicators
            DrawExitFlag(g);

            // Phase 2 — Team 9: Accessibility outline mode — draws bright borders
            // around every interactive object so they read against any background.
            if (Game.Instance.OutlineModeEnabled)
                DrawAccessibilityOutlines(g);

            g.ResetTransform();

            DrawSnowfall(g, W, H);          // tundra: screen-space falling snow
            DrawBubbles(g, W, H);           // coral: screen-space rising bubbles
            WeatherSystem.Draw(g, W, H);    // Phase 2 — Team 14: unified weather overlay
            DrawScreenFlashes(g, W, H);

            // ── Unified HUD (single call) ─────────────────────────────────────
            GameHUD.Draw(g, _player, W, H);

            if (_showRescue)    DrawRescuePrompt(g, W, H);
            if (_levelComplete) DrawComplete(g, W, H);
            DrawDevMenuButton(g);
        }

        /// <summary>
        /// Phase 2 — Team 9 (UI Programmer): Accessibility outline mode.
        /// Draws a thick coloured border around the player (cyan), living enemies (red),
        /// berries (gold) and health pickups (lime) so they are readable on any background.
        /// Called only when <see cref="Engine.Game.OutlineModeEnabled"/> is true.
        /// </summary>
        private void DrawAccessibilityOutlines(Graphics g)
        {
            // Player — cyan
            using (var pen = new System.Drawing.Pen(Color.Cyan, 2))
                g.DrawRectangle(pen, (int)_player.X, (int)_player.Y,
                    _player.Width, _player.Height);

            // Enemies — red (only living)
            using (var pen = new System.Drawing.Pen(Color.OrangeRed, 2))
                foreach (var e in _enemies)
                    if (e.IsAlive)
                        g.DrawRectangle(pen, (int)e.X, (int)e.Y, e.Width, e.Height);

            // Berries — gold
            using (var pen = new System.Drawing.Pen(Color.Gold, 1))
                foreach (var b in _berries)
                    if (!b.Collected)
                        g.DrawRectangle(pen, (int)b.X, (int)b.Y, b.Width, b.Height);

            // Health pickups — lime
            using (var pen = new System.Drawing.Pen(Color.LimeGreen, 2))
                foreach (var hp in _healthPickups)
                    if (!hp.Collected)
                        g.DrawRectangle(pen, hp.Hitbox);

            // Star coins — white
            using (var pen = new System.Drawing.Pen(Color.White, 2))
                foreach (var sc in _starCoins)
                    if (!sc.Collected)
                        g.DrawRectangle(pen, sc.Hitbox);
        }

        private void DrawBackground(Graphics g, int W, int H)
        {
            if (_bg != null)
            {
                // Painted background image available — draw it, then overlay parallax clouds on top
                g.DrawImage(_bg, 0, 0, W, H);
                _parallax?.Scroll(_cameraX, 0);
                _parallax?.Draw(g, W, H);
                return;
            }

            // No image — use the parallax stack as the full background (fallback)
            _parallax?.Scroll(_cameraX, 0);
            _parallax?.Draw(g, W, H);

            // If parallax also not ready, fall back to solid SMB3 sky gradient
            if (_parallax == null)
            {
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                    Color.FromArgb(92, 148, 252),
                    Color.FromArgb(180, 210, 255), 90f))
                    g.FillRectangle(br, 0, 0, W, H);
            }
       }

        /// <summary>
        /// Pre-renders all platforms into a world-space bitmap once after BuildLevel.
        /// DrawPlatforms then does a single g.DrawImage instead of looping and allocating
        /// GDI+ brushes/pens every frame, eliminating the first-frame GDI freeze.
        /// </summary>
        private void BakeTerrainCache()
        {
            _terrainCache?.Dispose();
            if (_platforms == null || _platforms.Count == 0) return;

            // Terrain bitmap covers the full level width at screen height
            int bmpW = _levelWidth;
            int bmpH = Game.Instance.CanvasHeight > 0 ? Game.Instance.CanvasHeight : 600;

            _terrainCache = new Bitmap(bmpW, bmpH);
            using (var tg = Graphics.FromImage(_terrainCache))
            {
                tg.Clear(Color.Transparent);

                // Resolve the per-island colour palette once
                Color baseCol, topCol, brickCol;
                switch (_islandId)
                {
                    case "wano":
                        baseCol  = Color.FromArgb(90,  80,  70);
                        topCol   = Color.FromArgb(60,  70,  80);
                        brickCol = Color.FromArgb(50,  55,  60);
                        break;
                    case "sky":
                        baseCol  = Color.FromArgb(200, 200, 220);
                        topCol   = Color.FromArgb(240, 240, 255);
                        brickCol = Color.FromArgb(180, 185, 210);
                        break;
                    case "harbor":
                        baseCol  = Color.FromArgb(145,  95,  48);
                        topCol   = Color.FromArgb(188, 138,  68);
                        brickCol = Color.FromArgb(105,  62,  22);
                        break;
                    case "coral":
                    case "sunken_gate":
                    case "kelp":
                    case "boiling_vent":
                    case "abyss":
                        baseCol  = Color.FromArgb(18,  52,  96);
                        topCol   = Color.FromArgb(22, 138, 152);
                        brickCol = Color.FromArgb(10,  33,  65);
                        break;
                    case "tundra":
                        baseCol  = Color.FromArgb(195, 215, 245);
                        topCol   = Color.FromArgb(235, 248, 255);
                        brickCol = Color.FromArgb(155, 180, 225);
                        break;
                    default:
                        baseCol  = Color.FromArgb(210, 160,  80);
                        topCol   = Color.FromArgb(60,  170,  50);
                        brickCol = Color.FromArgb(155, 100,  40);
                        break;
                }

                // Create brushes and pens once — reuse across all platforms
                using (var baseBr   = new SolidBrush(baseCol))
                using (var topBr    = new SolidBrush(topCol))
                using (var highlBr  = new SolidBrush(Color.FromArgb(90, 255, 255, 255)))
                using (var brickPen = new Pen(Color.FromArgb(55, brickCol), 1))
                {
                    foreach (var p in _platforms)
                    {
                        tg.FillRectangle(baseBr, p);

                        if (p.Height > 16)
                        {
                            for (int tx = p.Left + 32; tx < p.Right; tx += 32)
                                tg.DrawLine(brickPen, tx, p.Top, tx, p.Bottom);
                            for (int ty = p.Top + 16; ty < p.Bottom; ty += 16)
                                tg.DrawLine(brickPen, p.Left, ty, p.Right, ty);
                        }

                        tg.FillRectangle(topBr,   p.X, p.Y, p.Width, 6);
                        tg.FillRectangle(highlBr, p.X, p.Y, p.Width, 2);
                    }
                }
            }
        }

        private void DrawPlatforms(Graphics g, int H)
        {
            // Single BitBlt from the pre-baked terrain bitmap — no per-frame GDI allocations
            if (_terrainCache != null)
            {
                g.DrawImage(_terrainCache, 0, 0);
                return;
            }

            // ── Fallback: live render if cache is missing ─────────────────────
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

            // Use a static timer for smooth animation
            double glowTime = Environment.TickCount * 0.001;

            // ── Animated glow halo around goal flag ──────────────────────────────
            float glowPhase = (float)Math.Sin(glowTime * 2.0) * 0.5f + 0.5f;
            int glowAlpha = (int)(100 * glowPhase + 50);
            using (var br = new SolidBrush(Color.FromArgb(glowAlpha, 255, 200, 0)))
                g.FillEllipse(br, px - 30, top - 20, 70, 50);

            // ── Animated ">>> GO <<<" indicator above flag ──────────────────────────
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            using (var br = new SolidBrush(Color.Gold))
            {
                string arrow = ">>> GO <<<";
                g.DrawString(arrow, f, br, px - 35, top - 35);
            }

            // ── Animated arrows pointing down to goal ────────────────────────────
            float arrowPhase = (float)Math.Sin(glowTime * 2.5) * 4f;
            for (int i = 0; i < 3; i++)
            {
                int arrowY = (int)(top - 50 - (i * 12) - arrowPhase);
                if (arrowY > top - 70 && arrowY < top)
                {
                    int opacity = (int)(200 * (1f - Math.Abs(arrowPhase - (i * 4)) / 12f));
                    using (var pen = new Pen(Color.FromArgb(opacity, 255, 200, 0), 2))
                    {
                        g.DrawLine(pen, px - 12, arrowY, px, arrowY + 8);
                        g.DrawLine(pen, px + 12, arrowY, px, arrowY + 8);
                    }
                }
            }

            // ── SMB3-style goal flagpole ──────────────────────────────────────
            // Pole (silver/gray vertical bar) - made more visible
            using (var br = new SolidBrush(Color.FromArgb(220, 220, 230)))
                g.FillRectangle(br, px, top, 4, _exitFlag.Height);
            // Pole highlight
            using (var br = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
                g.FillRectangle(br, px, top, 2, _exitFlag.Height);
            // Gold ball on top - MUCH bigger and more prominent
            using (var br = new SolidBrush(Color.Gold))
                g.FillEllipse(br, px - 8, top - 13, 20, 20);
            using (var pen = new Pen(Color.DarkGoldenrod, 2))
                g.DrawEllipse(pen, px - 8, top - 13, 20, 20);

            // ── Checkered goal flag (SMB3 two-tone green) - ENLARGED ────────────────────
            int fw = 32, fh = 22;
            int fx = px + 2;
            using (var br = new SolidBrush(Color.FromArgb(50, 200, 50)))
                g.FillRectangle(br, fx, top, fw, fh);
            // Checker squares
            using (var br = new SolidBrush(Color.FromArgb(28, 140, 28)))
            {
                g.FillRectangle(br, fx,           top,        fw / 2, fh / 2);
                g.FillRectangle(br, fx + fw / 2,  top + fh / 2, fw / 2, fh / 2);
            }
            // "GOAL" label - bigger and bolder
            using (var f = new Font("Courier New", 9, FontStyle.Bold))
                g.DrawString("GOAL", f, Brushes.White, fx + 2, top + 4);

            // ── Border highlight to make flag POP ──────────────────────────────────
            using (var pen = new Pen(Color.Gold, 2))
                g.DrawRectangle(pen, fx - 2, top - 2, fw + 4, fh + 4);
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

            // ── Lives counter (♥ × N) ─────────────────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(220, Color.Crimson)))
                g.FillEllipse(br, 8, 58, 12, 12);
            using (var f = _infoFont)
                g.DrawString($"× {Game.Instance.CurrentLives}", f, Brushes.White, 24, 56);

            // ── INV / MED quick-key hints ─────────────────────────────────────
            using (var f = new Font("Courier New", 8, FontStyle.Bold))
            {
                g.DrawString("[I]INV", f, Brushes.Cyan,     8,  70);
                g.DrawString($"[H]MED×{PowerUpInventory.HealthItemCount}", f, Brushes.LimeGreen, 62, 70);
            }

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

            // ── Ability cooldown panels (Mega Man sub-weapon bars) ──────────────
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

                // SMB3 coin icon before the reward line
                using (var br = new SolidBrush(Color.FromArgb(255, 220, 0)))
                    g.FillEllipse(br, W - 324, 33, 12, 12);
                using (var br = new SolidBrush(Color.FromArgb(200, 150, 0)))
                {
                    g.FillEllipse(br, W - 321, 36, 6, 6);
                }

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
                {
                    g.FillEllipse(br, cx + 21, ry + 5, 5, 5);
                }

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
                    float fy = (float)((_levelAnim * 38f * (0.4f + i * 0.09f) + i * H / 55f) % H);
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
    }
}


