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
        // ── Completion tracking ───────────────────────────────────────────
        /// <summary>
        /// True once the inner scene has pushed its completion scenes (CardRoulette
        /// / CourseClear) onto the stack, meaning the level was beaten.
        /// </summary>
        private bool  _innerCompleted = false;
        private float _completionHoldTimer = 0f;
        private const float COMPLETION_HOLD = 5f;  // show result for 5 s then advance

        private int _innerSceneDepthAtEnter;  // stack depth when we entered

        // Saves the host GodMode flag so we can restore it when the scene exits
        private bool _previousGodMode;

        // ── Real AI - Initialize ONCE on first frame ────────────────────
        private bool _aiInitialized = false;

        // ── Diagnostics system for comprehensive bot action tracking ───────────
        private BotDiagnostics _diagnostics = new BotDiagnostics();

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

            // Record how deep the scene stack is right now.  When the inner
            // scene pushes CardRoulette/CourseClear on top, the depth increases;
            // that is how we detect "level beaten".
            _innerSceneDepthAtEnter = Game.Instance.Scenes.Depth;

            // Enter the inner scene so it initialises (loads level, spawns player, etc.)
            _inner.OnEnter();

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
            string report = _diagnostics.GenerateReport(_innerCompleted, _elapsed,
                _innerCompleted ? "" : "Level not beaten within timeout");
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

            // ── Completion detection ──────────────────────────────────────
            // When the inner scene pushes CardRoulette/CourseClear the scene
            // stack depth grows above the value recorded at entry.  That is
            // our signal that the level was beaten.
            if (!_innerCompleted && Game.Instance.Scenes.Depth > _innerSceneDepthAtEnter)
            {
                _innerCompleted = true;
            }

            // If beaten, let those result scenes run for a few seconds then advance.
            if (_innerCompleted)
            {
                _completionHoldTimer += dt;
                _diagnostics.Update(dt);

                // CardRoulette / CourseClear screens require dismissal.
                // The bot should interact with them to progress.
                var currentScene = Game.Instance.Scenes.Current;
                string sceneName = currentScene?.GetType().Name ?? "Unknown";

                // Inject Enter key to dismiss dialogs / advance card roulette
                if (_completionHoldTimer > 0.3f)
                {
                    input.InjectPressed(Keys.Enter);
                    _diagnostics.LogMiniGameInteraction(sceneName, "ADVANCE", "");
                }

                if (_completionHoldTimer >= COMPLETION_HOLD)
                {
                    input.ClearInjected();
                    Finish(true);
                    return;
                }

                // Let the current top scene (CardRoulette / CourseClear) update normally.
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
            // INITIALIZE REAL AI - ONE TIME ONLY!
            // ════════════════════════════════════════════════════════════════════
            if (!_aiInitialized)
            {
                Player player = GetPlayerFromScene(_inner);
                if (player != null && _bot != null)
                {
                    _bot.InitializeForScene(player, _inner, Game.Instance.Input);
                    _aiInitialized = true;
                    System.Diagnostics.Debug.WriteLine("[BOT_PLAY_SCENE] ✅ RealSmartBotAI initialized!");
                }
            }

            // ── Now inject bot input using OBSERVABLE AI ─────────────────
            _bot.InjectInput(Game.Instance.Input, dt);

            // Log what the bot is injecting for diagnostics
            if (input.IsHeld(Keys.Right))
                _diagnostics.LogInputInjected(Keys.Right, true);
            if (input.IsPressed(Keys.Z))
                _diagnostics.LogAbility("Attack", true, "key injected");
            if (input.IsPressed(Keys.Space))
                _diagnostics.LogAbility("Jump", true, "key injected");

            _inner.Update(dt);
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

        // ── Draw ──────────────────────────────────────────────────────────
        public override void Draw(Graphics g)
        {
            // Draw the real game scene first — full resolution, full fidelity.
            if (_innerCompleted)
                // When the level is beaten the top of the stack is the result scene
                Game.Instance.Scenes.Current?.Draw(g);
            else
                _inner.Draw(g);

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
            // Pop any result scenes that the inner scene may have pushed
            while (Game.Instance.Scenes.Depth > _innerSceneDepthAtEnter)
                Game.Instance.Scenes.Pop();

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
