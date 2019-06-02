using Gtk;
using Ryujinx.Common.Logging;
using System;
using System.IO;

namespace Ryujinx
{
    public class MainMenU
    {
        public static void MainMenu(HLE.Switch device)
        {
            Application.Init();

            Window MainWin = new Window(WindowType.Toplevel);
            MainWin.Title = "Ryujinx";
            MainWin.Icon = new Gdk.Pixbuf("./ryujinx.png");
            MainWin.SetDefaultSize(1280, 745);
            MainWin.WindowPosition = WindowPosition.Center;

            if (device.System.State.DiscordIntergrationEnabled == true)
            {
                Program.DiscordPresence.Details = "Main Menu";
                Program.DiscordPresence.State = "Idling";
                Program.DiscordPresence.Timestamps = new DiscordRPC.Timestamps(DateTime.UtcNow);

                Program.DiscordClient.SetPresence(Program.DiscordPresence);
            }

            VBox box = new VBox();
            MenuBar MenuBar = new MenuBar();
            TreeView GameTable = new TreeView();

            //Menu Bar
            MenuItem FileMenu = new MenuItem("File");
            MenuBar.Append(FileMenu);
            Menu FileSubmenu = new Menu();
            FileMenu.Submenu = FileSubmenu;

            MenuItem LoadApplicationFile = new MenuItem("Load Application from File");
            FileSubmenu.Append(LoadApplicationFile);
            LoadApplicationFile.Activated += (o, args) => Load_Application_File(o, args, MainWin, device);

            MenuItem LoadApplicationFolder = new MenuItem("Load Application from Folder");
            FileSubmenu.Append(LoadApplicationFolder);
            LoadApplicationFolder.Activated += (o, args) => Load_Application_Folder(o, args, MainWin, device);

            FileSubmenu.Append(new SeparatorMenuItem());

            MenuItem Exit = new MenuItem("Exit");
            FileSubmenu.Append(Exit);
            Exit.Activated += (o, args) => Exit_Pressed(o, args, MainWin);

            MenuItem OptionsMenu = new MenuItem("Options");
            MenuBar.Append(OptionsMenu);
            Menu OptionsSubmenu = new Menu();
            OptionsMenu.Submenu = OptionsSubmenu;

            FileSubmenu.Append(new SeparatorMenuItem());

            MenuItem GeneralSettingsMenu = new MenuItem("General Settings");
            OptionsSubmenu.Append(GeneralSettingsMenu);
            GeneralSettingsMenu.Activated += new EventHandler(General_Settings_Pressed);

            MenuItem ControlSettingsMenu = new MenuItem("Control Settings");
            OptionsSubmenu.Append(ControlSettingsMenu);
            ControlSettingsMenu.Activated += new EventHandler(Control_Settings_Pressed);

            MenuItem ToolsMenu = new MenuItem("Tools");
            MenuBar.Append(ToolsMenu);
            Menu ToolsSubmenu = new Menu();
            ToolsMenu.Submenu = ToolsSubmenu;

            MenuItem NFC = new MenuItem("Scan NFC Tag from File");
            ToolsSubmenu.Append(NFC);
            NFC.Sensitive = false;
            NFC.Activated += (o, args) => NFC_Pressed(o, args, MainWin);

            MenuItem HelpMenu = new MenuItem("Help");
            MenuBar.Append(HelpMenu);
            Menu HelpSubmenu = new Menu();
            HelpMenu.Submenu = HelpSubmenu;

            MenuItem About = new MenuItem("About");
            HelpSubmenu.Append(About);
            About.Activated += new EventHandler(About_Pressed);

            box.PackStart(MenuBar, false, false, 0);

            //Games grid thing
            ListStore TableStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
            GameTable.RowActivated += (o, args) => Row_Activated(o, args, TableStore, MainWin, device);

            GameTable.AppendColumn("Icon", new CellRendererPixbuf(), "pixbuf", 0);
            GameTable.AppendColumn("Game", new CellRendererText(), "text", 1);
            GameTable.AppendColumn("Version", new CellRendererText(), "text", 2);
            GameTable.AppendColumn("Time Played", new CellRendererText(), "text", 3);
            GameTable.AppendColumn("Last Played", new CellRendererText(), "text", 4);
            GameTable.AppendColumn("Path", new CellRendererText(), "text", 5);

            string dat = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameDirs.dat");
            if (File.Exists(dat) == false) { File.Create(dat).Close(); }
            string[] GameDirs = File.ReadAllLines(dat);
            string[] Games = new string[] { };

            foreach (string GameDir in GameDirs)
            {
                if (Directory.Exists(GameDir) == false) { Logger.PrintError(LogClass.Application, "There is an invalid game directory in \"GameDirs.dat\""); return; }
                DirectoryInfo GameDirInfo = new DirectoryInfo(GameDir);
                foreach (var Game in GameDirInfo.GetFiles())
                {
                    if ((Path.GetExtension(Game.ToString()) == ".xci") || (Path.GetExtension(Game.ToString()) == ".nca") || (Path.GetExtension(Game.ToString()) == ".nsp") || (Path.GetExtension(Game.ToString()) == ".pfs0") || (Path.GetExtension(Game.ToString()) == ".nro") || (Path.GetExtension(Game.ToString()) == ".nso"))
                    {
                        Array.Resize(ref Games, Games.Length + 1);
                        Games[Games.Length - 1] = Game.ToString();
                    }
                }
            }
            foreach (string GamePath in Games)
            {
                TableStore.AppendValues(new Gdk.Pixbuf("./ryujinx.png", 50, 50), "", "", "", "", GamePath);
            }

            GameTable.Model = TableStore;
            box.PackStart(GameTable, true, true, 0);

            MainWin.DeleteEvent += (obj, args) => Window_Close(obj, args, MainWin);
            MainWin.Add(box);
            MainWin.ShowAll();

            Application.Run();
        }

