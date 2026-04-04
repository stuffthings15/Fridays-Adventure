using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Fridays_Adventure.Audio;
using Fridays_Adventure.Data;
using Fridays_Adventure.Scenes;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Engine
{
    /// <summary>
    /// Playable crew member selection used by all gameplay scenes.
    /// </summary>
    public enum PlayableCharacter
    {
        MissFriday,
        Orca,
        Swan
    }

    public sealed class Game
    {
        public static Game Instance { get; private set; }
        public static event Action OpenLogbookRequested;
        public static event Action CloseRequested;
        public static void RequestOpenLogbook() => OpenLogbookRequested?.Invoke();
        public static void RequestClose()        => CloseRequested?.Invoke();

        public InputManager  Input  { get; }
        public SceneManager  Scenes { get; }
        public AudioManager  Audio  { get; }
        public SaveData      Save   { get; private set; }

        public int   PlayerBounty  { get; set; }
        public float ThreatLevel   { get; set; }
        public int   CrewBonds     { get; set; }
        public int   ShipHealth    { get; set; } = 100;
        public int   Cargo         { get; set; }
        public int   Water         { get; set; } = 50;
        public int   Food          { get; set; } = 30;
        public int   SeaStoneCount          { get; set; }
        public int   TotalBerriesCollected { get; set; }
        public bool  GodMode               { get; set; }
        public string PlayerName           { get; set; } = "";

        /// <summary>
        /// Tracks which numbered level the player is currently on (1–4).
        /// Increments only when a level is successfully cleared.
        /// </summary>
        public int  CurrentLevel      { get; set; } = 1;

        /// <summary>
        /// Set to true by a gameplay scene immediately before it pops on success.
        /// OverworldScene.OnResume reads and resets this to increment CurrentLevel.
        /// </summary>
        public bool LevelJustCompleted { get; set; } = false;

        /// <summary>
        /// Currently selected playable character (set from Crew screen).
        /// </summary>
        public PlayableCharacter SelectedCharacter { get; set; } = PlayableCharacter.MissFriday;

        // ── Team 1 (Game Director) — World / Level label system ────────────────
        // Idea 2: SMB3 "WORLD X-X" numbering tracked on the Game singleton so every
        // scene can display the correct label without its own state.

        /// <summary>Current SMB3-style world number (1-based).</summary>
        public int WorldNumber { get; set; } = 1;

        /// <summary>Current level number within the world (1-based).</summary>
        public int LevelNumber { get; set; } = 1;

        /// <summary>
        /// Human-readable label, e.g. "WORLD 1-2".
        /// Team 1 (Game Director) — Idea 2.
        /// </summary>
        public string WorldLevelLabel => $"WORLD {WorldNumber}-{LevelNumber}";

        // ── Team 1 (Game Director) — Coin/Life system ─────────────────────────
        // Idea 4: 100-coins grants an extra life (classic SMB3 mechanic).

        /// <summary>Total coins collected; resets to 0 after each 100 to grant a life.</summary>
        public int CoinCount { get; private set; }

        /// <summary>
        /// Adds coins to the counter.  Awards an extra life for every 100 collected.
        /// Team 1 (Game Director) — Idea 4: 100-coins = 1 life reward.
        /// </summary>
        public void AddCoins(int amount)
        {
            CoinCount += amount;
            while (CoinCount >= 100)
            {
                CoinCount  -= 100;
                CurrentLives++;
                FloatingText.Spawn("1UP!", CanvasWidth / 2, 80, System.Drawing.Color.LimeGreen, large: true);
                DebugLogger.LogInfo("Game.AddCoins", $"1UP granted! Lives={CurrentLives}");
            }
        }

        // ── Team 1 (Game Director) — Per-level star ratings ───────────────────
        // Idea 3: 1-3 stars per cleared level stored in SaveData.

        /// <summary>
        /// Returns the 0–3 star rating for a level.
        /// Team 1 (Game Director) — Idea 3.
        /// </summary>
        public int GetLevelStars(int world, int level)
        {
            return Save.GetInt($"stars_w{world}_l{level}", 0);
        }

        /// <summary>
        /// Saves the star rating for a level (only if it's better than the stored value).
        /// Team 1 (Game Director) — Idea 3.
        /// </summary>
        public void SetLevelStars(int world, int level, int stars)
        {
            string key = $"stars_w{world}_l{level}";
            int current = Save.GetInt(key, 0);
            if (stars > current)
                Save.SetInt(key, stars);
        }

        // ── Team 1 (Game Director) — P-Wing item (skip a level) ───────────────
        // Idea 6: Player can expend a P-Wing to skip the next level automatically.

        /// <summary>
        /// Number of P-Wings the player holds (found in Toad Houses, etc.).
        /// Team 1 (Game Director) — Idea 6.
        /// </summary>
        public int PWingCount { get; set; }

        /// <summary>
        /// Spends one P-Wing to mark the current level as cleared without playing it.
        /// Returns false if no P-Wings remain.
        /// Team 1 (Game Director) — Idea 6.
        /// </summary>
        public bool UsePWing()
        {
            if (PWingCount <= 0) return false;
            PWingCount--;
            LevelJustCompleted = true;
            DebugLogger.LogInfo("Game.UsePWing", $"P-Wing used. Remaining: {PWingCount}");
            return true;
        }

        // ── Team 1 (Game Director) — Toad House bonus flag ────────────────────
        // Idea 5: Toad House rooms appear between worlds on the overworld map.

        /// <summary>
        /// True while the current overworld node is a Toad House bonus room.
        /// Cleared when the room is entered.
        /// Team 1 (Game Director) — Idea 5.
        /// </summary>
        public bool PendingToadHouse { get; set; }

        // ── Team 1 (Game Director) — Save slot selection ─────────────────────
        // Idea 7: SMB3 has 2 save files; we expose a 3-slot system.

        /// <summary>
        /// Index of the active save slot (0, 1, or 2).
        /// Team 1 (Game Director) — Idea 7.
        /// </summary>
        public int SaveSlot { get; private set; }

        /// <summary>
        /// Switches to the given save slot and reloads the save data.
        /// Team 1 (Game Director) — Idea 7.
        /// </summary>
        public void SwitchSaveSlot(int slot)
        {
            SaveSlot = Math.Max(0, Math.Min(2, slot));
            Save = SaveData.LoadSlot(SaveSlot);
            DebugLogger.LogInfo("Game.SwitchSaveSlot", $"Loaded save slot {SaveSlot}");
        }

        // ── Team 1 (Game Director) — Airship level marker ─────────────────────
        // Idea 10: Airship levels are flagged so the intro card and music differ.

        /// <summary>
        /// True when the current level is an Airship encounter.
        /// Set by OverworldScene before transitioning.
        /// Team 1 (Game Director) — Idea 10.
        /// </summary>
        public bool IsAirshipLevel { get; set; }

        // ── Team 1 (Game Director) — World chapter clear detection ────────────
        // Idea 8: Game tracks whether ALL levels in a world have been cleared.

        /// <summary>
        /// Returns true if all <paramref name="levelCount"/> levels in the given world
        /// have been cleared (have at least 1 star).
        /// Team 1 (Game Director) — Idea 8.
        /// </summary>
        public bool IsWorldCleared(int world, int levelCount = 4)
        {
            for (int l = 1; l <= levelCount; l++)
                if (GetLevelStars(world, l) == 0)
                    return false;
            return true;
        }

        public int CanvasWidth  { get; private set; } = 900;
        public int CanvasHeight { get; private set; } = 600;

        private readonly GameCanvas _canvas;
        private readonly Timer      _timer;
        private const float FixedDt = 1f / 60f;

        // ── New cross-cutting systems ─────────────────────────────────────────

        /// <summary>
        /// Screen-shake manager (Team 17 VFX, Team 9 UI).
        /// Scenes and entities call Game.Instance.ScreenShake.Trigger(trauma).
        /// </summary>
        public ScreenShake ScreenShake { get; } = new ScreenShake();

        /// <summary>
        /// Floating score / damage text popups (Team 17 VFX, Team 9 UI).
        /// Scenes call Game.Instance.FloatingText.Spawn("+100", x, y, Color.Gold).
        /// </summary>
        public FloatingTextManager FloatingText { get; } = new FloatingTextManager();

        /// <summary>
        /// Per-session play statistics (Team 2 Producer, Team 19 QA).
        /// </summary>
        public SessionStats Stats => SessionStats.Instance;

        /// <summary>
        /// Player's current remaining lives (SMB3-style 3-lives system).
        /// </summary>
        public int CurrentLives { get; set; } = 3;

        /// <summary>
        /// Persistent achievement notification banner text.
        /// Set by AchievementSystem; cleared after the banner display timer expires.
        /// </summary>
        public string AchievementBannerText  { get; set; } = null;
        public Color  AchievementBannerColor { get; set; } = Color.Gold;
        private float _achievementBannerTimer = 0f;

        public Game(GameCanvas canvas)
        {
            Instance = this;
            _canvas  = canvas;
            Input    = new InputManager();
            Scenes   = new SceneManager();
            Audio    = new AudioManager();
            Save     = SaveData.Load();

            // Restore runtime fields from persisted save data.
            ApplySaveData(Save);

            _canvas.Render += OnRender;
            _canvas.Resize += (s, e) =>
            {
                CanvasWidth  = _canvas.Width;
                CanvasHeight = _canvas.Height;
            };
            CanvasWidth  = _canvas.Width;
            CanvasHeight = _canvas.Height;

            _timer          = new Timer { Interval = 16 };
            _timer.Tick    += OnTick;

            // Subscribe achievement notification banner to EventBus.
            EventBus.Subscribe<AchievementEarnedEvent>(OnAchievementEarned);
        }

        public void Start()
        {
            // ── Load feature flags and accessibility settings ──────────────────
            // Team 2 (Producer) — Idea 1: feature flags; Idea 4: accessibility.
            FeatureFlags.Load();
            AccessibilityOptions.SyncFromFlags();

            // ── Write build manifest and clean old logs ────────────────────────
            // Team 11 (Build Engineer) — Idea 1: manifest; Team 8: log cleanup.
            BuildEngineerExtensions.WriteBuildManifest();
            DebugLogger.CleanOldLogs(7);

            Audio.LoadAll();

            // Audio recovery: if old/corrupt save muted everything, restore usable defaults.
            int musicVol = Save.MusicVolume;
            int sfxVol   = Save.SfxVolume;
            if (musicVol <= 0) musicVol = 80;
            if (sfxVol <= 0)   sfxVol = 80;

            Audio.SetMusicVolume(musicVol);
            Audio.SetSfxVolume(sfxVol);
            Audio.ApplySavedPlaylists(Save.PlaylistData);
            Audio.Prewarm();             // open first track on background thread
            
            // PHASE 2 - Team 1: Game Director
            // Initialize difficulty modifiers from saved config
            DifficultyModifiers.Initialize();
            
            Scenes.Push(new LoadingScene());
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            Audio.StopMusic();

            SyncRuntimeToSaveData();
            Save.Save();

            // Write QA session close log.
            VisualDebugger.WriteSessionClose();
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                float dt = FixedDt;

                // Emergency path for unbeatable levels.
                if (Input.IsPressed(Keys.F8))
                    TryFailSafeCompleteLevel();

                // Global inventory hotkey during gameplay maps.
                var scene = Scenes.Current;
                if (scene != null && IsGameplayScene(scene) && Input.IsPressed(Keys.I))
                    Scenes.Push(new InventoryScene(GetActiveScenePlayer()));

                // Global medkit quick-use hotkey during gameplay maps.
                if (scene != null && IsGameplayScene(scene) && Input.IsPressed(Keys.H))
                {
                    var player = GetActiveScenePlayer();
                    if (PowerUpInventory.UseHealthItem(player))
                    {
                        Audio.BeepHeal();
                        SMB3Hud.ShowToast($"Used medkit. Remaining: {PowerUpInventory.HealthItemCount}");
                    }
                }

                Audio.Tick(dt);
                Scenes.Current?.Update(dt);
                ScreenShake.Update(dt);
                FloatingText.Update(dt);
                SessionStats.Instance.Tick(dt);

                // Advance SMB3 HUD timers: GET READY, world label, death fade, toasts.
                // Team 9 (UI Programmer) — Ideas 3–10: HUD timer advancement.
                SMB3Hud.Update(dt);
                SMB3Hud.UpdateToasts(dt);
                SMB3Hud.UpdateDeathFade(dt);

                // ── Wave 2 team feature tickers ────────────────────────────────
                // Team 1  (Game Director)   — level timer, course clear
                GameDirectorFeatures.Update(dt);
                // Team 4  (Lead Designer)   — power-up timers (star, float, etc.)
                PowerUpFeatureSet.Update(dt);
                // Team 7  (Gameplay Prog.)  — stomp chain, spin jump
                GameplayFeatures.Update(dt);
                // Team 8  (Systems Prog.)   — combo multiplier window
                SystemsFeatures.Update(dt);
                // Team 13 (Character Art)   — damage flash timer
                CharacterArtFeatures.Update(dt);
                // Team 14 (Environment Art) — water ripple, torch flicker, stars
                EnvironmentFeatures.Update(dt);
                // Team 16 (2D Animator)     — squash-and-stretch, coin spin
                AnimationFeatures.Update(dt);
                // Team 17 (VFX Artist)      — particle lifetimes
                VFXFeatures.Update(dt);
                // Team 9  (UI Programmer)   — coin spin, cursor blink
                UIFeatures.UpdateCoinSpin(dt);
                UIArtFeatures.UpdateCursor(dt);
                // Tech Lead (Team 3)        — frame-time histogram
                TechLeadFeatures.RecordFrameTime(dt);
                // QA (Team 19)              — frame spike detection
                QAFeatures.CheckFrameSpike(dt);

                if (_achievementBannerTimer > 0f)
                    _achievementBannerTimer -= dt;
                Input.EndFrame();
                _canvas.Invalidate();
            }
            catch (Exception ex)
            {
                // Error debugger entry for update-loop failures.
                DebugLogger.LogError("Game.OnTick", ex);
                Input.EndFrame();
            }
        }

        private void OnRender(System.Drawing.Graphics g)
        {
            try
            {
                // Crisp nearest-neighbour scaling — prevents sprite/background blurriness.
                g.SmoothingMode       = System.Drawing.Drawing2D.SmoothingMode.None;
                g.InterpolationMode   = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode     = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.CompositingQuality  = System.Drawing.Drawing2D.CompositingQuality.AssumeLinear;

                // Apply screen shake translation before drawing scene.
                ScreenShake.ApplyTranslation(g);
                Scenes.Current?.Draw(g);
                ScreenShake.ResetTranslation(g);

                // Floating text drawn after scene but before HUD overlay.
                FloatingText.Draw(g);

                // Team 17 (VFX Artist) — draw particle effects after scene.
                VFXFeatures.Draw(g);

                // Achievement banner (SMB3-style slide-in notification).
                if (_achievementBannerTimer > 0f)
                    DrawAchievementBanner(g);

                // Visual debugger overlay (F10 toggle).
                VisualDebugger.DrawOverlay(g, CanvasWidth, CanvasHeight);
            }
            catch (Exception ex)
            {
                // Error debugger entry for render-loop failures.
                DebugLogger.LogError("Game.OnRender", ex);
            }
        }

        /// <summary>
        /// Draws the SMB3-style achievement notification banner at the top of the screen.
        /// </summary>
        private void DrawAchievementBanner(System.Drawing.Graphics g)
        {
            const float ShowDuration = 3.0f;
            float alpha = _achievementBannerTimer < 0.5f
                ? _achievementBannerTimer / 0.5f
                : (_achievementBannerTimer > ShowDuration - 0.5f
                    ? (_achievementBannerTimer - (ShowDuration - 0.5f)) / 0.5f
                    : 1f);

            int W = CanvasWidth;
            using (var br = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((int)(200 * alpha), 10, 10, 30)))
                g.FillRectangle(br, 0, 0, W, 44);
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb((int)(180 * alpha), AchievementBannerColor), 2))
                g.DrawLine(pen, 0, 43, W, 43);

            using (var f = new System.Drawing.Font("Courier New", 11, System.Drawing.FontStyle.Bold))
            using (var br = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((int)(255 * alpha), AchievementBannerColor)))
            {
                string label = $"★  ACHIEVEMENT UNLOCKED: {AchievementBannerText}  ★";
                var sz = g.MeasureString(label, f);
                g.DrawString(label, f, br, (W - sz.Width) / 2f, 10);
            }
        }

        private void OnAchievementEarned(AchievementEarnedEvent evt)
        {
            AchievementBannerText  = evt.Achievement.Name;
            AchievementBannerColor = evt.Achievement.Color;
            _achievementBannerTimer = 3.0f;
        }

        /// <summary>
        /// Applies a loaded SaveData object to the game runtime state.
        /// </summary>
        /// <remarks>PHASE 2 - Team 8: JSON import support.</remarks>
        public void ApplySaveData(SaveData loaded)
        {
            if (loaded == null) return;

            Save = loaded;

            // Runtime stats restored from save data.
            PlayerBounty  = loaded.PlayerBounty;
            ThreatLevel   = loaded.ThreatLevel;
            CrewBonds     = loaded.CrewBonds;
            ShipHealth    = loaded.ShipHealth;
            Water         = loaded.Water;
            Food          = loaded.Food;
            SeaStoneCount = loaded.SeaStoneCount;

            // Persisted progression values.
            CurrentLevel = Math.Max(1, loaded.GetInt("runtime.currentLevel", CurrentLevel));
            WorldNumber  = Math.Max(1, loaded.GetInt("runtime.world", WorldNumber));
            LevelNumber  = Math.Max(1, loaded.GetInt("runtime.level", LevelNumber));
            CurrentLives = Math.Max(1, loaded.GetInt("runtime.lives", CurrentLives));
            PWingCount   = Math.Max(0, loaded.GetInt("runtime.pwing", PWingCount));

            int charIdx = loaded.GetInt("runtime.character", (int)SelectedCharacter);
            if (charIdx < 0 || charIdx > 2) charIdx = (int)PlayableCharacter.MissFriday;
            SelectedCharacter = (PlayableCharacter)charIdx;

            // Audio settings from imported save.
            Audio.SetMusicVolume(loaded.MusicVolume <= 0 ? 80 : loaded.MusicVolume);
            Audio.SetSfxVolume(loaded.SfxVolume <= 0 ? 80 : loaded.SfxVolume);
            Audio.ApplySavedPlaylists(loaded.PlaylistData);

            DebugLogger.LogInfo("Game.ApplySaveData", "Save data applied to runtime state.");
        }

        /// <summary>
        /// Copies current runtime state back into SaveData before persisting.
        /// </summary>
        /// <remarks>PHASE 2 - Team 8: JSON export support.</remarks>
        public void SyncRuntimeToSaveData()
        {
            Save.PlayerBounty  = PlayerBounty;
            Save.ThreatLevel   = ThreatLevel;
            Save.CrewBonds     = CrewBonds;
            Save.ShipHealth    = ShipHealth;
            Save.Water         = Water;
            Save.Food          = Food;
            Save.SeaStoneCount = SeaStoneCount;

            Save.MusicVolume = Audio.MusicVolume;
            Save.SfxVolume   = Audio.SfxVolume;

            Save.SetInt("runtime.currentLevel", CurrentLevel);
            Save.SetInt("runtime.world", WorldNumber);
            Save.SetInt("runtime.level", LevelNumber);
            Save.SetInt("runtime.lives", CurrentLives);
            Save.SetInt("runtime.pwing", PWingCount);
            Save.SetInt("runtime.character", (int)SelectedCharacter);
        }

        /// <summary>
        /// Emergency completion shortcut for unbeatable levels.
        /// Press F8 to instantly mark the current gameplay level complete.
        /// </summary>
        /// <remarks>PHASE 2 - Team 5: fail-safe completion path.</remarks>
        private void TryFailSafeCompleteLevel()
        {
            var scene = Scenes.Current;
            bool isGameplayLevel =
                scene is IslandScene || scene is SkyIslandScene || scene is UnderwaterScene ||
                scene is AirshipLevelScene || scene is FortressScene || scene is StormScene ||
                scene is BossScene || scene is WarlordBossScene;

            if (!isGameplayLevel) return;

            LevelJustCompleted = true;
            CurrentLevel = Math.Max(1, CurrentLevel + 1);
            SMB3Hud.ShowToast("FAIL-SAFE COMPLETE (F8): Level marked cleared.");
            DebugLogger.LogWarning("Game.FailSafeComplete",
                $"Scene '{scene.GetType().Name}' force-completed via F8.");

            // Return player to map so progression can continue.
            Scenes.Replace(new OverworldScene());
        }

        /// <summary>
        /// Attempts to resolve the active scene's player field for shared UI actions.
        /// </summary>
        public Entities.Player GetActiveScenePlayer()
        {
            var scene = Scenes.Current;
            if (scene == null) return null;

            var fi = scene.GetType().GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic);
            return fi?.GetValue(scene) as Entities.Player;
        }

        /// <summary>
        /// Returns true when the current scene is a gameplay map where HUD/inventory hotkeys are valid.
        /// </summary>
        private bool IsGameplayScene(Scene scene)
        {
            return scene is IslandScene || scene is SkyIslandScene || scene is StormScene ||
                   scene is BossScene || scene is WarlordBossScene || scene is FortressScene ||
                   scene is AirshipLevelScene || scene is UnderwaterScene;
        }

        /// <summary>
        /// Handles top-HUD click actions (inventory / medkit) for gameplay scenes.
        /// Returns true when the click is consumed.
        /// </summary>
        public bool TryHandleHudClick(Point p)
        {
            var scene = Scenes.Current;
            if (scene == null || !IsGameplayScene(scene)) return false;
            return SMB3Hud.HandleHudClick(p, GetActiveScenePlayer());
        }
    }
}
