using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Common.Configuration.Hid.Controller;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class InputButtonViewModel : BaseModel
    {
        private string _buttonTLName;
        private string _buttonTRName;
        private string _buttonBLName;
        private string _buttonBRName;

        private InputModel _buttonTL;
        private InputModel _buttonTR;
        private InputModel _buttonBL;
        private InputModel _buttonBR;

        private ControllerSettingsViewModel _viewModel;
        private bool _isLeft;

        public InputButtonViewModel(StickInputId side, ControllerSettingsViewModel viewModel)
        {
            _isLeft = side == StickInputId.Left;
            _viewModel = viewModel;
            _viewModel.OnNotifyChanges += InitValues;

            InitValues();
        }

        private void InitValues()
        {
            if (_isLeft)
            {
                _buttonTLName = LocaleManager.Instance[LocaleKeys.ControllerSettingsDPadUp];
                _buttonTRName = LocaleManager.Instance[LocaleKeys.ControllerSettingsDPadDown];
                _buttonBLName = LocaleManager.Instance[LocaleKeys.ControllerSettingsDPadLeft];
                _buttonBRName = LocaleManager.Instance[LocaleKeys.ControllerSettingsDPadRight];
            }
            else
            {
                _buttonTLName = LocaleManager.Instance[LocaleKeys.ControllerSettingsButtonX];
                _buttonTRName = LocaleManager.Instance[LocaleKeys.ControllerSettingsButtonA];
                _buttonBLName = LocaleManager.Instance[LocaleKeys.ControllerSettingsButtonY];
                _buttonBRName = LocaleManager.Instance[LocaleKeys.ControllerSettingsButtonB];
            }
        }

        public string ButtonTLName
        {
            get => _buttonTLName;
            set
            {
                _buttonTLName = value;
                OnPropertyChanged();
            }
        }

        public string ButtonTRName
        {
            get => _buttonTRName;
            set
            {
                _buttonTRName = value;
                OnPropertyChanged();
            }
        }

        public string ButtonBLName
        {
            get => _buttonBLName;
            set
            {
                _buttonBLName = value;
                OnPropertyChanged();
            }
        }

        public string ButtonBRName
        {
            get => _buttonBRName;
            set
            {
                _buttonBRName = value;
                OnPropertyChanged();
            }
        }

        public InputModel ButtonTL
        {
            get => _buttonTL;
            set
            {
                _buttonTL = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.DpadUp = value;
                }
                else
                {
                    _viewModel.Configuration.ButtonX = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel ButtonTR
        {
            get => _buttonTR;
            set
            {
                _buttonTR = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.DpadDown = value;
                }
                else
                {
                    _viewModel.Configuration.ButtonA = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel ButtonBL
        {
            get => _buttonBL;
            set
            {
                _buttonBL = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.DpadLeft = value;
                }
                else
                {
                    _viewModel.Configuration.ButtonY = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel ButtonBR
        {
            get => _buttonBR;
            set
            {
                _buttonBR = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.DpadRight = value;
                }
                else
                {
                    _viewModel.Configuration.ButtonB = value;
                }
                OnPropertyChanged();
            }
        }
    }
}