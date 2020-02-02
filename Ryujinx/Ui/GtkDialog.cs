using Gtk;
using System;
using System.IO;
using System.Reflection;

namespace Ryujinx.Ui
{
    internal class GtkDialog
    {
        internal static void CreateErrorDialog(string errorMessage)
        {
            MessageDialog errorDialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, null)
            {
                Title          = "Ryujinx - Error",
                Icon           = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png"),
                Text           = "Ryujinx has encountered an error",
                SecondaryText  = errorMessage,
                WindowPosition = WindowPosition.Center
            };
            errorDialog.SetSizeRequest(100, 20);
            errorDialog.Run();
            errorDialog.Dispose();
        }

        internal static MessageDialog CreateAcceptDialog(string iconType, string titleMessage, string textMessage, string secText)
        {
            MessageDialog messageDialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, null)
            {
                Title = titleMessage,
                Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), $"Ryujinx.Ui.assets.{iconType}.png"),
                Text = textMessage,
                SecondaryText = secText,
                WindowPosition = WindowPosition.Center
            };
            messageDialog.SetSizeRequest(100, 20);
            return messageDialog; 
        }

        internal static void CreateInfoDialog(string iconType, string titleMessage, string textMessage, string secText)
        {
            MessageDialog messageDialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, null)
            {
                Title = titleMessage,
                Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), $"Ryujinx.Ui.assets.{iconType}.png"),
                Text = textMessage,
                SecondaryText = secText,
                WindowPosition = WindowPosition.Center
            };
            messageDialog.SetSizeRequest(100, 20);
            messageDialog.Run();
            messageDialog.Dispose();
        }
    }
}
