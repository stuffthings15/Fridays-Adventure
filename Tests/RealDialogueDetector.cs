using System;
using System.Collections.Generic;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// REAL Dialogue Detection & Handling
    /// Detects dialogue boxes, popups, and narrative elements
    /// Dismisses them so bot can continue playing
    /// </summary>
    public class RealDialogueDetector
    {
        private Scene _scene;
        private InputManager _input;
        private bool _dialogueDetected = false;
        private float _dialogueDismissTimer = 0f;
        private int _dismissAttempts = 0;
        private const float DISMISS_TIMEOUT = 5f;  // Max 5 seconds to dismiss

        public bool IsDialogueActive { get; private set; }
        public string CurrentDialogueType { get; private set; } = "None";

        public RealDialogueDetector(Scene scene, InputManager input)
        {
            _scene = scene;
            _input = input;
            Console.WriteLine("[DIALOGUE] Detector initialized");
        }

        public void Update(float dt)
        {
            _dialogueDismissTimer += dt;

            // Detect dialogue
            DetectDialogueElements();

            // If dialogue detected, try to dismiss it
            if (IsDialogueActive)
            {
                if (_dialogueDismissTimer > 0.3f)  // Wait before next attempt
                {
                    DismissDialogue();
                    _dialogueDismissTimer = 0f;
                    _dismissAttempts++;
                }

                // Timeout - give up and continue
                if (_dismissAttempts > 15)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[DIALOGUE] Timeout dismissing {CurrentDialogueType} - forcing continue");
                    Console.ResetColor();
                    IsDialogueActive = false;
                    _dismissAttempts = 0;
                }
            }
        }

        private void DetectDialogueElements()
        {
            // Try to find dialogue boxes by type
            try
            {
                // Check for common dialogue/UI elements
                var uiLayerField = _scene.GetType().GetField("_uiLayer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (uiLayerField != null)
                {
                    var uiLayer = uiLayerField.GetValue(_scene);
                    if (uiLayer != null)
                    {
                        IsDialogueActive = true;
                        CurrentDialogueType = "UI_Layer";
                        LogDialogueDetection("UI Layer detected");
                        return;
                    }
                }

                // Check for narrative text
                var narrativeField = _scene.GetType().GetField("_narrative",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (narrativeField != null && narrativeField.GetValue(_scene) != null)
                {
                    IsDialogueActive = true;
                    CurrentDialogueType = "Narrative";
                    LogDialogueDetection("Narrative text detected");
                    return;
                }

                // Check for dialogue box
                var dialogueField = _scene.GetType().GetField("_dialogue",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (dialogueField != null && dialogueField.GetValue(_scene) != null)
                {
                    IsDialogueActive = true;
                    CurrentDialogueType = "DialogueBox";
                    LogDialogueDetection("Dialogue box detected");
                    return;
                }

                // Check scene depth - if multiple scenes on stack, there's probably a UI overlay
                if (_scene.GetType().Name.Contains("LevelScene") || _scene.GetType().Name.Contains("Scene"))
                {
                    // This is a heuristic - level scenes often have UI overlays
                    // We check by looking at scene stack depth
                    // If not found, reset
                    IsDialogueActive = false;
                    CurrentDialogueType = "None";
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DIALOGUE] Detection error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private void DismissDialogue()
        {
            if (!IsDialogueActive || _input == null) return;

            // Try multiple keys to dismiss
            _input.InjectPressed(System.Windows.Forms.Keys.Return);      // Enter
            _input.InjectPressed(System.Windows.Forms.Keys.Space);       // Space
            _input.InjectPressed(System.Windows.Forms.Keys.Z);           // Action key

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[DIALOGUE] Dismissing {CurrentDialogueType} (attempt {_dismissAttempts})");
            Console.ResetColor();
        }

        private void LogDialogueDetection(string message)
        {
            if (!_dialogueDetected)
            {
                _dialogueDetected = true;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[DIALOGUE] {message}");
                Console.ResetColor();
            }
        }

        public string GetStatus()
        {
            return $"DialogueActive={IsDialogueActive} | Type={CurrentDialogueType} | Attempts={_dismissAttempts}";
        }
    }
}
