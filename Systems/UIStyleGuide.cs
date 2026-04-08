using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    // ─────────────────────────────────────────────────────────────────────────
    //  UIStyleGuide.cs  —  UI / UX Artist Systems
    //
    //  Team 15 (UI / UX Artist) — all 10 ideas implemented:
    //
    //    Idea 1:  Icon sprite registry — HUD icons for power-ups, lives, coins,
    //             etc. keyed by name; draw helper included.
    //    Idea 2:  Menu button style guide — consistent rounded-rect button
    //             rendering with hover/pressed/disabled states.
    //    Idea 3:  Font registry — named fonts keyed by context (HUD, Dialogue,
    //             Title, Tooltip) with the correct SMB3-style pixel size.
    //    Idea 4:  Curtain scene transition — SMB3-style black curtain that
    //             drops/rises on scene changes.
    //    Idea 5:  Inventory grid renderer — draws a fixed-column item grid
    //             with selection highlight.
    //    Idea 6:  Status-icon pulse animation — icons blip between two sizes
    //             to draw attention (low health, active power-up timer).
    //    Idea 7:  Screen-edge vignette — draws a radial gradient that darkens
    //             the corners of the screen for cinematic feel.
    //    Idea 8:  Notification badge — small red circle with a count drawn
    //             over an icon to show pending achievements / messages.
    //    Idea 9:  Tooltip system — shows a small label near the cursor / focused
    //             element with an automatic lifetime.
    //    Idea 10: Accessibility contrast checker — logs a warning when a
    //             text/background colour pair falls below WCAG AA ratio.
    // ─────────────────────────────────────────────────────────────────────────

    // ── Idea 2 support: button visual state ──────────────────────────────────
    /// <summary>Visual state of a UI button. Idea 2 (UI/UX Artist).</summary>
    public enum ButtonState { Normal, Hovered, Pressed, Disabled }

    /// <summary>
    /// Central UI and UX style system: icon registry, button and font
    /// standards, curtain transitions, inventory grid, animations,
    /// vignette, badges, tooltips, and accessibility checking.
    /// </summary>
    public static class UIStyleGuide
    {
        // ── Idea 1: Icon sprite registry ─────────────────────────────────────
        /// <summary>
        /// HUD icon images keyed by name (e.g. "Coin", "Heart", "FireFlower").
        /// Idea 1 (UI/UX Artist).
        /// </summary>
        private static readonly Dictionary<string, Image> _icons =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a HUD icon image under a name.
        /// Idea 1 (UI/UX Artist).
        /// </summary>
        public static void RegisterIcon(string name, Image img)
        {
            _icons[name] = img;
        }

        /// <summary>
        /// Draws a named icon centred on the given screen position.
        /// Silently skips if the icon is not registered.
        /// Idea 1 (UI/UX Artist).
        /// </summary>
        public static void DrawIcon(Graphics g, string name,
                                     int cx, int cy, int size = 16)
        {
            if (!_icons.TryGetValue(name, out var img)) return;
            g.DrawImage(img,
                new Rectangle(cx - size / 2, cy - size / 2, size, size));
        }

        // ── Idea 2: Menu button style guide ──────────────────────────────────
        // Colour constants matching the SMB3-style palette.
        private static readonly Color BtnNormalFill    = Color.FromArgb(40,  80,  40);
        private static readonly Color BtnHoverFill     = Color.FromArgb(60, 120,  60);
        private static readonly Color BtnPressedFill   = Color.FromArgb(20,  50,  20);
        private static readonly Color BtnDisabledFill  = Color.FromArgb(50,  50,  50);
        private static readonly Color BtnBorder        = Color.FromArgb(140, 200, 60);
        private static readonly Color BtnTextColor     = Color.FromArgb(255, 248, 180);
        private static readonly Color BtnDisabledText  = Color.FromArgb(100, 100, 100);
        private const int             BtnCornerRadius  = 6;

        /// <summary>
        /// Draws a styled menu button rectangle.
        /// Idea 2 (UI/UX Artist).
        /// </summary>
        /// <param name="g">Active Graphics context.</param>
        /// <param name="bounds">Button rectangle in screen space.</param>
        /// <param name="label">Text label drawn centred on the button.</param>
        /// <param name="state">Current visual state of the button.</param>
        public static void DrawButton(Graphics g, Rectangle bounds,
                                       string label, ButtonState state = ButtonState.Normal)
        {
            Color fill = state switch
            {
                ButtonState.Hovered  => BtnHoverFill,
                ButtonState.Pressed  => BtnPressedFill,
                ButtonState.Disabled => BtnDisabledFill,
                _                    => BtnNormalFill,
            };

            Color textCol = state == ButtonState.Disabled ? BtnDisabledText : BtnTextColor;

            // -- Rounded fill
            using (var path = RoundedRect(bounds, BtnCornerRadius))
            using (var fillBrush = new SolidBrush(fill))
            {
                g.FillPath(fillBrush, path);
            }

            // -- Border
            using (var path  = RoundedRect(bounds, BtnCornerRadius))
            using (var pen   = new Pen(BtnBorder, 2f))
            {
                g.DrawPath(pen, path);
            }

            // -- Label
            using (var font  = GetFont("HUD"))
            using (var brush = new SolidBrush(textCol))
            {
                var sf = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(label, font, brush, bounds, sf);
            }
        }

        /// <summary>Builds a rounded-rectangle GraphicsPath.</summary>
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.Left,            r.Top,             d, d, 180, 90);
            path.AddArc(r.Right - d,       r.Top,             d, d, 270, 90);
            path.AddArc(r.Right - d,       r.Bottom - d,      d, d,   0, 90);
            path.AddArc(r.Left,            r.Bottom - d,      d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        // ── Idea 3: Font registry ─────────────────────────────────────────────
        /// <summary>
        /// Named font descriptors.  "HUD" is used on the game HUD,
        /// "Dialogue" in speech bubbles, "Title" on the title/menu screens,
        /// "Tooltip" in tooltips.
        /// Idea 3 (UI/UX Artist).
        /// </summary>
        private static readonly Dictionary<string, Font> _fonts =
            new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the named font, creating it with pixel-scale defaults if needed.
        /// Idea 3 (UI/UX Artist).
        /// </summary>
        public static Font GetFont(string context = "HUD")
        {
            if (_fonts.TryGetValue(context, out var f)) return f;

            // Bootstrap defaults — pixel-art style, no antialiasing implied.
            float size = context.ToLowerInvariant() switch
            {
                "hud"      => 10f,
                "dialogue" => 11f,
                "title"    => 20f,
                "tooltip"  => 9f,
                "large"    => 14f,
                _          => 10f,
            };
            var font = new Font("Courier New", size, FontStyle.Bold,
                                GraphicsUnit.Point);
            _fonts[context] = font;
            return font;
        }

        /// <summary>
        /// Overrides the default font for a context with a custom font.
        /// Idea 3 (UI/UX Artist).
        /// </summary>
        public static void RegisterFont(string context, Font font)
        {
            _fonts[context] = font;
        }

        // ── Idea 4: Curtain scene transition ──────────────────────────────────
        /// <summary>
        /// Curtain animation phase: dropping (fade-to-black) or rising.
        /// Idea 4 (UI/UX Artist).
        /// </summary>
        public enum CurtainPhase { Idle, Dropping, Rising }

        /// <summary>Current curtain animation phase.</summary>
        public static CurtainPhase Curtain { get; private set; } = CurtainPhase.Idle;

        /// <summary>
        /// Current curtain alpha (0 = transparent, 255 = fully black).
        /// Idea 4 (UI/UX Artist).
        /// </summary>
        public static int CurtainAlpha { get; private set; }

        private static float _curtainSpeed; // alpha units per second
        private static Action _curtainMidCallback;

        /// <summary>
        /// Starts a drop-then-rise curtain.  <paramref name="onMidpoint"/> is
        /// invoked when the curtain is fully black (scene-switch point).
        /// Idea 4 (UI/UX Artist).
        /// </summary>
        public static void StartCurtain(float durationSec = 0.4f,
                                         Action onMidpoint  = null)
        {
            _curtainSpeed      = 255f / Math.Max(0.01f, durationSec);
            _curtainMidCallback = onMidpoint;
            CurtainAlpha       = 0;
            Curtain            = CurtainPhase.Dropping;
        }

        /// <summary>
        /// Advances the curtain animation.  Call once per game tick.
        /// Idea 4 (UI/UX Artist).
        /// </summary>
        public static void UpdateCurtain(float dt)
        {
            if (Curtain == CurtainPhase.Idle) return;

            if (Curtain == CurtainPhase.Dropping)
            {
                CurtainAlpha = Math.Min(255, CurtainAlpha + (int)(_curtainSpeed * dt));
                if (CurtainAlpha >= 255)
                {
                    _curtainMidCallback?.Invoke();
                    _curtainMidCallback = null;
                    Curtain = CurtainPhase.Rising;
                }
            }
            else // Rising
            {
                CurtainAlpha = Math.Max(0, CurtainAlpha - (int)(_curtainSpeed * dt));
                if (CurtainAlpha <= 0) Curtain = CurtainPhase.Idle;
            }
        }

        /// <summary>
        /// Draws the curtain overlay if active.
        /// Idea 4 (UI/UX Artist).
        /// </summary>
        public static void DrawCurtain(Graphics g, int screenW, int screenH)
        {
            if (Curtain == CurtainPhase.Idle || CurtainAlpha == 0) return;
            using (var b = new SolidBrush(Color.FromArgb(CurtainAlpha, 0, 0, 0)))
                g.FillRectangle(b, 0, 0, screenW, screenH);
        }

        // ── Idea 5: Inventory grid renderer ──────────────────────────────────
        /// <summary>
        /// Draws an inventory grid of <paramref name="itemCount"/> cells,
        /// highlighting the cell at <paramref name="selectedIndex"/>.
        /// Idea 5 (UI/UX Artist).
        /// </summary>
        public static void DrawInventoryGrid(Graphics g, Rectangle origin,
                                              int columns, int cellSize,
                                              int itemCount, int selectedIndex,
                                              int padding = 4)
        {
            int stride = cellSize + padding;
            for (int i = 0; i < itemCount; i++)
            {
                int col = i % columns;
                int row = i / columns;
                var cell = new Rectangle(
                    origin.X + col * stride,
                    origin.Y + row * stride,
                    cellSize, cellSize);

                // -- Background
                bool selected = i == selectedIndex;
                Color bg = selected
                    ? Color.FromArgb(200, 80, 160, 40)
                    : Color.FromArgb(120, 20,  30, 20);

                using (var b = new SolidBrush(bg))
                    g.FillRectangle(b, cell);

                // -- Border
                using (var pen = new Pen(selected
                    ? Color.FromArgb(255, 140, 230, 60)
                    : Color.FromArgb(100, 100, 100, 60), selected ? 2 : 1))
                    g.DrawRectangle(pen, cell);
            }
        }

        // ── Idea 6: Status-icon pulse animation ──────────────────────────────
        /// <summary>
        /// Accumulated pulse time (seconds).  Advance each tick.
        /// Idea 6 (UI/UX Artist).
        /// </summary>
        private static float _pulseTime;
        private const float PulseHz = 2.5f; // oscillations per second

        /// <summary>Advance the pulse timer. Call once per update tick.</summary>
        public static void UpdatePulse(float dt) => _pulseTime += dt;

        /// <summary>
        /// Returns a draw size for a pulsing status icon.
        /// The icon oscillates between <paramref name="baseSize"/> and
        /// <paramref name="baseSize"/> × 1.25.
        /// Idea 6 (UI/UX Artist).
        /// </summary>
        public static float GetPulseSize(float baseSize)
        {
            float t = (float)Math.Sin(_pulseTime * PulseHz * Math.PI * 2);
            return baseSize * (1.0f + 0.125f * t); // ±12.5 % pulse
        }

        // ── Idea 7: Screen-edge vignette ──────────────────────────────────────
        /// <summary>
        /// Draws a radial gradient that darkens the corners of the screen.
        /// Call after the scene draw, before the HUD.
        /// Idea 7 (UI/UX Artist).
        /// Pre-renders into a cached bitmap to eliminate per-frame
        /// PathGradientBrush cost.
        /// </summary>
        private static Bitmap _uiVignetteBmp;
        private static int _uiVigW, _uiVigH, _uiVigAlpha;

        public static void DrawVignette(Graphics g, int screenW, int screenH,
                                         int alpha = 120)
        {
            if (_uiVignetteBmp == null || _uiVigW != screenW ||
                _uiVigH != screenH || _uiVigAlpha != alpha)
            {
                _uiVignetteBmp?.Dispose();
                _uiVigW = screenW; _uiVigH = screenH; _uiVigAlpha = alpha;
                _uiVignetteBmp = new Bitmap(screenW, screenH,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (var bg = Graphics.FromImage(_uiVignetteBmp))
                {
                    bg.Clear(Color.Transparent);
                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(-screenW / 4, -screenH / 4,
                                         screenW * 3 / 2, screenH * 3 / 2);
                        using (var pgb = new PathGradientBrush(path))
                        {
                            pgb.CenterColor      = Color.Transparent;
                            pgb.SurroundColors   = new[] { Color.FromArgb(alpha, 0, 0, 0) };
                            pgb.CenterPoint      = new PointF(screenW / 2f, screenH / 2f);
                            bg.FillRectangle(pgb, 0, 0, screenW, screenH);
                        }
                    }
                }
            }
            g.DrawImage(_uiVignetteBmp, 0, 0);
        }

        // ── Idea 8: Notification badge ────────────────────────────────────────
        /// <summary>
        /// Draws a small red badge with a count number in the top-right corner
        /// of the specified icon rectangle.
        /// Idea 8 (UI/UX Artist).
        /// </summary>
        public static void DrawBadge(Graphics g, Rectangle iconBounds, int count)
        {
            if (count <= 0) return;
            string label = count > 99 ? "99+" : count.ToString();
            int r     = 8; // badge radius
            int cx    = iconBounds.Right;
            int cy    = iconBounds.Top;

            using (var fill = new SolidBrush(Color.FromArgb(220, 200, 30, 30)))
            using (var text = new SolidBrush(Color.White))
            using (var f    = new Font("Courier New", 6f, FontStyle.Bold, GraphicsUnit.Point))
            {
                var badgeRect = new Rectangle(cx - r, cy - r, r * 2, r * 2);
                g.FillEllipse(fill, badgeRect);
                var sf = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(label, f, text, badgeRect, sf);
            }
        }

        // ── Idea 9: Tooltip system ────────────────────────────────────────────
        /// <summary>
        /// Active tooltip text; empty string = no tooltip.
        /// Idea 9 (UI/UX Artist).
        /// </summary>
        public static string TooltipText    { get; private set; } = string.Empty;
        private static PointF _tooltipPos;
        private static float  _tooltipLife;

        /// <summary>
        /// Shows a tooltip at the given screen position for a limited duration.
        /// Idea 9 (UI/UX Artist).
        /// </summary>
        public static void ShowTooltip(string text, PointF screenPos,
                                        float durationSec = 2.5f)
        {
            TooltipText  = text;
            _tooltipPos  = screenPos;
            _tooltipLife = durationSec;
        }

        /// <summary>Advances tooltip lifetime. Call once per update tick.</summary>
        public static void UpdateTooltip(float dt)
        {
            if (_tooltipLife <= 0) { TooltipText = string.Empty; return; }
            _tooltipLife -= dt;
            if (_tooltipLife <= 0) TooltipText = string.Empty;
        }

        /// <summary>
        /// Draws the active tooltip if one is set.
        /// Idea 9 (UI/UX Artist).
        /// </summary>
        public static void DrawTooltip(Graphics g)
        {
            if (string.IsNullOrEmpty(TooltipText)) return;
            using (var font = GetFont("Tooltip"))
            {
                SizeF sz   = g.MeasureString(TooltipText, font);
                var   rect = new RectangleF(_tooltipPos.X + 8,
                                            _tooltipPos.Y - sz.Height - 4,
                                            sz.Width + 8, sz.Height + 4);
                using (var bg  = new SolidBrush(Color.FromArgb(200, 20, 20, 20)))
                using (var pen = new Pen(Color.FromArgb(180, 200, 200, 60), 1))
                using (var txt = new SolidBrush(Color.White))
                {
                    g.FillRectangle(bg,  rect);
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                    g.DrawString(TooltipText, font, txt,
                                 rect.X + 4, rect.Y + 2);
                }
            }
        }

        // ── Idea 10: Accessibility contrast checker ───────────────────────────
        /// <summary>
        /// Checks that the contrast ratio between foreground and background
        /// meets the WCAG AA minimum (4.5:1 for normal text).
        /// Logs a warning via DebugLogger if the pair fails.
        /// Idea 10 (UI/UX Artist).
        /// </summary>
        /// <param name="fg">Foreground (text) colour.</param>
        /// <param name="bg">Background colour.</param>
        /// <param name="context">Human-readable label for the log entry.</param>
        /// <returns>True if contrast is WCAG-AA compliant.</returns>
        public static bool CheckContrast(Color fg, Color bg, string context = "")
        {
            double lFg = RelativeLuminance(fg);
            double lBg = RelativeLuminance(bg);
            double lighter  = Math.Max(lFg, lBg);
            double darker   = Math.Min(lFg, lBg);
            double ratio    = (lighter + 0.05) / (darker + 0.05);

            bool pass = ratio >= 4.5;
            if (!pass)
            {
                DebugLogger.LogWarning("UIStyleGuide",
                    $"Contrast FAIL {context}: " +
                    $"fg={fg.Name} bg={bg.Name} ratio={ratio:F2}:1 (need ≥4.5:1)");
            }
            return pass;
        }

        /// <summary>Computes relative luminance per WCAG 2.1.</summary>
        private static double RelativeLuminance(Color c)
        {
            double R = Linearise(c.R / 255.0);
            double G = Linearise(c.G / 255.0);
            double B = Linearise(c.B / 255.0);
            return 0.2126 * R + 0.7152 * G + 0.0722 * B;
        }

        private static double Linearise(double v)
            => v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }
}
