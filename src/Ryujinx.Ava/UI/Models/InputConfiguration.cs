using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Controller.Motion;
using Ryujinx.Common.Configuration.Hid.Keyboard;
using System;

namespace Ryujinx.Ava.UI.Models
{
    public class InputConfiguration : BaseModel
    {
        private float _deadzoneRight;
        private float _triggerThreshold;
        private float _deadzoneLeft;
        private double _gyroDeadzone;
        private int _sensitivity;
        private bool enableMotion;
        private float weakRumble;
        private float strongRumble;
        private float _rangeLeft;
        private float _rangeRight;

        public InputBackendType Backend { get; set; }

        /// <summary>
        /// Controller id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///  Controller's Type
        /// </summary>
        public ControllerType ControllerType { get; set; }

        /// <summary>
        ///  Player's Index for the controller
        /// </summary>
        public PlayerIndex PlayerIndex { get; set; }

        public StickInputId LeftJoystick { get; set; }
        public bool LeftInvertStickX { get; set; }
        public bool LeftInvertStickY { get; set; }
        public bool RightRotate90 { get; set; }
        public Key LeftControllerStickButton { get; set; }

        public StickInputId RightJoystick { get; set; }
        public bool RightInvertStickX { get; set; }
        public bool RightInvertStickY { get; set; }
        public bool LeftRotate90 { get; set; }
        public Key RightControllerStickButton { get; set; }

        public float DeadzoneLeft
        {
            get => _deadzoneLeft;
            set
            {
                _deadzoneLeft = MathF.Round(value, 3);

                OnPropertyChanged();
            }
        }

        public float RangeLeft
        {
            get => _rangeLeft;
            set
            {
                _rangeLeft = MathF.Round(value, 3);

                OnPropertyChanged();
            }
        }

        public float DeadzoneRight
        {
            get => _deadzoneRight;
            set
            {
                _deadzoneRight = MathF.Round(value, 3);

                OnPropertyChanged();
            }
        }

        public float RangeRight
        {
            get => _rangeRight;
            set
            {
                _rangeRight = MathF.Round(value, 3);

                OnPropertyChanged();
            }
        }

        public float TriggerThreshold
        {
            get => _triggerThreshold;
            set
            {
                _triggerThreshold = MathF.Round(value, 3);

                OnPropertyChanged();
            }
        }

        public MotionInputBackendType MotionBackend { get; set; }

        public Key ButtonMinus { get; set; }
        public Key ButtonL { get; set; }
        public Key ButtonZl { get; set; }
        public Key LeftButtonSl { get; set; }
        public Key LeftButtonSr { get; set; }
        public Key DpadUp { get; set; }
        public Key DpadDown { get; set; }
        public Key DpadLeft { get; set; }
        public Key DpadRight { get; set; }

        public Key ButtonPlus { get; set; }
        public Key ButtonR { get; set; }
        public Key ButtonZr { get; set; }
        public Key RightButtonSl { get; set; }
        public Key RightButtonSr { get; set; }
        public Key ButtonX { get; set; }
        public Key ButtonB { get; set; }
        public Key ButtonY { get; set; }
        public Key ButtonA { get; set; }


        public Key LeftStickUp { get; set; }
        public Key LeftStickDown { get; set; }
        public Key LeftStickLeft { get; set; }
        public Key LeftStickRight { get; set; }
        public Key LeftKeyboardStickButton { get; set; }

        public Key RightStickUp { get; set; }
        public Key RightStickDown { get; set; }
        public Key RightStickLeft { get; set; }
        public Key RightStickRight { get; set; }
        public Key RightKeyboardStickButton { get; set; }

        public int Sensitivity
        {
            get => _sensitivity;
            set
            {
                _sensitivity = value;

                OnPropertyChanged();
            }
        }

        public double GyroDeadzone
        {
            get => _gyroDeadzone;
            set
            {
                _gyroDeadzone = Math.Round(value, 3);

                OnPropertyChanged();
            }
        }

        public bool EnableMotion
        {
            get => enableMotion; set
            {
                enableMotion = value;

                OnPropertyChanged();
            }
        }

        public bool EnableCemuHookMotion { get; set; }
        public int Slot { get; set; }
        public int AltSlot { get; set; }
        public bool MirrorInput { get; set; }
        public string DsuServerHost { get; set; }
        public int DsuServerPort { get; set; }

