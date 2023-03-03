using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Input.Assigner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HidKey = Ryujinx.Common.Configuration.Hid.Key;
using Key = Ryujinx.Input.Key;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class HotkeyButtonKeyAssigner
    {
        internal class ButtonAssignedEventArgs : EventArgs
        {
            public ToggleButton Button { get; }
            public bool IsAssigned { get; }

            public ButtonAssignedEventArgs(ToggleButton button, bool isAssigned)
            {
                Button = button;
                IsAssigned = isAssigned;
            }
        }

        public ToggleButton ToggledButton { get; set; }

        private readonly Action<string, Hotkey> _updateHotkeyCallback;
        private bool _isWaitingForInput;
        private bool _shouldUnbind;
        public event EventHandler<ButtonAssignedEventArgs> ButtonAssigned;

        public HotkeyButtonKeyAssigner(ToggleButton toggleButton, Action<string, Hotkey> updateHotkeyCallback)
        {
            ToggledButton = toggleButton;
            _updateHotkeyCallback = updateHotkeyCallback;
        }

        public async void GetInputAndAssign(IButtonAssigner assigner)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ToggledButton.IsChecked = true;
            });

            if (_isWaitingForInput)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Cancel();
                });

                return;
            }

            _isWaitingForInput = true;

            assigner.Initialize();

            await Task.Run(async () =>
            {
                while (true)
                {
                    if (!_isWaitingForInput)
                    {
                        return;
                    }

                    await Task.Delay(10);

                    assigner.ReadInput();

                    if (assigner.HasAnyButtonPressed() || assigner.ShouldCancel())
                    {
                        break;
                    }
                }
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IEnumerable<PressedButton> pressedButtons = assigner.GetPressedButtons();
                bool isAssigned = pressedButtons.Any();

                if (_shouldUnbind)
                {
                    _updateHotkeyCallback((string)ToggledButton.Tag, new Hotkey(HidKey.Unbound));
                }
                else if (isAssigned)
                {
                    _updateHotkeyCallback((string)ToggledButton.Tag, CreateHotkey(pressedButtons));
                }

                _shouldUnbind = false;
                _isWaitingForInput = false;

                ToggledButton.IsChecked = false;

                ButtonAssigned?.Invoke(this, new ButtonAssignedEventArgs(ToggledButton, isAssigned));
            });
        }

        private Hotkey CreateHotkey(IEnumerable<PressedButton> buttons)
        {
            Key pressedKey = Key.Unknown;
            KeyModifier modifier = KeyModifier.None;
            ulong gamepadInputMask = 0UL;

            foreach (PressedButton button in buttons)
            {
                if (button.Type == PressedButtonType.Key)
                {
                    Key key = button.AsKey();

                    if (key == Key.ControlLeft || key == Key.ControlRight)
                    {
                        modifier |= KeyModifier.Control;
                    }
                    else if (key == Key.AltLeft || key == Key.AltRight)
                    {
                        modifier |= KeyModifier.Alt;
                    }
                    else if (key == Key.ShiftLeft || key == Key.ShiftRight)
                    {
                        modifier |= KeyModifier.Shift;
                    }
                    else
                    {
                        pressedKey = key;
                    }
                }
                else if (button.Type == PressedButtonType.Button)
                {
                    gamepadInputMask |= 1UL << (int)button.AsGamepadButtonInputId();
                }
            }

            return new Hotkey((HidKey)pressedKey, modifier, gamepadInputMask);
        }

        public void Cancel(bool shouldUnbind = false)
        {
            _isWaitingForInput = false;
            ToggledButton.IsChecked = false;
            _shouldUnbind = shouldUnbind;
        }
    }
}
