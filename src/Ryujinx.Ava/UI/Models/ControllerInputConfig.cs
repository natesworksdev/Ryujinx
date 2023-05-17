using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Controller.Motion;
using System;

namespace Ryujinx.Ava.UI.Models
{
    public class ControllerInputConfig : BaseModel
    {
        private StickInputId _leftJoystick;
        private bool _leftInvertStickX;
        private bool _leftInvertStickY;
        private bool _leftRotate90;
        private GamepadInputId _leftControllerStickButton;

        private StickInputId _rightJoystick;
        private bool _rightInvertStickX;
        private bool _rightInvertStickY;
        private bool _rightRotate90;
        private GamepadInputId _rightControllerStickButton;

        private GamepadInputId _buttonA;
        private GamepadInputId _buttonB;
        private GamepadInputId _buttonX;
        private GamepadInputId _buttonY;
        private GamepadInputId _buttonR;
        private GamepadInputId _rightButtonSl;
        private GamepadInputId _rightButtonSr;
        private GamepadInputId _buttonZr;
        private GamepadInputId _buttonPlus;

        private GamepadInputId _dpadUp;
        private GamepadInputId _dpadDown;
        private GamepadInputId _dpadLeft;
        private GamepadInputId _dpadRight;
        private GamepadInputId _buttonL;
        private GamepadInputId _leftButtonSl;
        private GamepadInputId _leftButtonSr;
        private GamepadInputId _buttonZl;
        private GamepadInputId _buttonMinus;

        private float _deadzoneLeft;
        private float _deadzoneRight;
        private float _rangeLeft;
        private float _rangeRight;
        private float _triggerThreshold;
        private double _gyroDeadzone;
        private int _sensitivity;
        private bool _enableRumble;
        private bool _enableMotion;
        private float _weakRumble;
        private float _strongRumble;
        private MotionInputBackendType _motionBackend;

        public bool EnableCemuHookMotion { get; set; }
        public string DsuServerHost { get; set; }
        public int DsuServerPort { get; set; }
        public int Slot { get; set; }
        public int AltSlot { get; set; }
        public bool MirrorInput { get; set; }

        public string Id { get; set; }
        public ControllerType ControllerType { get; set; }
        public PlayerIndex PlayerIndex { get; set; }

        public StickInputId LeftJoystick
        {
            get => _leftJoystick;
            set
            {
                _leftJoystick = value;
                OnPropertyChanged();
            }
        }

        public bool LeftInvertStickX
        {
            get => _leftInvertStickX;
            set
            {
                _leftInvertStickX = value;
                OnPropertyChanged();
            }
        }

        public bool LeftInvertStickY
        {
            get => _leftInvertStickY;
            set
            {
                _leftInvertStickY = value;
                OnPropertyChanged();
            }
        }

