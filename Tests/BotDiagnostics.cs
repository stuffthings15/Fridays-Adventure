using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Tests
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Bot Diagnostics System
    // Purpose: Real-time tracking of bot action execution, ability state, item collection,
    //          enemy detection, and mini-game interactions for comprehensive debugging.
    // ────────────────────────────────────────────

    /// <summary>
    /// Real-time diagnostics for bot behavior during level playback.
    /// Tracks input injection, ability cooldowns, item collection, enemy encounters,
    /// and scene transitions (CardRoulette, etc.).
    /// </summary>
    public sealed class BotDiagnostics
    {
        private readonly List<DiagnosticEvent> _events = new List<DiagnosticEvent>();
        private float _elapsedTime = 0f;
        private int _itemsDetected = 0;
        private int _enemiesDetected = 0;
        private int _attacksFired = 0;
        private int _jumpsFired = 0;
        private float _lastAttackTime = -1f;
        private float _lastJumpTime = -1f;

        public struct DiagnosticEvent
        {
            public float Time;
            public string Category;        // "INPUT", "ABILITY", "COLLECTION", "ENEMY", "SCENE", "STATE"
            public string Action;
            public string Details;
        }

        /// <summary>
        /// Initialize diagnostics for a new level run.
        /// </summary>
        public void StartLevel(string levelName)
        {
            _events.Clear();
            _elapsedTime = 0f;
            _itemsDetected = 0;
            _enemiesDetected = 0;
            _attacksFired = 0;
            _jumpsFired = 0;
            _lastAttackTime = -1f;
            _lastJumpTime = -1f;
            Log("SCENE", "Level Start", $"Loading {levelName}");
        }

        /// <summary>
        /// Update elapsed time and track state each frame.
        /// </summary>
        public void Update(float dt)
        {
            _elapsedTime += dt;
        }

        /// <summary>
        /// Log input injection (keys pressed/held by bot).
        /// </summary>
        public void LogInputInjected(Keys key, bool isHeld)
        {
            string action = isHeld ? "HOLD" : "PRESS";
            Log("INPUT", action, $"Key.{key}");
        }

        /// <summary>
        /// Log ability attempt/state (attack, jump, frost ball, etc.).
        /// </summary>
        public void LogAbility(string abilityName, bool succeeded, string reason = "")
        {
            string status = succeeded ? "SUCCESS" : "BLOCKED";
            if (abilityName == "Attack")
            {
                if (succeeded) _attacksFired++;
                Log("ABILITY", status, $"{abilityName} (total: {_attacksFired}) {reason}");
            }
            else if (abilityName == "Jump")
            {
                if (succeeded) _jumpsFired++;
                Log("ABILITY", status, $"{abilityName} (total: {_jumpsFired}) {reason}");
            }
            else
            {
                Log("ABILITY", status, $"{abilityName} {reason}");
            }
        }

        /// <summary>
        /// Log item collection (berries, health pickups, coins, etc.).
        /// </summary>
        public void LogItemCollected(string itemType, int count = 1)
        {
            _itemsDetected += count;
            Log("COLLECTION", "ITEM", $"{itemType} (total items: {_itemsDetected})");
        }

        /// <summary>
        /// Log enemy encounter or defeat.
        /// </summary>
        public void LogEnemyInteraction(string enemyType, string action)
        {
            if (action == "DEFEATED")
                _enemiesDetected++;
            Log("ENEMY", action.ToUpper(), $"{enemyType} (total defeated: {_enemiesDetected})");
        }

        /// <summary>
        /// Log scene transitions (entering/exiting CardRoulette, CourseClear, etc.).
        /// </summary>
        public void LogSceneTransition(string fromScene, string toScene)
        {
            Log("SCENE", "TRANSITION", $"{fromScene} → {toScene}");
        }

        /// <summary>
        /// Log bot state changes (stuck, recovering, completed, failed, etc.).
        /// </summary>
        public void LogStateChange(string previousState, string newState, string reason = "")
        {
            Log("STATE", "CHANGE", $"{previousState} → {newState} ({reason})");
        }

        /// <summary>
        /// Log mini-game interaction (CardRoulette selections, button presses, etc.).
        /// </summary>
        public void LogMiniGameInteraction(string gameName, string action, string result = "")
        {
            Log("MINIGAME", action.ToUpper(), $"{gameName} {result}");
        }

        /// <summary>
        /// Log hazard or damage event.
        /// </summary>
        public void LogHazardEncounter(string hazardType, bool avoided)
        {
            string result = avoided ? "AVOIDED" : "HIT";
            Log("HAZARD", result, hazardType);
        }

        /// <summary>
        /// Generate a comprehensive diagnostics report.
        /// </summary>
        public string GenerateReport(bool levelCompleted, float timeSpent, string failureReason = "")
        {
            var report = new StringBuilder();

            report.AppendLine("\n════════════════════════════════════════════════════════════");
            report.AppendLine("BOT DIAGNOSTICS REPORT");
            report.AppendLine("════════════════════════════════════════════════════════════\n");

            report.AppendLine("OVERALL STATS:");
            report.AppendLine($"  Level Completed: {(levelCompleted ? "✅ YES" : "❌ NO")}");
            report.AppendLine($"  Time Spent: {timeSpent:F1}s");
            report.AppendLine($"  Items Collected: {_itemsDetected}");
            report.AppendLine($"  Enemies Defeated: {_enemiesDetected}");
            report.AppendLine($"  Attacks Fired: {_attacksFired}");
            report.AppendLine($"  Jumps Performed: {_jumpsFired}");
            if (!string.IsNullOrEmpty(failureReason))
                report.AppendLine($"  Failure Reason: {failureReason}");
            report.AppendLine();

            report.AppendLine("DETAILED TIMELINE:");
            report.AppendLine("─────────────────────────────────────────────────────────");
            foreach (var evt in _events)
            {
                string line = $"[{evt.Time:F2}s] {evt.Category,-10} {evt.Action,-8} {evt.Details}";
                report.AppendLine(line);
            }

            report.AppendLine("\nDIAGNOSTIC ANALYSIS:");
            report.AppendLine("─────────────────────────────────────────────────────────");

            // Check for common issues
            if (_attacksFired == 0)
                report.AppendLine("⚠ WARNING: Attack ability never fired (check ability cooldown or input blocking)");
            if (_jumpsFired == 0)
                report.AppendLine("⚠ WARNING: Jump never triggered (check jump input or platform state)");
            if (_itemsDetected == 0)
                report.AppendLine("⚠ WARNING: No items collected (check collection zone or item spawning)");
            if (_enemiesDetected == 0 && !levelCompleted)
                report.AppendLine("⚠ WARNING: No enemies encountered (check enemy spawning or bot reached exit too quickly)");

            // Check for scene stuck issues
            int sceneTransitions = _events.FindAll(e => e.Category == "SCENE" && e.Action == "TRANSITION").Count;
            if (sceneTransitions > 10)
                report.AppendLine($"⚠ WARNING: Excessive scene transitions ({sceneTransitions}) — possible infinite loop");

            report.AppendLine("\n════════════════════════════════════════════════════════════\n");

            return report.ToString();
        }

        // ── Private helper ────────────────────────────────────────────────────
        private void Log(string category, string action, string details)
        {
            _events.Add(new DiagnosticEvent
            {
                Time = _elapsedTime,
                Category = category,
                Action = action,
                Details = details
            });
        }
    }
}
