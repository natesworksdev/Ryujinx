using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;
using System;
using System.Linq;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class ControllerSettingsWindow : UserControl
    {
        private bool _dialogOpen;

        private ButtonKeyAssigner _currentAssigner;
        internal ControllerSettingsViewModel ViewModel { get; set; }

        public ControllerSettingsWindow()
        {
            DataContext = ViewModel = new ControllerSettingsViewModel(this);

            InitializeComponent();

            foreach (ToggleButton button in SettingButtons.GetLogicalDescendants().OfType<ToggleButton>())
            {
                if (button is not CheckBox)
                {
                    button.Click += Button_Click;
                }
            }
            
            PointerPressed += MouseClick;
            DetachedFromVisualTree += OnViewDetachedFromVisualTree;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_currentAssigner != null && _currentAssigner.ToggledButton != null && !_currentAssigner.ToggledButton.IsPointerOver)
            {
                CancelAssignment();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                //In any case, if a button is clicked we want to cancel the current assignment
                //  New button -> Cancel old button assignment
                //  Same button -> Cancel current assignment
                if (_currentAssigner != null)
                {
                    ToggleButton oldButton = _currentAssigner.ToggledButton;

                    CancelAssignment();

                    if (button == oldButton)
                    {
                        return;
                    }
                }

                IKeyboard keyboard = (IKeyboard)ViewModel.AvaloniaKeyboardDriver.GetGamepad("0"); // Open Avalonia keyboard for cancel operations.
                IButtonAssigner assigner = CreateButtonAssigner(Equals(button.Tag, "stick"));

                _currentAssigner = new ButtonKeyAssigner(button);
                _currentAssigner.ButtonAssigned += OnButtonAssigned;
                _currentAssigner.GetInputAndAssign(assigner, keyboard);
            }
        }

        private void OnButtonAssigned(object sender, ButtonKeyAssigner.ButtonAssignedEventArgs args)
        {
            //In case the current assignment is canceled by another button click, we don't want to set the new assigner to null
            if (_currentAssigner == sender)
            {
                _currentAssigner = null;
            }

            args.Button.IsChecked = false;

            if (args.IsAssigned)
            {
                ViewModel.IsModified = true;
                CheckNextVisibleToggleButton(args.Button);
            }
        }

        private void OnViewDetachedFromVisualTree(object sender, Avalonia.VisualTreeAttachmentEventArgs e)
        {
            CancelAssignment();
        }

        public void SaveCurrentProfile()
        {
            ViewModel.Save();
        }

        private IButtonAssigner CreateButtonAssigner(bool forStick) => ViewModel.Devices[ViewModel.Device].Type switch
        {
            DeviceType.Keyboard => new KeyboardKeyAssigner((IKeyboard)ViewModel.SelectedGamepad),
            DeviceType.Controller => new GamepadButtonAssigner(ViewModel.SelectedGamepad,
                (ViewModel.Config as StandardControllerInputConfig).TriggerThreshold, forStick),
            _ => throw new Exception("Controller not supported")
        };

        /// <summary>
        /// Finds the next visible toggle button (if there is any left), sets it to checked and raises the click event
        /// </summary>
        /// <param name="currentButton">Currently checked button</param>
        private void CheckNextVisibleToggleButton(ToggleButton currentButton)
        {
            ToggleButton nextButton = SettingButtons.GetLogicalDescendants().OfType<ToggleButton>()
                .Where(element => element is not CheckBox)
                .Where(element => element.IsEffectivelyVisible)
                .SkipWhile(element => element != currentButton).Skip(1)
                .FirstOrDefault();

            if (nextButton != null)
            {
                nextButton.IsChecked = true;
                nextButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void MouseClick(object sender, PointerPressedEventArgs e)
        {
            bool shouldUnbind = e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed;
            CancelAssignment(shouldUnbind);
        }

        private async void PlayerIndexBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.IsModified && !_dialogOpen)
            {
                _dialogOpen = true;

                var result = await ContentDialogHelper.CreateConfirmationDialog(
                    LocaleManager.Instance[LocaleKeys.DialogControllerSettingsModifiedConfirmMessage],
                    LocaleManager.Instance[LocaleKeys.DialogControllerSettingsModifiedConfirmSubMessage],
                    LocaleManager.Instance[LocaleKeys.InputDialogYes],
                    LocaleManager.Instance[LocaleKeys.InputDialogNo],
                    LocaleManager.Instance[LocaleKeys.RyujinxConfirm]);

                if (result == UserResult.Yes)
                {
                    ViewModel.Save();
                }

                _dialogOpen = false;

                ViewModel.IsModified = false;

                if (e.AddedItems.Count > 0)
                {
                    var player = (PlayerModel)e.AddedItems[0];
                    ViewModel.PlayerId = player.Id;
                }
            }
        }

        private void CancelAssignment(bool unbind = false)
        {
            _currentAssigner?.Cancel(unbind);
            _currentAssigner = null;
        }

        public void Dispose()
        {
            CancelAssignment();
            ViewModel.Dispose();

            PointerPressed -= MouseClick;
            DetachedFromVisualTree -= OnViewDetachedFromVisualTree;
        }
    }
}