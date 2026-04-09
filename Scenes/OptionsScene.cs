using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    public sealed class OptionsScene : Scene
    {
        // Row model for Options menu items and documentation shortcuts.
        private enum RowType { Header, MusicVol, SfxVol, HowToPlayBtn, MoodHeader, TrackItem, ToolAction, BackBtn }

        private struct Row
        {
            public RowType Type;
            public string  Label;
            public string  Mood;
            public string  TrackFile;

            // Optional action used by debug/tool rows.
            public Action ToolAction;
        }

        private readonly List<Row> _rows = new List<Row>();
        private int    _sel;
        private string _expandedMood;
        private bool   _dirty;

        private float _sliderRepeat;
        private const float SliderRate = 0.1f;
        private const int   VolStep   = 5;

        /// <summary>Large clickable return button at the top of the options screen.</summary>
        private Rectangle _returnBtnTop;
        /// <summary>Large clickable return button at the bottom of the options screen.</summary>
        private Rectangle _returnBtnBottom;

        private static readonly Font TitleFont = new Font("Courier New", 26, FontStyle.Bold);
        private static readonly Font SecFont   = new Font("Courier New", 11,  FontStyle.Bold);
        private static readonly Font ItemFont  = new Font("Courier New", 12);
        private static readonly Font SmFont    = new Font("Courier New", 10);

        public override void OnEnter()  => Rebuild();
        public override void OnExit()   { if (_dirty) SyncAndSave(); }
        public override void OnPause()  { }
        public override void OnResume() => Rebuild();

        private void SyncAndSave()
        {
            var save = Game.Instance.Save;
            save.MusicVolume = Game.Instance.Audio.MusicVolume;
            save.SfxVolume   = Game.Instance.Audio.SfxVolume;
            foreach (string mood in new[] { "overworld", "combat", "island", "boss" })
            {
                var pl = Game.Instance.Audio.GetPlaylist(mood);
                save.PlaylistData[mood] = string.Join(",", pl);
            }
            save.Save();
        }

        private void Rebuild()
        {
            _rows.Clear();

            // Always-visible resume button so it's obvious how to close options.
            _rows.Add(new Row { Type = RowType.BackBtn, Label = "RESUME GAME" });

            // Quick inventory access from the options/pause menu.
            _rows.Add(new Row { Type = RowType.ToolAction, Label = "Inventory (I)", ToolAction = OpenInventory });

            // Manual save — syncs all runtime state to disk immediately.
            _rows.Add(new Row { Type = RowType.ToolAction, Label = "\u2714  Save Game", ToolAction = SaveGameManually });

            _rows.Add(new Row { Type = RowType.Header,   Label = "AUDIO" });
            _rows.Add(new Row { Type = RowType.MusicVol, Label = "Music Volume" });
            _rows.Add(new Row { Type = RowType.SfxVol,   Label = "SFX Volume"   });
            _rows.Add(new Row { Type = RowType.HowToPlayBtn, Label = "How To Play / Controls" });

            // PHASE 2 - Team 9: UI Programmer — Statistics Dashboard entry
            _rows.Add(new Row { Type = RowType.ToolAction, Label = "Statistics Dashboard", ToolAction = OpenStatisticsDashboard });

            // PHASE 2 - Team 9: UI Programmer — Settings Menu Integration
            _rows.Add(new Row { Type = RowType.ToolAction, Label = "Game Settings", ToolAction = OpenSettings });

            // PHASE 2 - Team 12: Art Director — CRT scanline filter toggle
            string crtLabel = Game.Instance.CrtFilterEnabled ? "CRT Filter: ON  [Toggle]" : "CRT Filter: OFF [Toggle]";
            _rows.Add(new Row { Type = RowType.ToolAction, Label = crtLabel, ToolAction = ToggleCrtFilter });

            // Phase 2 — Team 9: Accessibility outline mode toggle.
            string outlineLabel = Game.Instance.OutlineModeEnabled
                ? "Outline Mode: ON  [Toggle]"
                : "Outline Mode: OFF [Toggle]";
            _rows.Add(new Row { Type = RowType.ToolAction, Label = outlineLabel, ToolAction = ToggleOutlineMode });

            // Documentation hub — quick access to core project docs from anywhere.
            _rows.Add(new Row { Type = RowType.Header, Label = "DOCUMENTATION" });
            _rows.Add(new Row { Type = RowType.ToolAction, Label = "Open Documentation Folder", ToolAction = OpenDocumentationFolder });
            _rows.Add(new Row { Type = RowType.ToolAction, Label = "Open Master Documentation Index", ToolAction = OpenMasterDocumentationIndex });
            _rows.Add(new Row { Type = RowType.ToolAction, Label = "Open AI Docs", ToolAction = OpenAiDocs });
            _rows.Add(new Row { Type = RowType.ToolAction, Label = "Open Week 10 Running Log", ToolAction = OpenWeek10Log });
            _rows.Add(new Row { Type = RowType.ToolAction, Label = "Open README", ToolAction = OpenReadme });

            _rows.Add(new Row { Type = RowType.Header,   Label = "PLAYLISTS" });
            foreach (string mood in new[] { "overworld", "combat", "island", "boss" })
            {
                _rows.Add(new Row { Type = RowType.MoodHeader, Label = mood.ToUpper(), Mood = mood });
                if (_expandedMood == mood)
                    foreach (string t in ScanTracks(mood))
                        _rows.Add(new Row { Type = RowType.TrackItem, Mood = mood, TrackFile = t, Label = t });
            }

            // Debug/QA actions (only visible when dev mode is unlocked).
            if (Game.Instance.GodMode)
            {
                _rows.Add(new Row { Type = RowType.Header, Label = "DEBUG TOOLS" });
                _rows.Add(new Row { Type = RowType.ToolAction, Label = "Open Logs Folder", ToolAction = OpenLogsFolder });
                _rows.Add(new Row { Type = RowType.ToolAction, Label = "Capture Test Error", ToolAction = CaptureTestError });
            }

            // Application exit option
            _rows.Add(new Row { Type = RowType.Header, Label = "APPLICATION" });
            _rows.Add(new Row { Type = RowType.ToolAction, Label = "Exit to Desktop", ToolAction = () => Game.RequestClose() });

            _rows.Add(new Row { Type = RowType.BackBtn, Label = "RETURN TO GAME" });
            _sel = Math.Max(0, Math.Min(_sel, _rows.Count - 1));
        }

        private void ToggleCrtFilter()
        {
            Game.Instance.CrtFilterEnabled = !Game.Instance.CrtFilterEnabled;
            SMB3Hud.ShowToast(Game.Instance.CrtFilterEnabled ? "CRT Filter: ON" : "CRT Filter: OFF");
            Rebuild();  // refresh label text to show new state
        }

        /// <summary>
        /// Opens the inventory screen from the options/pause menu.
        /// Attempts to find the active gameplay player; if none, shows a toast.
        /// </summary>
        private static void OpenInventory()
        {
            var player = Game.Instance.GetActiveScenePlayer();
            if (player != null)
                Game.Instance.Scenes.Push(new InventoryScene(player));
            else
                SMB3Hud.ShowToast("Inventory requires an active level.");
        }

        /// <summary>
        /// Opens the statistics + performance dashboard scene.
        /// </summary>
        /// <remarks>PHASE 2 - Team 9: Statistics Dashboard / Performance Metrics Display</remarks>
        private static void OpenStatisticsDashboard()
        {
            Game.Instance.Scenes.Push(new StatisticsDashboardScene());
        }

        /// <summary>
        /// Phase 2 — Team 9 (UI Programmer): Accessibility outline toggle.
        /// Draws coloured borders around all entities so they stand out on any background.
        /// </summary>
        private void ToggleOutlineMode()
        {
            Game.Instance.OutlineModeEnabled = !Game.Instance.OutlineModeEnabled;
            SMB3Hud.ShowToast(Game.Instance.OutlineModeEnabled ? "Outline Mode: ON" : "Outline Mode: OFF");
            Rebuild();  // refresh label text to show new state
        }

        private List<string> ScanTracks(string mood)
        {
            var result = new List<string>();
            foreach (string t in Game.Instance.Audio.GetPlaylist(mood))
                if (!result.Contains(t)) result.Add(t);
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");
            if (Directory.Exists(folder))
                foreach (string f in Directory.GetFiles(folder, "music_" + mood + "*.mp3"))
                {
                    string name = Path.GetFileName(f);
                    if (!result.Contains(name)) result.Add(name);
                }
            if (result.Count == 0) result.Add("music_" + mood + "1.mp3");
            result.Sort();
            return result;
        }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(Keys.Up))
            {
                _sel = Math.Max(0, _sel - 1);
                if (_sel < _rows.Count && _rows[_sel].Type == RowType.Header)
                    _sel = Math.Max(0, _sel - 1);
            }
            if (input.IsPressed(Keys.Down))
            {
                _sel = Math.Min(_rows.Count - 1, _sel + 1);
                if (_sel < _rows.Count && _rows[_sel].Type == RowType.Header)
                    _sel = Math.Min(_rows.Count - 1, _sel + 1);
            }

            if (_sel < 0 || _sel >= _rows.Count) return;
            var row = _rows[_sel];

            if (row.Type == RowType.MusicVol || row.Type == RowType.SfxVol)
            {
                bool left  = input.IsPressed(Keys.Left)  || (_sliderRepeat <= 0 && input.LeftHeld);
                bool right = input.IsPressed(Keys.Right) || (_sliderRepeat <= 0 && input.RightHeld);
                _sliderRepeat -= dt;
                if (left || right)
                {
                    _sliderRepeat = SliderRate;
                    int delta = left ? -VolStep : VolStep;
                    if (row.Type == RowType.MusicVol)
                        Game.Instance.Audio.SetMusicVolume(Game.Instance.Audio.MusicVolume + delta);
                    else
                        Game.Instance.Audio.SetSfxVolume(Game.Instance.Audio.SfxVolume + delta);
                    _dirty = true;
                }
            }
            else { _sliderRepeat = 0; }

            if (input.InteractPressed)
                ActivateRow(row);

            if (input.PausePressed)
            {
                // Pop this scene; also pop PauseScene if it's underneath
                // so the player returns directly to gameplay
                Game.Instance.Scenes.Pop();
                if (Game.Instance.Scenes.Current is PauseScene)
                    Game.Instance.Scenes.Pop();
            }
        }

        private void ActivateRow(Row row)
        {
            switch (row.Type)
            {
                case RowType.MoodHeader:
                    _expandedMood = (_expandedMood == row.Mood) ? null : row.Mood;
                    Rebuild();
                    break;
                case RowType.TrackItem:
                    // Play the selected track immediately
                    Game.Instance.Audio.PlaySpecificTrack(row.Mood, row.TrackFile);
                    _dirty = true;
                    // Also ensure it's in the playlist
                    if (!Game.Instance.Audio.IsTrackInPlaylist(row.Mood, row.TrackFile))
                        Game.Instance.Audio.AddTrack(row.Mood, row.TrackFile);
                    Rebuild();
                    break;
                case RowType.HowToPlayBtn:
                    Game.Instance.Scenes.Push(new HowToPlayScene());
                    break;
                case RowType.ToolAction:
                    row.ToolAction?.Invoke();
                    break;
                case RowType.BackBtn:
                    // Pop this scene; also pop PauseScene if underneath
                    Game.Instance.Scenes.Pop();
                    if (Game.Instance.Scenes.Current is PauseScene)
                        Game.Instance.Scenes.Pop();
                    break;
            }
        }

        private static float RowHeight(RowType t)
        {
            switch (t)
            {
                case RowType.MusicVol:
                case RowType.SfxVol:    return 36f;
                case RowType.HowToPlayBtn: return 34f;
                case RowType.MoodHeader: return 34f;
                case RowType.TrackItem:  return 26f;
                case RowType.ToolAction: return 30f;
                case RowType.BackBtn:    return 42f;
                default:                 return 24f;
            }
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;

            // Large return buttons at top and bottom — pop all the way to gameplay
            if (_returnBtnTop.Contains(p) || _returnBtnBottom.Contains(p))
            {
                Game.Instance.Scenes.Pop();
                if (Game.Instance.Scenes.Current is PauseScene)
                    Game.Instance.Scenes.Pop();
                return;
            }

            float y = 80f;
            for (int i = 0; i < _rows.Count; i++)
            {
                float h   = RowHeight(_rows[i].Type);
                float top = _rows[i].Type == RowType.BackBtn ? y + 8 : y;
                if (p.Y >= top && p.Y < y + h)
                {
                    _sel = i;
                    ActivateRow(_rows[i]);
                    return;
                }
                y += h;
            }
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(210, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);
            SizeF tsz = g.MeasureString("OPTIONS", TitleFont);
            g.DrawString("OPTIONS", TitleFont, Brushes.Cyan, (W - tsz.Width) / 2f, 14f);

            // ── Top return button (always visible) ───────────────────────────
            DrawReturnBtn(g, W, 46, out _returnBtnTop);

            // Now Playing indicator
            string cur = Game.Instance.Audio.CurrentTrack;
            if (!string.IsNullOrEmpty(cur))
            {
                string np = "\u266B Now Playing: " + FormatTrackName(cur);
                g.DrawString(np, SmFont, Brushes.Cyan, 14, 54);
            }

            float y = 80f;
            for (int i = 0; i < _rows.Count; i++)
                DrawRow(g, _rows[i], i == _sel, W, ref y);

            // ── Bottom return button ─────────────────────────────────────────
            DrawReturnBtn(g, W, H - 72, out _returnBtnBottom);

            g.DrawString("Up/Down Navigate   Left/Right Volume   Enter Select   Esc Return to Game",
                         SmFont, Brushes.DimGray, 12, H - 20);
            DrawDevMenuButton(g);
        }

        /// <summary>
        /// Draws a large bright "RETURN TO GAME" button at the specified Y position.
        /// </summary>
        private static void DrawReturnBtn(Graphics g, int W, int btnY, out Rectangle rect)
        {
            int rbW = 280, rbH = 38;
            rect = new Rectangle((W - rbW) / 2, btnY, rbW, rbH);

            using (var br = new SolidBrush(Color.FromArgb(230, 200, 60, 20)))
                g.FillRectangle(br, rect);
            using (var pen = new Pen(Color.Gold, 3))
                g.DrawRectangle(pen, rect);
            using (var bf = new Font("Courier New", 14, FontStyle.Bold))
            {
                const string label = "\u25C0  RETURN TO GAME  \u25C0";
                SizeF sz = g.MeasureString(label, bf);
                g.DrawString(label, bf, Brushes.White,
                    rect.X + (rect.Width  - sz.Width)  / 2f,
                    rect.Y + (rect.Height - sz.Height) / 2f);
            }
        }

        private void DrawRow(Graphics g, Row row, bool sel, int W, ref float y)
        {
            const float lx = 110f;
            switch (row.Type)
            {
                case RowType.Header:
                    g.DrawString("-- " + row.Label + " --", SecFont, Brushes.DimGray, lx, y);
                    y += 24;
                    break;

                case RowType.MusicVol:
                case RowType.SfxVol:
                {
                    if (sel) Highlight(g, W, y, 36);
                    int vol = row.Type == RowType.MusicVol
                        ? Game.Instance.Audio.MusicVolume
                        : Game.Instance.Audio.SfxVolume;
                    DrawSliderRow(g, row.Label, vol, lx, y, sel);
                    y += 36;
                    break;
                }

                case RowType.HowToPlayBtn:
                {
                    if (sel) Highlight(g, W, y, 34);
                    using (var f = new Font("Courier New", 11, sel ? FontStyle.Bold : FontStyle.Regular))
                    {
                        Brush br = sel ? Brushes.Yellow : Brushes.LightCyan;
                        g.DrawString("▶ " + row.Label, f, br, lx, y + 7);
                    }
                    y += 34;
                    break;
                }

                case RowType.MoodHeader:
                {
                    if (sel) Highlight(g, W, y, 34);
                    bool exp = _expandedMood == row.Mood;
                    string arrow = exp ? "v" : ">";
                    var pl = Game.Instance.Audio.GetPlaylist(row.Mood);
                    string tracks = pl.Count == 1 ? "1 track" : pl.Count + " tracks";
                    string line = arrow + "  " + row.Label + "  (" + tracks + ")";
                    Brush br = sel ? Brushes.Yellow : Brushes.White;
                    using (var f = new Font("Courier New", 10, sel ? FontStyle.Bold : FontStyle.Regular))
                        g.DrawString(line, f, br, lx, y + 6);
                    y += 34;
                    break;
                }

                case RowType.TrackItem:
                {
                    if (sel) Highlight(g, W, y, 26);
                    bool on      = Game.Instance.Audio.IsTrackInPlaylist(row.Mood, row.TrackFile);
                    bool playing = row.TrackFile == Game.Instance.Audio.CurrentTrack;
                    string icon  = playing ? "\u266B" : (on ? "\u2713" : " ");
                    Brush br     = sel     ? Brushes.Yellow
                                 : playing ? Brushes.Cyan
                                 : on      ? Brushes.LimeGreen
                                 :           (Brush)Brushes.Gray;
                    string display = FormatTrackName(row.TrackFile);
                    g.DrawString("     " + icon + "  " + display, SmFont, br, lx, y + 6);
                    if (playing)
                        g.DrawString("\u25B6", SmFont, Brushes.Cyan, lx + 12, y + 6);
                    y += 26;
                    break;
                }

                case RowType.ToolAction:
                {
                    if (sel) Highlight(g, W, y, 30);
                    using (var f = new Font("Courier New", 10, sel ? FontStyle.Bold : FontStyle.Regular))
                    {
                        Brush br = sel ? Brushes.Yellow : Brushes.LightCyan;
                        g.DrawString("[TOOL] " + row.Label, f, br, lx, y + 7);
                    }
                    y += 30;
                    break;
                }

                case RowType.BackBtn:
                {
                    y += 8;
                    if (sel) Highlight(g, W, y, 34);
                    using (var f = new Font("Courier New", 12, FontStyle.Bold))
                    {
                        string label = "[ " + row.Label + " ]";
                        SizeF sz = g.MeasureString(label, f);
                        // Use a green tint for resume/return buttons to make them stand out.
                        bool isReturn = row.Label.IndexOf("RESUME", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        row.Label.IndexOf("RETURN", StringComparison.OrdinalIgnoreCase) >= 0;
                        Brush br = sel ? Brushes.Yellow
                                  : isReturn ? Brushes.LimeGreen
                                  : Brushes.White;
                        g.DrawString(label, f, br, (W - sz.Width) / 2f, y + 4);
                    }
                    y += 34;
                    break;
                }
            }
        }

        private void Highlight(Graphics g, int W, float y, float h)
        {
            using (var br = new SolidBrush(Color.FromArgb(45, Color.Cyan)))
                g.FillRectangle(br, 0, y, W, h);
        }

        private void DrawSliderRow(Graphics g, string label, int value, float lx, float y, bool sel)
        {
            Brush lb = sel ? Brushes.Yellow : Brushes.White;
            using (var f = new Font("Courier New", 10, sel ? FontStyle.Bold : FontStyle.Regular))
                g.DrawString(label, f, lb, lx, y + 8);
            float sx = lx + 165, sy = y + 11, sw = 220, sh = 14;
            using (var br = new SolidBrush(Color.FromArgb(70, 70, 110)))
                g.FillRectangle(br, sx, sy, sw, sh);
            float fill = (value / 100f) * sw;
            Color fc = sel ? Color.Cyan : Color.SteelBlue;
            using (var br = new SolidBrush(fc))
                g.FillRectangle(br, sx, sy, fill, sh);
            using (var pen = new Pen(sel ? Color.Cyan : Color.DimGray))
                g.DrawRectangle(pen, sx, sy, sw, sh);
            g.DrawString(value + "%", SmFont, lb, sx + sw + 8, y + 8);
        }

        /// <summary>
        /// Opens the runtime log folder used by the error/visual debugger.
        /// </summary>
        private static void OpenLogsFolder()
        {
            // Logs may be in the project root or the executable directory
            string logDir = FindProjectPath("Logs");
            if (logDir == null)
            {
                // Fall back to creating a Logs folder next to the executable
                logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            }
            Directory.CreateDirectory(logDir);
            try
            {
                Process.Start("explorer.exe", logDir);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("Options.OpenLogsFolder", ex);
            }
        }

        private static void OpenDocumentationFolder()
        {
            string docsDir = FindProjectPath("docs");
            if (docsDir == null)
            {
                SMB3Hud.ShowToast("Docs folder not found.");
                return;
            }
            try
            {
                Process.Start("explorer.exe", docsDir);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("Options.OpenDocumentationFolder", ex);
            }
        }

        private static void OpenMasterDocumentationIndex() => OpenDocument("docs", "MASTER_DOCUMENTATION_INDEX.md");
        private static void OpenAiDocs() => OpenDocument("docs", "AI_DOCS.md");
        private static void OpenWeek10Log() => OpenDocument("docs", "WEEK_10_LOG_TEMPLATE.md");
        private static void OpenReadme() => OpenDocument("README.md");

        /// <summary>
        /// Opens a document relative to the project root directory.
        /// Searches upward from the executable directory to find the project
        /// root (where docs/ or README.md lives).
        /// </summary>
        private static void OpenDocument(params string[] relativeParts)
        {
            string relPath = Path.Combine(relativeParts);
            string fullPath = FindProjectPath(relPath);
            if (fullPath == null)
            {
                SMB3Hud.ShowToast("File not found: " + relPath);
                return;
            }
            try
            {
                Process.Start(fullPath);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("Options.OpenDocument", ex);
                SMB3Hud.ShowToast("Cannot open: " + relPath);
            }
        }

        /// <summary>
        /// Finds a file or directory relative to the project root by searching
        /// upward from the executable's base directory.  Returns null if not found.
        /// </summary>
        private static string FindProjectPath(string relativePath)
        {
            // Start from the executable directory and walk up to find project root
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            // Check up to 6 parent directories (covers bin\Debug\netX.Y etc.)
            for (int i = 0; i < 6 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, relativePath);
                if (File.Exists(candidate) || Directory.Exists(candidate))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        /// <summary>
        /// Emits a controlled test error for QA verification of debugger capture.
        /// </summary>
        private static void CaptureTestError()
        {
            try
            {
                throw new InvalidOperationException("OptionsScene test error (intentional). Verify log and screenshot output.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("Options.CaptureTestError", ex);
            }
        }

        private static string FormatTrackName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return fileName;
            string name = fileName.Replace("music_", "").Replace(".mp3", "");
            // Capitalize and insert spaces before digits for readability
            if (name.Length > 0)
                name = char.ToUpper(name[0]) + name.Substring(1);
            // e.g. "island1" → "Island 1", "grandlinefog1" → "Grandlinefog 1"
            for (int i = 1; i < name.Length; i++)
            {
                if (char.IsDigit(name[i]) && !char.IsDigit(name[i - 1]))
                {
                    name = name.Substring(0, i) + " " + name.Substring(i);
                    break;
                }
            }
            return name;
        }

        /// <summary>
        /// Manually saves all runtime progress to disk.
        /// Syncs bounty, threat, crew bonds, visited nodes, volume settings,
        /// and current level counter, then writes the save file.
        /// </summary>
        /// <remarks>PHASE 2 - Team 9: UI Programmer — Save Game option</remarks>
        private static void SaveGameManually()
        {
            try
            {
                Game.Instance.SyncRuntimeToSaveData();
                Game.Instance.Save.Save();
                SMB3Hud.ShowToast("Game saved!");
                System.Diagnostics.Debug.WriteLine("[OptionsScene] Manual save completed.");
            }
            catch (Exception ex)
            {
                SMB3Hud.ShowToast("Save failed!");
                System.Diagnostics.Debug.WriteLine($"[OptionsScene] Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// PHASE 2 - Team 9: UI Programmer
        /// Opens the Game Settings menu scene
        /// </summary>
        private void OpenSettings()
        {
            Game.Instance.Scenes.Push(new SettingsScene());
        }
    }
}
