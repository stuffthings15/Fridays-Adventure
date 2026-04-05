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

        // ── PHASE 3: Team 1 (Game Director) — New SMB3 systems ────────────────

        /// <summary>
        /// Number of Warp Whistles held. Using one opens the World Warp panel.
        /// Team 1 (Game Director) — Phase 3.
        /// </summary>
        public int WarpWhistleCount { get; set; }

        /// <summary>
        /// Total star coins collected across all levels.
        /// Team 4 (Lead Game Designer) — Phase 3.
        /// </summary>
        public int StarCoinsTotal { get; set; }

        /// <summary>
        /// Star coins collected in the current level session (reset on level enter).
        /// Team 4 (Lead Game Designer) — Phase 3.
        /// </summary>
        public int StarCoinsThisLevel { get; set; }

        /// <summary>
        /// When true, the Overworld will push a ToadHouseScene on the next resume.
        /// Team 1 (Game Director) — Phase 3. (See also Idea 5 above.)
        /// </summary>
        // NOTE: PendingToadHouse is defined above (Idea 5). This duplicate is removed.

        /// <summary>
        /// Uses one Warp Whistle to advance to the given world number.
        /// Returns false if no whistles remain.
        /// Team 1 (Game Director) — Phase 3.
        /// </summary>
        public bool UseWarpWhistle(int targetWorld)
        {
            if (WarpWhistleCount <= 0) return false;
            WarpWhistleCount--;
            WorldNumber = Math.Max(1, targetWorld);
            LevelNumber = 1;
            LevelJustCompleted = false;
            DebugLogger.LogInfo("Game.UseWarpWhistle", $"Warped to World {WorldNumber}.");
            return true;
        }

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

        /// <summary>
        /// Elapsed seconds in the current level — set by IslandScene each frame.
        /// Read by GameHUD to display the speed-run clock.
        /// Team 1 (Game Director) — Phase 2 Idea 3: Speed Run Timer.
        /// </summary>
        public float LevelElapsedSeconds { get; set; }

        /// <summary>
        /// True on the second playthrough (New Game+ mode).
        /// Enemies receive a 1.5× HP bonus and drop better items.
        /// Team 1 (Game Director) — Phase 3 Idea 1: New Game+ Mode.
        /// </summary>
        public bool NewGamePlus { get; set; }

        /// <summary>
        /// When true, a CRT scanline overlay is drawn over the final frame.
        /// Team 12 (Art Director) — Phase 2 Wave 2 Idea 5: CRT scanline filter.
        /// </summary>
        public bool CrtFilterEnabled { get; set; }

        // ── Phase 2: Team 9 — Accessibility Outline Mode ──────────────────────
        /// <summary>
        /// When true, a solid coloured outline is drawn around every entity and
        /// interactive object so players with visual impairments can track them.
        /// Toggleable from the Options screen or Settings menu.
        /// Phase 2 — Team 9 (UI Programmer) Idea 10 / accessibility feature.
        /// </summary>
        public bool OutlineModeEnabled { get; set; }

        private readonly GameCanvas _canvas;
        private readonly Timer      _timer;

        // Global quick-access buttons (always visible): Inventory + Options.
        private Rectangle _globalInventoryBtn;
        private Rectangle _globalOptionsBtn;
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
        private float _producerReminderToastCooldown;

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

            // Team 2 (Producer) — runtime production system events.
            EventBus.Subscribe<SprintIntervalEvent>(OnSprintInterval);
            EventBus.Subscribe<PlaytimeLimitEvent>(OnPlaytimeLimit);
        }

        public void Start()
        {
            // ── Load feature flags and accessibility settings ──────────────────
            // Team 2 (Producer) — Idea 1: feature flags; Idea 4: accessibility.
            FeatureFlags.Load();
            AccessibilityOptions.SyncFromFlags();
            ABVariants.Set("HUD_LAYOUT", "smb3_classic");

            // Phase 3 (Team 8) — achievement unlock analytics logger subscription.
            AchievementUnlockLogger.EnsureSubscribed();

            // Team 2 (Producer) — Idea 6: playtime limit warning system.
            PlaytimeLimit.SetLimit(Save.GetInt("runtime.playtimeLimitMinutes", 0));

            // Team 2 (Producer) — Idea 7: local update checker stub.
            string updateNotice = UpdateChecker.CheckForUpdate();
            if (!string.IsNullOrWhiteSpace(updateNotice))
                SMB3Hud.ShowToast(updateNotice);

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

            // Team 3 (Technical Lead) — Idea 4: external config hot-reload watcher.
            HotReloadConfig.StartWatching();
            
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

            // Team 3 (Technical Lead) — cleanly stop config watcher on shutdown.
            HotReloadConfig.Stop();

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

                // Global quick overlays:
                // I = Inventory, Esc = Options (pause-style behavior) from any scene.
                bool handledGlobalOverlay = HandleGlobalOverlayHotkeys();
                if (!handledGlobalOverlay)
                    Scenes.Current?.Update(dt);

                // Team 3 (Technical Lead) — process deferred config reload events.
                HotReloadConfig.Tick();

                // Advance global curtain-wipe transitions.
                // Without this, SceneTransition.Begin(...) callbacks never fire,
                // which can stall level-complete flow on a black screen.
                SceneTransition.Update(dt);

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
                // Phase 3 (Team 3) — advanced replay frame capture.
                ReplaySystemAdvanced.CaptureFrame(dt);
                // Team 9  (UI Programmer)   — coin spin, cursor blink
                UIFeatures.UpdateCoinSpin(dt);
                UIArtFeatures.UpdateCursor(dt);

                // Team 2 (Producer) — Ideas 2/5/6 runtime producers.
                SprintTimer.Tick(dt);
                AutoSaveReminder.Tick(dt);
                PlaytimeLimit.Tick(dt);
                _producerReminderToastCooldown = Math.Max(0f, _producerReminderToastCooldown - dt);
                if (AutoSaveReminder.ShouldRemind && _producerReminderToastCooldown <= 0f)
                {
                    SMB3Hud.ShowToast("Reminder: Save your progress.");
                    _producerReminderToastCooldown = 30f;
                }

                // Tech Lead (Team 3)        — frame-time histogram
                TechLeadFeatures.RecordFrameTime(dt);
                FrameTimeHistogram.RecordFrame(dt);
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
                // ── High-definition rendering pipeline ───────────────────────────
                // Use high-quality bicubic interpolation for sprite scaling so
                // character art and backgrounds stay sharp at any resolution.
                g.InterpolationMode   = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode       = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode     = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality  = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.CompositingMode     = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                g.TextRenderingHint   = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Apply screen shake translation before drawing scene.
                ScreenShake.ApplyTranslation(g);
                Scenes.Current?.Draw(g);
                ScreenShake.ResetTranslation(g);

                // Always-visible quick-access UI (Inventory / Options).
                DrawGlobalQuickButtons(g);

                // Floating text drawn after scene but before HUD overlay.
                FloatingText.Draw(g);

                // Team 17 (VFX Artist) — draw particle effects after scene.
                VFXFeatures.Draw(g);

                // Achievement banner (SMB3-style slide-in notification).
                if (_achievementBannerTimer > 0f)
                    DrawAchievementBanner(g);

                // CRT scanline overlay (Phase 2 — Team 12 Art Director).
                if (CrtFilterEnabled)
                    DrawCrtOverlay(g);

                // Visual debugger overlay (F10 toggle).
                VisualDebugger.DrawOverlay(g, CanvasWidth, CanvasHeight);

                // Team 10 (Engine Programmer) — frame-time histogram overlay.
                // Shown in GodMode for dev/QA visibility.
                if (GodMode)
                    DrawPerfHistogramOverlay(g);
            }
            catch (Exception ex)
            {
                // Error debugger entry for render-loop failures.
                DebugLogger.LogError("Game.OnRender", ex);
            }
        }

        /// <summary>
        /// Draws a compact performance overlay with live frame graph + histogram summary.
        /// Team 10 (Engine Programmer) — Phase 2 Idea 4: Frame Time Histogram.
        /// </summary>
        private void DrawPerfHistogramOverlay(System.Drawing.Graphics g)
        {
            const int x = 8;
            const int y = 56;
            const int w = 230;
            const int h = 70;

            TechLeadFeatures.DrawFrameGraph(g, x, y, w, h);

            string summary = FrameTimeHistogram.GetSummary();
            int nl = summary.IndexOf('\n');
            if (nl > 0) summary = summary.Substring(0, nl);

            using (var f = new System.Drawing.Font("Courier New", 7, System.Drawing.FontStyle.Bold))
            using (var br = new System.Drawing.SolidBrush(System.Drawing.Color.Cyan))
                g.DrawString(summary, f, br, x + 2, y + h + 2);
        }

        /// <summary>
        /// CRT scanline filter — draws semi-transparent horizontal lines across the entire
        /// canvas to simulate an old CRT monitor. Toggled via Options menu.
        /// Phase 2 — Team 12 (Art Director) Wave 2 Idea 5.
        /// </summary>
        private void DrawCrtOverlay(System.Drawing.Graphics g)
        {
            using (var pen = new System.Drawing.Pen(
                System.Drawing.Color.FromArgb(28, 0, 0, 0), 1f))
            {
                for (int y = 0; y < CanvasHeight; y += 2)
                    g.DrawLine(pen, 0, y, CanvasWidth, y);
            }
            // Subtle vignette — darken the corners
            using (var vgPath = new System.Drawing.Drawing2D.GraphicsPath())
            {
                vgPath.AddEllipse(-100, -100, CanvasWidth + 200, CanvasHeight + 200);
                using (var pgb = new System.Drawing.Drawing2D.PathGradientBrush(vgPath))
                {
                    pgb.CenterColor    = System.Drawing.Color.Transparent;
                    pgb.SurroundColors = new[] { System.Drawing.Color.FromArgb(80, 0, 0, 0) };
                    g.FillRectangle(pgb, 0, 0, CanvasWidth, CanvasHeight);
                }
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

        private void OnSprintInterval(SprintIntervalEvent evt)
        {
            SMB3Hud.ShowToast($"Sprint checkpoint: {SessionStats.Instance.PlayTimeFormatted}");
        }

        private void OnPlaytimeLimit(PlaytimeLimitEvent evt)
        {
            if (evt.IsHardLimit)
            {
                SMB3Hud.ShowToast("Playtime limit reached. Consider taking a break.");
                DebugLogger.LogWarning("Game.PlaytimeLimit", "Hard playtime limit reached.");
            }
            else
            {
                SMB3Hud.ShowToast("Playtime warning: approaching session limit.");
            }
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
            AutoSaveReminder.NotifySaved();

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

        /// <summary>
        /// Handles always-visible quick UI button clicks (Inventory / Options).
        /// </summary>
        public bool TryHandleGlobalUiClick(Point p)
        {
            if (_globalInventoryBtn.Contains(p))
            {
                ToggleInventoryOverlay();
                return true;
            }

            if (_globalOptionsBtn.Contains(p))
            {
                OpenOptionsOverlay();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles global hotkeys that should work at all times.
        /// </summary>
        private bool HandleGlobalOverlayHotkeys()
        {
            var scene = Scenes.Current;
            if (scene == null || scene is LoadingScene) return false;

            // Inventory hotkey (I): toggle inventory scene.
            if (Input.InventoryPressed)
            {
                ToggleInventoryOverlay();
                return true;
            }

            // Pause/options hotkey (Esc): open options overlay from anywhere,
            // or close it if already on the OptionsScene.
            if (Input.PausePressed)
            {
                if (scene is OptionsScene)
                {
                    Scenes.Pop();
                    return true;
                }
                OpenOptionsOverlay();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Opens options if not already open.
        /// </summary>
        private void OpenOptionsOverlay()
        {
            if (Scenes.Current is OptionsScene) return;
            Scenes.Push(new OptionsScene());
        }

        /// <summary>
        /// Toggles inventory overlay scene.
        /// </summary>
        private void ToggleInventoryOverlay()
        {
            if (Scenes.Current is InventoryScene)
            {
                Scenes.Pop();
                return;
            }

            Scenes.Push(new InventoryScene(GetActiveScenePlayer()));
        }

        /// <summary>
        /// Draws always-visible quick buttons for inventory and options access.
        /// </summary>
        private void DrawGlobalQuickButtons(Graphics g)
        {
            int W = CanvasWidth;
            _globalInventoryBtn = new Rectangle(W - 206, 0, 102, 28);
            _globalOptionsBtn   = new Rectangle(W - 102, 0, 102, 28);

            using (var invBr = new SolidBrush(Color.FromArgb(205, 30, 95, 40)))
                g.FillRectangle(invBr, _globalInventoryBtn);
            using (var optBr = new SolidBrush(Color.FromArgb(205, 30, 65, 120)))
                g.FillRectangle(optBr, _globalOptionsBtn);

            using (var pen = new Pen(Color.FromArgb(220, 220, 220), 1))
            {
                g.DrawRectangle(pen, _globalInventoryBtn);
                g.DrawRectangle(pen, _globalOptionsBtn);
            }

            using (var f = new Font("Courier New", 9, FontStyle.Bold))
            {
                g.DrawString("I INVENTORY", f, Brushes.White, _globalInventoryBtn.X + 8, _globalInventoryBtn.Y + 7);
                g.DrawString("ESC OPTIONS", f, Brushes.White, _globalOptionsBtn.X + 8, _globalOptionsBtn.Y + 7);
            }
        }
    }
}
