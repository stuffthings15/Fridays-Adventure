using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;
using System.IO;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Shown immediately at startup. Handles two parallel tasks:
    /// 1) Audio system prewarm (background MCI track opening).
    /// 2) Automatic CC0 asset download on first launch (reads asset_manifest.json,
    ///    downloads enabled packs, extracts ZIPs, copies to Assets/third_party/).
    /// Once both tasks complete (or timeout), replaces itself with TitleScene.
    /// <remarks>PHASE 2 - Team 8: Automatic asset download integrated here.</remarks>
    /// </summary>
    public sealed class LoadingScene : Scene
    {
        private const float MinDisplay = 1.5f;   // always show for at least this long
        private const float Timeout    = 6.0f;   // hard fallback for audio — proceed regardless
        private const float AssetTimeout = 120f;  // hard fallback for asset download (2 min max)

        private float       _elapsed;
        private bool        _transitioned;
        private float       _dotTimer;
        private int         _dotCount;
        private Bitmap      _bg;
        private string      _tip;

        // ── Asset download state (thread-safe via volatile) ──────────
        private volatile bool   _assetsNeeded;
        private volatile bool   _assetsComplete;
        private bool            _assetsCancelFlag;  // non-volatile: passed by ref to downloader
        private volatile string _assetPackName = "";
        private volatile string _assetStatus   = "";
        private volatile int    _assetPackIndex;
        private volatile int    _assetPackTotal;
        private Thread          _downloadThread;

        public override void OnEnter()
        {
            // Load a generic background — character art should not be used as scene backgrounds.
            string spritesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites");
            string[] candidates = new[]
            {
                Path.Combine(spritesDir, "bg_title.png"),
                Path.Combine(spritesDir, "deck.jpg"),
            };
            foreach (string c in candidates)
                if (File.Exists(c)) { _bg = new Bitmap(c); break; }

            // Team 2 (Producer) — Idea 10: rotating tip cycling on loading screen.
            _tip = TipCycler.NextTip();

            // Start lyrical theme music immediately so it plays throughout loading and title.
            Game.Instance.Audio.PlayTheme();

            // ── Check if asset download is needed ────────────────────
            // Only downloads on first launch; marker file prevents re-download.
            _assetsNeeded = !AssetDownloader.AssetsAlreadyDownloaded();
            if (_assetsNeeded)
            {
                _assetsCancelFlag = false;
                _assetsComplete = false;
                _assetStatus = "Checking assets...";

                // Run download on a background thread so the UI stays responsive.
                _downloadThread = new Thread(DownloadAssetsWorker)
                {
                    IsBackground = true,
                    Name = "AssetDownloadThread"
                };
                _downloadThread.Start();
            }
            else
            {
                // Assets already present — nothing to do
                _assetsComplete = true;
            }
        }

        public override void OnExit()
        {
            // Signal cancellation if download is still running
            _assetsCancelFlag = true;
            _bg?.Dispose();
            _bg = null;
        }

        public override void Update(float dt)
        {
            if (_transitioned) return;

            _elapsed  += dt;
            _dotTimer += dt;
            if (_dotTimer >= 0.4f) { _dotCount = (_dotCount + 1) % 4; _dotTimer = 0f; }

            bool audioReady = Game.Instance.Audio.AudioSystemReady;

            // Wait for audio to be ready (or timeout at 6s)
            bool audioOk = audioReady || _elapsed >= Timeout;

            // Wait for asset download to finish (or timeout at 120s)
            bool assetsOk = _assetsComplete || _elapsed >= AssetTimeout;

            // Wait for minimum display time
            bool minTimeOk = _elapsed >= MinDisplay;

            if (minTimeOk && audioOk && assetsOk)
            {
                _transitioned = true;

                // ── Run the self-healing asset pipeline ──────────────
                // Scans for missing sprites/audio, resolves from vendors
                // or generates placeholders, then invalidates the cache.
                if (_assetsNeeded)
                    SpriteManager.InvalidateCache();

                // Run healing pipeline (fast: no network, just local scans)
                AssetHealingPipeline.RunFullPipeline();

                // ── Heal-assets mode: pipeline done, auto-exit ───────
                // The orchestrator script reads the report and decides
                // whether to relaunch for another cycle.
                if (Game.HealAssetsMode)
                {
                    System.Windows.Forms.Application.Exit();
                    return;
                }

                // Unattended QA: skip title screen and go straight to bot walkthrough
                if (Game.AutoQABot)
                {
                    DialogueScene.AutoAdvance = true;
                    ToadHouseScene.AutoAdvance = true;
                    Game.Instance.Scenes.ReplaceAll(new QABotWalkthroughScene());
                }
                else
                {
                    Game.Instance.Scenes.ReplaceAll(new TitleScene());
                }
            }
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Miss Friday background if available, otherwise fallback gradient.
            if (_bg != null)
            {
                g.DrawImage(_bg, 0, 0, W, H);
                using (var dim = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                    g.FillRectangle(dim, 0, 0, W, H);
            }
            else
            {
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                    Color.FromArgb(5, 12, 40), Color.FromArgb(15, 35, 90), 90f))
                    g.FillRectangle(br, 0, 0, W, H);
            }

            // Title
            float tagY;
            using (var f = new Font("Courier New", 40, FontStyle.Bold))
            {
                const string title = "Miss Friday's Adventure Part II";
                SizeF sz = g.MeasureString(title, f);
                float titleY = H * 0.28f;
                g.DrawString(title, f, Brushes.White, (W - sz.Width) / 2f, titleY);
                tagY = titleY + sz.Height + 4f;
            }

            // Tagline
            using (var f = new Font("Courier New", 13, FontStyle.Bold))
            {
                const string tag = "Ice-Ice Fruit  \u2022  The Sea Serpent  \u2022  The Grand Line";
                SizeF sz = g.MeasureString(tag, f);
                g.DrawString(tag, f, Brushes.DarkSlateGray, (W - sz.Width) / 2f, tagY);
            }

            // ── Progress bar ─────────────────────────────────────────
            bool  audioReady = Game.Instance.Audio.AudioSystemReady;
            float progress   = Math.Min(1f, _elapsed / MinDisplay);
            if (!audioReady && !_assetsComplete)
                progress = Math.Min(progress, 0.50f);
            else if (!audioReady)
                progress = Math.Min(progress, 0.88f);
            else if (!_assetsComplete)
                progress = Math.Min(progress, 0.90f);

            const int BarW = 440;
            const int BarH = 10;
            int barX = (W - BarW) / 2;
            int barY = (int)(H * 0.65f);

            // Track
            using (var br = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                g.FillRectangle(br, barX, barY, BarW, BarH);

            // Fill
            if (progress > 0f)
            {
                int fillW = Math.Max(1, (int)(BarW * progress));
                using (var br = new LinearGradientBrush(
                    new Rectangle(barX, barY, fillW, BarH),
                    Color.FromArgb(80, 140, 255), Color.FromArgb(140, 200, 255), 0f))
                    g.FillRectangle(br, barX, barY, fillW, BarH);
            }

            // Border
            using (var pen = new Pen(Color.FromArgb(120, 255, 255, 255)))
                g.DrawRectangle(pen, barX, barY, BarW, BarH);

            // Status text with animated dots
            string dots   = new string('.', _dotCount);
            string status;

            // Show asset download progress when downloading
            if (_assetsNeeded && !_assetsComplete)
            {
                string packLabel = _assetPackName ?? "";
                string packStatus = _assetStatus ?? "";
                int idx = _assetPackIndex;
                int tot = _assetPackTotal;
                if (tot > 0)
                    status = $"Downloading assets ({idx + 1}/{tot}): {packLabel} — {packStatus}{dots}";
                else
                    status = "Preparing asset download" + dots;
            }
            else if (audioReady)
            {
                status = "Ready!";
            }
            else
            {
                status = "Initializing audio" + dots;
            }

            using (var f  = new Font("Courier New", 11))
            using (var br = new SolidBrush(Color.FromArgb(130, 200, 200, 200)))
            {
                SizeF sz = g.MeasureString(status, f);
                g.DrawString(status, f, br, (W - sz.Width) / 2f, barY + 18);
            }

            // ── Asset download detail line (pack-level progress) ─────
            if (_assetsNeeded && !_assetsComplete && _assetPackTotal > 0)
            {
                // Draw a second smaller progress bar for pack-level progress
                float packProgress = (float)_assetPackIndex / Math.Max(1, _assetPackTotal);
                int packBarW = 300;
                int packBarH = 6;
                int packBarX = (W - packBarW) / 2;
                int packBarY = barY + 42;

                using (var bg = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                    g.FillRectangle(bg, packBarX, packBarY, packBarW, packBarH);
                if (packProgress > 0f)
                {
                    int pw = Math.Max(1, (int)(packBarW * packProgress));
                    using (var fill = new SolidBrush(Color.FromArgb(180, 100, 220, 140)))
                        g.FillRectangle(fill, packBarX, packBarY, pw, packBarH);
                }
                using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255)))
                    g.DrawRectangle(pen, packBarX, packBarY, packBarW, packBarH);

                // Label below pack progress bar
                using (var f = new Font("Courier New", 8))
                using (var br = new SolidBrush(Color.FromArgb(100, 180, 180, 180)))
                {
                    string label = "First-launch setup — downloading free CC0 asset packs";
                    SizeF sz = g.MeasureString(label, f);
                    g.DrawString(label, f, br, (W - sz.Width) / 2f, packBarY + 10);
                }
            }

            // Producer tip line.
            if (!string.IsNullOrEmpty(_tip))
            {
                using (var f = new Font("Courier New", 9, FontStyle.Bold))
                using (var br = new SolidBrush(Color.FromArgb(200, 220, 220, 180)))
                {
                    SizeF sz = g.MeasureString(_tip, f);
                    g.DrawString(_tip, f, br, (W - sz.Width) / 2f, H - 34);
                }
            }
        }

        // ── Background thread: asset download worker ─────────────────

        /// <summary>
        /// Runs on a background thread. Downloads all enabled asset packs
        /// from the manifest and copies them to the runtime Assets directory.
        /// </summary>
        private void DownloadAssetsWorker()
        {
            try
            {
                AssetDownloader.DownloadAllAssets(
                    onProgress: (idx, total, name, stat) =>
                    {
                        // Update volatile fields for the UI thread to read
                        _assetPackIndex = idx;
                        _assetPackTotal = total;
                        _assetPackName  = name ?? "";
                        _assetStatus    = stat ?? "";
                    },
                    cancel: ref _assetsCancelFlag);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[LoadingScene] Asset download failed: {ex.Message}");
            }
            finally
            {
                _assetsComplete = true;
            }
        }
    }
}
