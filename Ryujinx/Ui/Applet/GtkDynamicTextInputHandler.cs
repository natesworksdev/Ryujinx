using Gtk;
using Ryujinx.HLE;
using Ryujinx.Ui.Widgets;

namespace Ryujinx.Ui.Applet
{
    internal class GtkDynamicTextInputHandler : IDynamicTextInputHandler
    {
        private readonly Window _parent;
        private readonly RawInputToTextEntry _inputToTextEntry = new RawInputToTextEntry();

        private readonly Gdk.Key _acceptKey;
        private readonly Gdk.Key _cancelKey;

        public event DynamicTextChangedEvent TextChanged;

        public GtkDynamicTextInputHandler(Window parent, Gdk.Key acceptKey, Gdk.Key cancelKey)
        {
            _parent = parent;
            _parent.KeyPressEvent += HandleKeyPressEvent;
            _parent.KeyReleaseEvent += HandleKeyReleaseEvent;
            _acceptKey = acceptKey;
            _cancelKey = cancelKey;
        }

        [GLib.ConnectBefore()]
        private void HandleKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (args.Event.Key == _acceptKey)
            {
                InvokeTextChanged(true, false);
                _inputToTextEntry.Text = "";
            }
            else if (args.Event.Key == _cancelKey)
            {
                InvokeTextChanged(false, true);
                _inputToTextEntry.Text = "";
            }
            else
            {
                _inputToTextEntry.SendKeyPressEvent(o, args);
                InvokeTextChanged(false, false);
            }
        }

        private void InvokeTextChanged(bool isAccept, bool isCancel)
        {
            _inputToTextEntry.GetSelectionBounds(out int selectionStart, out int selectionEnd);
            TextChanged?.Invoke(_inputToTextEntry.Text, selectionStart, selectionEnd, isAccept, isCancel);
        }

        [GLib.ConnectBefore()]
        private void HandleKeyReleaseEvent(object o, KeyReleaseEventArgs args)
        {
            _inputToTextEntry.SendKeyReleaseEvent(o, args);
        }

        public void SetText(string text)
        {
            _inputToTextEntry.Text = text;
            _inputToTextEntry.Position = text.Length;
        }

        public void SetMaxLength(int maxLength)
        {
            _inputToTextEntry.MaxLength = maxLength;
        }

        public void Dispose()
        {
            _parent.KeyPressEvent -= HandleKeyPressEvent;
            _parent.KeyReleaseEvent -= HandleKeyReleaseEvent;
        }
    }
}