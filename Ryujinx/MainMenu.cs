using Gtk;
using GUI = Gtk.Builder.ObjectAttribute;
using Ryujinx.Common.Logging;
using System;
using System.IO;

namespace Ryujinx
{
    public class MainMenu : Window
    {
        internal HLE.Switch device { get; private set; }

        internal Application gtkapp { get; private set; }

        internal ListStore TableStore { get; private set; }

        //UI Controls
        [GUI] MenuItem NFC;
        [GUI] TreeView GameTable;

        public MainMenu(HLE.Switch _device, Application _gtkapp) : this(new Builder("Ryujinx.MainMenu.glade"), _device, _gtkapp) { }

        private MainMenu(Builder builder, HLE.Switch _device, Application _gtkapp) : base(builder.GetObject("MainWin").Handle)
        {
            device = _device;
            gtkapp = _gtkapp;

            if (device.System.State.DiscordIntergrationEnabled == true)
            {
                Program.DiscordPresence.Details    = "Main Menu";
                Program.DiscordPresence.State      = "Idling";
                Program.DiscordPresence.Timestamps = new DiscordRPC.Timestamps(DateTime.UtcNow);

                Program.DiscordClient.SetPresence(Program.DiscordPresence);
            }

            builder.Autoconnect(this);
            ApplyTheme();

            DeleteEvent += Window_Close;

            //disable some buttons
            NFC.Sensitive      = false;

            //Games grid thing
            GameTable.AppendColumn("Icon",        new CellRendererPixbuf(), "pixbuf", 0);
            GameTable.AppendColumn("Game",        new CellRendererText(),   "text",   1);
            GameTable.AppendColumn("Version",     new CellRendererText(),   "text",   2);
            GameTable.AppendColumn("DLC",         new CellRendererText(),   "text",   3);
            GameTable.AppendColumn("Time Played", new CellRendererText(),   "text",   4);
            GameTable.AppendColumn("Last Played", new CellRendererText(),   "text",   5);
            GameTable.AppendColumn("Path",        new CellRendererText(),   "text",   6);

            TableStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

            foreach (ApplicationLibrary.ApplicationData AppData in ApplicationLibrary.ApplicationLibraryData)
            {
                TableStore.AppendValues(AppData.Icon, AppData.Game, AppData.Version, AppData.DLC, AppData.TP, AppData.LP, AppData.Path);
            }

            GameTable.Model = TableStore;
        }

        public static void ApplyTheme()
        {
            var settings          = Settings.Default;
            settings.XftRgba      = "rgb";
            settings.XftDpi = 96;
            settings.XftHinting   = 1;
            settings.XftHintstyle = "hintfull";

            CssProvider css_provider = new CssProvider();

            if (GeneralSettings.SwitchConfig.EnableCustomTheme)
            {
                css_provider.LoadFromPath(GeneralSettings.SwitchConfig.CustomThemePath);
            }
            else
            {
                css_provider.LoadFromPath("Theme.css");
            }

            StyleContext.AddProviderForScreen(Gdk.Screen.Default, css_provider, 800);
        }

        //Events
        private void Row_Activated(object obj, RowActivatedArgs args)
        {
            TableStore.GetIter(out TreeIter treeiter, new TreePath(args.Path.ToString()));
            string path = (string)TableStore.GetValue(treeiter, 6);

            switch (System.IO.Path.GetExtension(path).ToLowerInvariant())
            {
                case ".xci":
                    Logger.PrintInfo(LogClass.Application, "Loading as XCI.");
                    device.LoadXci(path);
                    break;
                case ".nca":
                    Logger.PrintInfo(LogClass.Application, "Loading as NCA.");
                    device.LoadNca(path);
                    break;
                case ".nsp":
                case ".pfs0":
                    Logger.PrintInfo(LogClass.Application, "Loading as NSP.");
                    device.LoadNsp(path);
                    break;
                default:
                    Logger.PrintInfo(LogClass.Application, "Loading as homebrew.");
                    device.LoadProgram(path);
                    break;
            }

            Destroy();
            Application.Quit();
        }

