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
        public InputModel LeftControllerStickButton { get; set; }

        public StickInputId RightJoystick { get; set; }
        public bool RightInvertStickX { get; set; }
        public bool RightInvertStickY { get; set; }
        public bool LeftRotate90 { get; set; }
        public InputModel RightControllerStickButton { get; set; }

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

        public InputModel ButtonMinus { get; set; }
        public InputModel ButtonL { get; set; }
        public InputModel ButtonZl { get; set; }
        public InputModel LeftButtonSl { get; set; }
        public InputModel LeftButtonSr { get; set; }
        public InputModel DpadUp { get; set; }
        public InputModel DpadDown { get; set; }
        public InputModel DpadLeft { get; set; }
        public InputModel DpadRight { get; set; }

        public InputModel ButtonPlus { get; set; }
        public InputModel ButtonR { get; set; }
        public InputModel ButtonZr { get; set; }
        public InputModel RightButtonSl { get; set; }
        public InputModel RightButtonSr { get; set; }
        public InputModel ButtonX { get; set; }
        public InputModel ButtonB { get; set; }
        public InputModel ButtonY { get; set; }
        public InputModel ButtonA { get; set; }


        public InputModel LeftStickUp { get; set; }
        public InputModel LeftStickDown { get; set; }
        public InputModel LeftStickLeft { get; set; }
        public InputModel LeftStickRight { get; set; }
        public InputModel LeftKeyboardStickButton { get; set; }

        public InputModel RightStickUp { get; set; }
        public InputModel RightStickDown { get; set; }
        public InputModel RightStickLeft { get; set; }
        public InputModel RightStickRight { get; set; }
        public InputModel RightKeyboardStickButton { get; set; }

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
                    LeftControllerStickButton = controllerConfig.LeftJoyconStick.StickButton;

                    RightJoystick = controllerConfig.RightJoyconStick.Joystick;
                    RightInvertStickX = controllerConfig.RightJoyconStick.InvertStickX;
                    RightInvertStickY = controllerConfig.RightJoyconStick.InvertStickY;
                    RightRotate90 = controllerConfig.RightJoyconStick.Rotate90CW;
                    RightControllerStickButton = controllerConfig.RightJoyconStick.StickButton;

                    ButtonA = controllerConfig.RightJoycon.ButtonA;
                    ButtonB = controllerConfig.RightJoycon.ButtonB;
                    ButtonX = controllerConfig.RightJoycon.ButtonX;
                    ButtonY = controllerConfig.RightJoycon.ButtonY;
                    ButtonR = controllerConfig.RightJoycon.ButtonR;
                    RightButtonSl = controllerConfig.RightJoycon.ButtonSl;
                    RightButtonSr = controllerConfig.RightJoycon.ButtonSr;
                    ButtonZr = controllerConfig.RightJoycon.ButtonZr;
                    ButtonPlus = controllerConfig.RightJoycon.ButtonPlus;

                    DpadUp = controllerConfig.LeftJoycon.DpadUp;
                    DpadDown = controllerConfig.LeftJoycon.DpadDown;
                    DpadLeft = controllerConfig.LeftJoycon.DpadLeft;
                    DpadRight = controllerConfig.LeftJoycon.DpadRight;
                    ButtonMinus = controllerConfig.LeftJoycon.ButtonMinus;
                    LeftButtonSl = controllerConfig.LeftJoycon.ButtonSl;
                    LeftButtonSr = controllerConfig.LeftJoycon.ButtonSr;
                    ButtonZl = controllerConfig.LeftJoycon.ButtonZl;
                    ButtonL = controllerConfig.LeftJoycon.ButtonL;

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
                        DpadUp = DpadUp.AsKey(),
                        DpadDown = DpadDown.AsKey(),
                        DpadLeft = DpadLeft.AsKey(),
                        DpadRight = DpadRight.AsKey(),
                        ButtonL = ButtonL.AsKey(),
                        ButtonZl = ButtonZl.AsKey(),
                        ButtonSl = LeftButtonSl.AsKey(),
                        ButtonSr = LeftButtonSr.AsKey(),
                        ButtonMinus = ButtonMinus.AsKey(),
                    },
                    RightJoycon = new RightJoyconCommonConfig<Key>
                    {
                        ButtonA = ButtonA.AsKey(),
                        ButtonB = ButtonB.AsKey(),
                        ButtonX = ButtonX.AsKey(),
                        ButtonY = ButtonY.AsKey(),
                        ButtonPlus = ButtonPlus.AsKey(),
                        ButtonSl = RightButtonSl.AsKey(),
                        ButtonSr = RightButtonSr.AsKey(),
                        ButtonR = ButtonR.AsKey(),
                        ButtonZr = ButtonZr.AsKey(),
                    },
                    LeftJoyconStick = new JoyconConfigKeyboardStick<Key>
                    {
                        StickUp = LeftStickUp.AsKey(),
                        StickDown = LeftStickDown.AsKey(),
                        StickRight = LeftStickRight.AsKey(),
                        StickLeft = LeftStickLeft.AsKey(),
                        StickButton = LeftKeyboardStickButton.AsKey(),
                    },
                    RightJoyconStick = new JoyconConfigKeyboardStick<Key>
                    {
                        StickUp = RightStickUp.AsKey(),
                        StickDown = RightStickDown.AsKey(),
                        StickLeft = RightStickLeft.AsKey(),
                        StickRight = RightStickRight.AsKey(),
                        StickButton = RightKeyboardStickButton.AsKey(),
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
                        DpadUp = DpadUp.AsGid(),
                        DpadDown = DpadDown.AsGid(),
                        DpadLeft = DpadLeft.AsGid(),
                        DpadRight = DpadRight.AsGid(),
                        ButtonL = ButtonL.AsGid(),
                        ButtonZl = ButtonZl.AsGid(),
                        ButtonSl = LeftButtonSl.AsGid(),
                        ButtonSr = LeftButtonSr.AsGid(),
                        ButtonMinus = ButtonMinus.AsGid(),
                    },
                    RightJoycon = new RightJoyconCommonConfig<GamepadInputId>
                    {
                        ButtonA = ButtonA.AsGid(),
                        ButtonB = ButtonB.AsGid(),
                        ButtonX = ButtonX.AsGid(),
                        ButtonY = ButtonY.AsGid(),
                        ButtonPlus = ButtonPlus.AsGid(),
                        ButtonSl = RightButtonSl.AsGid(),
                        ButtonSr = RightButtonSr.AsGid(),
                        ButtonR = ButtonR.AsGid(),
                        ButtonZr = ButtonZr.AsGid(),
                    },
                    LeftJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>
                    {
                        Joystick = LeftJoystick,
                        InvertStickX = LeftInvertStickX,
                        InvertStickY = LeftInvertStickY,
                        Rotate90CW = LeftRotate90,
                        StickButton = LeftControllerStickButton.AsGid(),
                    },
                    RightJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>
                    {
                        Joystick = RightJoystick,
                        InvertStickX = RightInvertStickX,
                        InvertStickY = RightInvertStickY,
                        Rotate90CW = RightRotate90,
                        StickButton = RightControllerStickButton.AsGid(),
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