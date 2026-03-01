using System.Drawing;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    public sealed class GameOverScene : Scene
    {
        private float _timer;

        public override void OnEnter() { Game.Instance.Audio.StopMusic(); }
        public override void OnExit()  { }

        public override void Update(float dt)
        {
            _timer += dt;
            if (_timer > 0.8f &&
                (Game.Instance.Input.InteractPressed ||
                 Game.Instance.Input.AttackPressed))
                Game.Instance.Scenes.Replace(new TitleScene());
        }

        public override void HandleClick(Point p)
        {
            if (_timer > 0.8f)
                Game.Instance.Scenes.Replace(new TitleScene());
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 30, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("GAME  OVER", f);
                g.DrawString("GAME  OVER", f, Brushes.Crimson, (W-sz.Width)/2f, H*0.32f);
            }
            using (var f = new Font("Courier New", 11))
            {
                SizeF sz = g.MeasureString("The sea remembers those who dare it.", f);
                g.DrawString("The sea remembers those who dare it.", f,
                             Brushes.LightGray, (W-sz.Width)/2f, H*0.50f);
            }
            if (_timer > 0.8f)
                using (var f = new Font("Courier New", 10))
                {
                    SizeF sz = g.MeasureString("Press  Z  or  Enter  to return to title", f);
                    g.DrawString("Press  Z  or  Enter  to return to title", f,
                                 Brushes.White, (W-sz.Width)/2f, H*0.62f);
                }
        }
    }
}
