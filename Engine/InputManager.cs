using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.InteropServices;

namespace Fridays_Adventure.Engine
{
    public sealed class InputManager
    {
        private readonly HashSet<Keys> _held     = new HashSet<Keys>();
        private readonly HashSet<Keys> _pressed  = new HashSet<Keys>();
        private readonly HashSet<Keys> _released = new HashSet<Keys>();
        private readonly StringBuilder _typedBuf = new StringBuilder();

        // ── Gamepad support (XInput) ──────────────────────────────────────
        private GamepadState[] _gamepadStates = new GamepadState[4];
        private GamepadState[] _previousGamepadStates = new GamepadState[4];

        public struct GamepadState
        {
            public float LeftStickX;
            public float LeftStickY;
            public float RightStickX;
            public float RightStickY;
            public float LeftTrigger;
            public float RightTrigger;
            public bool ButtonA;
            public bool ButtonB;
            public bool ButtonX;
            public bool ButtonY;
            public bool ButtonLB;
            public bool ButtonRB;
            public bool ButtonStart;
            public bool ButtonBack;
            public bool DPadUp;
            public bool DPadDown;
            public bool DPadLeft;
            public bool DPadRight;
        }

        // ── Touch support ─────────────────────────────────────────────────
        public struct TouchPoint
        {
            public int Id;
            public float X;
            public float Y;
            public bool IsPressed;
        }

        private Dictionary<int, TouchPoint> _activeTouches = new Dictionary<int, TouchPoint>();
        private TouchPoint[] _touchButtonZones;
        private bool _touchEnabled = true;

        // ── XInput definitions ────────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential)]
        private struct XInputGamepadState
        {
            public ushort Buttons;
            public byte LeftTrigger;
            public byte RightTrigger;
            public short LeftStickX;
            public short LeftStickY;
            public short RightStickX;
            public short RightStickY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputState
        {
            public uint PacketNumber;
            public XInputGamepadState Gamepad;
        }

        private const ushort XINPUT_GAMEPAD_A = 0x1000;
        private const ushort XINPUT_GAMEPAD_B = 0x2000;
        private const ushort XINPUT_GAMEPAD_X = 0x4000;
        private const ushort XINPUT_GAMEPAD_Y = 0x8000;
        private const ushort XINPUT_GAMEPAD_LB = 0x0100;
        private const ushort XINPUT_GAMEPAD_RB = 0x0200;
        private const ushort XINPUT_GAMEPAD_BACK = 0x0020;
        private const ushort XINPUT_GAMEPAD_START = 0x0010;
        private const ushort XINPUT_GAMEPAD_DPAD_UP = 0x0001;
        private const ushort XINPUT_GAMEPAD_DPAD_DOWN = 0x0002;
        private const ushort XINPUT_GAMEPAD_DPAD_LEFT = 0x0004;
        private const ushort XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008;

        [DllImport("xinput1_4.dll", SetLastError = false)]
        private static extern uint XInputGetState(uint dwUserIndex, ref XInputState pState);
        private const uint ERROR_SUCCESS = 0;

        // ────────────────────────────────────────────────────────────────────
        // Bot input injection
        private readonly HashSet<Keys> _injectedHeld    = new HashSet<Keys>();
        private readonly HashSet<Keys> _injectedPressed = new HashSet<Keys>();

        public InputManager()
        {
            for (int i = 0; i < 4; i++)
            {
                _gamepadStates[i] = new GamepadState();
                _previousGamepadStates[i] = new GamepadState();
            }

            InitializeTouchButtonZones();
        }

        private void InitializeTouchButtonZones()
        {
            _touchButtonZones = new TouchPoint[8];

            // Bottom-left: Movement
            _touchButtonZones[0] = new TouchPoint { X = 60, Y = 740, IsPressed = false };
            _touchButtonZones[1] = new TouchPoint { X = 60, Y = 800, IsPressed = false };
            _touchButtonZones[2] = new TouchPoint { X = 20, Y = 770, IsPressed = false };
            _touchButtonZones[3] = new TouchPoint { X = 100, Y = 770, IsPressed = false };

            // Bottom-right: Actions
            _touchButtonZones[4] = new TouchPoint { X = 1860, Y = 740, IsPressed = false };
            _touchButtonZones[5] = new TouchPoint { X = 1860, Y = 800, IsPressed = false };
            _touchButtonZones[6] = new TouchPoint { X = 1820, Y = 770, IsPressed = false };
            _touchButtonZones[7] = new TouchPoint { X = 1920, Y = 770, IsPressed = false };
        }

