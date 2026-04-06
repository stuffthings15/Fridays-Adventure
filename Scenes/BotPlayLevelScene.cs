using System;
using System.Drawing;
using System.Windows.Forms;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Tests;

namespace Fridays_Adventure.Scenes
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Bot Play Level Scene
    // Purpose: Wraps any real game scene (IslandScene, StormScene, etc.) and
    //          drives it with BotPlayerController synthetic input so the player
    //          can watch the actual game run as if a human were playing.
    //          Used by QA Visual Mode and Demo Mode.
    // ────────────────────────────────────────────

    /// <summary>
    /// A transparent "overlay scene" that runs a real gameplay scene (passed in
    /// via <see cref="Scene"/> reference) using bot-injected input rather than
    /// the keyboard.
    ///
    /// Every frame:
    ///   1. BotPlayerController decides which keys to hold / press.
    ///   2. Those keys are injected into Game.Instance.Input.
    ///   3. The inner game scene's Update() runs — it sees them as real input.
    ///   4. The inner scene's Draw() renders the full game.
    ///   5. A small semi-transparent HUD overlay is drawn on top.
    ///   6. Injected keys are cleared so they don't bleed into the next frame.
    ///
    /// When the inner scene signals completion (pushes its own result scenes onto
    /// the stack, or after a timeout), BotPlayLevelScene cleans up and calls
    /// <see cref="_onFinished"/> so the caller (DemoModeScene or AutoTestLevelScene)
    /// can advance to the next level.
    /// </summary>
    public sealed class BotPlayLevelScene : Scene
    {
        // ── Inner scene being played ──────────────────────────────────────
        private readonly Scene _inner;

        // ── Bot brain ─────────────────────────────────────────────────────
        private readonly BotPlayerController _bot = new BotPlayerController();

        // ── Level identity (for display) ──────────────────────────────────
        private readonly string _levelName;

        // ── Timeout ───────────────────────────────────────────────────────
        private const float MAX_LEVEL_TIME = 90f;   // seconds before auto-advance
        private float _elapsed = 0f;

        // ── Event tracking for comprehensive bot logging ─────────────────
        private int _pickupsCollected = 0;
        private int _enemiesDefeated = 0;
        private int _cardRouletteSelectCount = 0;
        private bool _cardRouletteEntered = false;
        private float _cardRouletteStartTime = 0f;

        // ── Bot Activity Logger ───────────────────────────────────────────
        private Tests.ComprehensiveBotActivityLogger _botActivityLogger = null;
        // ── Completion tracking ───────────────────────────────────────────
        /// <summary>
        /// True once the inner scene has signalled completion, either by pushing
        /// result scenes (Path A) or by setting an internal flag (Path B).
        /// </summary>
        private bool  _innerCompleted = false;
        private float _completionHoldTimer = 0f;
        private const float COMPLETION_HOLD = 5f;  // show result for 5 s then advance

        /// <summary>
        /// True when completion was detected via the inner scene's own boolean
        /// flag (_complete, _levelComplete, _victory) rather than a stack Push.
        /// Path B scenes (StormScene, BossScene, SkyIslandScene, UnderwaterScene)
        /// will call Scenes.Pop() or Scenes.Replace() in their next Update(),
        /// which would corrupt the stack because the inner scene is NOT on the
        /// stack — BotPlayLevelScene is.  When this flag is true we must NEVER
        /// call _inner.Update() again, and we finish immediately.
        /// </summary>
        private bool _completedViaReflection = false;

        private int _innerSceneDepthAtEnter;  // stack depth when we entered

        // Saves the host GodMode flag so we can restore it when the scene exits
        private bool _previousGodMode;

        // ── Real AI - Initialize ONCE on first frame ────────────────────
        private bool _aiInitialized = false;

        // ── Diagnostics system for comprehensive bot action tracking ───────────
        private BotDiagnostics _diagnostics = new BotDiagnostics();

        // ── Reflection-based completion fields (Pop/Replace scenes) ──────
        // Scenes that complete via Pop() or Replace() don't increase stack
        // depth, so depth-based detection misses them.  We also check for
        // _complete, _levelComplete, and _victory fields on the inner scene.
        private System.Reflection.FieldInfo _innerCompleteField;
        private System.Reflection.FieldInfo _innerLevelCompleteField;
        private System.Reflection.FieldInfo _innerVictoryField;

        // ── Reflection-based failure fields ──────────────────────────────
        // StormScene sets _failed=true before calling Scenes.Replace(GameOverScene).
        // We detect it first so we can bail before the Replace corrupts the stack.
        private System.Reflection.FieldInfo _innerFailedField;

        /// <summary>
        /// True when the inner scene completes via Push (IslandScene pushes
        /// CardRoulette/CourseClear).  For Push-based scenes, Path B (reflection)
        /// must be DISABLED because <c>_levelComplete</c> is set 0.35 s before
        /// the Push fires.  If Path B activated in that window it would stop
        /// updating the inner scene, preventing the Push from ever happening.
        /// </summary>
        private bool _innerUsesPathA;

        // ── Callback fired when this level is done ────────────────────────
        private readonly Action<bool, float> _onFinished;  // (wasBeaten, timeElapsed)

        // ── Overlay HUD fonts ─────────────────────────────────────────────
        private Font _hudFont;
        private Font _labelFont;

        /// <summary>
        /// Creates a bot-driven wrapper around a real game scene.
        /// </summary>
        /// <param name="inner">
        ///   The real gameplay scene to run (e.g. new IslandScene("dino", "Dinosaur Island")).
        /// </param>
        /// <param name="levelName">Display name shown in the HUD overlay.</param>
        /// <param name="onFinished">
        ///   Callback invoked when the level ends.
        ///   Parameters: (bool wasBeaten, float secondsTaken)
        /// </param>
        public BotPlayLevelScene(Scene inner, string levelName, Action<bool, float> onFinished)
        {
            _inner      = inner;
            _levelName  = levelName;
            _onFinished = onFinished;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────
        public override void OnEnter()
        {
            _hudFont   = new Font("Courier New", 9,  FontStyle.Bold);
            _labelFont = new Font("Courier New", 11, FontStyle.Bold);

            _bot.Reset();
            _elapsed    = 0f;
            _innerCompleted = false;
            _completedViaReflection = false;
            _completionHoldTimer = 0f;
            _aiInitialized = false;  // Reset AI init flag

            // Start AI verification logging
            BotAIVerificationTool.Enable();

            // DISABLED: GodMode was making bot invincible - enemies never hurt it
            // This prevents proper testing of health/damage mechanics
            // The bot now takes damage and needs to be smart about avoiding enemies
            // Enemies CAN hurt the bot, and bot health items are critical
            _previousGodMode = Game.Instance.GodMode;
            Game.Instance.GodMode = false;  // Let enemies hurt the bot for real testing

            // Start diagnostics tracking
            _diagnostics.StartLevel(_levelName);

            // Initialize bot activity logger for file-based logging
            _botActivityLogger = new Tests.ComprehensiveBotActivityLogger();
            _botActivityLogger.InitializeForLevel(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"),
                _levelName,
                _levelName
            );

            // Record how deep the scene stack is right now.  When the inner
            // scene pushes CardRoulette/CourseClear on top, the depth increases;
            // that is how we detect "level beaten".
            _innerSceneDepthAtEnter = Game.Instance.Scenes.Depth;

            // Enter the inner scene so it initialises (loads level, spawns player, etc.)
            _inner.OnEnter();

            // Cache reflection fields for completion detection on scenes that
            // use Pop() or Replace() instead of Push() (StormScene, BossScene,
            // WarlordBossScene, SkyIslandScene, UnderwaterScene).
            var bindFlags = System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance;
            var innerType = _inner.GetType();
            _innerCompleteField      = innerType.GetField("_complete",      bindFlags);
            _innerLevelCompleteField = innerType.GetField("_levelComplete", bindFlags);
            _innerVictoryField       = innerType.GetField("_victory",       bindFlags);
            _innerFailedField        = innerType.GetField("_failed",        bindFlags);

            // IslandScene completes via Push (CardRoulette/CourseClear) — Path A.
            // All other gameplay scenes complete via Pop/Replace — Path B.
            // Path B must be disabled for Push-based scenes because
            // _levelComplete is set 0.35 s before the Push fires.
            _innerUsesPathA = _inner is IslandScene;

            // Quick test to verify AI can see entities
            Player player = GetPlayerFromScene(_inner);
            if (player != null)
            {
                BotAIVerificationTool.QuickTest(_inner, player);
            }
        }

        public override void OnExit()
        {
            _inner.OnExit();
            _hudFont?.Dispose();
            _labelFont?.Dispose();

            // Stop AI verification
            BotAIVerificationTool.Disable();

            // Restore whatever GodMode was before the bot started
            Game.Instance.GodMode = _previousGodMode;

            // Generate and display diagnostics report
            string completionPath = _innerCompleted
                ? (_completedViaReflection ? "Path B (reflection flag)" : "Path A (stack push)")
                : "Not completed";
            string report = _diagnostics.GenerateReport(_innerCompleted, _elapsed,
                _innerCompleted ? completionPath : "Level not beaten within timeout");
            Console.WriteLine(report);
        }

        public override void OnPause()
        {
            // Do NOT pause the inner scene — let it keep running.
        }

        public override void OnResume()
        {
            // Do NOT forward resume — we are never actually paused.
        }

        // ── Update ────────────────────────────────────────────────────────
        public override void Update(float dt)
        {
            _elapsed += dt;

            var input = Game.Instance.Input;

            // ESC always lets a human exit — signal abort via elapsed = -1
            if (input.PausePressed)
            {
                FinishAbort();
                return;
            }

            // ── Player death guard ────────────────────────────────────────
            // If the player dies the inner scene will call
            // Scenes.Replace(GameOverScene) — that would replace
            // BotPlayLevelScene (the actual stack top) and corrupt the demo.
            // Detect death BEFORE updating the inner scene and bail out.
            // Also check the inner scene's _failed flag (StormScene sets this
            // before calling Replace on the next frame).
            if (_aiInitialized && !_innerCompleted)
            {
                Player p = GetPlayerFromScene(_inner);
                bool playerDead = p != null && !p.IsAlive;
                bool sceneFailed = CheckInnerSceneFailedFlag();

                if (playerDead || sceneFailed)
                {
                    string reason = sceneFailed ? "scene _failed flag" : "HP reached 0";
                    System.Diagnostics.Debug.WriteLine(
                        $"[BOT] ☠️ Player died at T={_elapsed:F1}s ({reason}) — finishing level as failed");
                    _diagnostics.LogStateChange("PLAYING", "PLAYER_DEATH",
                        $"{reason} at elapsed={_elapsed:F1}s");
                    Finish(false);
                    return;
                }
            }

            // ── Completion detection ──────────────────────────────────────
            // Primary (Path A): the inner scene pushes CardRoulette/CourseClear
            // onto the stack, increasing depth above the recorded entry value.
            // Secondary (Path B): scenes that exit via Pop() or Replace()
            // (StormScene, BossScene, WarlordBossScene, SkyIslandScene,
            // UnderwaterScene) set an internal boolean flag instead.
            // We check both paths every frame.
            if (!_innerCompleted)
            {
                // Path A — depth-based (Push scenes like IslandScene)
                if (Game.Instance.Scenes.Depth > _innerSceneDepthAtEnter)
                {
                    _innerCompleted = true;
                    _completedViaReflection = false;
                    System.Diagnostics.Debug.WriteLine(
                        $"[BOT] ✅ Completion detected via stack depth increase");
                    _diagnostics.LogStateChange("PLAYING", "COMPLETED_PUSH",
                        "Inner scene pushed result scenes onto the stack");
                }
                // Path B — reflection-based (Pop/Replace scenes only)
                // Disabled for Push-based scenes (IslandScene) because
                // _levelComplete is set 0.35 s before the Push fires —
                // triggering Path B in that window would stop inner updates
                // and prevent the Push from ever happening.
                else if (!_innerUsesPathA && CheckInnerSceneCompletionFlag())
                {
                    _innerCompleted = true;
                    _completedViaReflection = true;
                    System.Diagnostics.Debug.WriteLine(
                        $"[BOT] ✅ Completion detected via inner scene flag " +
                        $"— will NOT update inner scene again to prevent stack corruption");
                    _diagnostics.LogStateChange("PLAYING", "COMPLETED_FLAG",
                        "Inner scene set completion flag (Pop/Replace path)");
                }
            }

            // ── Handle post-completion ────────────────────────────────────
            if (_innerCompleted)
            {
                _completionHoldTimer += dt;
                _diagnostics.Update(dt);

                // ── Path B (reflection): the inner scene has NOT pushed any
                // result scenes.  Its next Update() would call Scenes.Pop() or
                // Scenes.Replace(), which would corrupt the stack because the
                // inner scene is held by reference — BotPlayLevelScene is the
                // real stack top.  We must NOT update the inner scene.
                // Finish immediately after a short animation hold.
                if (_completedViaReflection)
                {
                    // Short hold so the victory/complete text is visible in
                    // the draw call, then advance.
                    if (_completionHoldTimer >= 1.5f)
                    {
                        Finish(true);
                        return;
                    }
                    // We still draw the inner scene (via Draw()) but never
                    // call its Update() again.
                    return;
                }

                // ── Path A (push): CardRoulette / CourseClear are on the
                // stack above us.  Spam dismiss keys to skip them quickly.
                var currentScene = Game.Instance.Scenes.Current;
                string sceneName = currentScene?.GetType().Name ?? "Unknown";

                bool isCardRoulette = sceneName.Contains("CardRoulette") ||
                                      sceneName.Contains("Roulette");

                if (_completionHoldTimer > 0.2f)
                {
                    input.InjectPressed(System.Windows.Forms.Keys.Enter);
                    input.InjectPressed(System.Windows.Forms.Keys.Space);

                    if (isCardRoulette)
                    {
                        input.InjectPressed(System.Windows.Forms.Keys.Z);
                        System.Diagnostics.Debug.WriteLine(
                            $"[BOT] CardRoulette dismiss at t={_completionHoldTimer:F1}s");
                    }

                    _diagnostics.LogMiniGameInteraction(sceneName, "DISMISS", "");
                }

                // Hard cap: after COMPLETION_HOLD seconds, force-advance
                if (_completionHoldTimer >= COMPLETION_HOLD)
                {
                    input.ClearInjected();
                    Finish(true);
                    return;
                }

                // Let the pushed result scene update so it can process dismissals
                Game.Instance.Scenes.Current?.Update(dt);
                input.ClearInjected();
                return;
            }

            // ── Timeout check ─────────────────────────────────────────────
            if (_elapsed >= MAX_LEVEL_TIME)
            {
                Finish(false);
                return;
            }

            // ── Inject bot input, run inner scene, clear injected ─────────
            _diagnostics.Update(dt);

            // ════════════════════════════════════════════════════════════════════
            // INITIALIZE OBSERVABLE BOT AI - ONE TIME ONLY!
            // ════════════════════════════════════════════════════════════════════
            if (!_aiInitialized)
            {
                Player player = GetPlayerFromScene(_inner);
                if (player != null && _bot != null)
                {
                    System.Diagnostics.Debug.WriteLine("");
                    System.Diagnostics.Debug.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                    System.Diagnostics.Debug.WriteLine("║ BOT INITIALIZATION                                            ║");
                    System.Diagnostics.Debug.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
                    System.Diagnostics.Debug.WriteLine($"Scene: {_inner.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"Player: X={player.X:F0} Y={player.Y:F0} HP={player.Health}");

                    _bot.InitializeForScene(player, _inner, Game.Instance.Input, _botActivityLogger);
                    _aiInitialized = true;

                    System.Diagnostics.Debug.WriteLine("[BOT] ✅ ObservableBotAI initialized!");
                    System.Diagnostics.Debug.WriteLine("");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[BOT] ❌ ERROR: Player is null or bot is null!");
                    System.Diagnostics.Debug.WriteLine($"[BOT] Player={player} | Bot={_bot}");
                }
            }

            // ── Now inject bot input using OBSERVABLE AI ─────────────────
            // Suppress input while the level intro drop animation is running.
            // During intro, HandleInput is blocked by the scene so injecting
            // keys has no effect — but the bot's stuck timer still ticks and
            // fires a backward escape that moves the player to X=0 before the
            // level has even started.
            bool introActive = GetIntroActiveFromScene(_inner);
            if (_aiInitialized && !introActive)
            {
                _bot.InjectInput(Game.Instance.Input, dt);
            }
            else if (introActive)
            {
                // Intro is running — keep the stuck anchor current so the timer
                // doesn't accumulate during the fixed-Y drop and immediately fire
                // a backward escape the moment the player touches down.
                _bot.ResetStuckAnchor();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[BOT] ⚠️ WARNING: AI not initialized, not injecting input!");
            }

            _inner.Update(dt);
            input.ClearInjected();

            // ── Post-update stack corruption recovery ─────────────────────
            // If the player died DURING _inner.Update(), the inner scene may
            // have called Scenes.Replace(GameOverScene), which pops
            // BotPlayLevelScene and pushes GameOverScene in its place.
            // Detect this by checking whether we're still on the stack.
            // If corrupted, pop the rogue scene(s), restore the stack to
            // the depth recorded at entry, and signal failure via callback.
            if (Game.Instance.Scenes.Current != this && !_innerCompleted)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[BOT] ⚠️ Stack corrupted — inner scene called Replace/Pop during Update. " +
                    $"Recovering... Depth now={Game.Instance.Scenes.Depth} expected≥{_innerSceneDepthAtEnter}");

                // Pop whatever the inner scene pushed until we're back to the
                // caller's level (DemoModeScene or AutoTestLevelScene).
                // Note: BotPlayLevelScene.OnExit() was already called by the
                // Replace, so we must NOT call it again.
                while (Game.Instance.Scenes.Depth > _innerSceneDepthAtEnter - 1)
                    Game.Instance.Scenes.Pop();

                _onFinished?.Invoke(false, _elapsed);
            }
        }

        /// <summary>
        /// Helper: Extract player from any scene type.
        /// Works with IslandScene, StormScene, and other level scenes.
        /// </summary>
        private Player GetPlayerFromScene(Scene scene)
        {
            if (scene == null) return null;

            // Try reflection to get _player field from known scene types
            var playerField = scene.GetType().GetField("_player", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (playerField != null && playerField.FieldType == typeof(Player))
            {
                return (Player)playerField.GetValue(scene);
            }

            return null;
        }

        /// <summary>
        /// Returns true while the inner scene's drop-in intro animation is running.
        /// During this window the game blocks HandleInput, so injecting bot keys
        /// is pointless and triggers spurious stuck-escapes that move the player
        /// backward before the level has even properly started.
        /// </summary>
        private bool GetIntroActiveFromScene(Scene scene)
        {
            if (scene == null) return false;
            var field = scene.GetType().GetField("_introActive",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(scene);
        }

        /// <summary>
        /// Checks whether the inner scene has set one of its completion flags
        /// (_complete, _levelComplete, or _victory) to true.  Used to detect
        /// level completion on scenes that exit via Pop() or Replace() instead
        /// of pushing result scenes onto the stack.
        /// </summary>
        private bool CheckInnerSceneCompletionFlag()
        {
            try
            {
                if (_innerCompleteField != null &&
                    _innerCompleteField.FieldType == typeof(bool) &&
                    (bool)_innerCompleteField.GetValue(_inner))
                    return true;

                if (_innerLevelCompleteField != null &&
                    _innerLevelCompleteField.FieldType == typeof(bool) &&
                    (bool)_innerLevelCompleteField.GetValue(_inner))
                    return true;

                if (_innerVictoryField != null &&
                    _innerVictoryField.FieldType == typeof(bool) &&
                    (bool)_innerVictoryField.GetValue(_inner))
                    return true;
            }
            catch { /* reflection is best-effort */ }

            return false;
        }

        /// <summary>
        /// Checks whether the inner scene has set its failure flag (_failed)
        /// to true.  StormScene sets this when the player dies, then calls
        /// Scenes.Replace(GameOverScene) on the next Update.  We must detect
        /// this BEFORE calling _inner.Update() to prevent stack corruption.
        /// </summary>
        private bool CheckInnerSceneFailedFlag()
        {
            try
            {
                if (_innerFailedField != null &&
                    _innerFailedField.FieldType == typeof(bool) &&
                    (bool)_innerFailedField.GetValue(_inner))
                    return true;
            }
            catch { /* reflection is best-effort */ }

            return false;
        }

        // ── Draw ──────────────────────────────────────────────────────────
        public override void Draw(Graphics g)
        {
            // Draw the real game scene first — full resolution, full fidelity.
            if (_innerCompleted && !_completedViaReflection)
            {
                // Path A: result scenes (CardRoulette/CourseClear) are on the
                // stack above us — draw whatever is on top.
                Game.Instance.Scenes.Current?.Draw(g);
            }
            else
            {
                // Not completed yet, or Path B (reflection): draw the inner
                // scene directly.  We must NOT call Scenes.Current?.Draw()
                // here because Current is BotPlayLevelScene (infinite recursion).
                _inner.Draw(g);
            }

            // ── Small bot HUD overlay (top-left corner) ───────────────────
            DrawBotOverlay(g);
        }

        // ── Overlay HUD ───────────────────────────────────────────────────
        private void DrawBotOverlay(Graphics g)
        {
            const int padX = 8, padY = 8;
            const int panelW = 300, panelH = 56;

            // Semi-transparent dark background
            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(br, padX, padY, panelW, panelH);

            using (var pen = new Pen(Color.FromArgb(200, Color.Cyan), 1))
                g.DrawRectangle(pen, padX, padY, panelW, panelH);

            // Level name + mode tag
            string modeTag = _innerCompleted ? "✅ COMPLETE" : $"T:{_elapsed:F1}s";
            g.DrawString($"🤖 BOT  {modeTag}", _labelFont, Brushes.Cyan, padX + 6, padY + 4);

            // Timeout bar
            float pct = Math.Min(1f, _elapsed / MAX_LEVEL_TIME);
            int barW = panelW - 12;
            using (var br = new SolidBrush(Color.FromArgb(80, Color.Gray)))
                g.FillRectangle(br, padX + 6, padY + 38, barW, 10);
            Color barCol = pct > 0.8f ? Color.OrangeRed : Color.LimeGreen;
            using (var br = new SolidBrush(barCol))
                g.FillRectangle(br, padX + 6, padY + 38, (int)(barW * pct), 10);
            using (var pen = new Pen(Color.White, 1))
                g.DrawRectangle(pen, padX + 6, padY + 38, barW, 10);

            // Level name label
            g.DrawString(_levelName, _hudFont, Brushes.Yellow, padX + 6, padY + 22);

            // ESC hint
            SizeF escSz = g.MeasureString("[ESC] Exit bot", _hudFont);
            g.DrawString("[ESC] Exit bot", _hudFont, Brushes.DimGray,
                padX + panelW - escSz.Width - 4, padY + panelH - escSz.Height - 4);
        }

        // ── Finish helpers ─────────────────────────────────────────────────
        private void Finish(bool beaten)
        {
            // Pop any result scenes that the inner scene may have pushed (Path A)
            while (Game.Instance.Scenes.Depth > _innerSceneDepthAtEnter)
                Game.Instance.Scenes.Pop();

            // For Path B completions the inner scene never got to call
            // Scenes.Pop() / set LevelJustCompleted itself, so we do it here.
            if (beaten && _completedViaReflection)
            {
                Game.Instance.LevelJustCompleted = true;
                System.Diagnostics.Debug.WriteLine(
                    $"[BOT] Setting LevelJustCompleted=true for Path B completion");
            }

            _onFinished?.Invoke(beaten, _elapsed);
        }

        /// <summary>
        /// Called when the user presses ESC.  Passes elapsed=-1 so the caller
        /// can tell this is an intentional abort and exit the entire demo.
        /// </summary>
        private void FinishAbort()
        {
            while (Game.Instance.Scenes.Depth > _innerSceneDepthAtEnter)
                Game.Instance.Scenes.Pop();

            _onFinished?.Invoke(false, -1f);   // -1 = user aborted
        }
    }
}
