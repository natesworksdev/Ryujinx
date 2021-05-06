using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;

namespace Ryujinx.Ava.Ui.Controls
{
    public class OffscreenTextBox : TextBox
    {
        public RoutedEvent<KeyEventArgs> GetKeyDownRoutedEvent()
        {
            return KeyDownEvent;
        }
        
        public RoutedEvent<KeyEventArgs> GetKeyUpRoutedEvent()
        {
            return KeyUpEvent;
        }

        public void SendKeyDownEvent(KeyEventArgs keyEvent)
        {
            OnKeyDown(keyEvent);
        }

        public void SendKeyUpEvent(KeyEventArgs keyEvent)
        {
            OnKeyUp(keyEvent);
        }

        public void SendText(string text)
        {
            OnTextInput(new TextInputEventArgs(){Text = text, Device = KeyboardDevice.Instance, Source = this, RoutedEvent = TextInputEvent});
           /* InputManager.Instance.ProcessInput(new RawTextInputEventArgs(
                KeyboardDevice.Instance,
                (ulong)DateTime.Now.Ticks,
                (Window)this.GetVisualRoot(),
                text
            ));*/
        }
    }
}