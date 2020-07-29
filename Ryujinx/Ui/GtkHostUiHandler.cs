using Gtk;
using Ryujinx.HLE;
using Ryujinx.HLE.HOS.Applets;
using System.Threading;

namespace Ryujinx.Ui
{
    internal class GtkHostUiHandler : IHostUiHandler
    {
        private readonly Window _parent;

        public GtkHostUiHandler(Window parent)
        {
            _parent = parent;
        }

        public string DisplayInputDialog(SoftwareKeyboardUiArgs args)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            string inputText = string.IsNullOrEmpty(args.InitialText) ? new string('?', args.AllowedStringSize.Max) : args.InitialText;

            Application.Invoke(delegate
            {
                var swkbdDialog = new InputDialog(_parent)
                {
                    Title = "Software Keyboard",
                    Text = args.HeaderText,
                    SecondaryText = args.SubtitleText,
                    AllowedInputLength = args.AllowedStringSize
                };

                swkbdDialog.InputEntry.Text = args.InitialText;
                swkbdDialog.InputEntry.PlaceholderText = args.GuideText;
                swkbdDialog.OkButton.Label = args.SubmitText;

                if (swkbdDialog.Run() == (int)ResponseType.Ok)
                {
                    inputText = swkbdDialog.InputEntry.Text;
                }

                mre.Set();
                swkbdDialog.Dispose();
            });

            mre.WaitOne();

            return inputText;
        }
    }
}
