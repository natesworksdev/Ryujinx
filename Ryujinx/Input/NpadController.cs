using Ryujinx.Common;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.Modules.Motion;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

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

        private MotionInput _motionInput;

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

            _motionInput = new MotionInput();

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

                if (_config is StandardControllerInputConfig controllerConfig && controllerConfig.Motion.EnableMotion)
                {
                    if (controllerConfig.Motion.MotionBackend == MotionInputBackendType.GamepadDriver)
                    {
                        if (_gamepad.Features.HasFlag(GamepadFeaturesFlag.Motion))
                        {
                            Vector3 accelerometer = _gamepad.GetMotionData(MotionInputId.Accelerometer);
                            Vector3 gyroscope = _gamepad.GetMotionData(MotionInputId.Gyroscope);

                            accelerometer = new Vector3(accelerometer.X, -accelerometer.Z, accelerometer.Y);
                            gyroscope = new Vector3(gyroscope.X, gyroscope.Z, gyroscope.Y);

                            _motionInput.Update(accelerometer, gyroscope, (ulong)PerformanceCounter.ElapsedNanoseconds / 1000, controllerConfig.Motion.Sensitivity, (float)controllerConfig.Motion.GyroDeadzone);
                        }
                    }
                    else if (controllerConfig.Motion.MotionBackend == MotionInputBackendType.CemuHooks)
                    {
                        // TODO
                        throw new NotImplementedException();
                    }
                }
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

        public GamepadInput GetHLEInputState()
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

        public SixAxisInput GetHLEMotionState()
        {
            float[] orientationForHLE = new float[9];
            Vector3 gyroscope;
            Vector3 accelerometer;
            Vector3 rotation;

            if (_gamepad.Features.HasFlag(GamepadFeaturesFlag.Motion))
            {
                gyroscope = Truncate(_motionInput.Gyroscrope * 0.0027f, 3);
                accelerometer = Truncate(_motionInput.Accelerometer, 3);
                rotation = Truncate(_motionInput.Rotation * 0.0027f, 3);

                Matrix4x4 orientation = _motionInput.GetOrientation();

                orientationForHLE[0] = Math.Clamp(orientation.M11, -1f, 1f);
                orientationForHLE[1] = Math.Clamp(orientation.M12, -1f, 1f);
                orientationForHLE[2] = Math.Clamp(orientation.M13, -1f, 1f);
                orientationForHLE[3] = Math.Clamp(orientation.M21, -1f, 1f);
                orientationForHLE[4] = Math.Clamp(orientation.M22, -1f, 1f);
                orientationForHLE[5] = Math.Clamp(orientation.M23, -1f, 1f);
                orientationForHLE[6] = Math.Clamp(orientation.M31, -1f, 1f);
                orientationForHLE[7] = Math.Clamp(orientation.M32, -1f, 1f);
                orientationForHLE[8] = Math.Clamp(orientation.M33, -1f, 1f);
            }
            else
            {
                gyroscope = new Vector3();
                accelerometer = new Vector3();
                rotation = new Vector3();
            }

            return new SixAxisInput()
            {
                Accelerometer = accelerometer,
                Gyroscope     = gyroscope,
                Rotation      = rotation,
                Orientation   = orientationForHLE
            };
        }

        private static Vector3 Truncate(Vector3 value, int decimals)
        {
            float power = MathF.Pow(10, decimals);

            value.X = float.IsNegative(value.X) ? MathF.Ceiling(value.X * power) / power : MathF.Floor(value.X * power) / power;
            value.Y = float.IsNegative(value.Y) ? MathF.Ceiling(value.Y * power) / power : MathF.Floor(value.Y * power) / power;
            value.Z = float.IsNegative(value.Z) ? MathF.Ceiling(value.Z * power) / power : MathF.Floor(value.Z * power) / power;

            return value;
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
