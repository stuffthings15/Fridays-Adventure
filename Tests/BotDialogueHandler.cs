using System;
using System.Diagnostics;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// PHASE 2 - Team 10: Engine Programmer
    /// Feature: Bot Dialogue Auto-Progression
    /// Purpose: Bot can automatically skip/proceed through all dialogue boxes
    /// ────────────────────────────────────────────────────────────────────
    /// The bot detects dialogue, narrative boxes, and interactive prompts,
    /// then auto-advances them so testing can continue uninterrupted.
    /// </summary>
    public class BotDialogueHandler
    {
        private InputManager _input;
        private float _dialogueSkipCooldown = 0f;
        private const float DIALOGUE_SKIP_INTERVAL = 0.1f;  // Min time between skips
        private int _dialoguesProcessed = 0;
        private bool _isInDialogue = false;

        /// <summary>
        /// Initialize the dialogue handler.
        /// </summary>
        public BotDialogueHandler(InputManager input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        /// <summary>
        /// Call once per frame to detect and auto-skip dialogues.
        /// Returns true if dialogue was detected and handled.
        /// </summary>
        public bool Update(float dt)
        {
            _dialogueSkipCooldown -= dt;

            // Check if we're in a dialogue situation and can skip
            if (ShouldSkipDialogue())
            {
                _dialogueSkipCooldown = DIALOGUE_SKIP_INTERVAL;
                _isInDialogue = true;
                _dialoguesProcessed++;

                // Inject Space to continue dialogue
                _input.InjectPressed(System.Windows.Forms.Keys.Space);
                
                // Also inject Enter as alternative
                _input.InjectPressed(System.Windows.Forms.Keys.Return);
                
                System.Diagnostics.Debug.WriteLine(
                    $"[BOT_DIALOGUE] Skipping dialogue #{_dialoguesProcessed}");

                return true;
            }

            _isInDialogue = false;
            return false;
        }

        /// <summary>
        /// Detect if bot should skip current dialogue.
        /// Override this method to add custom dialogue detection logic.
        /// </summary>
        protected virtual bool ShouldSkipDialogue()
        {
            // Cooldown check
            if (_dialogueSkipCooldown > 0f)
                return false;

            // You can add game-specific dialogue detection here
            // For now, this is a placeholder that subclasses can override
            return false;
        }

        /// <summary>
        /// Handle "press any key" prompts
        /// </summary>
        public void SkipPrompt()
        {
            _input.InjectPressed(System.Windows.Forms.Keys.Space);
        }

        /// <summary>
        /// Handle menu selection (for dialogue choices)
        /// </summary>
        public void SelectMenuOption(int optionIndex = 0)
        {
            // Default: select first option
            for (int i = 0; i < optionIndex; i++)
            {
                _input.InjectPressed(System.Windows.Forms.Keys.Down);
            }
            _input.InjectPressed(System.Windows.Forms.Keys.Return);
        }

        /// <summary>
        /// Get dialogue statistics
        /// </summary>
        public int DialoguesProcessed => _dialoguesProcessed;
        public bool IsCurrentlyInDialogue => _isInDialogue;

        /// <summary>
        /// Reset dialogue counter
        /// </summary>
        public void Reset()
        {
            _dialoguesProcessed = 0;
            _isInDialogue = false;
            _dialogueSkipCooldown = 0f;
        }

        /// <summary>
        /// Get summary of dialogue handling
        /// </summary>
        public string GetSummary()
        {
            return $"Dialogues handled: {_dialoguesProcessed}, Currently in dialogue: {_isInDialogue}";
        }
    }

    /// <summary>
    /// Game-specific dialogue handler for Fridays Adventure.
    /// Integrates with actual game scenes to detect dialogue boxes.
    /// </summary>
    public class GameDialogueHandler : BotDialogueHandler
    {
        private Scenes.Scene _currentScene;
        private float _dialogueDetectionTimeout = 0f;
        private const float DIALOGUE_TIMEOUT = 0.5f;

        public GameDialogueHandler(InputManager input) : base(input) { }

        /// <summary>
        /// Set the current scene for dialogue detection.
        /// </summary>
        public void SetCurrentScene(Scenes.Scene scene)
        {
            _currentScene = scene;
        }

        /// <summary>
        /// Override to detect actual dialogue in the game.
        /// </summary>
        protected override bool ShouldSkipDialogue()
        {
            // Base cooldown check
            if (DialoguesProcessed > 0 && _dialogueDetectionTimeout > 0f)
            {
                _dialogueDetectionTimeout -= 0.016f;  // Approximate frame time (60 FPS)
                return false;
            }

            // If no scene, can't detect dialogue
            if (_currentScene == null)
                return false;

            // Try to detect dialogue using reflection (works with any scene)
            return DetectDialogueInScene(_currentScene);
        }

        /// <summary>
        /// Attempt to detect dialogue box in the current scene.
        /// Uses reflection to check for common dialogue UI elements.
        /// </summary>
        private bool DetectDialogueInScene(Scenes.Scene scene)
        {
            try
            {
                // Check for dialogue box field
                var dialogueField = scene.GetType().GetField("_dialogueBox",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (dialogueField != null)
                {
                    var dialogueBox = dialogueField.GetValue(scene);
                    if (dialogueBox != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[BOT_DIALOGUE] Detected dialogue box");
                        _dialogueDetectionTimeout = DIALOGUE_TIMEOUT;
                        return true;
                    }
                }

                // Check for narrative box
                var narrativeField = scene.GetType().GetField("_narrativeBox",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (narrativeField != null)
                {
                    var narrativeBox = narrativeField.GetValue(scene);
                    if (narrativeBox != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[BOT_DIALOGUE] Detected narrative box");
                        _dialogueDetectionTimeout = DIALOGUE_TIMEOUT;
                        return true;
                    }
                }

                // Check for prompt/choice UI
                var promptField = scene.GetType().GetField("_prompt",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (promptField != null)
                {
                    var prompt = promptField.GetValue(scene);
                    if (prompt != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[BOT_DIALOGUE] Detected prompt box");
                        _dialogueDetectionTimeout = DIALOGUE_TIMEOUT;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BOT_DIALOGUE] Detection error: {ex.Message}");
            }

            return false;
        }
    }
}
