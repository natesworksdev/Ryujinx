using DiscordRPC;
using Gtk;
using GUI = Gtk.Builder.ObjectAttribute;
using Ryujinx.Audio;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Gal.OpenGL;
using Ryujinx.Profiler;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ryujinx
{
    public class MainMenu : Window
    {
        public static bool DiscordIntegrationEnabled { get; set; }

        public static DiscordRpcClient DiscordClient;

        public static RichPresence DiscordPresence;

        private static IGalRenderer renderer;

        private static IAalOutput audioOut;

        private static HLE.Switch device { get; set; }

        private static Application gtkapp { get; set; }

        private static ListStore TableStore { get; set; }

        //UI Controls
        [GUI] Window         MainWin;
        [GUI] MenuItem       NFC;
        [GUI] MenuItem       ControlSettingsMenu;
        [GUI] TreeView       GameTable;
        [GUI] ScrolledWindow GameTableWindow;
        [GUI] GLArea         GLScreen;

        public MainMenu(string[] args, Application _gtkapp) : this(new Builder("Ryujinx.GUI.MainMenu.glade"), args, _gtkapp) { }

        private MainMenu(Builder builder, string[] args, Application _gtkapp) : base(builder.GetObject("MainWin").Handle)
        {
            renderer = new OglRenderer();

            audioOut = InitializeAudioEngine();

            device = new HLE.Switch(renderer, audioOut);

            Configuration.Load(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));
            Configuration.InitialConfigure(device);

            gtkapp = _gtkapp;

            ApplyTheme();

            DeleteEvent += Window_Close;

            if (DiscordIntegrationEnabled)
            {
                DiscordClient = new DiscordRpcClient("568815339807309834");
                DiscordPresence = new RichPresence
                {
                    Assets = new Assets
                    {
                        LargeImageKey = "ryujinx",
                        LargeImageText = "Ryujinx is an emulator for the Nintendo Switch"
                    },
                    Details = "Main Menu",
                    State = "Idling",
                    Timestamps = new Timestamps(DateTime.UtcNow)
                };

                DiscordClient.Initialize();
                DiscordClient.SetPresence(DiscordPresence);
            }

            builder.Autoconnect(this);
            MainWin.Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxIcon.png");

            if (args.Length == 1)
            {
                GameTableWindow.Hide();
                GLScreen.Show();

                LoadApplication(args[0]);

                using (GlScreen screen = new GlScreen(device, renderer))
                {
                    screen.MainLoop();

                    Profile.FinishProfiling();

                    device.Dispose();
                }
            }
            else
            {
                GameTableWindow.Show();
                GLScreen.Hide();

                NFC.Sensitive = false;
                ControlSettingsMenu.Sensitive = false;

                GameTable.AppendColumn("Icon", new CellRendererPixbuf(), "pixbuf", 0);
                GameTable.AppendColumn("Game", new CellRendererText(), "text", 1);
                GameTable.AppendColumn("Version", new CellRendererText(), "text", 2);
                GameTable.AppendColumn("DLC", new CellRendererText(), "text", 3);
                GameTable.AppendColumn("Time Played", new CellRendererText(), "text", 4);
                GameTable.AppendColumn("Last Played", new CellRendererText(), "text", 5);
                GameTable.AppendColumn("Path", new CellRendererText(), "text", 6);

                TableStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

                foreach (ApplicationLibrary.ApplicationData AppData in ApplicationLibrary.ApplicationLibraryData)
                {
                    TableStore.AppendValues(AppData.Icon, AppData.Game, AppData.Version, AppData.DLC, AppData.TP, AppData.LP, AppData.Path);
                }

                GameTable.Model = TableStore;
            }
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
                css_provider.LoadFromPath("./GUI/assets/Theme.css");
            }

            StyleContext.AddProviderForScreen(Gdk.Screen.Default, css_provider, 800);
        }

        private static void LoadApplication(string path)
        {
            if (Directory.Exists(path))
            {
                string[] romFsFiles = Directory.GetFiles(path, "*.istorage");

                if (romFsFiles.Length == 0)
                {
                    romFsFiles = Directory.GetFiles(path, "*.romfs");
                }

                if (romFsFiles.Length > 0)
                {
                    Logger.PrintInfo(LogClass.Application, "Loading as cart with RomFS.");
                    device.LoadCart(path, romFsFiles[0]);
                }
                else
                {
                    Logger.PrintInfo(LogClass.Application, "Loading as cart WITHOUT RomFS.");
                    device.LoadCart(path);
                }
            }
            else if (File.Exists(path))
            {
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
            }
            else
            {
                Logger.PrintWarning(LogClass.Application, "Please specify a valid XCI/NCA/NSP/PFS0/NRO file");
            }

            if (DiscordIntegrationEnabled)
            {
                if (File.ReadAllLines(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RPsupported.dat")).Contains(device.System.TitleID))
                {
                    DiscordPresence.Assets.LargeImageKey = device.System.TitleID;
                }

                DiscordPresence.Details = $"Playing {device.System.TitleName}";
                DiscordPresence.State = device.System.TitleID.ToUpper();
                DiscordPresence.Assets.LargeImageText = device.System.TitleName;
                DiscordPresence.Assets.SmallImageKey = "ryujinx";
                DiscordPresence.Assets.SmallImageText = "Ryujinx is an emulator for the Nintendo Switch";
                DiscordPresence.Timestamps = new Timestamps(DateTime.UtcNow);

                DiscordClient.SetPresence(DiscordPresence);
            }
        }
        
        //Events
        private void Row_Activated(object obj, RowActivatedArgs args)
        {
            TableStore.GetIter(out TreeIter treeiter, new TreePath(args.Path.ToString()));
            string path = (string)TableStore.GetValue(treeiter, 6);

            LoadApplication(path);

            GameTableWindow.Hide();
            GLScreen.Show();

            Destroy();

            using (GlScreen screen = new GlScreen(device, renderer))
            {
                screen.MainLoop();

                Profile.FinishProfiling();

                device.Dispose();
            }

            audioOut.Dispose();
            Logger.Shutdown();
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
                LoadApplication(fc.Filename);

                GameTableWindow.Hide();
                GLScreen.Show();

                Destroy();

                using (GlScreen screen = new GlScreen(device, renderer))
                {
                    screen.MainLoop();

                    Profile.FinishProfiling();

                    device.Dispose();
                }

                audioOut.Dispose();
                Logger.Shutdown();
            }

            fc.Destroy();
        }

        private void Load_Application_Folder(object o, EventArgs args)
        {
            FileChooserDialog fc = new FileChooserDialog("Choose the folder to open", this, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            if (fc.Run() == (int)ResponseType.Accept)
            {
                LoadApplication(fc.Filename);

                GameTableWindow.Hide();
                GLScreen.Show();

                Destroy();

                using (GlScreen screen = new GlScreen(device, renderer))
                {
                    screen.MainLoop();

                    Profile.FinishProfiling();

                    device.Dispose();
                }

                audioOut.Dispose();
                Logger.Shutdown();
            }

            fc.Destroy();
        }

        private void Exit_Pressed(object o, EventArgs args)
        {
            audioOut.Dispose();
            DiscordClient.Dispose();
            Logger.Shutdown();
            Environment.Exit(0);
        }

        private void Window_Close(object obj, DeleteEventArgs args)
        {
            audioOut.Dispose();
            DiscordClient.Dispose();
            Logger.Shutdown();
            Environment.Exit(0);
        }

        private void General_Settings_Pressed(object o, EventArgs args)
        {
            var GSWin = new GeneralSettings(device);
            gtkapp.Register(GLib.Cancellable.Current);
            gtkapp.AddWindow(GSWin);
            GSWin.Show();
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
                Icon           = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxIcon.png"),
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

        /// <summary>
        /// Picks an <see cref="IAalOutput"/> audio output renderer supported on this machine
        /// </summary>
        /// <returns>An <see cref="IAalOutput"/> supported by this machine</returns>
        private static IAalOutput InitializeAudioEngine()
        {
            if (SoundIoAudioOut.IsSupported)
            {
                return new SoundIoAudioOut();
            }
            else if (OpenALAudioOut.IsSupported)
            {
                return new OpenALAudioOut();
            }
            else
            {
                return new DummyAudioOut();
            }
        }
    }
}
