// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 9: UI Programmer
// Feature: UI Systems Pack
// Purpose: Implements remaining Phase 2 Team 9 UI services.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Mini-map display helpers.
    /// </summary>
    /// <remarks>PHASE 2 - Team 9: Mini-map Display</remarks>
    public static class MiniMapDisplaySystem
    {
        /// <summary>Returns a compact minimap position label from world coords.</summary>
        /// <remarks>PHASE 2 - Team 9: Mini-map Display</remarks>
        public static string PositionLabel(float x, float y)
        {
            int gx = (int)Math.Floor(x / 64f);
            int gy = (int)Math.Floor(y / 64f);
            return $"Grid ({gx},{gy})";
        }
    }

    /// <summary>
    /// Tutorial overlay state manager.
    /// </summary>
    /// <remarks>PHASE 2 - Team 9: Tutorial Overlay</remarks>
    public static class TutorialOverlaySystem
    {
        private static bool _enabled = true;

        /// <summary>Current tutorial overlay enabled state.</summary>
        /// <remarks>PHASE 2 - Team 9: Tutorial Overlay</remarks>
        public static bool Enabled => _enabled;

        /// <summary>Toggles tutorial overlay visibility.</summary>
        /// <remarks>PHASE 2 - Team 9: Tutorial Overlay</remarks>
        public static void Toggle() => _enabled = !_enabled;
    }

    /// <summary>
    /// Notification queue for UI popups.
    /// </summary>
    /// <remarks>PHASE 2 - Team 9: Notification System</remarks>
    public static class NotificationSystem
    {
        private static readonly Queue<string> _queue = new Queue<string>();

        /// <summary>Enqueues one notification line.</summary>
        /// <remarks>PHASE 2 - Team 9: Notification System</remarks>
        public static void Push(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            _queue.Enqueue(message.Trim());
            while (_queue.Count > 24) _queue.Dequeue();
        }

        /// <summary>Returns current queued notifications.</summary>
        /// <remarks>PHASE 2 - Team 9: Notification System</remarks>
        public static IReadOnlyList<string> Snapshot() => _queue.ToList();
    }

    /// <summary>
    /// Keybind customization wrapper around GameConfig keymap.
    /// </summary>
    /// <remarks>PHASE 2 - Team 9: Keybind Customization</remarks>
    public static class KeybindCustomizationSystem
    {
        /// <summary>Sets keybind for an action name.</summary>
        /// <remarks>PHASE 2 - Team 9: Keybind Customization</remarks>
        public static void Set(string action, string keyName)
        {
            if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(keyName)) return;
            if (GameConfig.KeyMap.ContainsKey(action)) GameConfig.KeyMap[action] = keyName;
        }

        /// <summary>Returns current keybind map lines for display.</summary>
        /// <remarks>PHASE 2 - Team 9: Keybind Customization</remarks>
        public static IReadOnlyList<string> GetLines()
        {
            return GameConfig.KeyMap.OrderBy(kv => kv.Key)
                .Select(kv => kv.Key + " = " + kv.Value)
                .ToList();
        }
    }

    /// <summary>
    /// Chat/message channel for local UI use.
    /// </summary>
    /// <remarks>PHASE 2 - Team 9: Chat/Message System</remarks>
    public static class ChatMessageSystem
    {
        private static readonly Queue<string> _messages = new Queue<string>();

        /// <summary>Posts a chat message into local channel.</summary>
        /// <remarks>PHASE 2 - Team 9: Chat/Message System</remarks>
        public static void Post(string user, string message)
        {
            string u = string.IsNullOrWhiteSpace(user) ? "Player" : user.Trim();
            string m = string.IsNullOrWhiteSpace(message) ? "..." : message.Trim();
            _messages.Enqueue($"{u}: {m}");
            while (_messages.Count > 20) _messages.Dequeue();
        }

        /// <summary>Returns chat history snapshot.</summary>
        /// <remarks>PHASE 2 - Team 9: Chat/Message System</remarks>
        public static IReadOnlyList<string> History() => _messages.ToList();
    }

    /// <summary>
    /// Screenshot gallery scanner.
    /// </summary>
    /// <remarks>PHASE 2 - Team 9: Screenshot Gallery</remarks>
    public static class ScreenshotGallerySystem
    {
        /// <summary>Returns screenshot file names from screenshots folder.</summary>
        /// <remarks>PHASE 2 - Team 9: Screenshot Gallery</remarks>
        public static IReadOnlyList<string> ListShots()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            if (!Directory.Exists(dir)) return new[] { "No screenshots folder." };
            return Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderByDescending(x => x)
                .Take(30)
                .ToList();
        }
    }
}
