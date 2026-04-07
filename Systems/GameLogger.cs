// ────────────────────────────────────────────
// PHASE 2 - Team 10: Engine Programmer
// Feature: Structured Game Logger & Diagnostics
// Purpose: Centralized JSON-structured logging for ALL entity
//          interactions, game events, bot decisions, and state
//          snapshots.  Output goes to Visual Studio Output Window
//          (Debug.WriteLine) AND a JSON log file for post-run
//          analysis by Claude or any automated QA tool.
// ────────────────────────────────────────────

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Systems
{
    // ══════════════════════════════════════════════════════════════════
    // LOG LEVEL
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Severity for <see cref="GameLogger"/> entries.</summary>
    public enum GameLogLevel { DEBUG, INFO, WARNING, ERROR }

    // ══════════════════════════════════════════════════════════════════
    // GAME LOGGER — Singleton
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Centralized, thread-safe, structured JSON logger for every
    /// meaningful game event.  Designed so that the output file
    /// (<c>Logs/game_events.jsonl</c>) can be fed directly to an
    /// LLM for automated diagnosis.
    ///
    /// <para><b>Usage:</b></para>
    /// <code>
    /// GameLogger.Log("EntityAction", GameLogLevel.INFO, new {
    ///     entityType = "Enemy",
    ///     entityId   = "Goomba_01",
    ///     action     = "Defeated",
    ///     position   = new { x = 120, y = 340 }
    /// });
    /// </code>
    /// </summary>
    /// <remarks>PHASE 2 - Team 10 (Engine Programmer): Structured diagnostics</remarks>
    public static class GameLogger
    {
        // ── Configuration ─────────────────────────────────────────────
        /// <summary>Minimum level written to file. DEBUG in debug builds, INFO in release.</summary>
#if DEBUG
        public static GameLogLevel MinLevel { get; set; } = GameLogLevel.DEBUG;
#else
        public static GameLogLevel MinLevel { get; set; } = GameLogLevel.INFO;
#endif

        /// <summary>When true, every log line is also sent to Debug.WriteLine.</summary>
        public static bool WriteToVSOutput { get; set; } = true;

        /// <summary>When true, entity position logs are throttled to 1/s per entity.</summary>
        public static bool ThrottlePositionLogs { get; set; } = true;

        // ── File output ───────────────────────────────────────────────
        private static readonly string _logDir;
        private static readonly string _logPath;
        private static readonly object _fileLock = new object();
        private static StreamWriter    _writer;

        // ── Throttle map (entityKey → last-log-time) ──────────────────
        private static readonly ConcurrentDictionary<string, float> _throttleMap =
            new ConcurrentDictionary<string, float>();

        // ── Session clock ─────────────────────────────────────────────
        private static readonly Stopwatch _clock = Stopwatch.StartNew();

        // ── Ring buffer for state snapshots ────────────────────────────
        private static readonly object _ringLock = new object();
        private static readonly string[] _recentLines = new string[64];
        private static int _ringHead;

        // ══════════════════════════════════════════════════════════════
        // STATIC CONSTRUCTOR — opens log file
        // ══════════════════════════════════════════════════════════════

        static GameLogger()
        {
            _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(_logDir);

            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _logPath = Path.Combine(_logDir, $"game_events_{stamp}.jsonl");

            try
            {
                _writer = new StreamWriter(_logPath, false, Encoding.UTF8) { AutoFlush = true };
            }
            catch
            {
                // Fallback: if file can't be opened, VS Output is still used
                _writer = null;
            }

            // Session header
            WriteRaw("{\"event\":\"SessionStart\"," +
                     $"\"timestamp\":\"{DateTime.UtcNow:O}\"," +
                     $"\"machine\":\"{Environment.MachineName}\"," +
                     $"\"runtime\":\"{Environment.Version}\"}}");
        }

        // ══════════════════════════════════════════════════════════════
        // PUBLIC API — Generic structured log
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Write a structured JSON log line.
        /// <paramref name="data"/> is serialized as inline JSON key-value pairs.
        /// </summary>
        /// <param name="eventName">Short event tag, e.g. "EntityAction", "BotDecision".</param>
        /// <param name="level">Severity level.</param>
        /// <param name="data">Anonymous object whose public properties become JSON fields.</param>
        public static void Log(string eventName, GameLogLevel level, object data = null)
        {
            if (level < MinLevel) return;

            string json = BuildJson(eventName, level, data);
            WriteRaw(json);
        }

        // ══════════════════════════════════════════════════════════════
        // PUBLIC API — Typed helpers
        // ══════════════════════════════════════════════════════════════

        /// <summary>Log a game-system event (level start, save, scene transition).</summary>
        public static void LogSystem(string action, string detail = null)
        {
            Log("SystemEvent", GameLogLevel.INFO, new
            {
                action,
                detail = detail ?? "",
                scene  = Game.Instance?.Scenes?.Current?.GetType().Name ?? "none"
            });
        }

        /// <summary>Log any entity interaction (move, attack, collect, defeat, etc.).</summary>
        public static void LogEntity(string entityType, string entityId,
            string action, float x, float y, string state = null, string detail = null)
        {
            // Throttle noisy position logs to once per second per entity
            if (ThrottlePositionLogs && action == "Move")
            {
                string key = $"{entityType}_{entityId}";
                float now = (float)_clock.Elapsed.TotalSeconds;
                if (_throttleMap.TryGetValue(key, out float last) && now - last < 1f)
                    return;
                _throttleMap[key] = now;
            }

            Log("EntityAction", GameLogLevel.INFO, new
            {
                entityType,
                entityId,
                action,
                position = new { x = (int)x, y = (int)y },
                state  = state ?? "",
                detail = detail ?? ""
            });
        }

        /// <summary>Log an entity by directly reading its position from the object.</summary>
        public static void LogEntity(Entity entity, string entityType,
            string entityId, string action, string state = null, string detail = null)
        {
            if (entity == null) return;
            LogEntity(entityType, entityId, action, entity.X, entity.Y, state, detail);
        }

        /// <summary>Log a player action (jump, attack, take damage, etc.).</summary>
        public static void LogPlayer(string action, Player player, string detail = null)
        {
            if (player == null) return;
            Log("PlayerAction", GameLogLevel.INFO, new
            {
                action,
                position = new { x = (int)player.X, y = (int)player.Y },
                health   = player.Health,
                maxHp    = player.MaxHealth,
                grounded = player.IsGrounded,
                velX     = (int)player.VelocityX,
                velY     = (int)player.VelocityY,
                detail   = detail ?? ""
            });
        }

        /// <summary>Log a bot AI decision.</summary>
        public static void LogBot(string decision, float playerX, float playerY,
            string state = null, string detail = null)
        {
            Log("BotDecision", GameLogLevel.DEBUG, new
            {
                decision,
                position = new { x = (int)playerX, y = (int)playerY },
                state  = state ?? "",
                detail = detail ?? ""
            });
        }

        /// <summary>Log that the bot is stuck (no progress toward objective).</summary>
        public static void LogBotStuck(string level, string reason,
            float playerX, float playerY, float timeElapsed, string botState = null)
        {
            Log("BotStuck", GameLogLevel.ERROR, new
            {
                level,
                reason,
                botState = botState ?? "unknown",
                position = new { x = (int)playerX, y = (int)playerY },
                timeElapsed = Math.Round(timeElapsed, 1)
            });
        }

        /// <summary>Log a scene transition (push, pop, replace).</summary>
        public static void LogSceneTransition(string transitionType, string fromScene, string toScene)
        {
            Log("SceneTransition", GameLogLevel.INFO, new
            {
                transition = transitionType,
                from = fromScene ?? "none",
                to   = toScene ?? "none"
            });
        }

        /// <summary>Log a dialogue interaction.</summary>
        public static void LogDialogue(string speaker, string line, int choiceIndex = -1)
        {
            Log("Dialogue", GameLogLevel.INFO, new
            {
                speaker,
                line = line.Length > 80 ? line.Substring(0, 80) + "..." : line,
                choiceIndex
            });
        }

        /// <summary>Log a level completion or failure.</summary>
        public static void LogLevelResult(string levelId, string levelName,
            bool beaten, float time, int items, int kills)
        {
            Log("LevelResult", GameLogLevel.INFO, new
            {
                levelId,
                levelName,
                beaten,
                timeSec = Math.Round(time, 1),
                itemsCollected    = items,
                enemiesDefeated   = kills
            });
        }

        /// <summary>Log an item/inventory change.</summary>
        public static void LogInventory(string action, string itemName, int quantity = 1)
        {
            Log("Inventory", GameLogLevel.INFO, new
            {
                action,
                item     = itemName,
                quantity
            });
        }

        /// <summary>Log a physics interaction (collision, landing, bounce).</summary>
        public static void LogPhysics(string interaction, float x, float y, string detail = null)
        {
            Log("Physics", GameLogLevel.DEBUG, new
            {
                interaction,
                position = new { x = (int)x, y = (int)y },
                detail = detail ?? ""
            });
        }

        // ══════════════════════════════════════════════════════════════
        // STATE SNAPSHOT — Full game state capture on errors
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Captures a complete game-state snapshot and writes it as a single
        /// JSON line.  Call this when the bot gets stuck, a level fails, or
        /// any anomaly is detected.  The snapshot includes player state,
        /// active scene, and the last 16 log lines for context.
        /// </summary>
        public static void CaptureStateSnapshot(string reason,
            Player player = null, IEnumerable<EntitySnapshot> entities = null)
        {
            var sb = new StringBuilder(1024);
            sb.Append("{\"event\":\"StateSnapshot\",");
            sb.Append($"\"timestamp\":\"{DateTime.UtcNow:O}\",");
            sb.Append($"\"elapsed\":{_clock.Elapsed.TotalSeconds:F1},");
            sb.Append($"\"reason\":\"{Escape(reason)}\",");

            // Scene
            string scene = Game.Instance?.Scenes?.Current?.GetType().Name ?? "none";
            sb.Append($"\"scene\":\"{scene}\",");
            sb.Append($"\"level\":\"{Game.Instance?.CurrentLevelName ?? ""}\",");

            // Player
            if (player != null)
            {
                sb.Append("\"player\":{");
                sb.Append($"\"x\":{(int)player.X},\"y\":{(int)player.Y},");
                sb.Append($"\"hp\":{player.Health},\"maxHp\":{player.MaxHealth},");
                sb.Append($"\"grounded\":{(player.IsGrounded ? "true" : "false")},");
                sb.Append($"\"velX\":{(int)player.VelocityX},\"velY\":{(int)player.VelocityY}");
                sb.Append("},");
            }

            // Entities
            if (entities != null)
            {
                sb.Append("\"entities\":[");
                bool first = true;
                foreach (var e in entities)
                {
                    if (!first) sb.Append(",");
                    sb.Append($"{{\"type\":\"{Escape(e.Type)}\",\"id\":\"{Escape(e.Id)}\",");
                    sb.Append($"\"state\":\"{Escape(e.State)}\",");
                    sb.Append($"\"x\":{e.X},\"y\":{e.Y}}}");
                    first = false;
                }
                sb.Append("],");
            }

            // Recent log lines (last 16)
            sb.Append("\"recentLogs\":[");
            lock (_ringLock)
            {
                bool first = true;
                for (int i = 0; i < 16; i++)
                {
                    int idx = (_ringHead - 16 + i + _recentLines.Length) % _recentLines.Length;
                    string line = _recentLines[idx];
                    if (line == null) continue;
                    if (!first) sb.Append(",");
                    sb.Append($"\"{Escape(line)}\"");
                    first = false;
                }
            }
            sb.Append("]");

            sb.Append("}");
            WriteRaw(sb.ToString());
        }

        /// <summary>Lightweight struct for entity snapshots in state captures.</summary>
        public struct EntitySnapshot
        {
            public string Type;
            public string Id;
            public string State;
            public int X;
            public int Y;

            public EntitySnapshot(string type, string id, string state, float x, float y)
            {
                Type = type; Id = id; State = state;
                X = (int)x; Y = (int)y;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // BOT STUCK DETECTOR — integrated directly
        // ══════════════════════════════════════════════════════════════

        private static float _botAnchorX, _botAnchorY;
        private static float _botStuckTimer;
        private static bool  _botStuckFired;
        private const  float BOT_STUCK_THRESHOLD = 4f;
        private const  float BOT_STUCK_RADIUS    = 20f;

        /// <summary>Reset the stuck detector (call when a new level starts).</summary>
        public static void ResetBotStuckDetector(float x, float y)
        {
            _botAnchorX    = x;
            _botAnchorY    = y;
            _botStuckTimer = 0f;
            _botStuckFired = false;
        }

        /// <summary>
        /// Call every frame with the player's current position.
        /// Returns true if the bot is currently considered stuck.
        /// Automatically logs a BotStuck ERROR and a StateSnapshot
        /// the first time the stuck threshold is exceeded.
        /// </summary>
        public static bool UpdateBotStuckDetector(float dt, float x, float y,
            string levelName = null, string botState = null, Player player = null)
        {
            float dx = x - _botAnchorX;
            float dy = y - _botAnchorY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist > BOT_STUCK_RADIUS)
            {
                // Good progress — reset
                _botAnchorX    = x;
                _botAnchorY    = y;
                _botStuckTimer = 0f;
                _botStuckFired = false;
                return false;
            }

            _botStuckTimer += dt;

            if (_botStuckTimer >= BOT_STUCK_THRESHOLD && !_botStuckFired)
            {
                _botStuckFired = true;
                LogBotStuck(
                    levelName ?? Game.Instance?.CurrentLevelName ?? "unknown",
                    "NoProgress",
                    x, y, _botStuckTimer, botState);
                CaptureStateSnapshot("BotStuck", player);
            }

            return _botStuckTimer >= BOT_STUCK_THRESHOLD;
        }

        // ══════════════════════════════════════════════════════════════
        // FLUSH / SHUTDOWN
        // ══════════════════════════════════════════════════════════════

        /// <summary>Flush the file writer. Call on application exit.</summary>
        public static void Shutdown()
        {
            WriteRaw("{\"event\":\"SessionEnd\"," +
                     $"\"timestamp\":\"{DateTime.UtcNow:O}\"," +
                     $"\"elapsed\":{_clock.Elapsed.TotalSeconds:F1}}}");
            lock (_fileLock)
            {
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // INTERNAL — JSON builder (no external dependency)
        // ══════════════════════════════════════════════════════════════

        /// <summary>Builds a JSON line from an event name and an anonymous data object.</summary>
        private static string BuildJson(string eventName, GameLogLevel level, object data)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"event\":\"").Append(Escape(eventName)).Append("\",");
            sb.Append("\"level\":\"").Append(level).Append("\",");
            sb.Append("\"t\":").Append(_clock.Elapsed.TotalSeconds.ToString("F2")).Append(",");

            // Serialize anonymous object properties via reflection (lightweight)
            if (data != null)
            {
                foreach (var prop in data.GetType().GetProperties())
                {
                    object val = prop.GetValue(data);
                    sb.Append("\"").Append(prop.Name).Append("\":");
                    AppendValue(sb, val);
                    sb.Append(",");
                }
            }

            // Remove trailing comma
            if (sb[sb.Length - 1] == ',')
                sb.Length--;

            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>Appends a value as JSON (handles primitives, strings, booleans, nested objects).</summary>
        private static void AppendValue(StringBuilder sb, object val)
        {
            if (val == null) { sb.Append("null"); return; }

            Type t = val.GetType();

            if (val is string s)
            {
                sb.Append("\"").Append(Escape(s)).Append("\"");
            }
            else if (val is bool b)
            {
                sb.Append(b ? "true" : "false");
            }
            else if (val is int || val is long || val is short || val is byte)
            {
                sb.Append(val);
            }
            else if (val is float f)
            {
                sb.Append(f.ToString("F1"));
            }
            else if (val is double d)
            {
                sb.Append(d.ToString("F1"));
            }
            else if (t.IsClass || (t.IsValueType && !t.IsPrimitive))
            {
                // Nested anonymous type — serialize its properties recursively
                sb.Append("{");
                bool first = true;
                foreach (var prop in t.GetProperties())
                {
                    if (!first) sb.Append(",");
                    sb.Append("\"").Append(prop.Name).Append("\":");
                    AppendValue(sb, prop.GetValue(val));
                    first = false;
                }
                sb.Append("}");
            }
            else
            {
                sb.Append(val);
            }
        }

        /// <summary>Escapes special JSON characters in a string.</summary>
        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

        /// <summary>Writes a raw JSON line to file + VS output + ring buffer.</summary>
        private static void WriteRaw(string json)
        {
            // Ring buffer for state snapshots
            lock (_ringLock)
            {
                _recentLines[_ringHead % _recentLines.Length] = json;
                _ringHead++;
            }

            // Visual Studio Output Window
            if (WriteToVSOutput)
                Debug.WriteLine($"[GAME] {json}");

            // File output (thread-safe)
            lock (_fileLock)
            {
                try { _writer?.WriteLine(json); }
                catch { /* swallow I/O errors to never crash gameplay */ }
            }
        }
    }
}