        public bool LeftRotate90
        {
            get => _leftRotate90;
            set
            {
                _leftRotate90 = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId LeftControllerStickButton
        {
            get => _leftControllerStickButton;
            set
            {
                _leftControllerStickButton = value;
                OnPropertyChanged();
            }
        }

        public StickInputId RightJoystick
        {
            get => _rightJoystick;
            set
            {
                _rightJoystick = value;
                OnPropertyChanged();
            }
        }

        public bool RightInvertStickX
        {
            get => _rightInvertStickX;
            set
            {
                _rightInvertStickX = value;
                OnPropertyChanged();
            }
        }

        public bool RightInvertStickY
        {
            get => _rightInvertStickY;
            set
            {
                _rightInvertStickY = value;
                OnPropertyChanged();
            }
        }

        public bool RightRotate90
        {
            get => _rightRotate90;
            set
            {
                _rightRotate90 = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId RightControllerStickButton
        {
            get => _rightControllerStickButton;
            set
            {
                _rightControllerStickButton = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId ButtonA
        {
            get => _buttonA;
            set
            {
                _buttonA = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId ButtonB
        {
            get => _buttonB;
            set
            {
                _buttonB = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId ButtonX
        {
            get => _buttonX;
            set
            {
                _buttonX = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId ButtonY
        {
            get => _buttonY;
            set
            {
                _buttonY = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId ButtonR
        {
            get => _buttonR;
            set
            {
                _buttonR = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId RightButtonSl
        {
            get => _rightButtonSl;
            set
            {
                _rightButtonSl = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId RightButtonSr
        {
            get => _rightButtonSr;
            set
            {
                _rightButtonSr = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId ButtonZr
        {
            get => _buttonZr;
            set
            {
                _buttonZr = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId ButtonPlus
        {
            get => _buttonPlus;
            set
            {
                _buttonPlus = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId DpadUp
        {
            get => _dpadUp;
            set
            {
                _dpadUp = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId DpadDown
        {
            get => _dpadDown;
            set
            {
                _dpadDown = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId DpadLeft
        {
            get => _dpadLeft;
            set
            {
                _dpadLeft = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId DpadRight
        {
            get => _dpadRight;
            set
            {
                _dpadRight = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId ButtonL
        {
            get => _buttonL;
            set
            {
                _buttonL = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId LeftButtonSl
        {
            get => _leftButtonSl;
            set
            {
                _leftButtonSl = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId LeftButtonSr
        {
            get => _leftButtonSr;
            set
            {
                _leftButtonSr = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId ButtonZl
        {
            get => _buttonZl;
            set
            {
                _buttonZl = value;
                OnPropertyChanged();
            }
        }

        public GamepadInputId ButtonMinus
        {
            get => _buttonMinus;
            set
            {
                _buttonMinus = value;
                OnPropertyChanged();
            }
        }

        public float DeadzoneLeft
        {
            get => _deadzoneLeft;
            set
            {
                _deadzoneLeft = MathF.Round(value, 3);

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

        public float RangeLeft
        {
            get => _rangeLeft;
            set
            {
                _rangeLeft = MathF.Round(value, 3);

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

        public MotionInputBackendType MotionBackend
        {
            get => _motionBackend;
            set
            {
                _motionBackend = value;
                OnPropertyChanged();
            }
        }

        public bool EnableRumble
        {
            get => _enableRumble;
            set
            {
                _enableRumble = value;
                OnPropertyChanged();
            }
        }

        public bool EnableMotion
        {
            get => _enableMotion;
            set
            {
                _enableMotion = value;
                OnPropertyChanged();
            }
        }

        public float WeakRumble
        {
            get => _weakRumble;
            set
            {
                _weakRumble = value;
                OnPropertyChanged();
            }
        }
        public float StrongRumble
        {
            get => _strongRumble;
            set
            {
                _strongRumble = value;
                OnPropertyChanged();
            }
        }

        public ControllerInputConfig(InputConfig config)
        {
            if (config != null)
            {
                Id = config.Id;
                ControllerType = config.ControllerType;
                PlayerIndex = config.PlayerIndex;

                if (config is StandardControllerInputConfig controllerInput)
                {
                    LeftJoystick               = controllerInput.LeftJoyconStick.Joystick;
                    LeftInvertStickX           = controllerInput.LeftJoyconStick.InvertStickX;
                    LeftInvertStickY           = controllerInput.LeftJoyconStick.InvertStickY;
                    LeftRotate90               = controllerInput.LeftJoyconStick.Rotate90CW;
                    LeftControllerStickButton  = controllerInput.LeftJoyconStick.StickButton;

                    RightJoystick              = controllerInput.RightJoyconStick.Joystick;
                    RightInvertStickX          = controllerInput.RightJoyconStick.InvertStickX;
                    RightInvertStickY          = controllerInput.RightJoyconStick.InvertStickY;
                    RightRotate90              = controllerInput.RightJoyconStick.Rotate90CW;
                    RightControllerStickButton = controllerInput.RightJoyconStick.StickButton;

                    DpadUp        = controllerInput.LeftJoycon.DpadUp;
                    DpadDown      = controllerInput.LeftJoycon.DpadDown;
                    DpadLeft      = controllerInput.LeftJoycon.DpadLeft;
                    DpadRight     = controllerInput.LeftJoycon.DpadRight;
                    ButtonL       = controllerInput.LeftJoycon.ButtonL;
                    ButtonMinus   = controllerInput.LeftJoycon.ButtonMinus;
                    LeftButtonSl  = controllerInput.LeftJoycon.ButtonSl;
                    LeftButtonSr  = controllerInput.LeftJoycon.ButtonSr;
                    ButtonZl      = controllerInput.LeftJoycon.ButtonZl;

                    ButtonA       = controllerInput.RightJoycon.ButtonA;
                    ButtonB       = controllerInput.RightJoycon.ButtonB;
                    ButtonX       = controllerInput.RightJoycon.ButtonX;
                    ButtonY       = controllerInput.RightJoycon.ButtonY;
                    ButtonR       = controllerInput.RightJoycon.ButtonR;
                    ButtonPlus    = controllerInput.RightJoycon.ButtonPlus;
                    RightButtonSl = controllerInput.RightJoycon.ButtonSl;
                    RightButtonSr = controllerInput.RightJoycon.ButtonSr;
                    ButtonZr      = controllerInput.RightJoycon.ButtonZr;

                    DeadzoneLeft = controllerInput.DeadzoneLeft;
                    DeadzoneRight = controllerInput.DeadzoneRight;
                    RangeLeft = controllerInput.RangeLeft;
                    RangeRight = controllerInput.RangeRight;
                    TriggerThreshold = controllerInput.TriggerThreshold;

                    if (controllerInput.Motion != null)
                    {
                        EnableMotion = controllerInput.Motion.EnableMotion;
                        MotionBackend = controllerInput.Motion.MotionBackend;
                        GyroDeadzone = controllerInput.Motion.GyroDeadzone;
                        Sensitivity = controllerInput.Motion.Sensitivity;

                        if (controllerInput.Motion is CemuHookMotionConfigController cemuHook)
                        {
                            EnableCemuHookMotion = true;
                            DsuServerHost = cemuHook.DsuServerHost;
                            DsuServerPort = cemuHook.DsuServerPort;
                            Slot = cemuHook.Slot;
                            AltSlot = cemuHook.AltSlot;
                            MirrorInput = cemuHook.MirrorInput;
                        }

                        if (controllerInput.Rumble != null)
                        {
                            EnableRumble = controllerInput.Rumble.EnableRumble;
                            WeakRumble = controllerInput.Rumble.WeakRumble;
                            StrongRumble = controllerInput.Rumble.StrongRumble;
                        }
                    }
                }
            }
        }

        public InputConfig GetConfig()
        {
            var config = new StandardControllerInputConfig
            {
                Id = Id,
                Backend = InputBackendType.GamepadSDL2,
                PlayerIndex = PlayerIndex,
                ControllerType = ControllerType,
                LeftJoycon = new LeftJoyconCommonConfig<GamepadInputId>
                {
                    DpadUp = DpadUp,
                    DpadDown = DpadDown,
                    DpadLeft = DpadLeft,
                    DpadRight = DpadRight,
                    ButtonL = ButtonL,
                    ButtonMinus = ButtonMinus,
                    ButtonSl = LeftButtonSl,
                    ButtonSr = LeftButtonSr,
                    ButtonZl = ButtonZl
                },
                RightJoycon = new RightJoyconCommonConfig<GamepadInputId>
                {
                    ButtonA = ButtonA,
                    ButtonB = ButtonB,
                    ButtonX = ButtonX,
                    ButtonY = ButtonY,
                    ButtonPlus = ButtonPlus,
                    ButtonSl = RightButtonSl,
                    ButtonSr = RightButtonSr,
                    ButtonR = ButtonR,
                    ButtonZr = ButtonZr,
                },
                LeftJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>
                {
                    Joystick = LeftJoystick,
                    InvertStickX = LeftInvertStickX,
                    InvertStickY = LeftInvertStickY,
                    Rotate90CW = LeftRotate90,
                    StickButton = LeftControllerStickButton,
                },
                RightJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>
                {
                    Joystick = RightJoystick,
                    InvertStickX = RightInvertStickX,
                    InvertStickY = RightInvertStickY,
                    Rotate90CW = RightRotate90,
                    StickButton = RightControllerStickButton,
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
            };

            config.Motion.Sensitivity = Sensitivity;
            config.Motion.EnableMotion = EnableMotion;
            config.Motion.GyroDeadzone = GyroDeadzone;

            return config;
        }
    }
}