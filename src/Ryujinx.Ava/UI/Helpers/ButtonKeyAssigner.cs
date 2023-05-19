using Avalonia;
using Avalonia.Data;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class ButtonKeyAssigner
    {
        internal class ButtonAssignedEventArgs : EventArgs
        {
            public ToggleButton Button { get; }
            public bool IsAssigned { get; }
            public object Key { get;  }

            public ButtonAssignedEventArgs(ToggleButton button, bool isAssigned, object key)
            {
                Button = button;
                IsAssigned = isAssigned;
                Key = key;
            }
        }

        public ToggleButton ToggledButton { get; set; }

        private bool _isWaitingForInput;
        private bool _shouldUnbind;
        public event EventHandler<ButtonAssignedEventArgs> ButtonAssigned;

        public ButtonKeyAssigner(ToggleButton toggleButton)
        {
            ToggledButton = toggleButton;
        }

        public async void GetInputAndAssign(IButtonAssigner assigner, IKeyboard keyboard = null)
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

                    if (assigner.HasAnyButtonPressed() || assigner.ShouldCancel() || (keyboard != null && keyboard.IsPressed(Key.Escape)))
                    {
                        break;
                    }
                }
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                object pressedButton = assigner.GetPressedButton();

                if (_shouldUnbind)
                {
                    pressedButton = null;
                }

                _shouldUnbind = false;
                _isWaitingForInput = false;

                ToggledButton.IsChecked = false;

                ButtonAssigned?.Invoke(this, new ButtonAssignedEventArgs(ToggledButton, pressedButton != null, pressedButton));

            });
        }

        public void Cancel(bool shouldUnbind = false)
        {
            _isWaitingForInput = false;
            ToggledButton.IsChecked = false;
            _shouldUnbind = shouldUnbind;
        }
    }
}