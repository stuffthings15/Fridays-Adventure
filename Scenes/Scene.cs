using System.Drawing;

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
    }
}
