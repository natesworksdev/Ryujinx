using Gtk;

namespace Ryujinx.Ui.Widgets
{
    public class GtkInputDialog : MessageDialog
    {
        public Entry InputEntry { get; }

        public GtkInputDialog(Window parent, string title, string mainText, uint inputMax) : base(parent, DialogFlags.Modal | DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.OkCancel, null)
        {
            SetDefaultSize(300, 0);

            Title = title;

            Label mainTextLabel = new()
            {
                Text = mainText
            };

            InputEntry = new Entry
            {
                MaxLength = (int)inputMax
            };

            Label inputMaxTextLabel = new()
            {
                Text = $"(Max length: {inputMax})"
            };

#pragma warning disable IDE0055 // Disable formatting
            ((Box)MessageArea).PackStart(mainTextLabel,     true, true, 0);
            ((Box)MessageArea).PackStart(InputEntry,        true, true, 5);
            ((Box)MessageArea).PackStart(inputMaxTextLabel, true, true, 0);
#pragma warning restore IDE0055

            ShowAll();
        }
    }
}
