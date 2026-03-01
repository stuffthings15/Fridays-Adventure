using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    public sealed class OptionsScene : Scene
    {
        private enum RowType { Header, MusicVol, SfxVol, MoodHeader, TrackItem, BackBtn }

        private struct Row
        {
            public RowType Type;
            public string  Label;
            public string  Mood;
            public string  TrackFile;
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
            _rows.Add(new Row { Type = RowType.Header,   Label = "PLAYLISTS" });
            foreach (string mood in new[] { "overworld", "combat", "island", "boss" })
            {
                _rows.Add(new Row { Type = RowType.MoodHeader, Label = mood.ToUpper(), Mood = mood });
                if (_expandedMood == mood)
                    foreach (string t in ScanTracks(mood))
                        _rows.Add(new Row { Type = RowType.TrackItem, Mood = mood, TrackFile = t, Label = t });
            }
            _rows.Add(new Row { Type = RowType.BackBtn, Label = "Back" });
            _sel = Math.Max(0, Math.Min(_sel, _rows.Count - 1));
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
                    if (Game.Instance.Audio.IsTrackInPlaylist(row.Mood, row.TrackFile))
                        Game.Instance.Audio.RemoveTrack(row.Mood, row.TrackFile);
                    else
                        Game.Instance.Audio.AddTrack(row.Mood, row.TrackFile);
                    _dirty = true;
                    Rebuild();
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
                case RowType.MoodHeader: return 34f;
                case RowType.TrackItem:  return 26f;
                case RowType.BackBtn:    return 42f;
                default:                 return 24f;
            }
        }

        public override void HandleClick(Point p)
        {
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
            float y = 80f;
            for (int i = 0; i < _rows.Count; i++)
                DrawRow(g, _rows[i], i == _sel, W, ref y);
            g.DrawString("Up/Down Navigate   Left/Right Adjust   Enter Select   Esc Back",
                         SmFont, Brushes.DimGray, 12, H - 18);
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
                    bool on = Game.Instance.Audio.IsTrackInPlaylist(row.Mood, row.TrackFile);
                    string check = on ? "[v]" : "[ ]";
                    Brush br = sel ? Brushes.Yellow : (on ? Brushes.LimeGreen : (Brush)Brushes.Gray);
                    g.DrawString("     " + check + "  " + row.TrackFile, SmFont, br, lx, y + 6);
                    y += 26;
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
    }
}
