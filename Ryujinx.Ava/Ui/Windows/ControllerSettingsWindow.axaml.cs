using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Avalonia.Media;
using Avalonia.Threading;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Models;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Configuration;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Key = Ryujinx.Input.Key;
using StickInputId = Ryujinx.Input.StickInputId;

namespace Ryujinx.Ava.Ui.Windows
{
    public class ControllerSettingsWindow : UserControl
    {
        private bool _isWaitingForInput;
        private bool _mousePressed;
        private bool _middleMousePressed;
        private bool _dialogOpen;
        private bool _loaded;

        public Grid SettingButtons { get; set; }
        public ToggleButton CurrentToggledButton { get; set; }
        public ControllerSettingsViewModel ViewModel { get; set; }

        public ControllerSettingsWindow()
        {
            ViewModel = new ControllerSettingsViewModel(this);

            InitializeComponent();

            foreach (ILogical visual in SettingButtons.GetLogicalDescendants())
            {
                if (visual is ToggleButton button && !(visual is CheckBox))
                {
                    button.Checked   += Button_Checked;
                    button.Unchecked += Button_Unchecked;
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            SettingButtons = this.FindControl<Grid>("SettingButtons");
        }

        public void LoadBindings()
        {
            if (!_loaded)
            {
                _loaded = true;
                DataContext = ViewModel;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (CurrentToggledButton != null && !CurrentToggledButton.IsPointerOver)
            {
                ToggleButton button = CurrentToggledButton;

                CurrentToggledButton = null;
                button.IsChecked     = false;
            }
        }

        private void Button_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                if (button == CurrentToggledButton)
                {
                    return;
                }

                if (CurrentToggledButton == null && (bool)button.IsChecked)
                {
                    CurrentToggledButton = button;
                    _isWaitingForInput   = false;

                    bool isStick = button.Tag != null && button.Tag.ToString() == "stick";
                    
                    FocusManager.Instance.Focus(this, NavigationMethod.Pointer);

                    Task.Run(() => HandleButtonPressed(button, isStick));
                }
                else
                {
                    if (CurrentToggledButton != null)
                    {
                        ToggleButton oldButton = CurrentToggledButton;

                        CurrentToggledButton = null;
                        oldButton.IsChecked  = false;
                        button.IsChecked     = false;
                    }
                }
            }
        }

        public void SaveCurrentProfile()
        {
            ViewModel.Save();
        }

        public void HandleButtonPressed(ToggleButton button, bool forStick)
        {
            if (_isWaitingForInput)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    button.IsChecked = false;
                });

                return;
            }

            _mousePressed      = false;
            _isWaitingForInput = true;

            PointerPressed += MouseClick;

            IButtonAssigner assigner = CreateButtonAssigner(forStick);
            IKeyboard       keyboard = (IKeyboard)ViewModel.AvaloniaKeyboardDriver.GetGamepad("0"); // Open Avalonia keyboard for cancel operations.

            Thread inputThread = new(() =>
            {
                assigner.Initialize();

                while (true)
                {
                    if (!_isWaitingForInput)
                    {
                        return;
                    }

                    Thread.Sleep(10);

                    assigner.ReadInput();

                    if (_mousePressed || keyboard.IsPressed(Key.Escape) || assigner.HasAnyButtonPressed() || assigner.ShouldCancel())
                    {
                        break;
                    }
                }

                Dispatcher.UIThread.Post(() =>
                {
                    string pressedButton = assigner.GetPressedButton();
                    if (_middleMousePressed)
                    {
                        try
                        {
                            SetButtonText(button, "Unbound");
                        }
                        catch { }
                    }
                    else if (pressedButton != "")
                    {
                        try
                        {
                            SetButtonText(button, pressedButton);
                        }
                        catch { }
                    }

                    ViewModel.IsModified = true;

                    keyboard.Dispose();
                    
                    _middleMousePressed = false;
                    _isWaitingForInput  = false;
                    
                    button = CurrentToggledButton;
                    
                    CurrentToggledButton = null;

                    if (button != null)
                    {
                        button.IsChecked = false;
                    }

                    PointerPressed -= MouseClick;

                    static void SetButtonText(ToggleButton button, string text)
                    {
                        ILogical textBlock = button.GetLogicalDescendants().First(x => x is TextBlock);

                        if (textBlock != null && textBlock is TextBlock block)
                        {
                            block.Text = text;
                        }
                    }
                });
            });

            inputThread.Name         = "GUI.InputThread";
            inputThread.IsBackground = true;
            inputThread.Start();
        }

        private IButtonAssigner CreateButtonAssigner(bool forStick)
        {
            IButtonAssigner assigner;

            string selected = ViewModel.Devices[ViewModel.Device].Id;

            if (selected.StartsWith("keyboard"))
            {
                assigner = new KeyboardKeyAssigner((IKeyboard)ViewModel.SelectedGamepad);
            }
            else if (selected.StartsWith("controller"))
            {
                InputConfig config = ConfigurationState.Instance.Hid.InputConfig.Value.Find(inputConfig => inputConfig.Id == ViewModel.SelectedGamepad.Id);

                assigner = new GamepadButtonAssigner(ViewModel.SelectedGamepad, (config as StandardControllerInputConfig).TriggerThreshold, forStick);
            }
            else
            {
                throw new Exception("Controller not supported");
            }

            return assigner;
        }
        private void Button_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CurrentToggledButton != null)
            {
                ToggleButton button = CurrentToggledButton;

                CurrentToggledButton = null;
                button.IsChecked     = false;
            }
        }

        private void MouseClick(object sender, PointerPressedEventArgs e)
        {
            _mousePressed = true;

            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
            {
                _middleMousePressed = true;
            }
        }

        private async void PlayerIndexBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.IsModified && !_dialogOpen)
            {
                _dialogOpen = true;

                var result = await ContentDialogHelper.CreateConfirmationDialog(this.GetVisualRoot() as StyleableWindow,
                    LocaleManager.Instance["DialogControllerSettingsModifiedConfirmMessage"],
                    LocaleManager.Instance["DialogControllerSettingsModifiedConfirmSubMessage"]);

                if (result == UserResult.Yes)
                {
                    ViewModel.Save();
                }

                _dialogOpen = false;

                ViewModel.IsModified = false;

                if (e.AddedItems.Count > 0)
                {
                    (PlayerIndex key, _) = (KeyValuePair<PlayerIndex, string>)e.AddedItems[0];
                    ViewModel.PlayerId = key;
                }
            }
        }

        public void Dispose()
        {
            ViewModel.Dispose();
        }
    }
}