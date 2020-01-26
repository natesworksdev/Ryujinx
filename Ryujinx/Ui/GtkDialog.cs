using Gtk;
using Ryujinx.Updater.Parser;
using System;
using System.IO;
using System.Reflection;

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

        internal static MessageDialog CreateInfoDialog(string iconType, string titleMessage, string textMessage, string secText)
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
            return messageDialog;
        }

        internal static MessageDialog CreateProgressDialog(string iconType, string titleMessage, string textMessage, string secText)
        {
            MessageDialog messageDialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.None, null)
            {
                Title = titleMessage,
                Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), $"Ryujinx.Ui.assets.{iconType}.png"),
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
