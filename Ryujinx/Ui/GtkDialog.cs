using Gtk;
using System.Reflection;
using Ryujinx.Updater.Parser;
using System.IO;
using System;

namespace Ryujinx.Ui
{
    internal class GtkDialog
    {
        internal static void CreateDialog(string title, string text, string secondaryText)
        {
            MessageDialog errorDialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, null)
            {
                Title          = title,
                Icon           = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png"),
                Text           = text,
                SecondaryText  = secondaryText,
                WindowPosition = WindowPosition.Center
            };
            errorDialog.SetSizeRequest(100, 20);
            errorDialog.Run();
            errorDialog.Dispose();
        }

        internal static void CreateWarningDialog(string text, string secondaryText)
        {
            CreateDialog("Ryujinx - Warning", text, secondaryText);
        }

        internal static void CreateErrorDialog(string errorMessage)
        {
            CreateDialog("Ryujinx - Error", "Ryujinx has encountered an error", errorMessage);
        }

        internal static MessageDialog CreateAcceptDialog(string iconType, string acceptMessage)
        {
            MessageDialog messageDialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, null)
            {
                Title = "Ryujinx - Update",
                Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Update.png"),
                Text = "Would you like to update?",
                SecondaryText = "Version " + acceptMessage + " is available.",
                WindowPosition = WindowPosition.Center
            };
            messageDialog.SetSizeRequest(100, 20);
            return messageDialog; 
        }

        internal static MessageDialog CreateInfoDialog(string iconType, string titleMessage, string textMessage, string secText)
        {
            MessageDialog messageDialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, null)
            {
                Title = titleMessage,
                Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets." + iconType +".png"),
                Text = textMessage,
                SecondaryText = secText,
                WindowPosition = WindowPosition.Center
            };
            messageDialog.SetSizeRequest(100, 20);
            return messageDialog;
        }

        internal static MessageDialog CreateProgressDialog(string iconType, string titleMessage, string textMessage, string secText)
        {
            MessageDialog messageDialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.None, null)
            {
                Title = titleMessage,
                Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets." + iconType + ".png"),
                Text = textMessage,
                SecondaryText = secText,
                WindowPosition = WindowPosition.Center
            };
            Uri URL = new Uri(UpdateParser.BuildArt);
            UpdateParser.Package.DownloadFileAsync(URL, Path.Combine(UpdateParser.RyuDir, "Data", "Update", "RyujinxPackage.zip"));
            messageDialog.SetSizeRequest(100, 20);
            return messageDialog;
        }
    }
}
