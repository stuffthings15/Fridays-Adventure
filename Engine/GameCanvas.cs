using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Fridays_Adventure.Engine
{
    /// <summary>
    /// High-definition rendering surface for the game.
    /// Uses double-buffering and high-quality GDI+ settings.
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
            // Apply high-quality rendering defaults before the scene draws.
            var g = e.Graphics;
            g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode      = SmoothingMode.HighQuality;
            g.PixelOffsetMode    = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint  = TextRenderingHint.ClearTypeGridFit;
            Render?.Invoke(g);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
    }
}
