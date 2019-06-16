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

        [GUI] Window         MainWin;
        [GUI] CheckMenuItem  FullScreen;
        [GUI] MenuItem       NFC;
        [GUI] TreeView       GameTable;
        [GUI] ScrolledWindow GameTableWindow;
        [GUI] GLArea         GLScreen;

        public MainMenu(string[] args, Application _gtkapp) : this(new Builder("Ryujinx.GUI.MainMenu.glade"), args, _gtkapp) { }

        private MainMenu(Builder builder, string[] args, Application _gtkapp) : base(builder.GetObject("MainWin").Handle)
        {
            renderer = new OglRenderer();

            audioOut = InitializeAudioEngine();

            device   = new HLE.Switch(renderer, audioOut);

            Configuration.Load(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));
            Configuration.InitialConfigure(device);

            ApplicationLibrary.Init();

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

                GameTable.AppendColumn("Icon", new CellRendererPixbuf(), "pixbuf", 0);
                //GameTable.AppendColumn("Game", new CellRendererText(), "text", 1);
                //GameTable.AppendColumn("Version", new CellRendererText(), "text", 2);
                //GameTable.AppendColumn("DLC", new CellRendererText(), "text", 3);
                //GameTable.AppendColumn("Time Played", new CellRendererText(), "text", 4);
                //GameTable.AppendColumn("Last Played", new CellRendererText(), "text", 5);
                GameTable.AppendColumn("File Size", new CellRendererText(), "text", 6);
                GameTable.AppendColumn("Path", new CellRendererText(), "text", 7);

                TableStore      = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
                GameTable.Model = TableStore;

                UpdateGameTable();
            }
        }

        public static void UpdateGameTable()
        {
            TableStore.Clear();
            ApplicationLibrary.Init();

            foreach (ApplicationLibrary.ApplicationData AppData in ApplicationLibrary.ApplicationLibraryData)
            {
                TableStore.AppendValues(AppData.Icon, AppData.Game, AppData.Version, AppData.DLC, AppData.TP, AppData.LP, AppData.FileSize, AppData.Path);
            }
        }

        public static void ApplyTheme()
        {
            var settings          = Settings.Default;
            settings.XftRgba      = "rgb";
            settings.XftDpi       = 96;
            settings.XftHinting   = 1;
            settings.XftHintstyle = "hintfull";

            CssProvider css_provider = new CssProvider();

            if (SwitchSettings.SwitchConfig.EnableCustomTheme)
            {
                if (File.Exists(SwitchSettings.SwitchConfig.CustomThemePath) && (System.IO.Path.GetExtension(SwitchSettings.SwitchConfig.CustomThemePath) == ".css"))
                {
                    css_provider.LoadFromPath(SwitchSettings.SwitchConfig.CustomThemePath);
                }
                else
                {
                    Logger.PrintError(LogClass.Application, $"The \"custom_theme_path\" section in \"Config.json\" contains an invalid path: \"{SwitchSettings.SwitchConfig.CustomThemePath}\"");
                }
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

                DiscordPresence.Details               = $"Playing {device.System.TitleName}";
                DiscordPresence.State                 = string.IsNullOrWhiteSpace(device.System.TitleID) ? string.Empty : device.System.TitleID.ToUpper();
                DiscordPresence.Assets.LargeImageText = device.System.TitleName;
                DiscordPresence.Assets.SmallImageKey  = "ryujinx";
                DiscordPresence.Assets.SmallImageText = "Ryujinx is an emulator for the Nintendo Switch";
                DiscordPresence.Timestamps            = new Timestamps(DateTime.UtcNow);

                DiscordClient.SetPresence(DiscordPresence);
            }
        }
        
        //Events
        private void Row_Activated(object obj, RowActivatedArgs args)
        {
            TableStore.GetIter(out TreeIter treeiter, new TreePath(args.Path.ToString()));
            string path = (string)TableStore.GetValue(treeiter, 7);

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

        private void ReturnMain_Pressed(object o, EventArgs args)
        {
            GameTableWindow.Show();
            GLScreen.Hide();
            //will also have to write logic to kill running game
        }

        private void FullScreen_Toggled(object o, EventArgs args)
        {
            if (FullScreen.Active == true) { Fullscreen(); }
            else { Unfullscreen(); }
        }

        private void Settings_Pressed(object o, EventArgs args)
        {
            var SettingsWin = new SwitchSettings(device);
            gtkapp.Register(GLib.Cancellable.Current);
            gtkapp.AddWindow(SettingsWin);
            SettingsWin.Show();
        }

        private void Profiler_Pressed(object o, EventArgs args)
        {
            //Soon™
        }

        private void NFC_Pressed(object o, EventArgs args)
        {
            FileChooserDialog fc = new FileChooserDialog("Choose the file to open", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
            fc.Filter            = new FileFilter();
            fc.Filter.AddPattern("*.bin");

            if (fc.Run() == (int)ResponseType.Accept)
            {
                //Soon™
            }
            fc.Destroy();
        }

        private void About_Pressed(object o, EventArgs args)
        {
            var AboutWin = new AboutWindow();
            gtkapp.Register(GLib.Cancellable.Current);
            gtkapp.AddWindow(AboutWin);
            AboutWin.Show();
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
