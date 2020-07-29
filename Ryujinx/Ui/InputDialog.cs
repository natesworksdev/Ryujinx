using Gtk;
using System;

namespace Ryujinx.Ui
{
    public class InputDialog : MessageDialog
    {
        private Label _validationInfo;

        public Entry InputEntry { get; }
        public Button OkButton { get; }
        public Button CancelButton { get; }

        private (int Min, int Max) _stringrange;
        public (int Min, int Max) AllowedInputLength
        {
            get => _stringrange;
            set
            {
                _stringrange = value;
                OnInputChanged(this, EventArgs.Empty);

                if(_stringrange.Max < 0)
                {
                    _validationInfo.Visible = false;
                }
                else
                {
                    _validationInfo.Visible = true;
                    _validationInfo.Markup = $"<i>Must be {_stringrange.Min}-{_stringrange.Max} characters long</i>";
                }
            }
        }

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

            _stringrange = (0, -1); // default disable length check
        }

        public void OnInputChanged(object sender, EventArgs e)
        {
            if(_stringrange.Max < 0)
            {
                OkButton.Sensitive = true;

                return;
            }

            OkButton.Sensitive = _stringrange.Min <= InputEntry.Text.Length && InputEntry.Text.Length <= _stringrange.Max;
        }
    }
}