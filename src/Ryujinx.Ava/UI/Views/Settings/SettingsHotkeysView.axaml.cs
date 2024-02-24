using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Ryujinx.Ava.Input;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;

namespace Ryujinx.Ava.UI.Views.Settings
{
    public partial class SettingsHotkeysView : UserControl
    {
        private ButtonKeyAssigner _currentAssigner;
        private readonly IGamepadDriver _avaloniaKeyboardDriver;

        public SettingsHotkeysView()
        {
            InitializeComponent();

            foreach (ILogical visual in SettingButtons.GetLogicalDescendants())
            {
                if (visual is ToggleButton button and not CheckBox)
                {
                    button.IsCheckedChanged += Button_IsCheckedChanged;
                }
            }
            _avaloniaKeyboardDriver = new AvaloniaKeyboardDriver(this);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_currentAssigner != null && _currentAssigner.ToggledButton != null && !_currentAssigner.ToggledButton.IsPointerOver)
            {
                _currentAssigner.Cancel();
            }
        }

        private void MouseClick(object sender, PointerPressedEventArgs e)
        {
            bool shouldUnbind = e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed;

            _currentAssigner?.Cancel(shouldUnbind);

            PointerPressed -= MouseClick;
        }

        private void Button_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                if ((bool)button.IsChecked)
                {
                    if (_currentAssigner != null && button == _currentAssigner.ToggledButton)
                    {
                        return;
                    }

                    if (_currentAssigner == null)
                    {
                        _currentAssigner = new ButtonKeyAssigner(button);

                        this.Focus(NavigationMethod.Pointer);

                        PointerPressed += MouseClick;

                        var keyboard = (IKeyboard)_avaloniaKeyboardDriver.GetGamepad("0");
                        IButtonAssigner assigner = new KeyboardKeyAssigner(keyboard);

                        _currentAssigner.ButtonAssigned += (sender, e) =>
                        {
                            if (e.ButtonValue.HasValue)
                            {
                                var viewModel = (DataContext) as SettingsViewModel;
                                var buttonValue = e.ButtonValue.Value;

                                switch (button.Name)
                                {
                                    case "ToggleVsync":
                                        viewModel.KeyboardHotkeys.ToggleVsync = buttonValue.AsKey();
                                        break;
                                    case "Screenshot":
                                        viewModel.KeyboardHotkeys.Screenshot = buttonValue.AsKey();
                                        break;
                                    case "ShowUI":
                                        viewModel.KeyboardHotkeys.ShowUI = buttonValue.AsKey();
                                        break;
                                    case "Pause":
                                        viewModel.KeyboardHotkeys.Pause = buttonValue.AsKey();
                                        break;
                                    case "ToggleMute":
                                        viewModel.KeyboardHotkeys.ToggleMute = buttonValue.AsKey();
                                        break;
                                    case "ResScaleUp":
                                        viewModel.KeyboardHotkeys.ResScaleUp = buttonValue.AsKey();
                                        break;
                                    case "ResScaleDown":
                                        viewModel.KeyboardHotkeys.ResScaleDown = buttonValue.AsKey();
                                        break;
                                    case "VolumeUp":
                                        viewModel.KeyboardHotkeys.VolumeUp = buttonValue.AsKey();
                                        break;
                                    case "VolumeDown":
                                        viewModel.KeyboardHotkeys.VolumeDown = buttonValue.AsKey();
                                        break;
                                }
                            }
                        };

                        _currentAssigner.GetInputAndAssign(assigner, keyboard);
                    }
                    else
                    {
                        if (_currentAssigner != null)
                        {
                            _currentAssigner.Cancel();
                            _currentAssigner = null;
                            button.IsChecked = false;
                        }
                    }
                }
                else
                {
                    _currentAssigner?.Cancel();
                    _currentAssigner = null;
                }
            }
        }

        public void Dispose()
        {
            _currentAssigner?.Cancel();
            _currentAssigner = null;

            _avaloniaKeyboardDriver.Dispose();
        }
    }
}