        // ════════════════════════════════════════════════════════════════════
        // BOT INPUT INJECTION
        // ════════════════════════════════════════════════════════════════════

        public void InjectHeld(Keys k)
        {
            _injectedHeld.Add(k);
            _injectedPressed.Add(k);
        }

        public void InjectPressed(Keys k) => _injectedPressed.Add(k);

        public void ClearInjected()
        {
            _injectedHeld.Clear();
            _injectedPressed.Clear();
        }

        // ════════════════════════════════════════════════════════════════════
        // INPUT QUERIES
        // ════════════════════════════════════════════════════════════════════

        public bool IsHeld(Keys k)     => _held.Contains(k) || _injectedHeld.Contains(k);
        public bool IsPressed(Keys k)  => _pressed.Contains(k) || _injectedPressed.Contains(k);
        public bool IsReleased(Keys k) => _released.Contains(k);

        // ────────────────────────────────────────────────────────────────────
        // Combined keyboard + gamepad + touch
        // ────────────────────────────────────────────────────────────────────

        public bool LeftHeld        => IsHeld(Keys.Left) || IsHeld(Keys.A) || 
                                      _gamepadStates[0].DPadLeft || _gamepadStates[0].LeftStickX < -0.3f;

        public bool RightHeld       => IsHeld(Keys.Right) || IsHeld(Keys.D) ||
                                      _gamepadStates[0].DPadRight || _gamepadStates[0].LeftStickX > 0.3f;

        public bool UpHeld          => IsHeld(Keys.Up) || IsHeld(Keys.W) ||
                                      _gamepadStates[0].DPadUp || _gamepadStates[0].LeftStickY > 0.3f;

        public bool DownHeld        => IsHeld(Keys.Down) || IsHeld(Keys.S) ||
                                      _gamepadStates[0].DPadDown || _gamepadStates[0].LeftStickY < -0.3f;

        public bool SprintHeld      => IsHeld(Keys.ShiftKey) || IsHeld(Keys.LShiftKey) || IsHeld(Keys.RShiftKey) ||
                                      _gamepadStates[0].ButtonLB;

        public bool JumpPressed     => IsPressed(Keys.Space) || IsPressed(Keys.Up) || IsPressed(Keys.W) ||
                                      GamepadButtonPressed(0, GamepadButton.A);

        public bool JumpHeld        => IsHeld(Keys.Space) || IsHeld(Keys.Up) || IsHeld(Keys.W) ||
                                      _gamepadStates[0].ButtonA;

        public bool AttackPressed   => IsPressed(Keys.Z) || IsPressed(Keys.J) ||
                                      GamepadButtonPressed(0, GamepadButton.X);

        public bool AttackHeld      => IsHeld(Keys.Z) || IsHeld(Keys.J) ||
                                      _gamepadStates[0].ButtonX;

        public bool DodgePressed    => IsPressed(Keys.X) || IsPressed(Keys.K) ||
                                      GamepadButtonPressed(0, GamepadButton.B);

        public bool Ability1Pressed => IsPressed(Keys.Q) || GamepadButtonPressed(0, GamepadButton.Y);
        public bool Ability2Pressed => IsPressed(Keys.E) || GamepadButtonPressed(0, GamepadButton.LB);
        public bool Ability3Pressed => IsPressed(Keys.R) || GamepadButtonPressed(0, GamepadButton.RB);
        public bool AirDashPressed  => IsPressed(Keys.C) || GamepadButtonPressed(0, GamepadButton.RB);
        public bool FrostBallPressed => IsPressed(Keys.B) || GamepadButtonPressed(0, GamepadButton.Y);
        public bool InventoryPressed => IsPressed(Keys.I) || GamepadButtonPressed(0, GamepadButton.LB);
        public bool InteractPressed => IsPressed(Keys.F) || IsPressed(Keys.Enter) || GamepadButtonPressed(0, GamepadButton.A);
        public bool PausePressed    => IsPressed(Keys.Escape) || GamepadButtonPressed(0, GamepadButton.Start);
        public bool AnyMash         => IsPressed(Keys.Space) || IsPressed(Keys.Z) || IsPressed(Keys.X) ||
                                      _gamepadStates[0].ButtonA || _gamepadStates[0].ButtonX || _gamepadStates[0].ButtonB;

        // ════════════════════════════════════════════════════════════════════
        // GAMEPAD INPUT
        // ════════════════════════════════════════════════════════════════════

        public enum GamepadButton
        {
            A, B, X, Y, LB, RB, Back, Start
        }