        static void Row_Activated(object obj, RowActivatedArgs args, ListStore TableStore, Window window, HLE.Switch device)
        {
            TableStore.GetIter(out TreeIter treeiter, new TreePath(args.Path.ToString()));
            string path = (string)TableStore.GetValue(treeiter, 5);

            switch (Path.GetExtension(path).ToLowerInvariant())
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
            window.Destroy();
            Application.Quit();
        }

        static void Load_Application_File(object o, EventArgs args, Window window, HLE.Switch device)
        {
            FileChooserDialog fc = new FileChooserDialog("Choose the file to open", window, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
            fc.Filter = new FileFilter();
            fc.Filter.AddPattern("*.nsp");
            fc.Filter.AddPattern("*.xci");
            fc.Filter.AddPattern("*.nca");
            fc.Filter.AddPattern("*.nro");
            fc.Filter.AddPattern("*.nso");

            if (fc.Run() == (int)ResponseType.Accept)
            {
                switch (Path.GetExtension(fc.Filename).ToLowerInvariant())
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
                window.Destroy();
                Application.Quit();
            }
            fc.Destroy();
        }

        static void Load_Application_Folder(object o, EventArgs args, Window window, HLE.Switch device)
        {
            FileChooserDialog fc = new FileChooserDialog("Choose the folder to open", window, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

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
                window.Destroy();
                Application.Quit();
            }
            fc.Destroy();
        }

        static void Exit_Pressed(object o, EventArgs args, Window window)
        {
            window.Destroy();
            Application.Quit();
        }

        static void Window_Close(object obj, DeleteEventArgs args, Window window)
        {
            window.Destroy();
            Application.Quit();
            args.RetVal = true;
        }

        static void General_Settings_Pressed(object o, EventArgs args)
        {
            GeneralSettings.GeneralSettingsMenu();
        }

        static void Control_Settings_Pressed(object o, EventArgs args)
        {
            ControlSettings.ControlSettingsMenu();
        }

        static void NFC_Pressed(object o, EventArgs args, Window window)
        {
            FileChooserDialog fc = new FileChooserDialog("Choose the file to open", window, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
            fc.Filter = new FileFilter();
            fc.Filter.AddPattern("*.bin");

            if (fc.Run() == (int)ResponseType.Accept)
            {
                Console.WriteLine(fc.Filename);
            }
            fc.Destroy();
        }

        static void About_Pressed(object o, EventArgs args)
        {
            AboutDialog about = new AboutDialog();
            about.ProgramName = "Ryujinx";
            about.Version = "Version x.x.x";
            about.Authors = new string[] { "gdkchan", "Ac_K", "LDj3SNuD", "emmauss", "MerryMage", "MS-DOS1999", "Thog", "jD", "BaronKiko", "Dr.Hacknik", "Lordmau5", "(and Xpl0itR did a bit of work too :D)" };
            about.Copyright = "Unlicense";
            about.Comments = "Ryujinx is an emulator for the Nintendo Switch";
            about.Website = "https://github.com/Ryujinx/Ryujinx";
            about.Copyright = "Unlicense";
            about.WindowPosition = WindowPosition.Center;
            about.Run();
            about.Destroy();
        }
    }
}
