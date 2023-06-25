using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Controller.Motion;
using Ryujinx.Common.Configuration.Hid.Keyboard;
using System;

namespace Ryujinx.Ava.UI.Models
{
    internal class InputConfiguration<TKey, TStick> : BaseModel
    {
        private float _deadzoneRight;
        private float _triggerThreshold;
        private float _deadzoneLeft;
        private double _gyroDeadzone;
        private int _sensitivity;
        private bool _enableMotion;
        private float _weakRumble;
        private float _strongRumble;
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

        public TStick LeftJoystick { get; set; }
        public bool LeftInvertStickX { get; set; }
        public bool LeftInvertStickY { get; set; }
        public bool RightRotate90 { get; set; }
        public TKey LeftControllerStickButton { get; set; }

        public TStick RightJoystick { get; set; }
        public bool RightInvertStickX { get; set; }
        public bool RightInvertStickY { get; set; }
        public bool LeftRotate90 { get; set; }
        public TKey RightControllerStickButton { get; set; }

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

        public TKey ButtonMinus { get; set; }
        public TKey ButtonL { get; set; }
        public TKey ButtonZl { get; set; }
        public TKey LeftButtonSl { get; set; }
        public TKey LeftButtonSr { get; set; }
        public TKey DpadUp { get; set; }
        public TKey DpadDown { get; set; }
        public TKey DpadLeft { get; set; }
        public TKey DpadRight { get; set; }

        public TKey ButtonPlus { get; set; }
        public TKey ButtonR { get; set; }
        public TKey ButtonZr { get; set; }
        public TKey RightButtonSl { get; set; }
        public TKey RightButtonSr { get; set; }
        public TKey ButtonX { get; set; }
        public TKey ButtonB { get; set; }
        public TKey ButtonY { get; set; }
        public TKey ButtonA { get; set; }


        public TKey LeftStickUp { get; set; }
        public TKey LeftStickDown { get; set; }
        public TKey LeftStickLeft { get; set; }
        public TKey LeftStickRight { get; set; }
        public TKey LeftKeyboardStickButton { get; set; }

