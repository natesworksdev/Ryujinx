using Ryujinx.Ava.Common.Locale;
using Ryujinx.Common.Configuration.Hid.Controller;
using Avalonia.Flexbox;
using Ryujinx.Ava.UI.Models;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class InputTriggerViewModel : BaseModel
    {
        private string _triggerButtonName;
        private string _bumperButtonName;
        private string _consoleButtonName;

        private InputModel _triggerButton;
        private InputModel _bumperButton;
        private InputModel _consoleButton;

        private AlignItems _flexDirection;

        private ControllerSettingsViewModel _viewModel;
        private bool _isLeft;

        public InputTriggerViewModel() { }

        public InputTriggerViewModel(StickInputId side, ControllerSettingsViewModel viewModel)
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
                _triggerButtonName = LocaleManager.Instance[LocaleKeys.ControllerSettingsTriggerZL];
                _bumperButtonName = LocaleManager.Instance[LocaleKeys.ControllerSettingsTriggerL];
                _consoleButtonName = LocaleManager.Instance[LocaleKeys.ControllerSettingsButtonMinus];

                _triggerButton = _viewModel.Configuration.ButtonZl;
                _bumperButton = _viewModel.Configuration.ButtonL;
                _consoleButton = _viewModel.Configuration.ButtonMinus;
                _flexDirection = AlignItems.FlexStart;
            }
            else
            {
                _triggerButtonName = LocaleManager.Instance[LocaleKeys.ControllerSettingsTriggerZR];
                _bumperButtonName = LocaleManager.Instance[LocaleKeys.ControllerSettingsTriggerR];
                _consoleButtonName = LocaleManager.Instance[LocaleKeys.ControllerSettingsButtonPlus];

                _triggerButton = _viewModel.Configuration.ButtonZr;
                _bumperButton = _viewModel.Configuration.ButtonR;
                _consoleButton = _viewModel.Configuration.ButtonPlus;
                _flexDirection = AlignItems.FlexEnd;
            }
        }

        public string TriggerButtonName
        {
            get => _triggerButtonName;
            set
            {
                _triggerButtonName = value;
                OnPropertyChanged();
            }
        }

        public string BumperButtonName
        {
            get => _bumperButtonName;
            set
            {
                _triggerButtonName = value;
                OnPropertyChanged();
            }
        }

        public string ConsoleButtonName
        {
            get => _consoleButtonName;
            set
            {
                _triggerButtonName = value;
                OnPropertyChanged();
            }
        }

        public InputModel TriggerButton
        {
            get => _triggerButton;
            set
            {
                _triggerButton = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.ButtonZl = value;
                }
                else
                {
                    _viewModel.Configuration.ButtonZr = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel BumperButton
        {
            get => _bumperButton;
            set
            {
                _bumperButton = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.ButtonL = value;
                }
                else
                {
                    _viewModel.Configuration.ButtonR = value;
                }
                OnPropertyChanged();
            }
        }

        public InputModel ConsoleButton
        {
            get => _consoleButton;
            set
            {
                _consoleButton = value;
                if (_isLeft)
                {
                    _viewModel.Configuration.ButtonMinus = value;
                }
                else
                {
                    _viewModel.Configuration.ButtonPlus = value;
                }
                OnPropertyChanged();
            }
        }

        public AlignItems FlexDirection
        {
            get => _flexDirection;
            set
            {
                _flexDirection = value;
                OnPropertyChanged();
            }
        }
    }
}