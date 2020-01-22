using Gtk;
using System.Reflection;
using Ryujinx.Updater.Parser;
using System.IO;
using System;

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

        internal static async System.Threading.Tasks.Task<MessageDialog> CreateProgressDialogAsync(bool isInstall, string iconType, string titleMessage, string textMessage, string secText)
        {
            MessageDialog messageDialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.None, null)
            {
                Title = titleMessage,
                Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets." + iconType + ".png"),
                Text = textMessage,
                SecondaryText = secText,
                WindowPosition = WindowPosition.Center
            };
            messageDialog.SetSizeRequest(100, 20);
            if (isInstall == true)
            {
                await UpdateParser.ExtractPackageAsync();
            }
            else
            {
                Uri URL = new Uri(UpdateParser._BuildArt);
                UpdateParser._Package.DownloadFileAsync(URL, Path.Combine(UpdateParser._RyuDir, "Data", "Update", "RyujinxPackage.zip"));
            }
            return messageDialog;
        }
    }
}