        public bool EnableRumble { get; set; }
        public float WeakRumble
        {
            get => weakRumble; set
            {
                weakRumble = value;

                OnPropertyChanged();
            }
        }
        public float StrongRumble
        {
            get => strongRumble; set
            {
                strongRumble = value;

                OnPropertyChanged();
            }
        }

        public InputConfiguration(InputConfig config)
        {
            if (config != null)
            {
                Backend = config.Backend;
                Id = config.Id;
                ControllerType = config.ControllerType;
                PlayerIndex = config.PlayerIndex;

                if (config is StandardKeyboardInputConfig keyboardConfig)
                {
                    LeftStickUp = keyboardConfig.LeftJoyconStick.StickUp;
                    LeftStickDown = keyboardConfig.LeftJoyconStick.StickDown;
                    LeftStickLeft = keyboardConfig.LeftJoyconStick.StickLeft;
                    LeftStickRight = keyboardConfig.LeftJoyconStick.StickRight;
                    LeftKeyboardStickButton = keyboardConfig.LeftJoyconStick.StickButton;

                    RightStickUp = keyboardConfig.RightJoyconStick.StickUp;
                    RightStickDown = keyboardConfig.RightJoyconStick.StickDown;
                    RightStickLeft = keyboardConfig.RightJoyconStick.StickLeft;
                    RightStickRight = keyboardConfig.RightJoyconStick.StickRight;
                    RightKeyboardStickButton = keyboardConfig.RightJoyconStick.StickButton;

                    ButtonA = keyboardConfig.RightJoycon.ButtonA;
                    ButtonB = keyboardConfig.RightJoycon.ButtonB;
                    ButtonX = keyboardConfig.RightJoycon.ButtonX;
                    ButtonY = keyboardConfig.RightJoycon.ButtonY;
                    ButtonR = keyboardConfig.RightJoycon.ButtonR;
                    RightButtonSl = keyboardConfig.RightJoycon.ButtonSl;
                    RightButtonSr = keyboardConfig.RightJoycon.ButtonSr;
                    ButtonZr = keyboardConfig.RightJoycon.ButtonZr;
                    ButtonPlus = keyboardConfig.RightJoycon.ButtonPlus;

                    DpadUp = keyboardConfig.LeftJoycon.DpadUp;
                    DpadDown = keyboardConfig.LeftJoycon.DpadDown;
                    DpadLeft = keyboardConfig.LeftJoycon.DpadLeft;
                    DpadRight = keyboardConfig.LeftJoycon.DpadRight;
                    ButtonMinus = keyboardConfig.LeftJoycon.ButtonMinus;
                    LeftButtonSl = keyboardConfig.LeftJoycon.ButtonSl;
                    LeftButtonSr = keyboardConfig.LeftJoycon.ButtonSr;
                    ButtonZl = keyboardConfig.LeftJoycon.ButtonZl;
                    ButtonL = keyboardConfig.LeftJoycon.ButtonL;
                }
                else if (config is StandardControllerInputConfig controllerConfig)
                {
                    LeftJoystick = controllerConfig.LeftJoyconStick.Joystick;
                    LeftInvertStickX = controllerConfig.LeftJoyconStick.InvertStickX;
                    LeftInvertStickY = controllerConfig.LeftJoyconStick.InvertStickY;
                    LeftRotate90 = controllerConfig.LeftJoyconStick.Rotate90CW;
                    LeftControllerStickButton = (Key)(object)controllerConfig.LeftJoyconStick.StickButton;

                    RightJoystick = controllerConfig.RightJoyconStick.Joystick;
                    RightInvertStickX = controllerConfig.RightJoyconStick.InvertStickX;
                    RightInvertStickY = controllerConfig.RightJoyconStick.InvertStickY;
                    RightRotate90 = controllerConfig.RightJoyconStick.Rotate90CW;
                    RightControllerStickButton = (Key)(object)controllerConfig.RightJoyconStick.StickButton;

                    ButtonA = (Key)(object)controllerConfig.RightJoycon.ButtonA;
                    ButtonB = (Key)(object)controllerConfig.RightJoycon.ButtonB;
                    ButtonX = (Key)(object)controllerConfig.RightJoycon.ButtonX;
                    ButtonY = (Key)(object)controllerConfig.RightJoycon.ButtonY;
                    ButtonR = (Key)(object)controllerConfig.RightJoycon.ButtonR;
                    RightButtonSl = (Key)(object)controllerConfig.RightJoycon.ButtonSl;
                    RightButtonSr = (Key)(object)controllerConfig.RightJoycon.ButtonSr;
                    ButtonZr = (Key)(object)controllerConfig.RightJoycon.ButtonZr;
                    ButtonPlus = (Key)(object)controllerConfig.RightJoycon.ButtonPlus;

                    DpadUp = (Key)(object)controllerConfig.LeftJoycon.DpadUp;
                    DpadDown = (Key)(object)controllerConfig.LeftJoycon.DpadDown;
                    DpadLeft = (Key)(object)controllerConfig.LeftJoycon.DpadLeft;
                    DpadRight = (Key)(object)controllerConfig.LeftJoycon.DpadRight;
                    ButtonMinus = (Key)(object)controllerConfig.LeftJoycon.ButtonMinus;
                    LeftButtonSl = (Key)(object)controllerConfig.LeftJoycon.ButtonSl;
                    LeftButtonSr = (Key)(object)controllerConfig.LeftJoycon.ButtonSr;
                    ButtonZl = (Key)(object)controllerConfig.LeftJoycon.ButtonZl;
                    ButtonL = (Key)(object)controllerConfig.LeftJoycon.ButtonL;

                    DeadzoneLeft = controllerConfig.DeadzoneLeft;
                    DeadzoneRight = controllerConfig.DeadzoneRight;
                    RangeLeft = controllerConfig.RangeLeft;
                    RangeRight = controllerConfig.RangeRight;
                    TriggerThreshold = controllerConfig.TriggerThreshold;

                    if (controllerConfig.Motion != null)
                    {
                        EnableMotion = controllerConfig.Motion.EnableMotion;
                        MotionBackend = controllerConfig.Motion.MotionBackend;
                        GyroDeadzone = controllerConfig.Motion.GyroDeadzone;
                        Sensitivity = controllerConfig.Motion.Sensitivity;

                        if (controllerConfig.Motion is CemuHookMotionConfigController cemuHook)
                        {
                            EnableCemuHookMotion = true;
                            DsuServerHost = cemuHook.DsuServerHost;
                            DsuServerPort = cemuHook.DsuServerPort;
                            Slot = cemuHook.Slot;
                            AltSlot = cemuHook.AltSlot;
                            MirrorInput = cemuHook.MirrorInput;
                        }

                        if (controllerConfig.Rumble != null)
                        {
                            EnableRumble = controllerConfig.Rumble.EnableRumble;
                            WeakRumble = controllerConfig.Rumble.WeakRumble;
                            StrongRumble = controllerConfig.Rumble.StrongRumble;
                        }
                    }
                }
            }
        }

