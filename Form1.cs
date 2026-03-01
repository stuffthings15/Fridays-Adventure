using System;
using System.Drawing;
using System.Windows.Forms;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Forms;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure
{
    public partial class Form1 : Form
    {
        private Game       _game;
        private GameCanvas _canvas;

        public Form1()
        {
            InitializeComponent();

            // ── Fullscreen ────────────────────────────────────────────────────
            FormBorderStyle = FormBorderStyle.None;
            WindowState     = FormWindowState.Maximized;

            _canvas = new GameCanvas { Dock = DockStyle.Fill };
            Controls.Add(_canvas);

            _game = new Game(_canvas);

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Alt && e.KeyCode == Keys.F4) { Close(); return; }
                _game.Input.OnKeyDown(e.KeyCode);
                e.Handled = true;
            };
            KeyUp    += (s, e) => _game.Input.OnKeyUp(e.KeyCode);
            KeyPress += (s, e) => _game.Input.OnKeyChar(e.KeyChar);

            _canvas.MouseClick += (s, e) => _game.Scenes.Current?.HandleClick(e.Location);

            FormClosed += (s, e) => _game.Stop();

            Game.OpenLogbookRequested += () =>
            {
                var lb = new LogbookForm();
                lb.Show(this);
            };

            Game.CloseRequested += () => Close();

            // Start AFTER the window is fully shown so the message pump and
            // audio device are ready — prevents the title-screen music skip.
            Shown += (s, e) => _game.Start();
        }
    }
}
