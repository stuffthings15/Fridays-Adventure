using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    public sealed class DevMenuScene : Scene
    {
        private struct LevelEntry
        {
            // Human-readable label shown in the dev menu list.
            public string Label;

            // Optional scene factory for level-jump entries.
            public Func<Scene> Create;

            // Optional action for tool/debug entries.
            public Action Action;

            // Convenience flag to identify utility rows.
            public bool IsAction => Action != null;
        }

        private List<LevelEntry> _levels;
        private int _sel;
        private int _scroll;
        private const int VisibleRows = 10;
        private Rectangle _scrollUpBtn;
        private Rectangle _scrollDownBtn;

        // Debug summary cache (refreshed on a short interval for low overhead).
        private float _summaryRefreshTimer;
        private string _lastLogLine = "No errors logged yet.";
        private string _lastLogTime = "n/a";
        private string _lastLogPath = "Logs\\(no debug log yet)";
        private int _shotCount;

        public override void OnEnter()
        {
            Game.Instance.Audio.ContinueOrPlay("overworld");
            _levels = new List<LevelEntry>
            {
                // ── Original ──────────────────────────────────────────────
                new LevelEntry { Label = "Overworld Map",                   Create = () => new OverworldScene() },
                new LevelEntry { Label = "Dinosaur Island",                 Create = () => new IslandScene("dino",         "Dinosaur Island") },
                new LevelEntry { Label = "Storm Belt",                      Create = () => new StormScene() },
                new LevelEntry { Label = "Sky Island",                      Create = () => new SkyIslandScene() },
                new LevelEntry { Label = "Blade Nation",                    Create = () => new IslandScene("wano",         "Blade Nation") },
                new LevelEntry { Label = "Marine Blockade  (Boss)",         Create = () => new BossScene() },
                new LevelEntry { Label = "Warlord: Lord Sudo  (Boss)",      Create = () => new WarlordBossScene(WarlordConfig.FireLordSudo()) },
                // ── Sequel expansion ──────────────────────────────────────
                new LevelEntry { Label = "Harbor Town",                     Create = () => new IslandScene("harbor",       "Harbor Town") },
                new LevelEntry { Label = "Coral Reef",                      Create = () => new IslandScene("coral",        "Coral Reef") },
                new LevelEntry { Label = "Tundra Peak",                     Create = () => new IslandScene("tundra",       "Tundra Peak") },
                new LevelEntry { Label = "Tempest Strait  (Storm)",         Create = () => new StormScene() },
                new LevelEntry { Label = "Warlord: Lord Vanta  (Boss)",     Create = () => new WarlordBossScene(WarlordConfig.StormLordVanta()) },
                // ── New SMB3 feature scenes (direct access for validation) ───
                new LevelEntry { Label = "[NEW] Fortress Stage (SMB3)",        Create = () => new FortressScene() },
                new LevelEntry { Label = "[NEW] Airship Stage (SMB3)",         Create = () => new AirshipLevelScene() },
                new LevelEntry { Label = "[NEW] Underwater Stage (SMB3)",      Create = () => new UnderwaterScene() },
                new LevelEntry { Label = "[NEW] N-Spade Card Mini-Game",       Create = () => new CardMiniGameScene() },

                // ── Underwater chapter ────────────────────────────────────
                new LevelEntry { Label = "Sunken Gate",                     Create = () => new IslandScene("sunken_gate",  "Sunken Gate") },
                new LevelEntry { Label = "Kelp Labyrinth",                  Create = () => new IslandScene("kelp",         "Kelp Labyrinth") },
                new LevelEntry { Label = "Boiling Vent Ruins",              Create = () => new IslandScene("boiling_vent", "Boiling Vent Ruins") },
                new LevelEntry { Label = "Abyss Engine",                    Create = () => new IslandScene("abyss",        "Abyss Engine") },
                new LevelEntry { Label = "Centipede of the Deep  (Boss)",   Create = () => new WarlordBossScene(WarlordConfig.CentipedeOfTheDeep()) },

                // ── Phase 4+ tools / QA utilities ─────────────────────────
                new LevelEntry { Label = "[TOOLS] Open Logs Folder",        Action = OpenLogsFolder },
                new LevelEntry { Label = "[TOOLS] Clear Logs + Screenshots",Action = ClearLogsAndShots },
                new LevelEntry { Label = "[TOOLS] Capture Test Error",      Action = CaptureTestError },
                new LevelEntry { Label = "[TOOLS] Save Game to JSON",       Action = SaveGameAsJson },
                new LevelEntry { Label = "[TOOLS] Load Game from JSON",     Action = LoadGameFromJson },

                // ── QA / Reporting scenes ─────────────────────────────────
                new LevelEntry { Label = "[QA] Visual Debugger Report",     Create = () => new QAReportScene() },
                new LevelEntry { Label = "[QA] Achievements Browser",       Create = () => new AchievementsScene() },
            };

            // Prime the summary panel immediately when opening dev menu.
            _summaryRefreshTimer = 0f;
            RefreshDebugSummary();
        }

        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if ((input.IsPressed(System.Windows.Forms.Keys.Up) || input.IsPressed(System.Windows.Forms.Keys.W))
                && _sel > 0)
            {
                _sel--;
                if (_sel < _scroll) _scroll = _sel;
            }
            if ((input.IsPressed(System.Windows.Forms.Keys.Down) || input.IsPressed(System.Windows.Forms.Keys.S))
                && _sel < _levels.Count - 1)
            {
                _sel++;
                if (_sel >= _scroll + VisibleRows) _scroll = _sel - VisibleRows + 1;
            }

            if (input.InteractPressed)
            {
                ActivateEntry(_levels[_sel]);
                return;
            }

            if (input.PausePressed)
                Game.Instance.Scenes.Pop();

            // Refresh compact debugger summary at a fixed cadence.
            _summaryRefreshTimer -= dt;
            if (_summaryRefreshTimer <= 0f)
            {
                _summaryRefreshTimer = 0.5f;
                RefreshDebugSummary();
            }
        }

        public override void HandleMouseWheel(int delta)
        {
            if (delta > 0 && _sel > 0)
            {
                _sel--;
                if (_sel < _scroll) _scroll = _sel;
            }
            else if (delta < 0 && _sel < _levels.Count - 1)
            {
                _sel++;
                if (_sel >= _scroll + VisibleRows) _scroll = _sel - VisibleRows + 1;
            }
        }

        public override void HandleClick(Point p)
        {
            // Scroll arrows
            if (_scrollUpBtn.Contains(p) && _sel > 0)
            {
                _sel--;
                if (_sel < _scroll) _scroll = _sel;
                return;
            }
            if (_scrollDownBtn.Contains(p) && _sel < _levels.Count - 1)
            {
                _sel++;
                if (_sel >= _scroll + VisibleRows) _scroll = _sel - VisibleRows + 1;
                return;
            }

            int H = Game.Instance.CanvasHeight;
            int visible = Math.Min(VisibleRows, _levels.Count - _scroll);
            for (int i = 0; i < visible; i++)
            {
                float top = H * 0.28f + i * 40 - 4;
                if (p.Y >= top && p.Y < top + 38)
                {
                    _sel = _scroll + i;
                    ActivateEntry(_levels[_sel]);
                    return;
                }
            }
        }

        /// <summary>
        /// Activates either a level-jump scene or a dev utility action.
        /// </summary>
        private void ActivateEntry(LevelEntry entry)
        {
            if (entry.IsAction)
            {
                entry.Action?.Invoke();
                return;
            }

            if (entry.Create != null)
                Game.Instance.Scenes.Replace(entry.Create());
        }

        /// <summary>
        /// Opens the runtime log directory used by the error/visual debugger.
        /// </summary>
        private static void OpenLogsFolder()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);
            try
            {
                Process.Start("explorer.exe", logDir);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DevMenu.OpenLogsFolder", ex);
            }
        }

        /// <summary>
        /// Clears text logs and visual debugger screenshots for fresh QA passes.
        /// </summary>
        private static void ClearLogsAndShots()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            string shotDir = Path.Combine(logDir, "ErrorShots");
            try
            {
                if (Directory.Exists(logDir))
                {
                    foreach (string f in Directory.GetFiles(logDir, "*.log"))
                        File.Delete(f);
                }
                if (Directory.Exists(shotDir))
                {
                    foreach (string f in Directory.GetFiles(shotDir, "*.png"))
                        File.Delete(f);
                }
                DebugLogger.LogInfo("DevMenu.ClearLogsAndShots", "Manual clear completed.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DevMenu.ClearLogsAndShots", ex);
            }
        }

        /// <summary>
        /// Saves current runtime state to JSON for manual backup/recovery.
        /// </summary>
        private static void SaveGameAsJson()
        {
            try
            {
                Game.Instance.SyncRuntimeToSaveData();
                Game.Instance.Save.SaveJson();
                DebugLogger.LogInfo("DevMenu.SaveGameAsJson", $"Saved JSON: {SaveData.JsonSavePath}");
                SMB3Hud.ShowToast("Saved JSON backup.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DevMenu.SaveGameAsJson", ex);
            }
        }

        /// <summary>
        /// Loads runtime state from JSON backup and returns to overworld.
        /// </summary>
        private static void LoadGameFromJson()
        {
            try
            {
                var loaded = SaveData.LoadJson();
                Game.Instance.ApplySaveData(loaded);
                DebugLogger.LogInfo("DevMenu.LoadGameFromJson", $"Loaded JSON: {SaveData.JsonSavePath}");
                SMB3Hud.ShowToast("Loaded JSON backup.");
                Game.Instance.Scenes.Replace(new OverworldScene());
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DevMenu.LoadGameFromJson", ex);
            }
        }

        /// <summary>
        /// Emits a controlled test error to validate debugger logging + screenshot capture.
        /// </summary>
        private static void CaptureTestError()
        {
            try
            {
                // Intentional test exception for QA/debugger verification workflow.
                throw new InvalidOperationException("DevMenu test error (intentional). Verify log and screenshot output.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DevMenu.CaptureTestError", ex);
            }
        }

        /// <summary>
        /// Updates the cached debug summary state (latest rotating debug log + screenshot count).
        /// </summary>
        private void UpdateDebugSummary()
        {
            // Keep this wrapper for compatibility with older call sites.
            RefreshDebugSummary();
        }

        /// <summary>
        /// Reads the newest rotating debug log (`debug-YYYY-MM-DD.log`) and
        /// visual debugger screenshot counts for the compact QA panel.
        /// </summary>
        private void RefreshDebugSummary()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            string shotDir = Path.Combine(logDir, "ErrorShots");

            try
            {
                // Screenshot count for visual debugger output.
                _shotCount = Directory.Exists(shotDir)
                    ? Directory.GetFiles(shotDir, "*.png").Length
                    : 0;

                // DebugLogger writes rotating logs named debug-YYYY-MM-DD.log.
                // Pick the newest matching file for the summary panel.
                string[] logFiles = Directory.Exists(logDir)
                    ? Directory.GetFiles(logDir, "debug-*.log")
                    : Array.Empty<string>();

                if (logFiles.Length == 0)
                {
                    _lastLogLine = "No errors logged yet.";
                    _lastLogTime = "n/a";
                    _lastLogPath = "Logs\\(no debug log yet)";
                    return;
                }

                string newest = logFiles[0];
                DateTime newestTime = File.GetLastWriteTime(newest);
                for (int i = 1; i < logFiles.Length; i++)
                {
                    DateTime t = File.GetLastWriteTime(logFiles[i]);
                    if (t > newestTime)
                    {
                        newest = logFiles[i];
                        newestTime = t;
                    }
                }

                _lastLogPath = "Logs\\" + Path.GetFileName(newest);

                // Read all lines (safe for expected small daily log sizes).
                string[] lines = File.ReadAllLines(newest);
                if (lines.Length == 0)
                {
                    _lastLogLine = "(log file is empty)";
                    _lastLogTime = newestTime.ToString("yyyy-MM-dd HH:mm:ss");
                    return;
                }

                // Latest non-empty line is the most actionable quick summary.
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    _lastLogLine = line.Trim();

                    // Rotating log lines start with [HH:mm:ss.fff]
                    int close = line.IndexOf(']');
                    if (line.StartsWith("[") && close > 1)
                        _lastLogTime = DateTime.Now.ToString("yyyy-MM-dd") + " " + line.Substring(1, close - 1);
                    else
                        _lastLogTime = newestTime.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DevMenu.RefreshDebugSummary", ex);
                _lastLogLine = "Failed to read debugger logs.";
                _lastLogTime = "read error";
                _lastLogPath = "Logs\\(read error)";
            }
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Dark green developer background.
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(0, 18, 0), Color.FromArgb(0, 40, 10), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Header.
            using (var f = new Font("Courier New", 22, FontStyle.Bold))
            {
                const string title = "[ DEV  LEVEL  SELECT ]";
                SizeF sz = g.MeasureString(title, f);
                g.DrawString(title, f, Brushes.LimeGreen, (W - sz.Width) / 2f, H * 0.05f);
            }

            // God-mode indicator badge.
            const string badge = "⚡  GOD MODE ACTIVE  ⚡";
            using (var br = new SolidBrush(Color.FromArgb(180, 80, 60, 0)))
                g.FillRectangle(br, W / 2 - 160, (int)(H * 0.14f) - 4, 320, 28);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(badge, f);
                g.DrawString(badge, f, Brushes.Yellow, (W - sz.Width) / 2f, H * 0.14f);
            }

            // Level entries (scrolled window).
            int visible = Math.Min(VisibleRows, _levels.Count - _scroll);
            for (int i = 0; i < visible; i++)
            {
                int idx = _scroll + i;
                bool sel = idx == _sel;
                float ty = H * 0.28f + i * 40;

                if (sel)
                {
                    using (var br = new SolidBrush(Color.FromArgb(70, Color.LimeGreen)))
                        g.FillRectangle(br, 0, ty - 4, W, 38);
                    using (var pen = new Pen(Color.FromArgb(120, Color.LimeGreen), 1))
                        g.DrawLine(pen, 0, ty - 4, W, ty - 4);
                }

                using (var f = new Font("Courier New", 13, sel ? FontStyle.Bold : FontStyle.Regular))
                {
                    SizeF sz = g.MeasureString(_levels[idx].Label, f);
                    Brush br = _levels[idx].IsAction
                        ? (sel ? Brushes.Yellow : Brushes.LightCyan)
                        : (sel ? Brushes.LimeGreen : Brushes.DarkSeaGreen);
                    if (sel)
                        g.DrawString("▶", f, br, (W - sz.Width) / 2f - 24, ty);
                    g.DrawString(_levels[idx].Label, f, br, (W - sz.Width) / 2f, ty);
                }
            }

            // Scroll indicator + clickable arrows.
            int total = _levels.Count;
            if (total > VisibleRows)
            {
                float arrowY = H * 0.28f + VisibleRows * 40 + 4;

                // ▲ scroll-up button.
                _scrollUpBtn = new Rectangle(W / 2 - 100, (int)arrowY, 80, 28);
                bool canUp = _sel > 0;
                using (var br = new SolidBrush(canUp
                    ? Color.FromArgb(120, Color.LimeGreen)
                    : Color.FromArgb(40, Color.DimGray)))
                    g.FillRectangle(br, _scrollUpBtn);
                using (var f = new Font("Courier New", 12, FontStyle.Bold))
                    g.DrawString("▲ Up", f,
                        canUp ? Brushes.LimeGreen : Brushes.DimGray,
                        _scrollUpBtn.X + 14, _scrollUpBtn.Y + 4);

                // ▼ scroll-down button.
                _scrollDownBtn = new Rectangle(W / 2 + 20, (int)arrowY, 80, 28);
                bool canDown = _sel < total - 1;
                using (var br = new SolidBrush(canDown
                    ? Color.FromArgb(120, Color.LimeGreen)
                    : Color.FromArgb(40, Color.DimGray)))
                    g.FillRectangle(br, _scrollDownBtn);
                using (var f = new Font("Courier New", 12, FontStyle.Bold))
                    g.DrawString("▼ Dn", f,
                        canDown ? Brushes.LimeGreen : Brushes.DimGray,
                        _scrollDownBtn.X + 14, _scrollDownBtn.Y + 4);

                // Position counter.
                using (var f = new Font("Courier New", 9))
                    g.DrawString($"{_sel + 1} / {total}",
                                 f, Brushes.DimGray,
                                 W / 2f - 20, arrowY + 32);
            }

            // Footer hints.
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Up/Down  Navigate     Enter / Click  Launch     Esc  Back",
                             f, Brushes.DimGray, 12, H - 40);
            using (var f = new Font("Courier New", 9, FontStyle.Bold))
                g.DrawString("[TOOLS] rows support QA: open logs, clear logs, or generate a test error.",
                             f, Brushes.DimGray, 12, H - 22);

            DrawDebugSummaryPanel(g, W, H);
        }

        /// <summary>
        /// Compact QA/debugger panel showing latest log status and screenshot count.
        /// </summary>
        private void DrawDebugSummaryPanel(Graphics g, int W, int H)
        {
            const int panelW = 360;
            const int panelH = 124;
            int x = W - panelW - 12;
            int y = 58;

            using (var br = new SolidBrush(Color.FromArgb(180, 10, 10, 10)))
                g.FillRectangle(br, x, y, panelW, panelH);
            using (var pen = new Pen(Color.FromArgb(140, Color.LimeGreen), 1))
                g.DrawRectangle(pen, x, y, panelW, panelH);

            using (var titleFont = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("DEBUG SUMMARY", titleFont, Brushes.LimeGreen, x + 8, y + 6);

            using (var f = new Font("Courier New", 8, FontStyle.Bold))
            {
                g.DrawString("Latest:", f, Brushes.Gray, x + 8, y + 28);
                g.DrawString(_lastLogTime, f, Brushes.LightGray, x + 62, y + 28);

                g.DrawString("Message:", f, Brushes.Gray, x + 8, y + 46);
                string msg = _lastLogLine ?? string.Empty;
                if (msg.Length > 52) msg = msg.Substring(0, 52) + "...";
                g.DrawString(msg, f, Brushes.LightGray, x + 62, y + 46);

                g.DrawString("Screens:", f, Brushes.Gray, x + 8, y + 64);
                g.DrawString(_shotCount.ToString(), f, Brushes.Cyan, x + 62, y + 64);

                g.DrawString("Path:", f, Brushes.Gray, x + 8, y + 82);
                g.DrawString(_lastLogPath, f, Brushes.DarkGray, x + 62, y + 82);

                g.DrawString("Use [TOOLS] below list to inspect/reset artifacts.", f, Brushes.DimGray, x + 8, y + 102);
            }
        }
    }
}
