using System;
using System.Drawing;
using System.Windows.Forms;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Systems;
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

        // ── Bot Activity Logger ───────────────────────────────────────────
        private Tests.ComprehensiveBotActivityLogger _botActivityLogger = null;

        // ── Completion tracking ───────────────────────────────────────────
        /// <summary>
        /// True once the inner scene has signalled completion, either by pushing
        /// result scenes (Path A) or by setting an internal flag (Path B).
        /// </summary>
        private bool  _innerCompleted = false;
        private float _completionHoldTimer = 0f;

        /// <summary>
        /// Guard flag — true after Finish() or FinishAbort() has fired once.
        /// Prevents double-finishing which would duplicate results and leave
        /// BotPlayLevelScene stuck on the scene stack.
        /// </summary>
        private bool _finished = false;

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
            _finished   = false;
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

            // ── Fortress gate workaround ─────────────────────────────────────
            // FortressScene requires a BossKey in inventory to open the gate.
            // Grant one automatically so the bot can test the full level.
            if (_inner is FortressScene && Systems.PowerUpInventory.ReserveItem != Systems.SuitType.BossKey)
            {
                Systems.PowerUpInventory.SetReserve(Systems.SuitType.BossKey);
                System.Diagnostics.Debug.WriteLine(
                    "[BOT] Granted BossKey for FortressScene gate");
            }

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

            // Structured log: bot level start + stuck detector reset
            GameLogger.LogSystem("BotLevelStart", _levelName);
            if (player != null)
                GameLogger.ResetBotStuckDetector(player.X, player.Y);
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

            // ── Completion detection (Path B only — pre-update) ──────────
            // Path B: scenes that exit via Pop/Replace set an internal flag.
            // We must detect this BEFORE calling _inner.Update() because
            // the inner scene's next Update would call Pop/Replace and
            // corrupt the stack.  Path A (Push) is detected post-update
            // since the Push happens during _inner.Update().
            if (!_innerCompleted && !_innerUsesPathA && CheckInnerSceneCompletionFlag())
            {
                _innerCompleted = true;
                _completedViaReflection = true;
                System.Diagnostics.Debug.WriteLine(
                    $"[BOT] ✅ Completion detected via inner scene flag " +
                    $"— will NOT update inner scene again to prevent stack corruption");
                _diagnostics.LogStateChange("PLAYING", "COMPLETED_FLAG",
                    "Inner scene set completion flag (Pop/Replace path)");
            }

            // ── Handle post-completion (Path B only) ──────────────────────
            // Path B scenes set a completion flag and their next Update() would
            // call Pop/Replace.  We stop updating and finish after a short hold.
            // Path A is handled entirely in the post-update block below.
            if (_innerCompleted && _completedViaReflection)
            {
                _completionHoldTimer += dt;
                _diagnostics.Update(dt);

                if (_completionHoldTimer >= 1.5f)
                {
                    Finish(true);
                    return;
                }
                // Still draw the inner scene (via Draw()) but never Update it
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

                // GameLogger stuck detection — runs every frame the bot is active
                Player sp = GetPlayerFromScene(_inner);
                if (sp != null)
                {
                    string botState = _bot._comprehensiveBot?.CurrentState ?? "unknown";
                    GameLogger.UpdateBotStuckDetector(
                        dt, sp.X, sp.Y, _levelName, botState, sp);
                }
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

            // ── Post-update completion + corruption handling ──────────────
            // After _inner.Update(), three outcomes are possible:
            //
            // 1. NORMAL PLAY: Scenes.Current is still BotPlayLevelScene,
            //    nothing special happened.  Clear injected keys and return.
            //
            // 2. PATH A PUSH: The inner scene pushed result scenes
            //    (CardRoulette/CourseClear) — depth increased.  Because the
            //    game loop only calls Scenes.Current.Update(), BotPlayLevelScene
            //    will NOT be updated again until those scenes are dismissed.
            //    We must dismiss them RIGHT NOW in this same frame by injecting
            //    keys and updating them in a loop.
            //
            // 3. STACK CORRUPTION: The inner scene called Scenes.Replace()
            //    or Pop() (player death → GameOverScene).  Depth is unchanged
            //    or decreased but Scenes.Current != this.  Recover by popping
            //    the rogue scene and signalling failure.

            if (Game.Instance.Scenes.Depth > _innerSceneDepthAtEnter)
            {
                // ── Path A: result scenes pushed onto the stack ───────────
                // IslandScene pushed CardRoulette (and later CourseClear).
                // BotPlayLevelScene is buried in the stack — the game loop
                // won't call our Update again.  We MUST dismiss all result
                // scenes right now in a tight loop before returning.
                //
                // CardRoulette and CourseClear also have auto-advance logic
                // (via DialogueScene.AutoAdvance) as insurance, but the tight
                // loop is the primary mechanism because BotPlayLevelScene
                // must call Finish() to notify the caller (DemoModeScene).
                // Without Finish(), the demo doesn't know the level ended.
                _innerCompleted = true;
                _completedViaReflection = false;
                System.Diagnostics.Debug.WriteLine(
                    $"[BOT] ✅ Path A: Inner scene pushed result scenes (depth {Game.Instance.Scenes.Depth})");

                // Dismiss all pushed result scenes in a tight loop.
                // Each iteration: inject action keys → update the top scene →
                // advance SceneTransition (CardRoulette/CourseClear use curtain
                // wipes that fire mid-callbacks to pop/push scenes).
                // Safety cap prevents infinite loops.
                int safety = 600;  // ~10 s at 60 fps steps
                float dismissDt = 0.016f;
                while (Game.Instance.Scenes.Depth > _innerSceneDepthAtEnter && safety-- > 0)
                {
                    // Only inject action keys when no transition is animating —
                    // SceneTransition.Begin returns early if already active, so
                    // the result scene's advance logic would silently fail.
                    if (!Systems.SceneTransition.IsActive)
                    {
                        input.InjectPressed(System.Windows.Forms.Keys.Space);
                        input.InjectPressed(System.Windows.Forms.Keys.Enter);
                        input.InjectPressed(System.Windows.Forms.Keys.Z);
                    }

                    Game.Instance.Scenes.Current?.Update(dismissDt);
                    Systems.SceneTransition.Update(dismissDt);
                    input.ClearInjected();
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[BOT] Path A dismiss complete — depth back to {Game.Instance.Scenes.Depth}");
                input.ClearInjected();
                Finish(true);
                return;
            }
            else if (Game.Instance.Scenes.Current != this)
            {
                // ── Stack corruption: inner scene called Replace/Pop ──────
                System.Diagnostics.Debug.WriteLine(
                    $"[BOT] ⚠️ Stack corrupted — inner scene called Replace/Pop. " +
                    $"Recovering... Depth={Game.Instance.Scenes.Depth}");

                while (Game.Instance.Scenes.Depth > _innerSceneDepthAtEnter - 1)
                    Game.Instance.Scenes.Pop();

                _onFinished?.Invoke(false, _elapsed);
                return;
            }

            // Normal play — just clear injected keys
            input.ClearInjected();
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
            const int panelW = 300, panelH = 72;

            // Semi-transparent dark background
            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(br, padX, padY, panelW, panelH);

            using (var pen = new Pen(Color.FromArgb(200, Color.Cyan), 1))
                g.DrawRectangle(pen, padX, padY, panelW, panelH);

            // Level name + mode tag
            string modeTag = _innerCompleted ? "✅ COMPLETE" : $"T:{_elapsed:F1}s";
            g.DrawString($"🤖 BOT  {modeTag}", _labelFont, Brushes.Cyan, padX + 6, padY + 4);

            // Bot state + player HP (from unified bot)
            string botState = "";
            string hpText   = "";
            if (_bot._comprehensiveBot != null)
            {
                botState = _bot._comprehensiveBot.CurrentState ?? "";
                Player p = GetPlayerFromScene(_inner);
                if (p != null)
                    hpText = $"HP:{p.Health}/{p.MaxHealth}";
            }
            g.DrawString($"{_levelName}  {hpText}", _hudFont, Brushes.Yellow,
                padX + 6, padY + 22);
            g.DrawString(botState, _hudFont, Brushes.LimeGreen,
                padX + 6, padY + 36);

            // Timeout bar
            float pct = Math.Min(1f, _elapsed / MAX_LEVEL_TIME);
            int barW = panelW - 12;
            using (var br = new SolidBrush(Color.FromArgb(80, Color.Gray)))
                g.FillRectangle(br, padX + 6, padY + 54, barW, 10);
            Color barCol = pct > 0.8f ? Color.OrangeRed : Color.LimeGreen;
            using (var br = new SolidBrush(barCol))
                g.FillRectangle(br, padX + 6, padY + 54, (int)(barW * pct), 10);
            using (var pen = new Pen(Color.White, 1))
                g.DrawRectangle(pen, padX + 6, padY + 54, barW, 10);

            // ESC hint
            SizeF escSz = g.MeasureString("[ESC] Exit bot", _hudFont);
            g.DrawString("[ESC] Exit bot", _hudFont, Brushes.DimGray,
                padX + panelW - escSz.Width - 4, padY + panelH - escSz.Height - 4);
        }

        // ── Finish helpers ─────────────────────────────────────────────────
        private void Finish(bool beaten)
        {
            // Guard: prevent double-finishing (would duplicate results and
            // leave BotPlayLevelScene stuck on the scene stack forever).
            if (_finished) return;
            _finished = true;

            // Structured log: level result
            GameLogger.LogLevelResult(
                _levelName, _levelName, beaten, _elapsed,
                0, 0);
            GameLogger.LogSystem(beaten ? "BotLevelBeaten" : "BotLevelFailed", _levelName);

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

            // Pop BotPlayLevelScene itself so the caller (DemoModeScene or
            // AutoTestLevelScene) becomes the active scene and can advance.
            // The guard above prevents the abort callback's own Pop() from
            // causing a double-pop — if the callback already popped us,
            // Current != this and we skip.
            if (Game.Instance.Scenes.Current == this)
                Game.Instance.Scenes.Pop();
        }

        /// <summary>
        /// Called when the user presses ESC.  Passes elapsed=-1 so the caller
        /// can tell this is an intentional abort and exit the entire demo.
        /// </summary>
        private void FinishAbort()
        {
            if (_finished) return;
            _finished = true;

            while (Game.Instance.Scenes.Depth > _innerSceneDepthAtEnter)
                Game.Instance.Scenes.Pop();

            _onFinished?.Invoke(false, -1f);   // -1 = user aborted

            // Pop self if the abort callback didn't already remove us.
            if (Game.Instance.Scenes.Current == this)
                Game.Instance.Scenes.Pop();
        }
    }
}