        public TKey RightStickUp { get; set; }
        public TKey RightStickDown { get; set; }
        public TKey RightStickLeft { get; set; }
        public TKey RightStickRight { get; set; }
        public TKey RightKeyboardStickButton { get; set; }

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
            get => _enableMotion; set
            {
                _enableMotion = value;

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
            get => _weakRumble; set
            {
                _weakRumble = value;

                OnPropertyChanged();
            }
        }
        public float StrongRumble
        {
            get => _strongRumble; set
            {
                _strongRumble = value;

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
                    LeftStickUp = (TKey)(object)keyboardConfig.LeftJoyconStick.StickUp;
                    LeftStickDown = (TKey)(object)keyboardConfig.LeftJoyconStick.StickDown;
                    LeftStickLeft = (TKey)(object)keyboardConfig.LeftJoyconStick.StickLeft;
                    LeftStickRight = (TKey)(object)keyboardConfig.LeftJoyconStick.StickRight;
                    LeftKeyboardStickButton = (TKey)(object)keyboardConfig.LeftJoyconStick.StickButton;

                    RightStickUp = (TKey)(object)keyboardConfig.RightJoyconStick.StickUp;
                    RightStickDown = (TKey)(object)keyboardConfig.RightJoyconStick.StickDown;
                    RightStickLeft = (TKey)(object)keyboardConfig.RightJoyconStick.StickLeft;
                    RightStickRight = (TKey)(object)keyboardConfig.RightJoyconStick.StickRight;
                    RightKeyboardStickButton = (TKey)(object)keyboardConfig.RightJoyconStick.StickButton;

                    ButtonA = (TKey)(object)keyboardConfig.RightJoycon.ButtonA;
                    ButtonB = (TKey)(object)keyboardConfig.RightJoycon.ButtonB;
                    ButtonX = (TKey)(object)keyboardConfig.RightJoycon.ButtonX;
                    ButtonY = (TKey)(object)keyboardConfig.RightJoycon.ButtonY;
                    ButtonR = (TKey)(object)keyboardConfig.RightJoycon.ButtonR;
                    RightButtonSl = (TKey)(object)keyboardConfig.RightJoycon.ButtonSl;
                    RightButtonSr = (TKey)(object)keyboardConfig.RightJoycon.ButtonSr;
                    ButtonZr = (TKey)(object)keyboardConfig.RightJoycon.ButtonZr;
                    ButtonPlus = (TKey)(object)keyboardConfig.RightJoycon.ButtonPlus;

                    DpadUp = (TKey)(object)keyboardConfig.LeftJoycon.DpadUp;
                    DpadDown = (TKey)(object)keyboardConfig.LeftJoycon.DpadDown;
                    DpadLeft = (TKey)(object)keyboardConfig.LeftJoycon.DpadLeft;
                    DpadRight = (TKey)(object)keyboardConfig.LeftJoycon.DpadRight;
                    ButtonMinus = (TKey)(object)keyboardConfig.LeftJoycon.ButtonMinus;
                    LeftButtonSl = (TKey)(object)keyboardConfig.LeftJoycon.ButtonSl;
                    LeftButtonSr = (TKey)(object)keyboardConfig.LeftJoycon.ButtonSr;
                    ButtonZl = (TKey)(object)keyboardConfig.LeftJoycon.ButtonZl;
                    ButtonL = (TKey)(object)keyboardConfig.LeftJoycon.ButtonL;
                }
                else if (config is StandardControllerInputConfig controllerConfig)
                {
                    LeftJoystick = (TStick)(object)controllerConfig.LeftJoyconStick.Joystick;
                    LeftInvertStickX = controllerConfig.LeftJoyconStick.InvertStickX;
                    LeftInvertStickY = controllerConfig.LeftJoyconStick.InvertStickY;
                    LeftRotate90 = controllerConfig.LeftJoyconStick.Rotate90CW;
                    LeftControllerStickButton = (TKey)(object)controllerConfig.LeftJoyconStick.StickButton;

                    RightJoystick = (TStick)(object)controllerConfig.RightJoyconStick.Joystick;
                    RightInvertStickX = controllerConfig.RightJoyconStick.InvertStickX;
                    RightInvertStickY = controllerConfig.RightJoyconStick.InvertStickY;
                    RightRotate90 = controllerConfig.RightJoyconStick.Rotate90CW;
                    RightControllerStickButton = (TKey)(object)controllerConfig.RightJoyconStick.StickButton;

                    ButtonA = (TKey)(object)controllerConfig.RightJoycon.ButtonA;
                    ButtonB = (TKey)(object)controllerConfig.RightJoycon.ButtonB;
                    ButtonX = (TKey)(object)controllerConfig.RightJoycon.ButtonX;
                    ButtonY = (TKey)(object)controllerConfig.RightJoycon.ButtonY;
                    ButtonR = (TKey)(object)controllerConfig.RightJoycon.ButtonR;
                    RightButtonSl = (TKey)(object)controllerConfig.RightJoycon.ButtonSl;
                    RightButtonSr = (TKey)(object)controllerConfig.RightJoycon.ButtonSr;
                    ButtonZr = (TKey)(object)controllerConfig.RightJoycon.ButtonZr;
                    ButtonPlus = (TKey)(object)controllerConfig.RightJoycon.ButtonPlus;

                    DpadUp = (TKey)(object)controllerConfig.LeftJoycon.DpadUp;
                    DpadDown = (TKey)(object)controllerConfig.LeftJoycon.DpadDown;
                    DpadLeft = (TKey)(object)controllerConfig.LeftJoycon.DpadLeft;
                    DpadRight = (TKey)(object)controllerConfig.LeftJoycon.DpadRight;
                    ButtonMinus = (TKey)(object)controllerConfig.LeftJoycon.ButtonMinus;
                    LeftButtonSl = (TKey)(object)controllerConfig.LeftJoycon.ButtonSl;
                    LeftButtonSr = (TKey)(object)controllerConfig.LeftJoycon.ButtonSr;
                    ButtonZl = (TKey)(object)controllerConfig.LeftJoycon.ButtonZl;
                    ButtonL = (TKey)(object)controllerConfig.LeftJoycon.ButtonL;

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
                return new StandardKeyboardInputConfig()
                {
                    Id = Id,
                    Backend = Backend,
                    PlayerIndex = PlayerIndex,
                    ControllerType = ControllerType,
                    LeftJoycon = new LeftJoyconCommonConfig<Ryujinx.Common.Configuration.Hid.Key>()
                    {
                        DpadUp = (Ryujinx.Common.Configuration.Hid.Key)(object)DpadUp,
                        DpadDown = (Ryujinx.Common.Configuration.Hid.Key)(object)DpadDown,
                        DpadLeft = (Ryujinx.Common.Configuration.Hid.Key)(object)DpadLeft,
                        DpadRight = (Ryujinx.Common.Configuration.Hid.Key)(object)DpadRight,
                        ButtonL = (Ryujinx.Common.Configuration.Hid.Key)(object)ButtonL,
                        ButtonZl = (Ryujinx.Common.Configuration.Hid.Key)(object)ButtonZl,
                        ButtonSl = (Ryujinx.Common.Configuration.Hid.Key)(object)LeftButtonSl,
                        ButtonSr = (Ryujinx.Common.Configuration.Hid.Key)(object)LeftButtonSr,
                        ButtonMinus = (Ryujinx.Common.Configuration.Hid.Key)(object)ButtonMinus,
                    },
                    RightJoycon = new RightJoyconCommonConfig<Ryujinx.Common.Configuration.Hid.Key>()
                    {
                        ButtonA = (Ryujinx.Common.Configuration.Hid.Key)(object)ButtonA,
                        ButtonB = (Ryujinx.Common.Configuration.Hid.Key)(object)ButtonB,
                        ButtonX = (Ryujinx.Common.Configuration.Hid.Key)(object)ButtonX,
                        ButtonY = (Ryujinx.Common.Configuration.Hid.Key)(object)ButtonY,
                        ButtonPlus = (Ryujinx.Common.Configuration.Hid.Key)(object)ButtonPlus,
                        ButtonSl = (Ryujinx.Common.Configuration.Hid.Key)(object)RightButtonSl,
                        ButtonSr = (Ryujinx.Common.Configuration.Hid.Key)(object)RightButtonSr,
                        ButtonR = (Ryujinx.Common.Configuration.Hid.Key)(object)ButtonR,
                        ButtonZr = (Ryujinx.Common.Configuration.Hid.Key)(object)ButtonZr,
                    },
                    LeftJoyconStick = new JoyconConfigKeyboardStick<Ryujinx.Common.Configuration.Hid.Key>()
                    {
                        StickUp = (Ryujinx.Common.Configuration.Hid.Key)(object)LeftStickUp,
                        StickDown = (Ryujinx.Common.Configuration.Hid.Key)(object)LeftStickDown,
                        StickRight = (Ryujinx.Common.Configuration.Hid.Key)(object)LeftStickRight,
                        StickLeft = (Ryujinx.Common.Configuration.Hid.Key)(object)LeftStickLeft,
                        StickButton = (Ryujinx.Common.Configuration.Hid.Key)(object)LeftKeyboardStickButton,
                    },
                    RightJoyconStick = new JoyconConfigKeyboardStick<Ryujinx.Common.Configuration.Hid.Key>()
                    {
                        StickUp = (Ryujinx.Common.Configuration.Hid.Key)(object)RightStickUp,
                        StickDown = (Ryujinx.Common.Configuration.Hid.Key)(object)RightStickDown,
                        StickLeft = (Ryujinx.Common.Configuration.Hid.Key)(object)RightStickLeft,
                        StickRight = (Ryujinx.Common.Configuration.Hid.Key)(object)RightStickRight,
                        StickButton = (Ryujinx.Common.Configuration.Hid.Key)(object)RightKeyboardStickButton,
                    },
                    Version = InputConfig.CurrentVersion,
                };

            }
            else if (Backend == InputBackendType.GamepadSDL2)
            {
                var config = new StandardControllerInputConfig()
                {
                    Id = Id,
                    Backend = Backend,
                    PlayerIndex = PlayerIndex,
                    ControllerType = ControllerType,
                    LeftJoycon = new LeftJoyconCommonConfig<GamepadInputId>()
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
                    RightJoycon = new RightJoyconCommonConfig<GamepadInputId>()
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
                    LeftJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>()
                    {
                        Joystick = (StickInputId)(object)LeftJoystick,
                        InvertStickX = LeftInvertStickX,
                        InvertStickY = LeftInvertStickY,
                        Rotate90CW = LeftRotate90,
                        StickButton = (GamepadInputId)(object)LeftControllerStickButton,
                    },
                    RightJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>()
                    {
                        Joystick = (StickInputId)(object)RightJoystick,
                        InvertStickX = RightInvertStickX,
                        InvertStickY = RightInvertStickY,
                        Rotate90CW = RightRotate90,
                        StickButton = (GamepadInputId)(object)RightControllerStickButton,
                    },
                    Rumble = new RumbleConfigController()
                    {
                        EnableRumble = EnableRumble,
                        WeakRumble = WeakRumble,
                        StrongRumble = StrongRumble,
                    },
                    Version = InputConfig.CurrentVersion,
                    DeadzoneLeft = DeadzoneLeft,
                    DeadzoneRight = DeadzoneRight,
                    RangeLeft = RangeLeft,
                    RangeRight = RangeRight,
                    TriggerThreshold = TriggerThreshold,
                    Motion = EnableCemuHookMotion
                           ? new CemuHookMotionConfigController()
                           {
                               DsuServerHost = DsuServerHost,
                               DsuServerPort = DsuServerPort,
                               Slot = Slot,
                               AltSlot = AltSlot,
                               MirrorInput = MirrorInput,
                               MotionBackend = MotionInputBackendType.CemuHook,
                           }
                           : new StandardMotionConfigController()
                           {
                               MotionBackend = MotionInputBackendType.GamepadDriver,
                           },
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
