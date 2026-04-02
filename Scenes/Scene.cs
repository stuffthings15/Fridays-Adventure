using System.Drawing;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    public abstract class Scene
    {
        public abstract void OnEnter();
        public abstract void OnExit();
        public virtual  void OnPause()  { }
        public virtual  void OnResume() { }
        public abstract void Update(float dt);
        public abstract void Draw(Graphics g);
        public virtual  void HandleClick(Point p) { }

        /// <summary>Called when the mouse wheel is scrolled. Delta is positive for scroll-up, negative for scroll-down.</summary>
        public virtual  void HandleMouseWheel(int delta) { }

        // ── Dev Menu overlay — visible on every screen once GodMode is unlocked ─
        private Rectangle _devMenuBtn;

        /// <summary>
        /// Call at the end of every Draw() override. Only renders when GodMode is on.
        /// </summary>
        protected void DrawDevMenuButton(Graphics g)
        {
            if (!Game.Instance.GodMode) return;
            int W = Game.Instance.CanvasWidth;
            _devMenuBtn = new Rectangle(W - 120, 0, 120, 28);
            using (var br = new SolidBrush(Color.FromArgb(200, 40, 120, 0)))
                g.FillRectangle(br, _devMenuBtn);
            using (var pen = new Pen(Color.LimeGreen, 1))
                g.DrawRectangle(pen, _devMenuBtn);
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            {
                const string label = "\u26a1 DEV MENU";
                SizeF sz = g.MeasureString(label, f);
                g.DrawString(label, f, Brushes.LimeGreen,
                    _devMenuBtn.X + (_devMenuBtn.Width  - sz.Width)  / 2f,
                    _devMenuBtn.Y + (_devMenuBtn.Height - sz.Height) / 2f);
            }
        }

        /// <summary>
        /// Call at the start of every HandleClick() override. Returns true if the
        /// click was consumed (navigated to the dev menu).
        /// </summary>
        protected bool HandleDevMenuClick(Point p)
        {
            if (!Game.Instance.GodMode) return false;
            if (!_devMenuBtn.Contains(p)) return false;
            // Avoid pushing a second dev menu if we are already on it
            if (Game.Instance.Scenes.Current is DevMenuScene) return true;
            Game.Instance.Scenes.Push(new DevMenuScene());
            return true;
        }
    }
}
