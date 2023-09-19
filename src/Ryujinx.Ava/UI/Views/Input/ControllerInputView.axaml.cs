using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels.Input;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;

namespace Ryujinx.Ava.UI.Views.Input
{
    public partial class ControllerInputView : UserControl
    {
        private ButtonKeyAssigner _currentAssigner;

        public ControllerInputView()
        {
            InitializeComponent();

            foreach (ILogical visual in SettingButtons.GetLogicalDescendants())
            {
                if (visual is ToggleButton button and not CheckBox)
                {
                    button.IsCheckedChanged += Button_IsCheckedChanged;
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_currentAssigner != null && _currentAssigner.ToggledButton != null && !_currentAssigner.ToggledButton.IsPointerOver)
            {
                _currentAssigner.Cancel();
            }
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

                    bool isStick = button.Tag != null && button.Tag.ToString() == "stick";

                    if (_currentAssigner == null && (bool)button.IsChecked)
                    {
                        _currentAssigner = new ButtonKeyAssigner(button);

                        this.Focus(NavigationMethod.Pointer);

                        PointerPressed += MouseClick;

                        IKeyboard keyboard = (IKeyboard)(DataContext as ControllerInputViewModel).parentModel.AvaloniaKeyboardDriver.GetGamepad("0"); // Open Avalonia keyboard for cancel operations.
                        IButtonAssigner assigner = CreateButtonAssigner(isStick);

                        _currentAssigner.ButtonAssigned += (sender, e) =>
                        {
                            if (e.ButtonValue.HasValue)
                            {
                                var viewModel = (DataContext as ControllerInputViewModel);
                                var buttonValue = e.ButtonValue.Value;
                                viewModel.parentModel.IsModified = true;

                                switch (button.Name)
                                {
                                    case "ButtonZl":
                                        viewModel.Config.ButtonZl = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "ButtonL":
                                        viewModel.Config.ButtonL = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "ButtonMinus":
                                        viewModel.Config.ButtonMinus = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "LeftStickButton":
                                        viewModel.Config.LeftStickButton = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "LeftJoystick":
                                        viewModel.Config.LeftJoystick = buttonValue.AsGamepadStickId();
                                        break;
                                    case "DpadUp":
                                        viewModel.Config.DpadUp = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "DpadDown":
                                        viewModel.Config.DpadDown = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "DpadLeft":
                                        viewModel.Config.DpadLeft = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "DpadRight":
                                        viewModel.Config.DpadRight = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "LeftButtonSr":
                                        viewModel.Config.LeftButtonSr = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "LeftButtonSl":
                                        viewModel.Config.LeftButtonSl = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "RightButtonSr":
                                        viewModel.Config.RightButtonSr = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "RightButtonSl":
                                        viewModel.Config.RightButtonSl = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "ButtonZr":
                                        viewModel.Config.ButtonZr = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "ButtonR":
                                        viewModel.Config.ButtonR = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "ButtonPlus":
                                        viewModel.Config.ButtonPlus = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "ButtonA":
                                        viewModel.Config.ButtonA = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "ButtonB":
                                        viewModel.Config.ButtonB = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "ButtonX":
                                        viewModel.Config.ButtonX = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "ButtonY":
                                        viewModel.Config.ButtonY = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "RightStickButton":
                                        viewModel.Config.RightStickButton = buttonValue.AsGamepadButtonInputId();
                                        break;
                                    case "RightJoystick":
                                        viewModel.Config.RightJoystick = buttonValue.AsGamepadStickId();
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
                            ToggleButton oldButton = _currentAssigner.ToggledButton;

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

        private void MouseClick(object sender, PointerPressedEventArgs e)
        {
            bool shouldUnbind = e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed;

            _currentAssigner?.Cancel(shouldUnbind);

            PointerPressed -= MouseClick;
        }

        private IButtonAssigner CreateButtonAssigner(bool forStick)
        {
            IButtonAssigner assigner;

            assigner = new GamepadButtonAssigner((DataContext as ControllerInputViewModel).parentModel.SelectedGamepad, ((DataContext as ControllerInputViewModel).parentModel.Config as StandardControllerInputConfig).TriggerThreshold, forStick);

            return assigner;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _currentAssigner?.Cancel();
            _currentAssigner = null;
        }
    }
}
