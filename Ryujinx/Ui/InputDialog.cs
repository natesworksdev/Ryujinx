using Gtk;
using System;

namespace Ryujinx.Ui
{
    public class InputDialog : MessageDialog
    {
        private int _inputmin, _inputmax;
        private Predicate<int> _checkLength;
        private Label _validationInfo;

        public Entry InputEntry { get; }
        public Button OkButton { get; }
        public Button CancelButton { get; }

        public InputDialog(Window parent)
            : base(parent, DialogFlags.Modal | DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.None, null)
        {
            SetDefaultSize(300, 0);

            _validationInfo = new Label() { Visible = false };

            InputEntry = new Entry() { Visible = true };
            InputEntry.Activated += (object sender, EventArgs e) => { if (OkButton.IsSensitive) Respond(ResponseType.Ok); };
            InputEntry.Changed += OnInputChanged;

            OkButton = (Button)AddButton("OK", ResponseType.Ok);
            CancelButton = (Button)AddButton("Cancel", ResponseType.Cancel);

            ((Box)MessageArea).PackEnd(_validationInfo, true, true, 0);
            ((Box)MessageArea).PackEnd(InputEntry, true, true, 4);

            SetInputLengthValidation(0, int.MaxValue); // disable by default
        }

        public void SetInputLengthValidation(int min, int max)
        {
            _inputmin = Math.Min(min, max);
            _inputmax = Math.Max(min, max);

            _validationInfo.Visible = false;

            if (_inputmin <= 0 && _inputmax == int.MaxValue) // disable
            {
                _validationInfo.Visible = false;
                _checkLength = (length) => true;
            }
            else if (_inputmin > 0 && _inputmax == int.MaxValue)
            {
                _validationInfo.Visible = true;
                _validationInfo.Markup = $"<i>Must be at least {_inputmin} characters long</i>";
                _checkLength = (length) => _inputmin <= length;
            }
            else
            {
                _validationInfo.Visible = true;
                _validationInfo.Markup = $"<i>Must be {_inputmin}-{_inputmax} characters long</i>";
                _checkLength = (length) => _inputmin <= length && length <= _inputmax;
            }

            OnInputChanged(this, EventArgs.Empty);
        }

        private void OnInputChanged(object sender, EventArgs e)
        {
            OkButton.Sensitive = _checkLength(InputEntry.Text.Length);
        }
    }
}