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
using StickInputId = Ryujinx.Common.Configuration.Hid.Controller.StickInputId;

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
                            if (e.IsAssigned)
                            {
                                var viewModel = (DataContext as ControllerInputViewModel);
                                viewModel.parentModel.IsModified = true;

                                switch (button.Name)
                                {
                                    case "ButtonZl":
                                        viewModel.Config.ButtonZl = (GamepadInputId)e.Key;
                                        break;
                                    case "ButtonL":
                                        viewModel.Config.ButtonL = (GamepadInputId)e.Key;
                                        break;
                                    case "ButtonMinus":
                                        viewModel.Config.ButtonMinus = (GamepadInputId)e.Key;
                                        break;
                                    case "LeftStickButton":
                                        viewModel.Config.LeftStickButton = (GamepadInputId)e.Key;
                                        break;
                                    case "LeftJoystick":
                                        viewModel.Config.LeftJoystick = (StickInputId)e.Key;
                                        break;
                                    case "DpadUp":
                                        viewModel.Config.DpadUp = (GamepadInputId)e.Key;
                                        break;
                                    case "DpadDown":
                                        viewModel.Config.DpadDown = (GamepadInputId)e.Key;
                                        break;
                                    case "DpadLeft":
                                        viewModel.Config.DpadLeft = (GamepadInputId)e.Key;
                                        break;
                                    case "DpadRight":
                                        viewModel.Config.DpadRight = (GamepadInputId)e.Key;
                                        break;
                                    case "LeftButtonSr":
                                        viewModel.Config.LeftButtonSr = (GamepadInputId)e.Key;
                                        break;
                                    case "LeftButtonSl":
                                        viewModel.Config.LeftButtonSl = (GamepadInputId)e.Key;
                                        break;
                                    case "RightButtonSr":
                                        viewModel.Config.RightButtonSr = (GamepadInputId)e.Key;
                                        break;
                                    case "RightButtonSl":
                                        viewModel.Config.RightButtonSl = (GamepadInputId)e.Key;
                                        break;
                                    case "ButtonZr":
                                        viewModel.Config.ButtonZr = (GamepadInputId)e.Key;
                                        break;
                                    case "ButtonR":
                                        viewModel.Config.ButtonR = (GamepadInputId)e.Key;
                                        break;
                                    case "ButtonPlus":
                                        viewModel.Config.ButtonPlus = (GamepadInputId)e.Key;
                                        break;
                                    case "ButtonA":
                                        viewModel.Config.ButtonA = (GamepadInputId)e.Key;
                                        break;
                                    case "ButtonB":
                                        viewModel.Config.ButtonB = (GamepadInputId)e.Key;
                                        break;
                                    case "ButtonX":
                                        viewModel.Config.ButtonX = (GamepadInputId)e.Key;
                                        break;
                                    case "ButtonY":
                                        viewModel.Config.ButtonY = (GamepadInputId)e.Key;
                                        break;
                                    case "RightStickButton":
                                        viewModel.Config.RightStickButton = (GamepadInputId)e.Key;
                                        break;
                                    case "RightJoystick":
                                        viewModel.Config.RightJoystick = (StickInputId)e.Key;
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
