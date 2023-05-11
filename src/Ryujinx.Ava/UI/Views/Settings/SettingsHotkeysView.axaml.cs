using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Ryujinx.Ava.Input;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;
using System;
using System.Linq;

namespace Ryujinx.Ava.UI.Views.Settings
{
    public partial class SettingsHotkeysView : UserControl
    {
        private ButtonKeyAssigner _currentAssigner;
        private readonly IGamepadDriver AvaloniaKeyboardDriver;
    
        public SettingsHotkeysView()
        {
            InitializeComponent();
            AvaloniaKeyboardDriver = new AvaloniaKeyboardDriver(this);

            foreach (ToggleButton button in HotkeysPage.GetLogicalDescendants().OfType<ToggleButton>())
            {
                button.Click += Button_Click;
            }

            PointerPressed += MouseClick;
            DetachedFromVisualTree += OnViewDetachedFromVisualTree;
        }

        private void OnViewDetachedFromVisualTree(object sender, Avalonia.VisualTreeAttachmentEventArgs e)
        {
            _currentAssigner?.Cancel();
            _currentAssigner = null;
        }

        private void MouseClick(object sender, PointerPressedEventArgs e)
        {
            bool shouldUnbind = e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed;

            _currentAssigner?.Cancel(shouldUnbind);
            _currentAssigner = null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                // NOTE: In any case, if a button is clicked we want to cancel the current assignment.
                //       It is either a new button, so cancel the old one or it is the same button, so cancel the assignment.
                if (_currentAssigner != null)
                {
                    ToggleButton oldButton = _currentAssigner.ToggledButton;

                    _currentAssigner.Cancel();
                    _currentAssigner = null;

                    if (button == oldButton)
                    {
                        return;
                    }
                }

                IKeyboard keyboard = (IKeyboard)AvaloniaKeyboardDriver.GetGamepad(AvaloniaKeyboardDriver.GamepadsIds[0]);
                IButtonAssigner assigner = new KeyboardKeyAssigner(keyboard);

                _currentAssigner = new ButtonKeyAssigner(button);
                _currentAssigner.ButtonAssigned += OnButtonAssigned;
                _currentAssigner.GetInputAndAssign(assigner, keyboard);
            }
        }

        private void OnButtonAssigned(object sender, ButtonKeyAssigner.ButtonAssignedEventArgs args)
        {
            args.Button.IsChecked = false;

            if (_currentAssigner == sender)
            {
                _currentAssigner = null;
            }
        }

        public void Dispose()
        {
            _currentAssigner?.Cancel();
            _currentAssigner = null;

            PointerPressed -= MouseClick;
        }
    }
}