        public bool GamepadButtonPressed(int padIndex, GamepadButton button)
        {
            if (padIndex < 0 || padIndex >= 4) return false;

            bool isPressed = GetGamepadButton(_gamepadStates[padIndex], button);
            bool wasPrevious = GetGamepadButton(_previousGamepadStates[padIndex], button);

            return isPressed && !wasPrevious;
        }

        public bool GamepadButtonHeld(int padIndex, GamepadButton button)
        {
            if (padIndex < 0 || padIndex >= 4) return false;
            return GetGamepadButton(_gamepadStates[padIndex], button);
        }

        private bool GetGamepadButton(GamepadState state, GamepadButton button) => button switch
        {
            GamepadButton.A => state.ButtonA,
            GamepadButton.B => state.ButtonB,
            GamepadButton.X => state.ButtonX,
            GamepadButton.Y => state.ButtonY,
            GamepadButton.LB => state.ButtonLB,
            GamepadButton.RB => state.ButtonRB,
            GamepadButton.Back => state.ButtonBack,
            GamepadButton.Start => state.ButtonStart,
            _ => false
        };

        public void UpdateGamepads()
        {
            System.Array.Copy(_gamepadStates, _previousGamepadStates, 4);

            for (int i = 0; i < 4; i++)
            {
                UpdateGamepadState(i);
            }
        }

        private void UpdateGamepadState(int index)
        {
            try
            {
                XInputState state = new XInputState();
                uint result = XInputGetState((uint)index, ref state);

                if (result != ERROR_SUCCESS)
                {
                    _gamepadStates[index] = new GamepadState();
                    return;
                }

                var gamepad = state.Gamepad;
                _gamepadStates[index] = new GamepadState
                {
                    LeftStickX = gamepad.LeftStickX / 32768f,
                    LeftStickY = gamepad.LeftStickY / 32768f,
                    RightStickX = gamepad.RightStickX / 32768f,
                    RightStickY = gamepad.RightStickY / 32768f,
                    LeftTrigger = gamepad.LeftTrigger / 255f,
                    RightTrigger = gamepad.RightTrigger / 255f,
                    ButtonA = (gamepad.Buttons & XINPUT_GAMEPAD_A) != 0,
                    ButtonB = (gamepad.Buttons & XINPUT_GAMEPAD_B) != 0,
                    ButtonX = (gamepad.Buttons & XINPUT_GAMEPAD_X) != 0,
                    ButtonY = (gamepad.Buttons & XINPUT_GAMEPAD_Y) != 0,
                    ButtonLB = (gamepad.Buttons & XINPUT_GAMEPAD_LB) != 0,
                    ButtonRB = (gamepad.Buttons & XINPUT_GAMEPAD_RB) != 0,
                    ButtonStart = (gamepad.Buttons & XINPUT_GAMEPAD_START) != 0,
                    ButtonBack = (gamepad.Buttons & XINPUT_GAMEPAD_BACK) != 0,
                    DPadUp = (gamepad.Buttons & XINPUT_GAMEPAD_DPAD_UP) != 0,
                    DPadDown = (gamepad.Buttons & XINPUT_GAMEPAD_DPAD_DOWN) != 0,
                    DPadLeft = (gamepad.Buttons & XINPUT_GAMEPAD_DPAD_LEFT) != 0,
                    DPadRight = (gamepad.Buttons & XINPUT_GAMEPAD_DPAD_RIGHT) != 0,
                };
            }
            catch
            {
                _gamepadStates[index] = new GamepadState();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // TOUCH INPUT
        // ════════════════════════════════════════════════════════════════════

        public void RegisterTouch(int id, float x, float y, bool isPressed)
        {
            if (!_touchEnabled) return;

            if (isPressed)
            {
                _activeTouches[id] = new TouchPoint { Id = id, X = x, Y = y, IsPressed = true };
            }
            else
            {
                _activeTouches.Remove(id);
            }
        }

        public TouchPoint[] GetActiveTouches() => _activeTouches.Values.ToArray();
        public void SetTouchEnabled(bool enabled) => _touchEnabled = enabled;

        // ════════════════════════════════════════════════════════════════════
        // FRAME MANAGEMENT
        // ════════════════════════════════════════════════════════════════════

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

        public void OnKeyChar(char c)
        {
            if (c >= 32 && c != 127) _typedBuf.Append(c);
        }

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
            _injectedHeld.Clear();
            _injectedPressed.Clear();
            _activeTouches.Clear();
        }
    }
}