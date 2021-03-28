using Gtk;
using Ryujinx.HLE;
using Ryujinx.Ui.Widgets;

namespace Ryujinx.Ui.Applet
{
    internal class GtkDynamicTextInputHandler : IDynamicTextInputHandler
    {
        private readonly Window _parent;
        private readonly RawInputToTextEntry _inputToTextEntry = new RawInputToTextEntry();

        public event DynamicTextChangedEvent TextChanged;

        public GtkDynamicTextInputHandler(Window parent)
        {
            _parent = parent;
            _parent.KeyPressEvent += HandleKeyPressEvent;
            _parent.KeyReleaseEvent += HandleKeyReleaseEvent;
        }

        [GLib.ConnectBefore()]
        private void HandleKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Return)
            {
                InvokeTextChanged(true, false);
                _inputToTextEntry.Text = "";
            }
            else if (args.Event.Key == Gdk.Key.Page_Up) // TODO other key?
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