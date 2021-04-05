using Ryujinx.Common;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Controller.Motion;
using Ryujinx.HLE.HOS.Services.Hid;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using CemuHookClient = Ryujinx.Input.Motion.CemuHook.Client;
using ConfigControllerType = Ryujinx.Common.Configuration.Hid.ControllerType;

namespace Ryujinx.Input.HLE
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

        class HLEKeyboardMappingEntry
        {
            public Key TargetKey;
            public byte Target;
        }

        private static readonly HLEKeyboardMappingEntry[] KeyMapping = new HLEKeyboardMappingEntry[]
        {
            new HLEKeyboardMappingEntry { TargetKey = Key.A, Target = 0x4  },
            new HLEKeyboardMappingEntry { TargetKey = Key.B, Target = 0x5  },
            new HLEKeyboardMappingEntry { TargetKey = Key.C, Target = 0x6  },
            new HLEKeyboardMappingEntry { TargetKey = Key.D, Target = 0x7  },
            new HLEKeyboardMappingEntry { TargetKey = Key.E, Target = 0x8  },
            new HLEKeyboardMappingEntry { TargetKey = Key.F, Target = 0x9  },
            new HLEKeyboardMappingEntry { TargetKey = Key.G, Target = 0xA  },
            new HLEKeyboardMappingEntry { TargetKey = Key.H, Target = 0xB  },
            new HLEKeyboardMappingEntry { TargetKey = Key.I, Target = 0xC  },
            new HLEKeyboardMappingEntry { TargetKey = Key.J, Target = 0xD  },
            new HLEKeyboardMappingEntry { TargetKey = Key.K, Target = 0xE  },
            new HLEKeyboardMappingEntry { TargetKey = Key.L, Target = 0xF  },
            new HLEKeyboardMappingEntry { TargetKey = Key.M, Target = 0x10 },
            new HLEKeyboardMappingEntry { TargetKey = Key.N, Target = 0x11 },
            new HLEKeyboardMappingEntry { TargetKey = Key.O, Target = 0x12 },
            new HLEKeyboardMappingEntry { TargetKey = Key.P, Target = 0x13 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Q, Target = 0x14 },
            new HLEKeyboardMappingEntry { TargetKey = Key.R, Target = 0x15 },
            new HLEKeyboardMappingEntry { TargetKey = Key.S, Target = 0x16 },
            new HLEKeyboardMappingEntry { TargetKey = Key.T, Target = 0x17 },
            new HLEKeyboardMappingEntry { TargetKey = Key.U, Target = 0x18 },
            new HLEKeyboardMappingEntry { TargetKey = Key.V, Target = 0x19 },
            new HLEKeyboardMappingEntry { TargetKey = Key.W, Target = 0x1A },
            new HLEKeyboardMappingEntry { TargetKey = Key.X, Target = 0x1B },
            new HLEKeyboardMappingEntry { TargetKey = Key.Y, Target = 0x1C },
            new HLEKeyboardMappingEntry { TargetKey = Key.Z, Target = 0x1D },

            new HLEKeyboardMappingEntry { TargetKey = Key.Number1, Target = 0x1E },
            new HLEKeyboardMappingEntry { TargetKey = Key.Number2, Target = 0x1F },
            new HLEKeyboardMappingEntry { TargetKey = Key.Number3, Target = 0x20 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Number4, Target = 0x21 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Number5, Target = 0x22 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Number6, Target = 0x23 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Number7, Target = 0x24 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Number8, Target = 0x25 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Number9, Target = 0x26 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Number0, Target = 0x27 },

            new HLEKeyboardMappingEntry { TargetKey = Key.Enter,        Target = 0x28 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Escape,       Target = 0x29 },
            new HLEKeyboardMappingEntry { TargetKey = Key.BackSpace,    Target = 0x2A },
            new HLEKeyboardMappingEntry { TargetKey = Key.Tab,          Target = 0x2B },
            new HLEKeyboardMappingEntry { TargetKey = Key.Space,        Target = 0x2C },
            new HLEKeyboardMappingEntry { TargetKey = Key.Minus,        Target = 0x2D },
            new HLEKeyboardMappingEntry { TargetKey = Key.Plus,         Target = 0x2E },
            new HLEKeyboardMappingEntry { TargetKey = Key.BracketLeft,  Target = 0x2F },
            new HLEKeyboardMappingEntry { TargetKey = Key.BracketRight, Target = 0x30 },
            new HLEKeyboardMappingEntry { TargetKey = Key.BackSlash,    Target = 0x31 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Tilde,        Target = 0x32 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Semicolon,    Target = 0x33 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Quote,        Target = 0x34 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Grave,        Target = 0x35 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Comma,        Target = 0x36 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Period,       Target = 0x37 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Slash,        Target = 0x38 },
            new HLEKeyboardMappingEntry { TargetKey = Key.CapsLock,     Target = 0x39 },

            new HLEKeyboardMappingEntry { TargetKey = Key.F1,  Target = 0x3a },
            new HLEKeyboardMappingEntry { TargetKey = Key.F2,  Target = 0x3b },
            new HLEKeyboardMappingEntry { TargetKey = Key.F3,  Target = 0x3c },
            new HLEKeyboardMappingEntry { TargetKey = Key.F4,  Target = 0x3d },
            new HLEKeyboardMappingEntry { TargetKey = Key.F5,  Target = 0x3e },
            new HLEKeyboardMappingEntry { TargetKey = Key.F6,  Target = 0x3f },
            new HLEKeyboardMappingEntry { TargetKey = Key.F7,  Target = 0x40 },
            new HLEKeyboardMappingEntry { TargetKey = Key.F8,  Target = 0x41 },
            new HLEKeyboardMappingEntry { TargetKey = Key.F9,  Target = 0x42 },
            new HLEKeyboardMappingEntry { TargetKey = Key.F10, Target = 0x43 },
            new HLEKeyboardMappingEntry { TargetKey = Key.F11, Target = 0x44 },
            new HLEKeyboardMappingEntry { TargetKey = Key.F12, Target = 0x45 },

            new HLEKeyboardMappingEntry { TargetKey = Key.PrintScreen, Target = 0x46 },
            new HLEKeyboardMappingEntry { TargetKey = Key.ScrollLock,  Target = 0x47 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Pause,       Target = 0x48 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Insert,      Target = 0x49 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Home,        Target = 0x4A },
            new HLEKeyboardMappingEntry { TargetKey = Key.PageUp,      Target = 0x4B },
            new HLEKeyboardMappingEntry { TargetKey = Key.Delete,      Target = 0x4C },
            new HLEKeyboardMappingEntry { TargetKey = Key.End,         Target = 0x4D },
            new HLEKeyboardMappingEntry { TargetKey = Key.PageDown,    Target = 0x4E },
            new HLEKeyboardMappingEntry { TargetKey = Key.Right,       Target = 0x4F },
            new HLEKeyboardMappingEntry { TargetKey = Key.Left,        Target = 0x50 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Down,        Target = 0x51 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Up,          Target = 0x52 },

            new HLEKeyboardMappingEntry { TargetKey = Key.NumLock,        Target = 0x53 },
            new HLEKeyboardMappingEntry { TargetKey = Key.KeypadDivide,   Target = 0x54 },
            new HLEKeyboardMappingEntry { TargetKey = Key.KeypadMultiply, Target = 0x55 },
            new HLEKeyboardMappingEntry { TargetKey = Key.KeypadSubtract, Target = 0x56 },
            new HLEKeyboardMappingEntry { TargetKey = Key.KeypadAdd,      Target = 0x57 },
            new HLEKeyboardMappingEntry { TargetKey = Key.KeypadEnter,    Target = 0x58 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Keypad1,        Target = 0x59 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Keypad2,        Target = 0x5A },
            new HLEKeyboardMappingEntry { TargetKey = Key.Keypad3,        Target = 0x5B },
            new HLEKeyboardMappingEntry { TargetKey = Key.Keypad4,        Target = 0x5C },
            new HLEKeyboardMappingEntry { TargetKey = Key.Keypad5,        Target = 0x5D },
            new HLEKeyboardMappingEntry { TargetKey = Key.Keypad6,        Target = 0x5E },
            new HLEKeyboardMappingEntry { TargetKey = Key.Keypad7,        Target = 0x5F },
            new HLEKeyboardMappingEntry { TargetKey = Key.Keypad8,        Target = 0x60 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Keypad9,        Target = 0x61 },
            new HLEKeyboardMappingEntry { TargetKey = Key.Keypad0,        Target = 0x62 },
            new HLEKeyboardMappingEntry { TargetKey = Key.KeypadDecimal,  Target = 0x63 },

            new HLEKeyboardMappingEntry { TargetKey = Key.F13, Target = 0x68 },
            new HLEKeyboardMappingEntry { TargetKey = Key.F14, Target = 0x69 },
            new HLEKeyboardMappingEntry { TargetKey = Key.F15, Target = 0x6A },
            new HLEKeyboardMappingEntry { TargetKey = Key.F16, Target = 0x6B },
            new HLEKeyboardMappingEntry { TargetKey = Key.F17, Target = 0x6C },
            new HLEKeyboardMappingEntry { TargetKey = Key.F18, Target = 0x6D },
            new HLEKeyboardMappingEntry { TargetKey = Key.F19, Target = 0x6E },
            new HLEKeyboardMappingEntry { TargetKey = Key.F20, Target = 0x6F },
            new HLEKeyboardMappingEntry { TargetKey = Key.F21, Target = 0x70 },
            new HLEKeyboardMappingEntry { TargetKey = Key.F22, Target = 0x71 },
            new HLEKeyboardMappingEntry { TargetKey = Key.F23, Target = 0x72 },
            new HLEKeyboardMappingEntry { TargetKey = Key.F24, Target = 0x73 },

            new HLEKeyboardMappingEntry { TargetKey = Key.ControlLeft,  Target = 0xE0 },
            new HLEKeyboardMappingEntry { TargetKey = Key.ShiftLeft,    Target = 0xE1 },
            new HLEKeyboardMappingEntry { TargetKey = Key.AltLeft,      Target = 0xE2 },
            new HLEKeyboardMappingEntry { TargetKey = Key.WinLeft,      Target = 0xE3 },
            new HLEKeyboardMappingEntry { TargetKey = Key.ControlRight, Target = 0xE4 },
            new HLEKeyboardMappingEntry { TargetKey = Key.ShiftRight,   Target = 0xE5 },
            new HLEKeyboardMappingEntry { TargetKey = Key.AltRight,     Target = 0xE6 },
            new HLEKeyboardMappingEntry { TargetKey = Key.WinRight,     Target = 0xE7 },
        };

        private static readonly HLEKeyboardMappingEntry[] KeyModifierMapping = new HLEKeyboardMappingEntry[]
        {
            new HLEKeyboardMappingEntry { TargetKey = Key.ControlLeft,  Target = 0 },
            new HLEKeyboardMappingEntry { TargetKey = Key.ShiftLeft,    Target = 1 },
            new HLEKeyboardMappingEntry { TargetKey = Key.AltLeft,      Target = 2 },
            new HLEKeyboardMappingEntry { TargetKey = Key.WinLeft,      Target = 3 },
            new HLEKeyboardMappingEntry { TargetKey = Key.ControlRight, Target = 4 },
            new HLEKeyboardMappingEntry { TargetKey = Key.ShiftRight,   Target = 5 },
            new HLEKeyboardMappingEntry { TargetKey = Key.AltRight,     Target = 6 },
            new HLEKeyboardMappingEntry { TargetKey = Key.WinRight,     Target = 7 },
            new HLEKeyboardMappingEntry { TargetKey = Key.CapsLock,     Target = 8 },
            new HLEKeyboardMappingEntry { TargetKey = Key.ScrollLock,   Target = 9 },
            new HLEKeyboardMappingEntry { TargetKey = Key.NumLock,      Target = 10 },
        };

        private bool _isValid;
        private string _id;

        private MotionInput _motionInput;

        private IGamepad _gamepad;
        private InputConfig _config;

        public IGamepadDriver GamepadDriver { get; private set; }
        public GamepadStateSnapshot State { get; private set; }

        public string Id => _id;

        private CemuHookClient _cemuHookClient;

        public NpadController(CemuHookClient cemuHookClient)
        {
            State = default;
            _id = null;
            _isValid = false;
            _cemuHookClient = cemuHookClient;
        }

        public bool UpdateDriverConfiguration(IGamepadDriver gamepadDriver, InputConfig config)
        {
            GamepadDriver = gamepadDriver;

            _gamepad?.Dispose();

            _id = config.Id;
            _gamepad = GamepadDriver.GetGamepad(_id);
            _isValid = _gamepad != null;

            UpdateUserConfiguration(config);

            return _isValid;
        }

        public void UpdateUserConfiguration(InputConfig config)
        {
            if (config is StandardControllerInputConfig controllerConfig)
            {
                bool needMotionInputUpdate = _config == null || (_config is StandardControllerInputConfig oldControllerConfig &&
                                                                (oldControllerConfig.Motion.EnableMotion != controllerConfig.Motion.EnableMotion) &&
                                                                (oldControllerConfig.Motion.MotionBackend != controllerConfig.Motion.MotionBackend));

                if (needMotionInputUpdate)
                {
                    UpdateMotionInput(controllerConfig.Motion);
                }
            }
            else
            {
                // non controller doesn't have motions.
                _motionInput = null;
            }

            _config = config;

            if (_isValid)
            {
                _gamepad.SetConfiguration(config);
            }
        }

        private void UpdateMotionInput(MotionConfigController motionConfig)
        {
            if (motionConfig.MotionBackend != MotionInputBackendType.CemuHook)
            {
                _motionInput = new MotionInput();
             }
            else
            {
                _motionInput = null;
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
                    else if (controllerConfig.Motion.MotionBackend == MotionInputBackendType.CemuHook && controllerConfig.Motion is CemuHookMotionConfigController cemuControllerConfig)
                    {
                        int clientId = (int)controllerConfig.PlayerIndex;

                        // First of all ensure we are registered
                        _cemuHookClient.RegisterClient(clientId, cemuControllerConfig.DsuServerHost, cemuControllerConfig.DsuServerPort);

                        // Then request data
                        _cemuHookClient.RequestData(clientId, cemuControllerConfig.Slot);

                        if (controllerConfig.ControllerType == ConfigControllerType.JoyconPair && !cemuControllerConfig.MirrorInput)
                        {
                            _cemuHookClient.RequestData(clientId, cemuControllerConfig.AltSlot);
                        }

                        // Finally, get motion input data
                        _cemuHookClient.TryGetData(clientId, cemuControllerConfig.Slot, out _motionInput);
                    }
                }
            }
            else
            {
                // Reset states
                State = default;
                _motionInput = null;
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

            if (_motionInput != null)
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

        public KeyboardInput? GetHLEKeyboardInput()
        {
            if (_gamepad is IKeyboard keyboard)
            {
                KeyboardStateSnapshot keyboardState = keyboard.GetKeyboardStateSnapshot();

                KeyboardInput hidKeyboard = new KeyboardInput
                {
                    Modifier = 0,
                    Keys = new int[0x8]
                };

                foreach (HLEKeyboardMappingEntry entry in KeyMapping)
                {
                    int value = keyboardState.IsPressed(entry.TargetKey) ? 1 : 0;

                    hidKeyboard.Keys[entry.Target / 0x20] |= (value << (entry.Target % 0x20));
                }

                foreach (HLEKeyboardMappingEntry entry in KeyModifierMapping)
                {
                    int value = keyboardState.IsPressed(entry.TargetKey) ? 1 : 0;

                    hidKeyboard.Modifier |= value << entry.Target;
                }

                return hidKeyboard;
            }

            return null;
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
