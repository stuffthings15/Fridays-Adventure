using System;
using System.Collections.Generic;
using Fridays_Adventure.Scenes;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Engine
{
    public sealed class SceneManager
    {
        private readonly Stack<Scene> _stack = new Stack<Scene>();
        public Scene Current => _stack.Count > 0 ? _stack.Peek() : null;

        /// <summary>
        /// Number of scenes currently on the stack.
        /// Used by the debug overlay and performance profiler to show scene depth.
        /// </summary>
        public int Depth => _stack.Count;

        public void Push(Scene scene)
        {
            try
            {
                string from = Current?.GetType().Name;
                Current?.OnPause();
                _stack.Push(scene);
                scene.OnEnter();
                GameLogger.LogSceneTransition("Push", from, scene.GetType().Name);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SceneManager.Push", ex);
                throw;
            }
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;
            try
            {
                string from = _stack.Peek().GetType().Name;
                _stack.Pop().OnExit();
                Current?.OnResume();
                GameLogger.LogSceneTransition("Pop", from, Current?.GetType().Name);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SceneManager.Pop", ex);
                throw;
            }
        }

        public void Replace(Scene scene)
        {
            try
            {
                string from = _stack.Count > 0 ? _stack.Peek().GetType().Name : null;
                if (_stack.Count > 0)
                    _stack.Pop().OnExit();
                _stack.Push(scene);
                scene.OnEnter();
                GameLogger.LogSceneTransition("Replace", from, scene.GetType().Name);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SceneManager.Replace", ex);
                throw;
            }
        }

        /// <summary>
        /// Pops every scene off the stack (calling OnExit on each), then
        /// pushes the new scene. Use this when returning to the title screen
        /// to prevent stale scenes from accumulating underneath.
        /// </summary>
        public void ReplaceAll(Scene scene)
        {
            try
            {
                while (_stack.Count > 0)
                    _stack.Pop().OnExit();
                _stack.Push(scene);
                scene.OnEnter();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SceneManager.ReplaceAll", ex);
                throw;
            }
        }
    }
}
