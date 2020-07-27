using Gtk;
using System;

namespace Ryujinx.Ui
{
    public class InputDialog : MessageDialog
    {
        private Entry _input;
        private Button _okButton;

        public string InputText { get => _input.Text; set => _input.Text = value; }
        public string InputPlaceholderText { get => _input.PlaceholderText; set => _input.PlaceholderText = value; }
        public string SubmitButtonText { get => _okButton.Label; set => _okButton.Label = value; }

        public InputDialog(Window parent)
            : base(parent, DialogFlags.Modal | DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.None, null)
        {
            SetDefaultSize(300, 0);

            _input = new Entry() { Visible = true };
            _input.Activated += (object sender, EventArgs e) => Respond(ResponseType.Ok);

            _okButton = (Button)AddButton("OK", ResponseType.Ok);
            AddButton("Cancel", ResponseType.Cancel);

            ((Box)MessageArea).PackEnd(_input, true, true, 4);
        }
    }
}