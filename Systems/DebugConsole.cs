using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// In-game debug command console (Team 3 / Team 8).
    ///
    /// Type commands at runtime to warp levels, toggle modes, inspect state,
    /// or tune parameters without recompiling.
    ///
    /// Team 3  (Technical Lead)        — Idea 3: scene stack inspector command.
    /// Team 8  (Systems Programmer)    — Idea 8: debug command console.
    /// Team 2  (Producer)              — Idea 8: cheat key sheet displayed here.
    ///
    /// ── Activation ──────────────────────────────────────────────────────────
    /// Toggled with the tilde/backtick key (` or ~).
    /// Wire in Form1.KeyDown → DebugConsole.HandleKey(keyCode, keyChar).
    ///
    /// ── Built-in commands ───────────────────────────────────────────────────
    ///   god           — toggle God Mode.
    ///   warp <id>     — warp to level by node id (e.g. "warp dino").
    ///   lives <n>     — set lives count.
    ///   score <n>     — add score.
    ///   clear         — clear the console log.
    ///   errors        — print the last 5 debug logger entries.
    ///   scenes        — list the active scene stack.
    ///   help          — print command reference.
    /// </summary>
    public static class DebugConsole
    {
        // ── Visibility ─────────────────────────────────────────────────────────
        /// <summary>True while the console overlay is shown.</summary>
        public static bool IsOpen { get; private set; }

        // ── Console state ──────────────────────────────────────────────────────
        private static string  _input   = "";                    // current typing buffer
        private static List<string> _log = new List<string>();   // output history
        private const int MaxLog  = 20;   // lines kept in memory
        private const int MaxInput = 64;  // characters allowed per command

        // ── Font ───────────────────────────────────────────────────────────────
        private static readonly Font _font = new Font("Courier New", 10, FontStyle.Regular);

        // ── Error deduplication (Team 3 — Idea 9) ─────────────────────────────
        // Tracks last logged error context + message to suppress repeat floods.
        private static string _lastErrContext  = null;
        private static string _lastErrMsg      = null;
        private static int    _lastErrRepeat   = 0;
        private const  int    MaxRepeatSuppress = 5;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Toggles the console open/closed.</summary>
        public static void Toggle() => IsOpen = !IsOpen;

        /// <summary>
        /// Programmatically prints a line to the console log without the user typing.
        /// Called by DebugLogger to feed errors / warnings into the overlay.
        /// Team 3 (Technical Lead) — Idea 8: live error feed.
        /// </summary>
        public static void Print(string line)
        {
            AppendLog(line);
        }

        /// <summary>
        /// Processes a key event from the WinForms host.
        /// Returns true if the key was consumed by the console (suppress game input).
        /// </summary>
        public static bool HandleKey(System.Windows.Forms.Keys key, char keyChar)
        {
            if (!IsOpen) return false;

            switch (key)
            {
                case System.Windows.Forms.Keys.Return:
                    Execute(_input.Trim());
                    _input = "";
                    return true;

                case System.Windows.Forms.Keys.Back:
                    if (_input.Length > 0)
                        _input = _input.Substring(0, _input.Length - 1);
                    return true;

                case System.Windows.Forms.Keys.Escape:
                    IsOpen = false;
                    return true;

                default:
                    // Printable characters only.
                    if (keyChar >= 32 && keyChar < 127 && _input.Length < MaxInput)
                        _input += keyChar;
                    return true;
            }
        }

        /// <summary>
        /// Deduplicates error log entries that repeat in quick succession.
        /// Called by DebugLogger before writing — returns true to suppress the entry.
        /// Team 3 (Technical Lead) — Idea 9: error deduplication / rate limiter.
        /// </summary>
        public static bool ShouldSuppress(string context, string msg)
        {
            if (context == _lastErrContext && msg == _lastErrMsg)
            {
                _lastErrRepeat++;
                return _lastErrRepeat > MaxRepeatSuppress;
            }
            // New unique error — reset tracking.
            _lastErrContext = context;
            _lastErrMsg     = msg;
            _lastErrRepeat  = 0;
            return false;
        }

        // ── Draw ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws the console overlay at the bottom of the screen.
        /// Call from Game.OnRender AFTER all other draws.
        /// </summary>
        public static void Draw(Graphics g, int W, int H)
        {
            if (!IsOpen) return;

            int consoleH = 260;
            int y        = H - consoleH;

            // ── Panel ──────────────────────────────────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(220, 6, 8, 16)))
                g.FillRectangle(br, 0, y, W, consoleH);
            using (var pen = new Pen(Color.FromArgb(180, 60, 200, 60), 1))
                g.DrawLine(pen, 0, y, W, y);

            // ── Title bar ──────────────────────────────────────────────────────
            g.DrawString("[ DEBUG CONSOLE — type 'help' for commands ]", _font,
                         Brushes.LimeGreen, 8, y + 4);

            // ── Log lines ──────────────────────────────────────────────────────
            int lineY = y + 22;
            int start = Math.Max(0, _log.Count - MaxLog + 1);
            for (int i = start; i < _log.Count; i++)
            {
                string line = _log[i];
                Brush  br2  = line.StartsWith("[ERR]") ? Brushes.OrangeRed
                            : line.StartsWith("[OK]")  ? Brushes.LimeGreen
                            :                            Brushes.LightGray;
                g.DrawString(line, _font, br2, 8, lineY);
                lineY += 14;
            }

            // ── Input prompt ───────────────────────────────────────────────────
            bool cursorOn = (int)(Environment.TickCount / 400) % 2 == 0;
            string prompt = "> " + _input + (cursorOn ? "_" : " ");
            g.DrawString(prompt, _font, Brushes.Cyan, 8, H - 22);
        }

        // ── Command execution ──────────────────────────────────────────────────

        private static void Execute(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            AppendLog($"> {raw}");

            string[] parts = raw.Split(' ');
            string   cmd   = parts[0].ToLowerInvariant();

            switch (cmd)
            {
                case "god":
                    Game.Instance.GodMode = !Game.Instance.GodMode;
                    AppendLog($"[OK] GodMode = {Game.Instance.GodMode}");
                    break;

                case "lives":
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int lv))
                    {
                        Game.Instance.CurrentLives = lv;
                        AppendLog($"[OK] Lives set to {lv}");
                    }
                    else AppendLog("[ERR] Usage: lives <number>");
                    break;

                case "score":
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int sc))
                    {
                        Game.Instance.PlayerBounty += sc;
                        AppendLog($"[OK] Score += {sc}  (total: {Game.Instance.PlayerBounty})");
                    }
                    else AppendLog("[ERR] Usage: score <number>");
                    break;

                case "warp":
                    if (parts.Length >= 2)
                    {
                        string nid = parts[1].ToLowerInvariant();
                        WarpToNode(nid);
                    }
                    else AppendLog("[ERR] Usage: warp <node_id>  (e.g. warp dino)");
                    break;

                case "clear":
                    _log.Clear();
                    break;

                case "errors":
                    var entries = VisualDebugger.GetSnapshot();
                    int shown   = Math.Min(5, entries.Count);
                    for (int i = entries.Count - shown; i < entries.Count; i++)
                        AppendLog($"  [{entries[i].Level}] {entries[i].Context}: {entries[i].Details.Replace('\n',' ')}");
                    if (shown == 0) AppendLog("  No errors recorded.");
                    break;

                case "scenes":
                    AppendLog($"  SceneStack depth: {Game.Instance.Scenes.Depth}");
                    AppendLog($"  Current: {Game.Instance.Scenes.Current?.GetType().Name ?? "null"}");
                    break;

                case "fps":
                    AppendLog($"  FrameTime: {PerformanceProfiler.LastFrameMs:F2}ms  " +
                              $"Update:{PerformanceProfiler.LastUpdateMs:F2}ms  " +
                              $"Render:{PerformanceProfiler.LastRenderMs:F2}ms");
                    break;

                case "help":
                    AppendLog("  god | lives <n> | score <n> | warp <id>");
                    AppendLog("  clear | errors | scenes | fps | help");
                    break;

                default:
                    AppendLog($"[ERR] Unknown command: {cmd}");
                    break;
            }
        }

        /// <summary>Warps the player to a specific level node by id.</summary>
        private static void WarpToNode(string nodeId)
        {
            switch (nodeId)
            {
                case "dino":         Game.Instance.Scenes.Replace(new Fridays_Adventure.Scenes.IslandScene("dino",         "Dinosaur Island")); break;
                case "wano":         Game.Instance.Scenes.Replace(new Fridays_Adventure.Scenes.IslandScene("wano",         "Blade Nation")); break;
                case "harbor":       Game.Instance.Scenes.Replace(new Fridays_Adventure.Scenes.IslandScene("harbor",       "Harbor Town")); break;
                case "coral":        Game.Instance.Scenes.Replace(new Fridays_Adventure.Scenes.IslandScene("coral",        "Coral Reef")); break;
                case "tundra":       Game.Instance.Scenes.Replace(new Fridays_Adventure.Scenes.IslandScene("tundra",       "Tundra Peak")); break;
                case "overworld":    Game.Instance.Scenes.Replace(new Fridays_Adventure.Scenes.OverworldScene()); break;
                case "title":        Game.Instance.Scenes.Replace(new Fridays_Adventure.Scenes.TitleScene()); break;
                case "boss":         Game.Instance.Scenes.Replace(new Fridays_Adventure.Scenes.BossScene()); break;
                default:
                    AppendLog($"[ERR] Unknown node: {nodeId}");
                    return;
            }
            AppendLog($"[OK] Warped to: {nodeId}");
            IsOpen = false;
        }

        private static void AppendLog(string line)
        {
            _log.Add(line);
            if (_log.Count > MaxLog * 2) _log.RemoveAt(0);
        }
    }
}
