using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// QA Report Scene — displays the Visual Debugger availability log and
    /// in-memory error entries for QA Testers to review between play sessions.
    ///
    /// Team 19 (QA Tester)    — primary consumer of this scene.
    /// Team 3  (Tech Lead)    — wires VisualDebugger data source.
    /// Team 2  (Producer)     — SessionStats summary displayed here.
    ///
    /// ── Access ───────────────────────────────────────────────────────────────
    /// Navigate here from DevMenuScene or TitleScene (when GodMode is active):
    ///   Game.Instance.Scenes.Push(new QAReportScene());
    /// </summary>
    public sealed class QAReportScene : Scene
    {
        // ── Fonts ─────────────────────────────────────────────────────────────
        private Font _titleFont;
        private Font _headFont;
        private Font _bodyFont;
        private Font _smallFont;

        // ── Data ──────────────────────────────────────────────────────────────
        private List<VisualDebugger.VisualEntry> _entries;
        private string _availLogTail;      // last N lines of the availability log
        private int    _scrollOffset;      // current scroll row
        private const int RowH = 52;

        // ── Availability log path ─────────────────────────────────────────────
        private static readonly string AvailLog =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "qa-availability.log");

        // ── Buttons ───────────────────────────────────────────────────────────
        private Rectangle _btnBack;
        private Rectangle _btnExport;

        // ── OnEnter ───────────────────────────────────────────────────────────
        public override void OnEnter()
        {
            _titleFont = new Font("Courier New", 18, FontStyle.Bold);
            _headFont  = new Font("Courier New", 10, FontStyle.Bold);
            _bodyFont  = new Font("Courier New", 9);
            _smallFont = new Font("Courier New", 8);

            _entries  = VisualDebugger.GetSnapshot();
            _entries.Reverse();  // most recent first

            _availLogTail = ReadLogTail(AvailLog, 12);
            _scrollOffset = 0;
        }

        public override void OnExit()
        {
            _titleFont?.Dispose();
            _headFont?.Dispose();
            _bodyFont?.Dispose();
            _smallFont?.Dispose();
        }

        // ── Update ────────────────────────────────────────────────────────────
        public override void Update(float dt) { }

        // ── Draw ──────────────────────────────────────────────────────────────
        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Background ────────────────────────────────────────────────────
            g.FillRectangle(Brushes.Black, 0, 0, W, H);
            using (var br = new SolidBrush(Color.FromArgb(28, 8, 40)))
                g.FillRectangle(br, 0, 0, W, H);

            // Header panel.
            using (var br = new SolidBrush(Color.FromArgb(40, 10, 60)))
                g.FillRectangle(br, 0, 0, W, 56);
            using (var pen = new Pen(Color.FromArgb(120, 160, 80, 220), 2))
                g.DrawLine(pen, 0, 55, W, 55);

            // Title.
            g.DrawString("QA REPORT — VISUAL DEBUGGER", _titleFont, Brushes.MediumOrchid, 12, 10);
            g.DrawString(BuildInfo.Summary, _smallFont, Brushes.Gray, W / 2f, 38);

            // ── Summary badges ────────────────────────────────────────────────
            int bx = W - 340;
            DrawBadge(g, bx,       16, $"ERR: {VisualDebugger.ErrorCount}",  Color.OrangeRed);
            DrawBadge(g, bx + 100, 16, $"INF: {VisualDebugger.InfoCount}",   Color.SteelBlue);
            DrawBadge(g, bx + 200, 16, $"ACH: {AchievementSystem.EarnedCount()}/{AchievementSystem.All.Count}", Color.Gold);

            // ── Session stats panel (right column) ────────────────────────────
            int statsX = W - 220;
            DrawPanel(g, statsX, 64, 210, 180);
            g.DrawString("SESSION STATS", _headFont, Brushes.MediumOrchid, statsX + 8, 68);
            g.DrawString(SessionStats.Instance.ToDisplayString(), _bodyFont, Brushes.LightGray, statsX + 8, 86);

            // ── Availability log tail ─────────────────────────────────────────
            DrawPanel(g, statsX, 256, 210, 120);
            g.DrawString("AVAILABILITY LOG", _headFont, Brushes.MediumOrchid, statsX + 8, 260);
            g.DrawString(_availLogTail, _smallFont, Brushes.Gray, statsX + 8, 276);

            // ── Error entry list (left/center) ────────────────────────────────
            int listX = 8, listY = 64, listW = statsX - 20;
            int visibleRows = (H - 120) / RowH;

            // Clip entries area.
            g.SetClip(new Rectangle(listX, listY, listW, H - 120));

            for (int i = 0; i < _entries.Count && i < visibleRows + _scrollOffset; i++)
            {
                int ri = i - _scrollOffset;
                if (ri < 0) continue;

                var e  = _entries[i];
                int ey = listY + ri * RowH;
                if (ey + RowH > H - 60) break;

                bool isError = e.Level == "ERROR";

                // Entry background.
                using (var br = new SolidBrush(isError
                    ? Color.FromArgb(40, 80, 20, 10)
                    : Color.FromArgb(25, 10, 20, 50)))
                    g.FillRectangle(br, listX, ey, listW, RowH - 2);

                using (var pen = new Pen(isError ? Color.FromArgb(100, Color.OrangeRed) : Color.FromArgb(60, Color.SteelBlue)))
                    g.DrawLine(pen, listX, ey, listX + listW, ey);

                // Level badge.
                Color lc = isError ? Color.OrangeRed : Color.SteelBlue;
                using (var br = new SolidBrush(lc))
                    g.FillRectangle(br, listX, ey + 2, 44, 18);
                g.DrawString(e.Level, _smallFont, Brushes.White, listX + 2, ey + 4);

                // Timestamp + context.
                g.DrawString($"{e.Timestamp:HH:mm:ss.fff}  {e.Context}", _headFont, new SolidBrush(lc), listX + 50, ey + 4);

                // Details text.
                string detailSnip = e.Details?.Length > 120 ? e.Details.Substring(0, 120) + "..." : e.Details;
                g.DrawString(detailSnip, _smallFont, Brushes.LightGray, listX + 4, ey + 24);

                // Screenshot thumbnail.
                if (!string.IsNullOrEmpty(e.ScreenshotPath) && File.Exists(e.ScreenshotPath))
                {
                    try
                    {
                        using (var thumb = Image.FromFile(e.ScreenshotPath))
                            g.DrawImage(thumb, listW - 60, ey + 4, 56, 44);
                    }
                    catch { }
                }
            }

            g.ResetClip();

            // ── Empty state ───────────────────────────────────────────────────
            if (_entries.Count == 0)
            {
                g.DrawString("No errors recorded this session. All systems nominal.",
                             _headFont, Brushes.LimeGreen, listX + 20, listY + 40);
            }

            // ── Scroll hint ───────────────────────────────────────────────────
            if (_entries.Count > visibleRows)
            {
                g.DrawString($"▲▼ Scroll  ({_scrollOffset + 1}–{Math.Min(_scrollOffset + visibleRows, _entries.Count)}/{_entries.Count})",
                             _smallFont, Brushes.Gray, listX, H - 52);
            }

            // ── Buttons ───────────────────────────────────────────────────────
            _btnBack   = new Rectangle(8,      H - 44, 100, 34);
            _btnExport = new Rectangle(116,    H - 44, 160, 34);

            DrawButton(g, _btnBack,   "◀ BACK",           Color.FromArgb(60, 30, 90));
            DrawButton(g, _btnExport, "OPEN HTML REPORT",  Color.FromArgb(30, 60, 90));

            DrawDevMenuButton(g);
        }

        // ── HandleClick ───────────────────────────────────────────────────────
        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;

            if (_btnBack.Contains(p))
            {
                Game.Instance.Scenes.Pop();
                return;
            }

            if (_btnExport.Contains(p))
            {
                // Open the HTML report in the default browser.
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "visual-report.html");
                if (File.Exists(path))
                {
                    try { System.Diagnostics.Process.Start(path); }
                    catch (Exception ex) { DebugLogger.LogError("QAReport.OpenHtml", ex); }
                }
                return;
            }
        }

        // ── HandleMouseWheel ──────────────────────────────────────────────────
        public override void HandleMouseWheel(int delta)
        {
            int rows = _entries.Count;
            int max  = Math.Max(0, rows - (Game.Instance.CanvasHeight - 120) / RowH);
            _scrollOffset = Math.Max(0, Math.Min(max, _scrollOffset + (delta > 0 ? -1 : 1)));
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static void DrawBadge(Graphics g, int x, int y, string text, Color color)
        {
            using (var br = new SolidBrush(Color.FromArgb(60, color)))
                g.FillRectangle(br, x, y, 90, 22);
            using (var pen = new Pen(color, 1))
                g.DrawRectangle(pen, x, y, 90, 22);
            using (var f = new Font("Courier New", 9, FontStyle.Bold))
                g.DrawString(text, f, new SolidBrush(color), x + 4, y + 3);
        }

        private static void DrawPanel(Graphics g, int x, int y, int w, int h)
        {
            using (var br = new SolidBrush(Color.FromArgb(28, 28, 45)))
                g.FillRectangle(br, x, y, w, h);
            using (var pen = new Pen(Color.FromArgb(70, 80, 120)))
                g.DrawRectangle(pen, x, y, w, h);
        }

        private static void DrawButton(Graphics g, Rectangle r, string text, Color bg)
        {
            using (var br = new SolidBrush(bg))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.FromArgb(160, Color.White), 1))
                g.DrawRectangle(pen, r);
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            {
                var sz = g.MeasureString(text, f);
                g.DrawString(text, f, Brushes.White,
                    r.X + (r.Width  - sz.Width)  / 2f,
                    r.Y + (r.Height - sz.Height) / 2f);
            }
        }

        private static string ReadLogTail(string path, int lines)
        {
            if (!File.Exists(path)) return "(no log file yet)";
            try
            {
                var all = File.ReadAllLines(path);
                int start = Math.Max(0, all.Length - lines);
                return string.Join(Environment.NewLine, all, start, all.Length - start);
            }
            catch { return "(read error)"; }
        }
    }
}
