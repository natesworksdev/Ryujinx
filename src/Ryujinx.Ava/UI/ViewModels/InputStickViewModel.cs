using Ryujinx.Ava.Common.Locale;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class InputStickViewModel : BaseModel
    {
        private StickInputId _side;
        private string _stickName;

        private bool _isController;
        private bool _invertXAxis;
        private bool _invertYAxis;
        private bool _rotate90;

        private Key _joystick;
        private Key _keyboardStickButton;
        private Key _controllerStickButton;
        private Key _stickUp;
        private Key _stickDown;
        private Key _stickLeft;
        private Key _stickRight;

        private float _deadzone;
        private float _range;

        public StickInputId Side
        {
            get => _side;
            set
            {
                switch (value)
                {
                    case StickInputId.Left:
                        StickName = LocaleManager.Instance[LocaleKeys.ControllerSettingsLStick];
                        break;
                    case StickInputId.Right:
                        StickName = LocaleManager.Instance[LocaleKeys.ControllerSettingsRStick];
                        break;
                }

                _side = value;
            }
        }

        public InputStickViewModel(StickInputId side)
        {
            Side = side;
        }

        public string StickName
        {
            get => _stickName;
            set
            {
                _stickName = value;
                OnPropertyChanged();
            }
        }

        public bool IsController
        {
            get => _isController;
            set
            {
                _isController = value;
                OnPropertyChanged();
            }
        }

        public bool InvertXAxis
        {
            get => _invertXAxis;
            set
            {
                _invertXAxis = value;
                OnPropertyChanged();
            }
        }

        public bool InvertYAxis
        {
            get => _invertYAxis;
            set
            {
                _invertYAxis = value;
                OnPropertyChanged();
            }
        }

        public bool Rotate90
        {
            get => _rotate90;
            set
            {
                _rotate90 = value;
                OnPropertyChanged();
            }
        }

        public Key Joystick
        {
            get => _joystick;
            set
            {
                _joystick = value;
                OnPropertyChanged();
            }
        }

        public Key KeyboardStickButton
        {
            get => _keyboardStickButton;
            set
            {
                _keyboardStickButton = value;
                OnPropertyChanged();
            }
        }

        public Key ControllerStickButton
        {
            get => _controllerStickButton;
            set
            {
                _controllerStickButton = value;
                OnPropertyChanged();
            }
        }

        public Key StickUp
        {
            get => _stickUp;
            set
            {
                _stickUp = value;
                OnPropertyChanged();
            }
        }

        public Key StickDown
        {
            get => _stickDown;
            set
            {
                _stickDown = value;
                OnPropertyChanged();
            }
        }

        public Key StickLeft
        {
            get => _stickLeft;
            set
            {
                _stickLeft = value;
                OnPropertyChanged();
            }
        }

        public Key StickRight
        {
            get => _stickRight;
            set
            {
                _stickRight = value;
                OnPropertyChanged();
            }
        }

        public float Deadzone
        {
            get => _deadzone;
            set
            {
                _deadzone = value;
                OnPropertyChanged();
            }
        }

        public float Range
        {
            get => _range;
            set
            {
                _range = value;
                OnPropertyChanged();
            }
        }
    }
}