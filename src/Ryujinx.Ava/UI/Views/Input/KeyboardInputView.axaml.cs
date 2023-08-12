using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels.Input;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;
using Key = Ryujinx.Common.Configuration.Hid.Key;

namespace Ryujinx.Ava.UI.Views.Input
{
    public partial class KeyboardInputView : UserControl
    {
        private ButtonKeyAssigner _currentAssigner;

        public KeyboardInputView()
        {
            InitializeComponent();

            foreach (ILogical visual in SettingButtons.GetLogicalDescendants())
            {
                if (visual is ToggleButton button && !(visual is CheckBox))
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

                        IKeyboard keyboard = (IKeyboard)(DataContext as KeyboardInputViewModel).parentModel.AvaloniaKeyboardDriver.GetGamepad("0"); // Open Avalonia keyboard for cancel operations.
                        IButtonAssigner assigner = CreateButtonAssigner(isStick);

                        _currentAssigner.ButtonAssigned += (sender, e) =>
                        {
                            if (e.IsAssigned)
                            {
                                var viewModel = (DataContext as KeyboardInputViewModel);
                                viewModel.parentModel.IsModified = true;

                                switch (button.Name)
                                {
                                    case "ButtonZl":
                                        viewModel.Config.ButtonZl = (Key)e.Key;
                                        break;
                                    case "ButtonL":
                                        viewModel.Config.ButtonL = (Key)e.Key;
                                        break;
                                    case "ButtonMinus":
                                        viewModel.Config.ButtonMinus = (Key)e.Key;
                                        break;
                                    case "LeftStickButton":
                                        viewModel.Config.LeftStickButton = (Key)e.Key;
                                        break;
                                    case "LeftStickUp":
                                        viewModel.Config.LeftStickUp = (Key)e.Key;
                                        break;
                                    case "LeftStickDown":
                                        viewModel.Config.LeftStickDown = (Key)e.Key;
                                        break;
                                    case "LeftStickRight":
                                        viewModel.Config.LeftStickRight = (Key)e.Key;
                                        break;
                                    case "LeftStickLeft":
                                        viewModel.Config.LeftStickLeft = (Key)e.Key;
                                        break;
                                    case "DpadUp":
                                        viewModel.Config.DpadUp = (Key)e.Key;
                                        break;
                                    case "DpadDown":
                                        viewModel.Config.DpadDown = (Key)e.Key;
                                        break;
                                    case "DpadLeft":
                                        viewModel.Config.DpadLeft = (Key)e.Key;
                                        break;
                                    case "DpadRight":
                                        viewModel.Config.DpadRight = (Key)e.Key;
                                        break;
                                    case "LeftButtonSr":
                                        viewModel.Config.LeftButtonSr = (Key)e.Key;
                                        break;
                                    case "LeftButtonSl":
                                        viewModel.Config.LeftButtonSl = (Key)e.Key;
                                        break;
                                    case "RightButtonSr":
                                        viewModel.Config.RightButtonSr = (Key)e.Key;
                                        break;
                                    case "RightButtonSl":
                                        viewModel.Config.RightButtonSl = (Key)e.Key;
                                        break;
                                    case "ButtonZr":
                                        viewModel.Config.ButtonZr = (Key)e.Key;
                                        break;
                                    case "ButtonR":
                                        viewModel.Config.ButtonR = (Key)e.Key;
                                        break;
                                    case "ButtonPlus":
                                        viewModel.Config.ButtonPlus = (Key)e.Key;
                                        break;
                                    case "ButtonA":
                                        viewModel.Config.ButtonA = (Key)e.Key;
                                        break;
                                    case "ButtonB":
                                        viewModel.Config.ButtonB = (Key)e.Key;
                                        break;
                                    case "ButtonX":
                                        viewModel.Config.ButtonX = (Key)e.Key;
                                        break;
                                    case "ButtonY":
                                        viewModel.Config.ButtonY = (Key)e.Key;
                                        break;
                                    case "RightStickButton":
                                        viewModel.Config.RightStickButton = (Key)e.Key;
                                        break;
                                    case "RightStickUp":
                                        viewModel.Config.RightStickUp = (Key)e.Key;
                                        break;
                                    case "RightStickDown":
                                        viewModel.Config.RightStickDown = (Key)e.Key;
                                        break;
                                    case "RightStickRight":
                                        viewModel.Config.RightStickRight = (Key)e.Key;
                                        break;
                                    case "RightStickLeft":
                                        viewModel.Config.RightStickLeft = (Key)e.Key;
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

            assigner = new KeyboardKeyAssigner((IKeyboard)(DataContext as KeyboardInputViewModel).parentModel.SelectedGamepad);

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
