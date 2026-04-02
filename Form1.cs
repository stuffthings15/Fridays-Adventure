using System;
using System.Drawing;
using System.Windows.Forms;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Forms;
using Fridays_Adventure.Scenes;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure
{
    public partial class Form1 : Form
    {
        private Game       _game;
        private GameCanvas _canvas;

        public Form1()
        {
            InitializeComponent();

            // ── Application icon ─────────────────────────────────────────────
            string icoPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "pirate_ship.ico");
            if (System.IO.File.Exists(icoPath))
                Icon = new Icon(icoPath);

            // ── Fullscreen ────────────────────────────────────────────────────
            FormBorderStyle = FormBorderStyle.None;
            WindowState     = FormWindowState.Maximized;

            _canvas = new GameCanvas { Dock = DockStyle.Fill };
            Controls.Add(_canvas);

            _game = new Game(_canvas);

            // Visual debugger hook: capture the current frame when an error is logged.
            DebugLogger.ScreenshotProvider = () =>
            {
                var bmp = new Bitmap(_canvas.Width, _canvas.Height);
                _canvas.DrawToBitmap(bmp, new Rectangle(0, 0, _canvas.Width, _canvas.Height));
                return bmp;
            };

            // Keep VisualDebugger provider in sync with DebugLogger provider.
            // This allows RecordError fallback capture even if no screenshot path
            // is passed from the logger pipeline.
            VisualDebugger.ScreenshotProvider = DebugLogger.ScreenshotProvider;

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Alt && e.KeyCode == Keys.F4) { Close(); return; }

                // F10 toggles the in-game Visual Debugger overlay.
                // Team 3 (Technical Lead) / Team 19 (QA Tester) — overlay toggle.
                if (e.KeyCode == Keys.F10)
                {
                    VisualDebugger.ToggleOverlay();
                    e.Handled = true;
                    return;
                }

                _game.Input.OnKeyDown(e.KeyCode);
                e.Handled = true;
            };
            KeyUp    += (s, e) => _game.Input.OnKeyUp(e.KeyCode);
            KeyPress += (s, e) => _game.Input.OnKeyChar(e.KeyChar);

            _canvas.MouseClick += (s, e) => _game.Scenes.Current?.HandleClick(e.Location);
            _canvas.MouseWheel += (s, e) => _game.Scenes.Current?.HandleMouseWheel(e.Delta);

            FormClosed += (s, e) => _game.Stop();

            Game.OpenLogbookRequested += () =>
            {
                var lb = new LogbookForm();
                lb.Show(this);
            };

            Game.CloseRequested += () => Close();

            // Run startup validation (asset checks, log rotation, platform report)
            // before the game loop begins so errors are captured from the first frame.
            // Team 11 (Build Engineer) — StartupValidator.
            StartupValidator.Run();

            // Team 11 Wave 2 — Build self-test
            BuildEngineerFeatures.RunStartupSelfTest();

            // Team 19 Wave 2 — QA regression checklist
            QAFeatures.RunRegressionChecklist();

            // Team 2 Wave 2 — Daily streak tracking
            ProducerFeatures.UpdateDailyStreak();

            // Start AFTER the window is fully shown so the message pump and
            // audio device are ready — prevents the title-screen music skip.
            Shown += (s, e) => _game.Start();
        }
    }
}