        private void Load_Application_File(object o, EventArgs args)
        {
            FileChooserDialog fc = new FileChooserDialog("Choose the file to open", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
            fc.Filter            = new FileFilter();
            fc.Filter.AddPattern("*.nsp");
            fc.Filter.AddPattern("*.xci");
            fc.Filter.AddPattern("*.nca");
            fc.Filter.AddPattern("*.nro");
            fc.Filter.AddPattern("*.nso");

            if (fc.Run() == (int)ResponseType.Accept)
            {
                switch (System.IO.Path.GetExtension(fc.Filename).ToLowerInvariant())
                {
                    case ".xci":
                        Logger.PrintInfo(LogClass.Application, "Loading as XCI.");
                        device.LoadXci(fc.Filename);
                        break;
                    case ".nca":
                        Logger.PrintInfo(LogClass.Application, "Loading as NCA.");
                        device.LoadNca(fc.Filename);
                        break;
                    case ".nsp":
                    case ".pfs0":
                        Logger.PrintInfo(LogClass.Application, "Loading as NSP.");
                        device.LoadNsp(fc.Filename);
                        break;
                    default:
                        Logger.PrintInfo(LogClass.Application, "Loading as homebrew.");
                        device.LoadProgram(fc.Filename);
                        break;
                }

                Destroy();
                Application.Quit();
            }

            fc.Destroy();
        }

        private void Load_Application_Folder(object o, EventArgs args)
        {
            FileChooserDialog fc = new FileChooserDialog("Choose the folder to open", this, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            if (fc.Run() == (int)ResponseType.Accept)
            {
                string[] romFsFiles = Directory.GetFiles(fc.Filename, "*.istorage");

                if (romFsFiles.Length == 0)
                {
                    romFsFiles = Directory.GetFiles(fc.Filename, "*.romfs");
                }

                if (romFsFiles.Length > 0)
                {
                    Logger.PrintInfo(LogClass.Application, "Loading as cart with RomFS.");
                    device.LoadCart(fc.Filename, romFsFiles[0]);
                }
                else
                {
                    Logger.PrintInfo(LogClass.Application, "Loading as cart WITHOUT RomFS.");
                    device.LoadCart(fc.Filename);
                }

                Destroy();
                Application.Quit();
            }

            fc.Destroy();
        }

        private void Exit_Pressed(object o, EventArgs args)
        {
            Environment.Exit(0);
        }

        private void Window_Close(object obj, DeleteEventArgs args)
        {
            Environment.Exit(0);
        }

        private void General_Settings_Pressed(object o, EventArgs args)
        {
            var GSWin = new GeneralSettings(device);
            gtkapp.Register(GLib.Cancellable.Current);
            gtkapp.AddWindow(GSWin);
            GSWin.Show();
        }

        private void Control_Settings_Pressed(object o, EventArgs args)
        {
            ControlSettings.ControlSettingsMenu();
        }

        private void NFC_Pressed(object o, EventArgs args)
        {
            FileChooserDialog fc = new FileChooserDialog("Choose the file to open", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
            fc.Filter            = new FileFilter();
            fc.Filter.AddPattern("*.bin");

            if (fc.Run() == (int)ResponseType.Accept)
            {
                Console.WriteLine(fc.Filename); //temp
            }
            fc.Destroy();
        }

        private void About_Pressed(object o, EventArgs args)
        {
            AboutDialog about  = new AboutDialog
            {
                ProgramName    = "Ryujinx",
                Icon           = new Gdk.Pixbuf("./ryujinxIcon.png"),
                Version        = "Version x.x.x",
                Authors        = new string[] { "gdkchan", "Ac_K", "LDj3SNuD", "emmauss", "MerryMage", "MS-DOS1999", "Thog", "jD", "BaronKiko", "Dr.Hacknik", "Lordmau5", "(and Xpl0itR did a bit of work too :D)" },
                Copyright      = "Unlicense",
                Comments       = "Ryujinx is an emulator for the Nintendo Switch",
                Website        = "https://github.com/Ryujinx/Ryujinx",
                WindowPosition = WindowPosition.Center,
            };

            about.Run();
            about.Destroy();
        }
    }
}
