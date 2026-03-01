using System.Collections.Generic;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure.Engine
{
    public sealed class SceneManager
    {
        private readonly Stack<Scene> _stack = new Stack<Scene>();
        public Scene Current => _stack.Count > 0 ? _stack.Peek() : null;

        public void Push(Scene scene)
        {
            Current?.OnPause();
            _stack.Push(scene);
            scene.OnEnter();
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;
            _stack.Pop().OnExit();
            Current?.OnResume();
        }

        public void Replace(Scene scene)
        {
            if (_stack.Count > 0)
                _stack.Pop().OnExit();
            _stack.Push(scene);
            scene.OnEnter();
        }
    }
}
