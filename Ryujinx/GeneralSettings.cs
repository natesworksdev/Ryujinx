using Gtk;
using System;
using System.IO;
using System.Reflection;

namespace Ryujinx
{
    public class GeneralSettings
    {
        public static void GeneralSettingsMenu()
        {
            Window GSWin         = new Window(WindowType.Toplevel);
            GSWin.Title          = "General Settings";
            GSWin.Resizable      = false;
            GSWin.WindowPosition = WindowPosition.Center;
            GSWin.SetDefaultSize(854, 360);

            VBox box = new VBox(false, 2);

            //Load Icon
            using (Stream iconstream   = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ryujinx.ryujinxIcon.png"))
            using (StreamReader reader = new StreamReader(iconstream))
            {
                Gdk.Pixbuf RyujinxIcon = new Gdk.Pixbuf(iconstream);
                GSWin.Icon             = RyujinxIcon;
            }

            //settings stuff will replace this block
            Label myLabel = new Label { Text = "General Settings" };
            box.PackStart(myLabel, true, true, 3);

            HBox ButtonBox     = new HBox(true, 3);
            Alignment BoxAlign = new Alignment(1, 0, 0, 0);

            Button Save   = new Button("Save");
            Save.Pressed += (o, args) => Save_Pressed(o, args, GSWin);
            ButtonBox.Add(Save);

            Button Cancel   = new Button("Cancel");
            Cancel.Pressed += (o, args) => Cancel_Pressed(o, args, GSWin);
            ButtonBox.Add(Cancel);

            BoxAlign.SetPadding(0, 5, 0, 7);
            BoxAlign.Add(ButtonBox);
            box.PackStart(BoxAlign, false, false, 3);

            GSWin.Add(box);
            GSWin.ShowAll();
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
