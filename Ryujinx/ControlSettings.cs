using Gtk;
using System;

namespace Ryujinx
{
    public class ControlSettings
    {
        public static void ControlSettingsMenu()
        {
            Window CSWin = new Window(WindowType.Toplevel);
            CSWin.Title = "Control Settings";
            CSWin.Icon = new Gdk.Pixbuf("./ryujinx.png");
            CSWin.SetDefaultSize(854, 360);
            CSWin.Resizable = false;
            CSWin.WindowPosition = WindowPosition.Center;

            VBox box = new VBox(false, 2);

            //settings stuff will replace this block
            Label myLabel = new Label { Text = "General Settings" };
            box.PackStart(myLabel, true, true, 3);

            HBox ButtonBox = new HBox(true, 3);
            Alignment BoxAlign = new Alignment(1, 0, 0, 0);

            Button ok = new Button("OK");
            ok.Pressed += (o, args) => OK_Pressed(o, args, CSWin);
            ButtonBox.Add(ok);

            Button close = new Button("Close");
            close.Pressed += (o, args) => Close_Pressed(o, args, CSWin);
            ButtonBox.Add(close);

            BoxAlign.SetPadding(0, 5, 0, 7);
            BoxAlign.Add(ButtonBox);
            box.PackStart(BoxAlign, false, false, 3);

            CSWin.Add(box);
            CSWin.ShowAll();
        }

        static void OK_Pressed(object o, EventArgs args, Window window)
        {
            //save settings stuff will go here
            window.Destroy();
        }

        static void Close_Pressed(object o, EventArgs args, Window window)
        {
            window.Destroy();
        }
    }
}
