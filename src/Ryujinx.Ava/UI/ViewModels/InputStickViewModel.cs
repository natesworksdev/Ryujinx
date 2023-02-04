using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Common.Configuration.Hid.Controller;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class InputStickViewModel : BaseModel
    {
        private string _stickName;

        private bool _isController;
        private bool _invertXAxis;
        private bool _invertYAxis;
        private bool _rotate90;

        private StickInputId _joystick;
        private InputModel _keyboardStickButton;
        private InputModel _controllerStickButton;
        private InputModel _stickUp;
        private InputModel _stickDown;
        private InputModel _stickLeft;
        private InputModel _stickRight;

        private float _deadzone;
        private float _range;

        private ControllerSettingsViewModel _viewModel;
        private bool _isLeft;

        public InputStickViewModel() { }

        public InputStickViewModel(StickInputId side, ControllerSettingsViewModel viewModel)
        {
            _isLeft = side == StickInputId.Left;
            _viewModel = viewModel;
            _viewModel.OnNotifyChanges += InitValues;

            InitValues();
        }

        private void InitValues()
        {
            IsController = _viewModel.IsController;

            if (_isLeft)
            {
                StickName = LocaleManager.Instance[LocaleKeys.ControllerSettingsLStick];
                InvertXAxis = _viewModel.Configuration.LeftInvertStickX;
                InvertYAxis = _viewModel.Configuration.LeftInvertStickY;
                Rotate90 = _viewModel.Configuration.LeftRotate90;
                Joystick = _viewModel.Configuration.LeftJoystick;
                KeyboardStickButton = _viewModel.Configuration.LeftKeyboardStickButton;
                ControllerStickButton = _viewModel.Configuration.LeftControllerStickButton;
                StickUp = _viewModel.Configuration.LeftStickUp;
                StickDown = _viewModel.Configuration.LeftStickDown;
                StickLeft = _viewModel.Configuration.LeftStickLeft;
                StickRight = _viewModel.Configuration.LeftStickRight;
            }
            else
            {
                StickName = LocaleManager.Instance[LocaleKeys.ControllerSettingsRStick];
                InvertXAxis = _viewModel.Configuration.RightInvertStickX;
                InvertYAxis = _viewModel.Configuration.RightInvertStickY;
                Rotate90 = _viewModel.Configuration.RightRotate90;
                Joystick = _viewModel.Configuration.RightJoystick;
                KeyboardStickButton = _viewModel.Configuration.RightKeyboardStickButton;
                ControllerStickButton = _viewModel.Configuration.RightControllerStickButton;
                StickUp = _viewModel.Configuration.RightStickUp;
                StickDown = _viewModel.Configuration.RightStickDown;
                StickLeft = _viewModel.Configuration.RightStickLeft;
                StickRight = _viewModel.Configuration.RightStickRight;
            }
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
                if (_isLeft)
                {
                    _viewModel.Configuration.LeftInvertStickX = value;
                }
                else
                {
                    _viewModel.Configuration.RightInvertStickX = value;
                }
                OnPropertyChanged();
            }
        }

        public bool InvertYAxis
        {
            get => _invertYAxis;
            set
            {
                _invertYAxis = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.LeftInvertStickY = value;
                }
                else
                {
                    _viewModel.Configuration.RightInvertStickY = value;
                }
                OnPropertyChanged();
            }
        }

        public bool Rotate90
        {
            get => _rotate90;
            set
            {
                _rotate90 = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.LeftRotate90 = value;
                }
                else
                {
                    _viewModel.Configuration.RightRotate90 = value;
                }
                OnPropertyChanged();
            }
        }

        public StickInputId Joystick
        {
            get => _joystick;
            set
            {
                _joystick = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.LeftJoystick = value;
                }
                else
                {
                    _viewModel.Configuration.RightJoystick = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel KeyboardStickButton
        {
            get => _keyboardStickButton;
            set
            {
                _keyboardStickButton = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.LeftKeyboardStickButton = value;
                }
                else
                {
                    _viewModel.Configuration.RightKeyboardStickButton = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel ControllerStickButton
        {
            get => _controllerStickButton;
            set
            {
                _controllerStickButton = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.LeftControllerStickButton = value;
                }
                else
                {
                    _viewModel.Configuration.RightControllerStickButton = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel StickUp
        {
            get => _stickUp;
            set
            {
                _stickUp = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.LeftStickUp = value;
                }
                else
                {
                    _viewModel.Configuration.RightStickUp = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel StickDown
        {
            get => _stickDown;
            set
            {
                _stickDown = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.LeftStickDown = value;
                }
                else
                {
                    _viewModel.Configuration.RightStickDown = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel StickLeft
        {
            get => _stickLeft;
            set
            {
                _stickLeft = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.LeftStickLeft = value;
                }
                else
                {
                    _viewModel.Configuration.RightStickLeft = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel StickRight
        {
            get => _stickRight;
            set
            {
                _stickRight = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.LeftStickRight = value;
                }
                else
                {
                    _viewModel.Configuration.RightStickRight = value;
                }
                OnPropertyChanged();
            }
        }

        public float Deadzone
        {
            get => _deadzone;
            set
            {
                _deadzone = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.DeadzoneLeft = value;
                }
                else
                {
                    _viewModel.Configuration.DeadzoneRight = value;
                }
                OnPropertyChanged();
            }
        }

        public float Range
        {
            get => _range;
            set
            {
                _range = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.RangeLeft = value;
                }
                else
                {
                    _viewModel.Configuration.RangeRight = value;
                }
                OnPropertyChanged();
            }
        }
    }
}