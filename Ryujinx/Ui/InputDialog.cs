using Gtk;
using System;

namespace Ryujinx.Ui
{
    public class InputDialog : MessageDialog
    {
        public Entry InputEntry { get; }
        public Button OkButton { get; }
        public Button CancelButton { get; }

        private (int Min, int Max) _stringrange;
        public (int Min, int Max) AllowedInputSize
        {
            get => _stringrange;
            set
            {
                _stringrange = value; 
                OnInputChanged(this, EventArgs.Empty);
            }
        }

        public InputDialog(Window parent)
            : base(parent, DialogFlags.Modal | DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.None, null)
        {
            SetDefaultSize(300, 0);

            InputEntry = new Entry() { Visible = true };
            InputEntry.Activated += (object sender, EventArgs e) => { if (OkButton.IsSensitive) Respond(ResponseType.Ok); };
            InputEntry.Changed += OnInputChanged;

            OkButton = (Button)AddButton("OK", ResponseType.Ok);
            CancelButton = (Button)AddButton("Cancel", ResponseType.Cancel);

            ((Box)MessageArea).PackEnd(InputEntry, true, true, 4);

            _stringrange = (0, 1024); // decent defaults
        }

        public void OnInputChanged(object sender, EventArgs e)
        {
            OkButton.Sensitive = _stringrange.Min <= InputEntry.Text.Length && InputEntry.Text.Length <= _stringrange.Max;
        }
    }
}