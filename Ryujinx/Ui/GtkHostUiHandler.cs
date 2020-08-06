using Gtk;
using Ryujinx.Common.Logging;
using Ryujinx.HLE;
using Ryujinx.HLE.HOS.Applets;
using System;
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

        public bool DisplayMessageDialog(string title, string message)
        {
            ManualResetEvent dialogCloseEvent = new ManualResetEvent(false);
            bool okPressed = false;

            Application.Invoke(delegate
            {
                MessageDialog msgDialog = null;
                try
                {
                    msgDialog = new MessageDialog(_parent, DialogFlags.DestroyWithParent, MessageType.Info, ButtonsType.Ok, null)
                    {
                        Title = title,
                        Text = message,
                        UseMarkup = true
                    };

                    msgDialog.SetDefaultSize(400, 0);

                    msgDialog.Response += (object o, ResponseArgs args) =>
                    {
                        if (args.ResponseId == ResponseType.Ok) okPressed = true;
                        dialogCloseEvent.Set();
                        msgDialog?.Dispose();
                    };

                    msgDialog.Show();
                }
                catch (Exception e)
                {
                    Logger.Error?.Print(LogClass.Application, $"Error displaying Message Dialog: {e}");
                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();

            return okPressed;
        }

        public bool DisplayInputDialog(SoftwareKeyboardUiArgs args, out string userText)
        {
            ManualResetEvent dialogCloseEvent = new ManualResetEvent(false);
            bool okPressed = false;
            bool error = false;
            string inputText = args.InitialText ?? "";

            Application.Invoke(delegate
            {
                try
                {
                    var swkbdDialog = new InputDialog(_parent)
                    {
                        Title = "Software Keyboard",
                        Text = args.HeaderText,
                        SecondaryText = args.SubtitleText
                    };

                    swkbdDialog.InputEntry.Text = inputText;
                    swkbdDialog.InputEntry.PlaceholderText = args.GuideText;
                    swkbdDialog.OkButton.Label = args.SubmitText;

                    swkbdDialog.SetInputLengthValidation(args.StringLengthMin, args.StringLengthMax);

                    if (swkbdDialog.Run() == (int)ResponseType.Ok)
                    {
                        inputText = swkbdDialog.InputEntry.Text;
                        okPressed = true;
                    }

                    swkbdDialog.Dispose();
                }
                catch (Exception e)
                {
                    error = true;
                    Logger.Error?.Print(LogClass.Application, $"Error displaying Software Keyboard: {e}");
                }
                finally
                {
                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();

            userText = error ? null : inputText;

            return error || okPressed;
        }
    }
}
