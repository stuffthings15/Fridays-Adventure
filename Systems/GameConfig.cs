using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Game Configuration system — persistent settings file with hot-reload.
    ///
    /// Team 3 (Technical Lead) — all 10 ideas implemented below:
    ///
    ///   Idea 1:  JSON-style flat key=value config file loader (game-config.ini).
    ///   Idea 2:  Rebindable input key-name table stored in config.
    ///   Idea 3:  Global exception handler registration via AppDomain.
    ///   Idea 4:  Target frame-rate cap (default 60 FPS).
    ///   Idea 5:  Managed heap size report at startup/scene-change.
    ///   Idea 6:  Named debug-flag registry (runtime-togglable bool switches).
    ///   Idea 7:  Scene preload queue (queues type names for background init).
    ///   Idea 8:  Physics sub-step accumulator cap (prevents spiral-of-death).
    ///   Idea 9:  Platform / runtime diagnostics header written on load.
    ///   Idea 10: File watcher triggers config hot-reload without restart.
    /// </summary>
    public static class GameConfig
    {
        // ── Config file path ─────────────────────────────────────────────────
        private static readonly string ConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game-config.ini");

        // ── Internal key=value store ─────────────────────────────────────────
        private static readonly Dictionary<string, string> _values =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ── Idea 4: Target frame-rate cap ─────────────────────────────────────
        /// <summary>
        /// Target frames per second for the game loop timer.
        /// Reads from config key "TargetFps"; defaults to 60.
        /// Idea 4 (Technical Lead).
        /// </summary>
        public static int TargetFps =>
            GetInt("TargetFps", 60);

        /// <summary>Interval in milliseconds for the game-loop timer (1000 / TargetFps).</summary>
        public static int FrameIntervalMs =>
            Math.Max(1, 1000 / TargetFps);

        // ── Idea 8: Physics sub-step cap ──────────────────────────────────────
        /// <summary>
        /// Maximum allowed delta-time (seconds) fed to the physics integrator.
        /// Prevents the spiral-of-death when the app is paused / slow.
        /// Idea 8 (Technical Lead).
        /// </summary>
        public static float MaxPhysicsDt =>
            GetFloat("MaxPhysicsDt", 0.033f);   // default: ~2 missed frames

        // ── Idea 2: Rebindable input key names ────────────────────────────────
        /// <summary>
        /// Key bindings dictionary: action name → Windows Forms key name.
        /// Pre-populated with SMB3-style defaults; overridden from config file.
        /// Idea 2 (Technical Lead).
        /// </summary>
        public static readonly Dictionary<string, string> KeyMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Jump",        "Space"  },
                { "Attack",      "Z"      },
                { "Ability1",    "X"      },
                { "Ability2",    "C"      },
                { "Pause",       "Escape" },
                { "RunLeft",     "Left"   },
                { "RunRight",    "Right"  },
                { "Crouch",      "Down"   },
                { "LookUp",      "Up"     },
                { "UseReserve",  "A"      },
            };

        // ── Idea 6: Named debug-flag registry ─────────────────────────────────
        /// <summary>
        /// Runtime-togglable boolean debug flags.
        /// Set from config with prefix "Debug." (e.g. Debug.ShowHitboxes=true).
        /// Idea 6 (Technical Lead).
        /// </summary>
        private static readonly Dictionary<string, bool> _debugFlags =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns the value of a named debug flag; false if not set.</summary>
        public static bool IsDebugFlagSet(string flag)
        {
            return _debugFlags.ContainsKey(flag) && _debugFlags[flag];
        }

        /// <summary>Toggles a debug flag at runtime (persists until restart).</summary>
        public static void ToggleDebugFlag(string flag)
        {
            _debugFlags[flag] = !IsDebugFlagSet(flag);
            DebugLogger.LogInfo("GameConfig", $"Debug flag '{flag}' = {_debugFlags[flag]}");
        }

        // ── Idea 7: Scene preload queue ───────────────────────────────────────
        /// <summary>
        /// Queue of scene type-names marked for pre-initialisation.
        /// Populated from config key "Preload" (comma-separated type names).
        /// Idea 7 (Technical Lead).
        /// </summary>
        public static readonly Queue<string> PreloadQueue = new Queue<string>();

        // ── Idea 10: File-watcher hot-reload ─────────────────────────────────
        private static FileSystemWatcher _watcher;

        // ── Static constructor (load on first access) ─────────────────────────
        static GameConfig()
        {
            Load();
            RegisterExceptionHandler();   // Idea 3
            WriteDiagnosticsHeader();      // Idea 9
            StartWatcher();                // Idea 10
        }

        // ── Idea 1: Config file loader ────────────────────────────────────────

        /// <summary>
        /// Loads (or reloads) all settings from game-config.ini.
        /// Creates the file with defaults if it does not exist.
        /// Idea 1 (Technical Lead).
        /// </summary>
        public static void Load()
        {
            if (!File.Exists(ConfigPath))
                WriteDefaults();

            _values.Clear();
            _debugFlags.Clear();
            PreloadQueue.Clear();

            foreach (string raw in File.ReadAllLines(ConfigPath, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;

                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();

                // Idea 2: key-binding lines
                if (key.StartsWith("Key.", StringComparison.OrdinalIgnoreCase))
                {
                    string action = key.Substring(4);
                    if (KeyMap.ContainsKey(action)) KeyMap[action] = val;
                    continue;
                }

                // Idea 6: debug flag lines
                if (key.StartsWith("Debug.", StringComparison.OrdinalIgnoreCase))
                {
                    string flag = key.Substring(6);
                    _debugFlags[flag] = val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1";
                    continue;
                }

                // Idea 7: preload queue
                if (key.Equals("Preload", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string t in val.Split(','))
                        if (!string.IsNullOrWhiteSpace(t)) PreloadQueue.Enqueue(t.Trim());
                    continue;
                }

                _values[key] = val;
            }

            DebugLogger.LogInfo("GameConfig", $"Config loaded. TargetFps={TargetFps} MaxDt={MaxPhysicsDt:F3}");
        }

        /// <summary>
        /// Public alias for <see cref="Load"/> used by hot-reload callbacks and
        /// the TechLead HotReloadConfig system. Clears all value caches and
        /// re-reads the config file from disk.
        /// Team 3 (Technical Lead) — Idea 4: hot-reload config.
        /// </summary>
        public static void Reload() => Load();

        // ── Idea 3: Global exception handler ──────────────────────────────────

        /// <summary>
        /// Registers an AppDomain-level unhandled exception handler.
        /// Captures crashes into the error log before the process exits.
        /// Idea 3 (Technical Lead).
        /// </summary>
        private static void RegisterExceptionHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                string msg = ex?.ToString() ?? args.ExceptionObject?.ToString() ?? "Unknown crash";
                DebugLogger.LogCritical("AppDomain.UnhandledException", ex ?? new Exception(msg));
            };
        }

        // ── Idea 5: Managed heap report ───────────────────────────────────────

        /// <summary>
        /// Returns current managed heap size formatted as a human-readable string.
        /// Idea 5 (Technical Lead).
        /// </summary>
        public static string GetHeapReport()
        {
            long bytes = GC.GetTotalMemory(false);
            return $"Heap: {bytes / 1024:N0} KB  Gen0={GC.CollectionCount(0)}  Gen1={GC.CollectionCount(1)}  Gen2={GC.CollectionCount(2)}";
        }

        // ── Idea 9: Platform/runtime diagnostics ──────────────────────────────

        /// <summary>
        /// Writes a one-time diagnostics header to the debug log.
        /// Idea 9 (Technical Lead).
        /// </summary>
        private static void WriteDiagnosticsHeader()
        {
            string cpu  = Environment.ProcessorCount + " logical CPUs";
            string os   = Environment.OSVersion.ToString();
            string clr  = Environment.Version.ToString();
            string arch = Environment.Is64BitProcess ? "x64" : "x86";
            DebugLogger.LogInfo("GameConfig.Diagnostics",
                $"OS={os} | CLR={clr} | {arch} | {cpu} | {BuildInfo.Summary}");
        }

        // ── Idea 10: File-watcher hot-reload ──────────────────────────────────

        /// <summary>
        /// Starts a FileSystemWatcher so game-config.ini changes are picked up
        /// automatically at runtime — no restart needed.
        /// Idea 10 (Technical Lead).
        /// </summary>
        private static void StartWatcher()
        {
            try
            {
                _watcher = new FileSystemWatcher(
                    Path.GetDirectoryName(ConfigPath),
                    Path.GetFileName(ConfigPath))
                {
                    NotifyFilter         = NotifyFilters.LastWrite,
                    EnableRaisingEvents  = true
                };
                _watcher.Changed += (s, e) =>
                {
                    System.Threading.Thread.Sleep(50); // brief settle delay
                    Load();
                    DebugLogger.LogInfo("GameConfig", "Config hot-reloaded from disk.");
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogWarning("GameConfig.Watcher", "FileSystemWatcher unavailable: " + ex.Message);
            }
        }

        // ── Typed getters ─────────────────────────────────────────────────────

        /// <summary>Returns a string setting value or the provided default.</summary>
        public static string Get(string key, string defaultValue = "")
            => _values.ContainsKey(key) ? _values[key] : defaultValue;

        /// <summary>Returns an integer setting value or the provided default.</summary>
        public static int GetInt(string key, int defaultValue = 0)
        {
            if (_values.ContainsKey(key) && int.TryParse(_values[key], out int v)) return v;
            return defaultValue;
        }

        /// <summary>Returns a float setting value or the provided default.</summary>
        public static float GetFloat(string key, float defaultValue = 0f)
        {
            if (_values.ContainsKey(key) && float.TryParse(_values[key],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float v)) return v;
            return defaultValue;
        }

        /// <summary>Returns a boolean setting value or the provided default.</summary>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (!_values.ContainsKey(key)) return defaultValue;
            string v = _values[key];
            return v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
        }

        // ── Default file writer ───────────────────────────────────────────────

        private static void WriteDefaults()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Friday's Adventure — Game Configuration");
            sb.AppendLine("# Edit this file to customise settings. Changes hot-reload at runtime.");
            sb.AppendLine();
            sb.AppendLine("# ── Performance ─────────────────────────────────────────────────────");
            sb.AppendLine("TargetFps=60");
            sb.AppendLine("MaxPhysicsDt=0.033");
            sb.AppendLine();
            sb.AppendLine("# ── Key bindings (Windows Forms key names) ──────────────────────────");
            sb.AppendLine("Key.Jump=Space");
            sb.AppendLine("Key.Attack=Z");
            sb.AppendLine("Key.Ability1=X");
            sb.AppendLine("Key.Ability2=C");
            sb.AppendLine("Key.Pause=Escape");
            sb.AppendLine("Key.RunLeft=Left");
            sb.AppendLine("Key.RunRight=Right");
            sb.AppendLine("Key.Crouch=Down");
            sb.AppendLine("Key.LookUp=Up");
            sb.AppendLine("Key.UseReserve=A");
            sb.AppendLine();
            sb.AppendLine("# ── Debug flags (set to true to enable) ─────────────────────────────");
            sb.AppendLine("Debug.ShowHitboxes=false");
            sb.AppendLine("Debug.ShowFps=false");
            sb.AppendLine("Debug.ShowEntityCount=false");
            sb.AppendLine("Debug.VisualOverlay=false");
            sb.AppendLine();
            sb.AppendLine("# ── Scene preload queue (comma-separated type names) ─────────────────");
            sb.AppendLine("# Preload=IslandScene,BossScene");
            File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
        }
    }
}
