using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Reflection;

namespace Ryujinx.Ava.UI.Helpers
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
            var args = Activator.CreateInstance(typeof(TextInputEventArgs), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[]
            {
                text,
                KeyboardDevice.Instance,
                this,
                TextInputEvent
            }, null) as TextInputEventArgs;
            OnTextInput(args);
        }
    }
}