using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Visual Debugger — collates in-memory error entries with their screenshots and
    /// generates an HTML report linking each error to the game frame it occurred in.
    ///
    /// Team 3  (Technical Lead)       — real-time error overlay, screenshot collation.
    /// Team 8  (Systems Programmer)   — screenshot capture pipeline, HTML export.
    /// Team 19 (QA Tester)            — availability log, session open/close stamps,
    ///                                  error count badge, QAReportScene data source.
    ///
    /// ── Integration ──────────────────────────────────────────────────────────
    /// 1. At startup in Form1 / Program.cs, set the screenshot provider:
    ///      VisualDebugger.ScreenshotProvider = () => canvas.CaptureFrame();
    ///
    /// 2. DebugLogger.LogError automatically calls VisualDebugger.RecordError.
    ///
    /// 3. In Game.OnRender (after scene draw) call:
    ///      VisualDebugger.DrawOverlay(g, W, H);
    ///
    /// 4. Toggle overlay with F10 (wired in InputManager or Form KeyDown).
    ///
    /// 5. At application exit call:
    ///      VisualDebugger.WriteSessionClose();
    /// </summary>
    public static class VisualDebugger
    {
        // ── Config ────────────────────────────────────────────────────────────
        /// <summary>Maximum entries retained in memory at once.</summary>
        private const int MaxEntries = 60;

        /// <summary>Number of entries shown in the in-game overlay panel.</summary>
        private const int OverlayRows = 6;

        // ── State ─────────────────────────────────────────────────────────────
        /// <summary>Toggles the in-game overlay panel (bind to F10).</summary>
        public static bool OverlayVisible { get; set; } = false;

        /// <summary>Optional screenshot provider wired by the WinForms host.</summary>
        public static Func<Bitmap> ScreenshotProvider { get; set; }

        // ── Counters (public for QAReportScene / badge display) ───────────────
        /// <summary>Total ERROR entries recorded this session.</summary>
        public static int ErrorCount { get; private set; }

        /// <summary>Total INFO entries recorded this session.</summary>
        public static int InfoCount { get; private set; }

        // ── Storage ───────────────────────────────────────────────────────────
        private static readonly object _sync = new object();
        private static readonly List<VisualEntry> _entries = new List<VisualEntry>(MaxEntries);

        // ── Paths ─────────────────────────────────────────────────────────────
        private static readonly string LogDir   = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string ShotDir  = Path.Combine(LogDir, "ErrorShots");
        private static readonly string HtmlPath = Path.Combine(LogDir, "visual-report.html");
        private static readonly string AvailLog = Path.Combine(LogDir, "qa-availability.log");

        // ── Session open ──────────────────────────────────────────────────────
        private static readonly DateTime _sessionStart = DateTime.Now;

        static VisualDebugger()
        {
            Directory.CreateDirectory(LogDir);
            Directory.CreateDirectory(ShotDir);
            AppendAvailLog($"[SESSION OPEN]  {_sessionStart:yyyy-MM-dd HH:mm:ss}  {BuildInfo.Summary}");
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records an ERROR entry with an automatic screenshot capture.
        /// Called by DebugLogger.WriteEntry for every LogError call.
        /// </summary>
        public static void RecordError(string context, string details, string existingScreenshotPath)
        {
            lock (_sync)
            {
                ErrorCount++;
                string shot = existingScreenshotPath ?? TryCaptureScreenshot(DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));

                var entry = CreateEntry("ERROR", context, details, shot);
                PushEntry(entry);

                AppendAvailLog(
                    $"  ERROR #{ErrorCount:D4}  {entry.Timestamp:HH:mm:ss.fff}" +
                    $"  [{context}]  shot={shot ?? "none"}");

                WriteHtmlReport();
            }
        }

        /// <summary>
        /// Records an INFO entry (no screenshot).
        /// Called by DebugLogger.WriteEntry for every LogInfo call.
        /// </summary>
        public static void RecordInfo(string context, string details)
        {
            lock (_sync)
            {
                InfoCount++;
                var entry = CreateEntry("INFO", context, details, null);
                PushEntry(entry);
            }
        }

        /// <summary>
        /// Toggles the in-game debug overlay (F10).
        /// </summary>
        public static void ToggleOverlay() => OverlayVisible = !OverlayVisible;

        /// <summary>
        /// Returns a snapshot of the most recent entries for QAReportScene.
        /// </summary>
        public static List<VisualEntry> GetSnapshot()
        {
            lock (_sync) { return new List<VisualEntry>(_entries); }
        }

        // ── In-game overlay draw ──────────────────────────────────────────────

        /// <summary>
        /// Draws the in-game visual debug overlay.
        /// Call from Game.OnRender after Scenes.Current.Draw(g).
        /// </summary>
        public static void DrawOverlay(Graphics g, int W, int H)
        {
            if (!OverlayVisible) return;

            lock (_sync)
            {
                // ── Panel background ──────────────────────────────────────────
                int pw = 430, ph = Math.Min(OverlayRows, _entries.Count) * 58 + 38;
                int px = W - pw - 6, py = 8;

                using (var br = new SolidBrush(Color.FromArgb(215, 8, 8, 18)))
                    g.FillRectangle(br, px, py, pw, ph);
                using (var pen = new Pen(Color.FromArgb(140, 255, 70, 70), 1))
                    g.DrawRectangle(pen, px, py, pw, ph);

                // ── Header ────────────────────────────────────────────────────
                using (var fH = new Font("Courier New", 8, FontStyle.Bold))
                {
                    g.DrawString(
                        $"[VISUAL DEBUGGER]  ERR:{ErrorCount}  INFO:{InfoCount}  [F10]",
                        fH, Brushes.OrangeRed, px + 6, py + 5);
                }

                // ── Entries (most recent = last in list → show in reverse) ────
                int iy = py + 24;
                int start = Math.Max(0, _entries.Count - OverlayRows);

                using (var fL  = new Font("Courier New", 7, FontStyle.Bold))
                using (var fD  = new Font("Courier New", 7))
                {
                    for (int i = start; i < _entries.Count; i++)
                    {
                        var e = _entries[i];
                        Color lc = e.Level == "ERROR" ? Color.OrangeRed : Color.LimeGreen;

                        g.DrawString($"[{e.Level}] {e.Timestamp:HH:mm:ss}", fL,
                                     new SolidBrush(lc), px + 6, iy);
                        g.DrawString(Truncate(e.Context, 24), fL,
                                     Brushes.Cyan, px + 130, iy);
                        g.DrawString(Truncate(e.Details, 65), fD,
                                     Brushes.LightGray, px + 6, iy + 14);

                        // Thumbnail if available (50×38 pixels)
                        if (!string.IsNullOrEmpty(e.ScreenshotPath) &&
                            File.Exists(e.ScreenshotPath))
                        {
                            try
                            {
                                using (var thumb = Image.FromFile(e.ScreenshotPath))
                                    g.DrawImage(thumb, px + pw - 56, iy, 50, 38);
                            }
                            catch { /* thumbnail load failure is non-fatal */ }
                        }

                        iy += 58;
                    }
                }
            }
        }

        // ── Session close ─────────────────────────────────────────────────────

        /// <summary>
        /// Records session close in the QA availability log.
        /// Call at application exit.
        /// </summary>
        public static void WriteSessionClose()
        {
            SessionStats.Instance.WriteSummaryToLog();
            var elapsed = DateTime.Now - _sessionStart;
            AppendAvailLog(
                $"[SESSION CLOSE] {DateTime.Now:yyyy-MM-dd HH:mm:ss}" +
                $"  Duration:{elapsed:hh\\:mm\\:ss}" +
                $"  Errors:{ErrorCount}  Info:{InfoCount}" +
                $"  Deaths:{SessionStats.Instance.DeathCount}");
            AppendAvailLog(new string('-', 80));
        }

        // ── HTML report ───────────────────────────────────────────────────────

        private static void WriteHtmlReport()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8'>");
                sb.AppendLine($"<title>Friday's Adventure — Visual Debug Report</title>");
                sb.AppendLine("<style>");
                sb.AppendLine("body{background:#08080f;color:#d0d0e0;font-family:monospace;font-size:13px;margin:20px;}");
                sb.AppendLine("h1{color:#ff5030;letter-spacing:2px;}");
                sb.AppendLine(".entry{border:1px solid #333;margin:10px 0;padding:10px 12px;background:#10101c;overflow:hidden;}");
                sb.AppendLine(".error{border-left:4px solid #ff4020;}.info{border-left:4px solid #204090;}");
                sb.AppendLine("img{float:right;max-width:280px;max-height:210px;border:1px solid #444;margin-left:12px;}");
                sb.AppendLine(".lvl-error{color:#ff6040;font-weight:bold;}.lvl-info{color:#40a0ff;}");
                sb.AppendLine(".meta{color:#666;font-size:11px;margin-top:4px;}.details{color:#90d0ff;white-space:pre-wrap;margin-top:6px;}");
                sb.AppendLine("</style></head><body>");
                sb.AppendLine($"<h1>Friday's Adventure — Visual Debug Report</h1>");
                sb.AppendLine($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} &nbsp;|&nbsp; {HtmlEncode(BuildInfo.Summary)}</p>");
                sb.AppendLine($"<p>Errors: <b style='color:#ff5030'>{ErrorCount}</b> &nbsp; Info: <b style='color:#4080ff'>{InfoCount}</b></p>");

                for (int i = _entries.Count - 1; i >= 0; i--)
                {
                    var e  = _entries[i];
                    string css = e.Level == "ERROR" ? "error" : "info";
                    string lcs = e.Level == "ERROR" ? "lvl-error" : "lvl-info";
                    sb.AppendLine($"<div class='entry {css}'>");
                    if (!string.IsNullOrEmpty(e.ScreenshotPath) && File.Exists(e.ScreenshotPath))
                        sb.AppendLine($"  <img src='{HtmlEncode(e.ScreenshotPath)}' alt='screenshot'/>");
                    sb.AppendLine($"  <span class='{lcs}'>[{e.Level}]</span> <b>{HtmlEncode(e.Context)}</b>");
                    sb.AppendLine($"  <div class='meta'>{e.Timestamp:yyyy-MM-dd HH:mm:ss.fff} &nbsp;|&nbsp; ID: {e.Id}</div>");
                    sb.AppendLine($"  <div class='details'>{HtmlEncode(e.Details)}</div>");
                    sb.AppendLine("</div>");
                }
                sb.AppendLine("</body></html>");
                File.WriteAllText(HtmlPath, sb.ToString(), Encoding.UTF8);
            }
            catch { /* Report write must never throw */ }
        }

        // ── Screenshot capture ────────────────────────────────────────────────

        private static string TryCaptureScreenshot(string id)
        {
            try
            {
                if (ScreenshotProvider == null) return null;
                using (Bitmap bmp = ScreenshotProvider())
                {
                    if (bmp == null) return null;
                    string path = Path.Combine(ShotDir, "error_" + id + ".png");
                    bmp.Save(path, ImageFormat.Png);
                    return path;
                }
            }
            catch { return null; }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static VisualEntry CreateEntry(string level, string context, string details, string shot)
        {
            return new VisualEntry
            {
                Id             = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"),
                Timestamp      = DateTime.Now,
                Level          = level,
                Context        = context ?? "(null context)",
                Details        = Truncate(details ?? "(null details)", 400),
                ScreenshotPath = shot
            };
        }

        private static void PushEntry(VisualEntry entry)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries) _entries.RemoveAt(0);
        }

        private static void AppendAvailLog(string line)
        {
            try { File.AppendAllText(AvailLog, line + Environment.NewLine); }
            catch { }
        }

        private static string Truncate(string s, int max)
        {
            if (s == null) return "(null)";
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        /// <summary>Minimal HTML encoder — avoids System.Web dependency.</summary>
        private static string HtmlEncode(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&#39;");
        }

        // ── Entry model (public for QAReportScene) ────────────────────────────
        /// <summary>One recorded debug entry: level, context, details, screenshot path.</summary>
        public sealed class VisualEntry
        {
            public string   Id             { get; set; }
            public DateTime Timestamp      { get; set; }
            public string   Level          { get; set; }
            public string   Context        { get; set; }
            public string   Details        { get; set; }
            public string   ScreenshotPath { get; set; }
        }
    }
}
