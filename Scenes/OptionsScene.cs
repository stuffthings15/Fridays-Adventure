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

            _rows.Add(new Row { Type = RowType.BackBtn, Label = "Back" });
            _sel = Math.Max(0, Math.Min(_sel, _rows.Count - 1));
        }

        private static void ToggleCrtFilter()
        {
            Game.Instance.CrtFilterEnabled = !Game.Instance.CrtFilterEnabled;
            SMB3Hud.ShowToast(Game.Instance.CrtFilterEnabled ? "CRT Filter: ON" : "CRT Filter: OFF");
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
        private static void ToggleOutlineMode()
        {
            Game.Instance.OutlineModeEnabled = !Game.Instance.OutlineModeEnabled;
            SMB3Hud.ShowToast(Game.Instance.OutlineModeEnabled ? "Outline Mode: ON" : "Outline Mode: OFF");
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

            if (input.PausePressed) Game.Instance.Scenes.Pop();
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
            g.DrawString("Up/Down Navigate   Left/Right Volume   Enter Select   Esc Back",
                         SmFont, Brushes.DimGray, 12, H - 36);
            g.DrawString("SMB3-style tips: tap jump for short hops, stomp enemies from above, and use momentum.",
                         SmFont, Brushes.DimGray, 12, H - 18);
            DrawDevMenuButton(g);
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
                        SizeF sz = g.MeasureString("[ Back ]", f);
                        Brush br = sel ? Brushes.Yellow : Brushes.White;
                        g.DrawString("[ Back ]", f, br, (W - sz.Width) / 2f, y + 4);
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
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
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
        /// PHASE 2 - Team 9: UI Programmer
        /// Opens the Game Settings menu scene
        /// </summary>
        private void OpenSettings()
        {
            Game.Instance.Scenes.Push(new SettingsScene());
        }
    }
}
