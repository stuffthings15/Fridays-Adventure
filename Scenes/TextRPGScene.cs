// ────────────────────────────────────────────
// PHASE 2 - Team 9: UI Programmer
// Feature: Embedded Text RPG
// Purpose: Hosts the Text RPG mini-game inside the main
//          game window instead of opening a separate form.
//          A TextRPGHostPanel overlays WinForms controls on
//          top of the game canvas and is removed when the
//          scene is popped.
// ────────────────────────────────────────────
using System;
using System.Drawing;
using System.Windows.Forms;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Embeds the Text RPG mini-game directly inside the main game
    /// window. A Panel-based host sits on top of the canvas and is
    /// removed when the scene is popped.
    /// </summary>
    /// <remarks>PHASE 2 - Team 9: Embedded Text RPG (no separate window)</remarks>
    public sealed class TextRPGScene : Scene
    {
        private TextRPG.TextRPGHostPanel _host;
        private readonly bool _demoMode;

        /// <summary>
        /// Creates a new TextRPGScene.
        /// </summary>
        /// <param name="demoMode">
        /// When true the RPG opens directly in Video Demo mode
        /// instead of showing the title screen.
        /// </param>
        public TextRPGScene(bool demoMode = false)
        {
            _demoMode = demoMode;
        }

        public override void OnEnter()
        {
            // Flag tells Form1 to stop intercepting keyboard input
            // so the WinForms buttons / text fields work correctly.
            Game.Instance.TextRPGActive = true;

            // Create the Panel-based host for the TextRPG screens.
            _host = new TextRPG.TextRPGHostPanel();

            // When the Quit button inside the RPG fires Close(),
            // the host raises CloseRequested — pop this scene.
            _host.CloseRequested += OnHostCloseRequested;

            // Show the appropriate starting screen.
            if (_demoMode)
                _host.ShowScreen(new TextRPG.Screens.DemoScreen(_host));
            else
                _host.ShowScreen(new TextRPG.Screens.TitleScreen(_host));

            // Add the host panel on top of the game canvas.
            Form parentForm = Game.Instance.Canvas.FindForm();
            if (parentForm != null)
            {
                parentForm.Controls.Add(_host);
                _host.BringToFront();
                _host.Focus();
            }
        }

        /// <summary>
        /// Handles the Quit button inside the TextRPG — pops this scene.
        /// </summary>
        private void OnHostCloseRequested()
        {
            Game.Instance.Scenes.Pop();
        }

        public override void OnExit()
        {
            Game.Instance.TextRPGActive = false;

            if (_host != null)
            {
                _host.CloseRequested -= OnHostCloseRequested;

                // Remove from parent form and dispose all child controls.
                Control parent = _host.Parent;
                parent?.Controls.Remove(_host);
                _host.Dispose();
                _host = null;
            }
        }

        public override void Update(float dt)
        {
            // The TextRPG handles all its own input through WinForms controls.
            // Nothing to update here — the game loop is effectively paused.
        }

        public override void Draw(Graphics g)
        {
            // The TextRPG renders itself with WinForms controls layered on top
            // of the canvas, so there is nothing to draw in the GDI+ pipeline.
        }
    }
}
