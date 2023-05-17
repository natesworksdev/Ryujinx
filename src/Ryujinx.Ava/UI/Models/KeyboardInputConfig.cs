using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Keyboard;

namespace Ryujinx.Ava.UI.Models
{
    public class KeyboardInputConfig : BaseModel
    {
        private Key _leftStickUp;
        private Key _leftStickDown;
        private Key _leftStickLeft;
        private Key _leftStickRight;
        private Key _leftKeyboardStickButton;

        private Key _rightStickUp;
        private Key _rightStickDown;
        private Key _rightStickLeft;
        private Key _rightStickRight;
        private Key _rightKeyboardStickButton;

        private Key _buttonA;
        private Key _buttonB;
        private Key _buttonX;
        private Key _buttonY;
        private Key _buttonR;
        private Key _rightButtonSl;
        private Key _rightButtonSr;
        private Key _buttonZr;
        private Key _buttonPlus;

        private Key _dpadUp;
        private Key _dpadDown;
        private Key _dpadLeft;
        private Key _dpadRight;
        private Key _buttonL;
        private Key _leftButtonSl;
        private Key _leftButtonSr;
        private Key _buttonZl;
        private Key _buttonMinus;

        public string Id { get; set; }
        public ControllerType ControllerType { get; set; }
        public PlayerIndex PlayerIndex { get; set; }

        public Key LeftStickUp
        {
            get => _leftStickUp;
            set
            {
                _leftStickUp = value;
                OnPropertyChanged();
            }
        }

        public Key LeftStickDown
        {
            get => _leftStickDown;
            set
            {
                _leftStickDown = value;
                OnPropertyChanged();
            }
        }

        public Key LeftStickLeft
        {
            get => _leftStickLeft;
            set
            {
                _leftStickLeft = value;
                OnPropertyChanged();
            }
        }

        public Key LeftStickRight
        {
            get => _leftStickRight;
            set
            {
                _leftStickRight = value;
                OnPropertyChanged();
            }
        }

        public Key LeftKeyboardStickButton
        {
            get => _leftKeyboardStickButton;
            set
            {
                _leftKeyboardStickButton = value;
                OnPropertyChanged();
            }
        }

        public Key RightStickUp
        {
            get => _rightStickUp;
            set
            {
                _rightStickUp = value;
                OnPropertyChanged();
            }
        }

        public Key RightStickDown
        {
            get => _rightStickDown;
            set
            {
                _rightStickDown = value;
                OnPropertyChanged();
            }
        }

        public Key RightStickLeft
        {
            get => _rightStickLeft;
            set
            {
                _rightStickLeft = value;
                OnPropertyChanged();
            }
        }

        public Key RightStickRight
        {
            get => _rightStickRight;
            set
            {
                _rightStickRight = value;
                OnPropertyChanged();
            }
        }

        public Key RightKeyboardStickButton
        {
            get => _rightKeyboardStickButton;
            set
            {
                _rightKeyboardStickButton = value;
                OnPropertyChanged();
            }
        }

        public Key ButtonA
        {
            get => _buttonA;
            set
            {
                _buttonA = value;
                OnPropertyChanged();
            }
        }

        public Key ButtonB
        {
            get => _buttonB;
            set
            {
                _buttonB = value;
                OnPropertyChanged();
            }
        }

        public Key ButtonX
        {
            get => _buttonX;
            set
            {
                _buttonX = value;
                OnPropertyChanged();
            }
        }

        public Key ButtonY
        {
            get => _buttonY;
            set
            {
                _buttonY = value;
                OnPropertyChanged();
            }
        }

        public Key ButtonR
        {
            get => _buttonR;
            set
            {
                _buttonR = value;
                OnPropertyChanged();
            }
        }

        public Key RightButtonSl
        {
            get => _rightButtonSl;
            set
            {
                _rightButtonSl = value;
                OnPropertyChanged();
            }
        }

        public Key RightButtonSr
        {
            get => _rightButtonSr;
            set
            {
                _rightButtonSr = value;
                OnPropertyChanged();
            }
        }

        public Key ButtonZr
        {
            get => _buttonZr;
            set
            {
                _buttonZr = value;
                OnPropertyChanged();
            }
        }

        public Key ButtonPlus
        {
            get => _buttonPlus;
            set
            {
                _buttonPlus = value;
                OnPropertyChanged();
            }
        }

