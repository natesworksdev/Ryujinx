using Gtk;
using System;
using System.IO;
using System.Reflection;

namespace Ryujinx
{
    public class ControlSettings
    {
        public static void ControlSettingsMenu()
        {
            Window CSWin         = new Window(WindowType.Toplevel);
            CSWin.Title          = "Control Settings";
            CSWin.Icon           = new Gdk.Pixbuf("./ryujinxIcon.png");
            CSWin.Resizable      = false;
            CSWin.WindowPosition = WindowPosition.Center;
            CSWin.SetDefaultSize(854, 360);

            VBox box = new VBox(false, 2);

            //settings stuff will replace this block
            Label myLabel = new Label { Text = "Control Settings" };
            box.PackStart(myLabel, true, true, 3);

            HBox ButtonBox     = new HBox(true, 3);
            Alignment BoxAlign = new Alignment(1, 0, 0, 0);

            Button Save   = new Button("Save");
            Save.Pressed += (o, args) => Save_Pressed(o, args, CSWin);
            ButtonBox.Add(Save);

            Button Cancel   = new Button("Cancel");
            Cancel.Pressed += (o, args) => Cancel_Pressed(o, args, CSWin);
            ButtonBox.Add(Cancel);

            BoxAlign.SetPadding(0, 5, 0, 7);
            BoxAlign.Add(ButtonBox);
            box.PackStart(BoxAlign, false, false, 3);

            CSWin.Add(box);
            CSWin.ShowAll();
        }

        static void Save_Pressed(object o, EventArgs args, Window window)
        {
            //save settings stuff will go here
            window.Destroy();
        }

        static void Cancel_Pressed(object o, EventArgs args, Window window)
        {
            window.Destroy();
        }
    }
}
