using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fridays_Adventure.Engine
{
    /// <summary>
    /// Double-buffered rendering surface for the game.
    /// Quality settings are applied in Game.OnRender — canvas just passes through.
    /// </summary>
    public sealed class GameCanvas : Panel
    {
        public event Action<Graphics> Render;

        public GameCanvas()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.FromArgb(8, 16, 32);
            ResizeRedraw = true;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Render?.Invoke(e.Graphics);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
    }
}
