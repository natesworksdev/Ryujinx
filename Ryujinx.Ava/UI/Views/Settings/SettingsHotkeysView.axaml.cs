using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ryujinx.Ava.Input;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;
using System.Collections.Generic;

namespace Ryujinx.Ava.UI.Views.Settings
{
    public partial class SettingsHotkeysView : UserControl
    {
        public SettingsViewModel ViewModel;

        private HotkeyButtonKeyAssigner _currentAssigner;
        private readonly MainWindow _mainWindow;
        private readonly IGamepadDriver _avaloniaKeyboardDriver;

        public SettingsHotkeysView()
        {
            InitializeComponent();

            _mainWindow = (MainWindow)((IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime).MainWindow;
            _avaloniaKeyboardDriver = new AvaloniaKeyboardDriver(this);
        }

        private void MouseClick(object sender, PointerPressedEventArgs e)
        {
            bool shouldUnbind = e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed;

            _currentAssigner?.Cancel(shouldUnbind);

            PointerPressed -= MouseClick;
        }

        private void Button_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                if (_currentAssigner != null && button == _currentAssigner.ToggledButton)
                {
                    return;
                }

                if (_currentAssigner == null && button.IsChecked != null && (bool)button.IsChecked)
                {
                    _currentAssigner = new HotkeyButtonKeyAssigner(button, ViewModel.UpdateHotkey);

                    FocusManager.Instance?.Focus(this, NavigationMethod.Pointer);

                    PointerPressed += MouseClick;

                    _currentAssigner.GetInputAndAssign(CreateButtonAssigner());
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

        private IButtonAssigner CreateButtonAssigner()
        {
            List<IButtonAssigner> assigners = new List<IButtonAssigner>();

            IGamepadDriver keyboardDriver;
            IGamepadDriver gamepadDriver = _mainWindow.InputManager.GamepadDriver;

            if (_mainWindow.InputManager.KeyboardDriver is AvaloniaKeyboardDriver)
            {
                // NOTE: To get input in this window, we need to bind a custom keyboard driver instead of using the InputManager one as the main window isn't focused...
                keyboardDriver = _avaloniaKeyboardDriver;
            }
            else
            {
                keyboardDriver = _mainWindow.InputManager.KeyboardDriver;
            }

            foreach (string id in keyboardDriver.GamepadsIds)
            {
                assigners.Add(new KeyboardKeyAssigner((IKeyboard)keyboardDriver.GetGamepad(id), allowModifiers: true));
            }

            foreach (string id in gamepadDriver.GamepadsIds)
            {
                assigners.Add(new GamepadButtonAssigner(gamepadDriver.GetGamepad(id), 0.2f, forStick: false));
            }

            return new MultiButtonAssigner(assigners);
        }

        private void Button_Unchecked(object sender, RoutedEventArgs e)
        {
            _currentAssigner?.Cancel();
            _currentAssigner = null;
        }

        public void Dispose()
        {
            _currentAssigner?.Cancel();
            _currentAssigner = null;
        }
    }
}