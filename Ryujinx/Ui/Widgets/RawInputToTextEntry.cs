using Gtk;

namespace Ryujinx.Ui.Widgets
{
    public class RawInputToTextEntry : Entry
    {
        public void SendKeyPressEvent(object o, KeyPressEventArgs args)
        {
            base.OnKeyPressEvent(args.Event);
        }

        public void SendKeyReleaseEvent(object o, KeyReleaseEventArgs args)
        {
            base.OnKeyReleaseEvent(args.Event);
        }
    }
}
