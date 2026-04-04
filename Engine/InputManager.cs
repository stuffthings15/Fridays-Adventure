using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Fridays_Adventure.Engine
{
    public sealed class InputManager
    {
        private readonly HashSet<Keys> _held     = new HashSet<Keys>();
        private readonly HashSet<Keys> _pressed  = new HashSet<Keys>();
        private readonly HashSet<Keys> _released = new HashSet<Keys>();
        private readonly StringBuilder _typedBuf = new StringBuilder();

        public bool IsHeld(Keys k)     => _held.Contains(k);
        public bool IsPressed(Keys k)  => _pressed.Contains(k);
        public bool IsReleased(Keys k) => _released.Contains(k);

        public bool LeftHeld        => IsHeld(Keys.Left)  || IsHeld(Keys.A);
        public bool RightHeld       => IsHeld(Keys.Right) || IsHeld(Keys.D);
        public bool UpHeld          => IsHeld(Keys.Up)    || IsHeld(Keys.W);
        public bool DownHeld        => IsHeld(Keys.Down)  || IsHeld(Keys.S);
        public bool SprintHeld      => IsHeld(Keys.ShiftKey) || IsHeld(Keys.LShiftKey) || IsHeld(Keys.RShiftKey);
        public bool JumpPressed     => IsPressed(Keys.Space) || IsPressed(Keys.Up) || IsPressed(Keys.W);
        public bool JumpHeld        => IsHeld(Keys.Space)    || IsHeld(Keys.Up)    || IsHeld(Keys.W);
        public bool AttackPressed   => IsPressed(Keys.Z) || IsPressed(Keys.J);
        public bool AttackHeld      => IsHeld(Keys.Z)    || IsHeld(Keys.J);
        public bool DodgePressed    => IsPressed(Keys.X) || IsPressed(Keys.K);
        public bool Ability1Pressed => IsPressed(Keys.Q);
        public bool Ability2Pressed => IsPressed(Keys.E);
        public bool Ability3Pressed => IsPressed(Keys.R);
        /// <summary>C key — air dash burst (Phase 2, Team 7 #2).</summary>
        public bool AirDashPressed  => IsPressed(Keys.C);
        public bool InteractPressed => IsPressed(Keys.F) || IsPressed(Keys.Enter);
        public bool PausePressed    => IsPressed(Keys.Escape);
        public bool AnyMash         => IsPressed(Keys.Space) || IsPressed(Keys.Z) || IsPressed(Keys.X);

        public void OnKeyDown(Keys key)
        {
            if (_held.Add(key))
                _pressed.Add(key);
        }

        public void OnKeyUp(Keys key)
        {
            _held.Remove(key);
            _released.Add(key);
        }

        /// <summary>Called from Form KeyPress; accumulates printable characters for text input.</summary>
        public void OnKeyChar(char c)
        {
            if (c >= 32 && c != 127) _typedBuf.Append(c);
        }

        /// <summary>Returns all characters typed this frame and clears the buffer.</summary>
        public string ConsumeTyped()
        {
            string s = _typedBuf.ToString();
            _typedBuf.Clear();
            return s;
        }

        public void EndFrame()
        {
            _pressed.Clear();
            _released.Clear();
            _typedBuf.Clear();
        }

        public void Reset()
        {
            _held.Clear();
            _pressed.Clear();
            _released.Clear();
        }
    }
}