        public Key DpadUp
        {
            get => _dpadUp;
            set
            {
                _dpadUp = value;
                OnPropertyChanged();
            }
        }

        public Key DpadDown
        {
            get => _dpadDown;
            set
            {
                _dpadDown = value;
                OnPropertyChanged();
            }
        }

        public Key DpadLeft
        {
            get => _dpadLeft;
            set
            {
                _dpadLeft = value;
                OnPropertyChanged();
            }
        }

        public Key DpadRight
        {
            get => _dpadRight;
            set
            {
                _dpadRight = value;
                OnPropertyChanged();
            }
        }

        public Key ButtonL
        {
            get => _buttonL;
            set
            {
                _buttonL = value;
                OnPropertyChanged();
            }
        }

        public Key LeftButtonSl
        {
            get => _leftButtonSl;
            set
            {
                _leftButtonSl = value;
                OnPropertyChanged();
            }
        }

        public Key LeftButtonSr
        {
            get => _leftButtonSr;
            set
            {
                _leftButtonSr = value;
                OnPropertyChanged();
            }
        }

        public Key ButtonZl
        {
            get => _buttonZl;
            set
            {
                _buttonZl = value;
                OnPropertyChanged();
            }
        }

        public Key ButtonMinus
        {
            get => _buttonMinus;
            set
            {
                _buttonMinus = value;
                OnPropertyChanged();
            }
        }

        public KeyboardInputConfig(InputConfig config)
        {
            if (config != null)
            {
                if (config is StandardKeyboardInputConfig keyboardConfig)
                {
                    LeftStickUp     = keyboardConfig.LeftJoyconStick.StickUp;
                    LeftStickDown   = keyboardConfig.LeftJoyconStick.StickDown;
                    LeftStickLeft   = keyboardConfig.LeftJoyconStick.StickLeft;
                    LeftStickRight  = keyboardConfig.LeftJoyconStick.StickRight;

                    RightStickUp    = keyboardConfig.RightJoyconStick.StickUp;
                    RightStickDown  = keyboardConfig.RightJoyconStick.StickDown;
                    RightStickLeft  = keyboardConfig.RightJoyconStick.StickLeft;
                    RightStickRight = keyboardConfig.RightJoyconStick.StickRight;

                    ButtonA       = keyboardConfig.RightJoycon.ButtonA;
                    ButtonB       = keyboardConfig.RightJoycon.ButtonB;
                    ButtonX       = keyboardConfig.RightJoycon.ButtonX;
                    ButtonY       = keyboardConfig.RightJoycon.ButtonY;
                    ButtonR       = keyboardConfig.RightJoycon.ButtonR;
                    RightButtonSl = keyboardConfig.RightJoycon.ButtonSl;
                    RightButtonSr = keyboardConfig.RightJoycon.ButtonSr;
                    ButtonZr      = keyboardConfig.RightJoycon.ButtonZr;
                    ButtonPlus    = keyboardConfig.RightJoycon.ButtonPlus;

                    DpadUp        = keyboardConfig.LeftJoycon.DpadUp;
                    DpadDown      = keyboardConfig.LeftJoycon.DpadDown;
                    DpadLeft      = keyboardConfig.LeftJoycon.DpadLeft;
                    DpadRight     = keyboardConfig.LeftJoycon.DpadRight;
                    ButtonMinus   = keyboardConfig.LeftJoycon.ButtonMinus;
                    LeftButtonSl  = keyboardConfig.LeftJoycon.ButtonSl;
                    LeftButtonSr  = keyboardConfig.LeftJoycon.ButtonSr;
                    ButtonZl      = keyboardConfig.LeftJoycon.ButtonZl;
                    ButtonL       = keyboardConfig.LeftJoycon.ButtonL;
                }
            }
        }

        public InputConfig GetConfig()
        {
            var config = new StandardKeyboardInputConfig
            {
                Id = Id,
                Backend = InputBackendType.WindowKeyboard,
                PlayerIndex = PlayerIndex,
                ControllerType = ControllerType,
                LeftJoycon =
                    new LeftJoyconCommonConfig<Key>
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
                RightJoycon =
                    new RightJoyconCommonConfig<Key>
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
                LeftJoyconStick =
                    new JoyconConfigKeyboardStick<Key>
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

            return config;
        }
    }
}