using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Gamepad;
using Ryujinx.HLE.HOS.Services.Hid;
using System;
using System.Runtime.CompilerServices;

using GamepadInputId = Ryujinx.Gamepad.GamepadInputId;
using StickInputId = Ryujinx.Gamepad.StickInputId;

namespace Ryujinx.Input
{
    public class NpadController : IDisposable
    {
        private class HLEButtonMappingEntry
        {
            public GamepadInputId DriverInputId;
            public ControllerKeys HLEInput;
        }

        private static readonly HLEButtonMappingEntry[] _hleButtonMapping = new HLEButtonMappingEntry[]
        {
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.A, HLEInput = ControllerKeys.A },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.B, HLEInput = ControllerKeys.B },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.X, HLEInput = ControllerKeys.X },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.Y, HLEInput = ControllerKeys.Y },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.LeftStick, HLEInput = ControllerKeys.LStick },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.RightStick, HLEInput = ControllerKeys.RStick },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.LeftShoulder, HLEInput = ControllerKeys.L },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.RightShoulder, HLEInput = ControllerKeys.R },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.LeftTrigger, HLEInput = ControllerKeys.Zl },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.RightTrigger, HLEInput = ControllerKeys.Zr },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.DpadUp, HLEInput = ControllerKeys.DpadUp },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.DpadDown, HLEInput = ControllerKeys.DpadDown },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.DpadLeft, HLEInput = ControllerKeys.DpadLeft },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.DpadRight, HLEInput = ControllerKeys.DpadRight },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.Minus, HLEInput = ControllerKeys.Minus },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.Plus, HLEInput = ControllerKeys.Plus },

            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.SingleLeftTrigger0, HLEInput = ControllerKeys.SlLeft },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.SingleRightTrigger0, HLEInput = ControllerKeys.SrLeft },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.SingleLeftTrigger1, HLEInput = ControllerKeys.SlRight },
            new HLEButtonMappingEntry { DriverInputId = GamepadInputId.SingleRightTrigger1, HLEInput = ControllerKeys.SrRight },
        }; 

        private bool _isValid;
        private string _id;

        private IGamepad _gamepad;
        private InputConfig _config;

        public IGamepadDriver GamepadDriver { get; private set; }
        public GamepadStateSnapshot State { get; private set; }

        public string Id => _id;

        public NpadController()
        {
            State = default;
            _id = null;
            _isValid = false;
        }

        public bool UpdateDriverConfiguration(IGamepadDriver gamepadDriver, InputConfig config)
        {
            GamepadDriver = gamepadDriver;

            _gamepad?.Dispose();

            _id = config.Id;
            _gamepad = GamepadDriver.GetGamepad(_id);
            _config = config;
            _isValid = _gamepad != null;

            UpdateUserConfiguration(config);

            return _isValid;
        }

        public void UpdateUserConfiguration(InputConfig config)
        {
            _config = config;

            if (_isValid)
            {
                _gamepad.SetConfiguration(config);
            }
        }

        public void Update()
        {
            if (_isValid && GamepadDriver != null)
            {
                State = _gamepad.GetMappedStateSnapshot();
            }
            else
            {
                // Reset state
                State = default;
            }
        }

        private static short ClampAxis(float value)
        {
            if (value <= -short.MaxValue)
            {
                return -short.MaxValue;
            }
            else
            {
                return (short)(value * short.MaxValue);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JoystickPosition ApplyDeadzone(float x, float y, float deadzone)
        {
            return new JoystickPosition
            {
                Dx = ClampAxis(MathF.Abs(x) > deadzone ? x : 0.0f),
                Dy = ClampAxis(MathF.Abs(y) > deadzone ? y : 0.0f)
            };
        }

        public GamepadInput GetHLEState()
        {
            GamepadInput state = new GamepadInput();

            // First update all buttons
            foreach (HLEButtonMappingEntry entry in _hleButtonMapping)
            {
                if (State.IsPressed(entry.DriverInputId))
                {
                    state.Buttons |= entry.HLEInput;
                }
            }

            if (_gamepad is IKeyboard)
            {
                (float leftAxisX, float leftAxisY) = State.GetStick(StickInputId.Left);
                (float rightAxisX, float rightAxisY) = State.GetStick(StickInputId.Right);

                state.LStick = new JoystickPosition
                {
                    Dx = ClampAxis(leftAxisX),
                    Dy = ClampAxis(leftAxisY)
                };

                state.RStick = new JoystickPosition
                {
                    Dx = ClampAxis(rightAxisX),
                    Dy = ClampAxis(rightAxisY)
                };
            }
            else if (_config is StandardControllerInputConfig controllerConfig)
            {
                (float leftAxisX, float leftAxisY) = State.GetStick(StickInputId.Left);
                (float rightAxisX, float rightAxisY) = State.GetStick(StickInputId.Right);

                state.LStick = ApplyDeadzone(leftAxisX, leftAxisY, controllerConfig.DeadzoneLeft);
                state.RStick = ApplyDeadzone(rightAxisX, rightAxisY, controllerConfig.DeadzoneRight);
            }

            return state;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _gamepad?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