        public InputConfiguration()
        {
        }

        public InputConfig GetConfig()
        {
            if (Backend == InputBackendType.WindowKeyboard)
            {
                return new StandardKeyboardInputConfig
                {
                    Id = Id,
                    Backend = Backend,
                    PlayerIndex = PlayerIndex,
                    ControllerType = ControllerType,
                    LeftJoycon = new LeftJoyconCommonConfig<Key>
                    {
                        DpadUp = DpadUp,
                        DpadDown = DpadDown,
                        DpadLeft = DpadLeft,
                        DpadRight = DpadRight,
                        ButtonL = ButtonL,
                        ButtonZl = ButtonZl,
                        ButtonSl = LeftButtonSl,
                        ButtonSr = LeftButtonSr,
                        ButtonMinus = ButtonMinus
                    },
                    RightJoycon = new RightJoyconCommonConfig<Key>
                    {
                        ButtonA = ButtonA,
                        ButtonB = ButtonB,
                        ButtonX = ButtonX,
                        ButtonY = ButtonY,
                        ButtonPlus = ButtonPlus,
                        ButtonSl = RightButtonSl,
                        ButtonSr = RightButtonSr,
                        ButtonR = ButtonR,
                        ButtonZr = ButtonZr
                    },
                    LeftJoyconStick = new JoyconConfigKeyboardStick<Key>
                    {
                        StickUp = LeftStickUp,
                        StickDown = LeftStickDown,
                        StickRight = LeftStickRight,
                        StickLeft = LeftStickLeft,
                        StickButton = LeftKeyboardStickButton
                    },
                    RightJoyconStick = new JoyconConfigKeyboardStick<Key>
                    {
                        StickUp = RightStickUp,
                        StickDown = RightStickDown,
                        StickLeft = RightStickLeft,
                        StickRight = RightStickRight,
                        StickButton = RightKeyboardStickButton
                    },
                    Version = InputConfig.CurrentVersion
                };

            }
            else if (Backend == InputBackendType.GamepadSDL2)
            {
                var config = new StandardControllerInputConfig
                {
                    Id = Id,
                    Backend = Backend,
                    PlayerIndex = PlayerIndex,
                    ControllerType = ControllerType,
                    LeftJoycon = new LeftJoyconCommonConfig<GamepadInputId>
                    {
                        DpadUp = (GamepadInputId)(object)DpadUp,
                        DpadDown = (GamepadInputId)(object)DpadDown,
                        DpadLeft = (GamepadInputId)(object)DpadLeft,
                        DpadRight = (GamepadInputId)(object)DpadRight,
                        ButtonL = (GamepadInputId)(object)ButtonL,
                        ButtonZl = (GamepadInputId)(object)ButtonZl,
                        ButtonSl = (GamepadInputId)(object)LeftButtonSl,
                        ButtonSr = (GamepadInputId)(object)LeftButtonSr,
                        ButtonMinus = (GamepadInputId)(object)ButtonMinus,
                    },
                    RightJoycon = new RightJoyconCommonConfig<GamepadInputId>
                    {
                        ButtonA = (GamepadInputId)(object)ButtonA,
                        ButtonB = (GamepadInputId)(object)ButtonB,
                        ButtonX = (GamepadInputId)(object)ButtonX,
                        ButtonY = (GamepadInputId)(object)ButtonY,
                        ButtonPlus = (GamepadInputId)(object)ButtonPlus,
                        ButtonSl = (GamepadInputId)(object)RightButtonSl,
                        ButtonSr = (GamepadInputId)(object)RightButtonSr,
                        ButtonR = (GamepadInputId)(object)ButtonR,
                        ButtonZr = (GamepadInputId)(object)ButtonZr,
                    },
                    LeftJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>
                    {
                        Joystick = LeftJoystick,
                        InvertStickX = LeftInvertStickX,
                        InvertStickY = LeftInvertStickY,
                        Rotate90CW = LeftRotate90,
                        StickButton = (GamepadInputId)(object)LeftControllerStickButton,
                    },
                    RightJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>
                    {
                        Joystick = RightJoystick,
                        InvertStickX = RightInvertStickX,
                        InvertStickY = RightInvertStickY,
                        Rotate90CW = RightRotate90,
                        StickButton = (GamepadInputId)(object)RightControllerStickButton,
                    },
                    Rumble = new RumbleConfigController
                    {
                        EnableRumble = EnableRumble,
                        WeakRumble = WeakRumble,
                        StrongRumble = StrongRumble
                    },
                    Version = InputConfig.CurrentVersion,
                    DeadzoneLeft = DeadzoneLeft,
                    DeadzoneRight = DeadzoneRight,
                    RangeLeft = RangeLeft,
                    RangeRight = RangeRight,
                    TriggerThreshold = TriggerThreshold,
                    Motion = EnableCemuHookMotion
                           ? new CemuHookMotionConfigController
                           {
                               DsuServerHost = DsuServerHost,
                               DsuServerPort = DsuServerPort,
                               Slot = Slot,
                               AltSlot = AltSlot,
                               MirrorInput = MirrorInput,
                               MotionBackend = MotionInputBackendType.CemuHook
                           }
                           : new StandardMotionConfigController
                           {
                               MotionBackend = MotionInputBackendType.GamepadDriver
                           }
                };

                config.Motion.Sensitivity = Sensitivity;
                config.Motion.EnableMotion = EnableMotion;
                config.Motion.GyroDeadzone = GyroDeadzone;

                return config;
            }

            return null;
        }
    }
}