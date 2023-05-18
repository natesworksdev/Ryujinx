using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels.Input;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;

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
                    button.Checked += Button_Checked;
                    button.Unchecked += Button_Unchecked;
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

        private void Button_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                if (_currentAssigner != null && button == _currentAssigner.ToggledButton)
                {
                    return;
                }

                bool isStick = button.Tag != null && button.Tag.ToString() == "stick";

                if (_currentAssigner == null && (bool)button.IsChecked)
                {
                    _currentAssigner = new ButtonKeyAssigner(button);

                    FocusManager.Instance.Focus(this, NavigationMethod.Pointer);

                    PointerPressed += MouseClick;

                    IKeyboard keyboard = (IKeyboard)(DataContext as KeyboardInputViewModel).parentModel.AvaloniaKeyboardDriver.GetGamepad("0"); // Open Avalonia keyboard for cancel operations.
                    IButtonAssigner assigner = CreateButtonAssigner(isStick);

                    _currentAssigner.ButtonAssigned += (sender, e) =>
                    {
                        if (e.IsAssigned)
                        {
                            (DataContext as KeyboardInputViewModel).parentModel.IsModified = true;
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
        }

        private void Button_Unchecked(object sender, RoutedEventArgs e)
        {
            _currentAssigner?.Cancel();
            _currentAssigner = null;
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

        public void Dispose()
        {
            _currentAssigner?.Cancel();
            _currentAssigner = null;
        }
    }
}