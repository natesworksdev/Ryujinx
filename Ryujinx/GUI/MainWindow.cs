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
using System.Threading;

namespace Ryujinx
{
    public class MainWindow : Window
    {
        public static bool DiscordIntegrationEnabled { get; set; }

        public static DiscordRpcClient DiscordClient;

        public static RichPresence DiscordPresence;

        private static IGalRenderer _renderer;

        private static IAalOutput _audioOut;

        internal static HLE.Switch _device;

        private static Application _gtkapp;

        private static ListStore _TableStore;

        private static bool _GameLoaded = false;

#pragma warning disable 649
        [GUI] Window         MainWin;
        [GUI] CheckMenuItem  FullScreen;
        [GUI] MenuItem       Nfc;
        [GUI] Box            Box;
        [GUI] ScrolledWindow GameTableWindow;
        [GUI] TreeView       GameTable;
        [GUI] GLArea         GlScreen;
#pragma warning restore 649

        public MainWindow(string[] args, Application gtkapp) : this(new Builder("Ryujinx.GUI.MainWindow.glade"), args, gtkapp) { }

        private MainWindow(Builder builder, string[] args, Application gtkapp) : base(builder.GetObject("MainWin").Handle)
        {
            _renderer = new OglRenderer();

            _audioOut = InitializeAudioEngine();

            _device   = new HLE.Switch(_renderer, _audioOut);

            Configuration.Load(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));
            Configuration.InitialConfigure(_device);

            ApplicationLibrary.Init();

            _gtkapp = gtkapp;

            ApplyTheme();

            DeleteEvent += Window_Close;

            if (DiscordIntegrationEnabled)
            {
                DiscordClient   = new DiscordRpcClient("568815339807309834");
                DiscordPresence = new RichPresence
                {
                    Assets = new Assets
                    {
                        LargeImageKey  = "ryujinx",
                        LargeImageText = "Ryujinx is an emulator for the Nintendo Switch"
                    },
                    Details    = "Main Menu",
                    State      = "Idling",
                    Timestamps = new Timestamps(DateTime.UtcNow)
                };

                DiscordClient.Initialize();
                DiscordClient.SetPresence(DiscordPresence);
            }

            builder.Autoconnect(this);
            MainWin.Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxIcon.png");

            if (args.Length == 1)
            {
                //Box.Remove(GameTableWindow);

                LoadApplication(args[0]);
            }
            else
            {
                Box.Remove(GlScreen);

                Nfc.Sensitive = false;

                GameTable.AppendColumn("Icon"       , new CellRendererPixbuf(), "pixbuf", 0);
                GameTable.AppendColumn("Game"       , new CellRendererText()  , "text"  , 1);
                GameTable.AppendColumn("Time Played", new CellRendererText()  , "text"  , 2);
                GameTable.AppendColumn("Last Played", new CellRendererText()  , "text"  , 3);
                GameTable.AppendColumn("File Size"  , new CellRendererText()  , "text"  , 4);
                GameTable.AppendColumn("Path"       , new CellRendererText()  , "text"  , 5);

                _TableStore     = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
                GameTable.Model = _TableStore;

                UpdateGameTable();
            }
        }

        public static void UpdateGameTable()
        {
            _TableStore.Clear();
            ApplicationLibrary.Init();

            foreach (ApplicationLibrary.ApplicationData AppData in ApplicationLibrary.ApplicationLibraryData)
            {
                _TableStore.AppendValues(AppData.Icon, $"{AppData.GameName}\n{AppData.GameId.ToUpper()}", AppData.TimePlayed, AppData.LastPlayed, AppData.FileSize, AppData.Path);
            }
        }

        public static void ApplyTheme()
        {
            Settings settings     = Settings.Default;
            settings.XftRgba      = "rgb";
            settings.XftDpi       = 96;
            settings.XftHinting   = 1;
            settings.XftHintstyle = "hintfull";

            CssProvider cssProvider = new CssProvider();

            if (SwitchSettings.SwitchConfig.EnableCustomTheme)
            {
                if (File.Exists(SwitchSettings.SwitchConfig.CustomThemePath) && (System.IO.Path.GetExtension(SwitchSettings.SwitchConfig.CustomThemePath) == ".css"))
                {
                    cssProvider.LoadFromPath(SwitchSettings.SwitchConfig.CustomThemePath);
                }
                else
                {
                    Logger.PrintError(LogClass.Application, $"The \"custom_theme_path\" section in \"Config.json\" contains an invalid path: \"{SwitchSettings.SwitchConfig.CustomThemePath}\"");
                }
            }
            else
            {
                cssProvider.LoadFromPath("./GUI/assets/Theme.css");
            }

            StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, 800);
        }

        private static void LoadApplication(string path)
        {
            if (_GameLoaded)
            {
                MessageDialog eRrOr = new MessageDialog(null, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "A game has already been loaded, please unload the game and try again");
                eRrOr.SetSizeRequest(100, 20);
                eRrOr.Title = "Ryujinx - Error";
                eRrOr.Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxIcon.png");
                eRrOr.WindowPosition = WindowPosition.Center;
                eRrOr.Run();
                eRrOr.Destroy();
            }

            else if (Directory.Exists(path))
            {
                string[] romFsFiles = Directory.GetFiles(path, "*.istorage");

                if (romFsFiles.Length == 0)
                {
                    romFsFiles = Directory.GetFiles(path, "*.romfs");
                }

                if (romFsFiles.Length > 0)
                {
                    Logger.PrintInfo(LogClass.Application, "Loading as cart with RomFS.");
                    _device.LoadCart(path, romFsFiles[0]);
                }
                else
                {
                    Logger.PrintInfo(LogClass.Application, "Loading as cart WITHOUT RomFS.");
                    _device.LoadCart(path);
                }

                new Thread(new ThreadStart(CreateGameWindow)).Start();

                _GameLoaded = true;
            }

            else if (File.Exists(path))
            {
                switch (System.IO.Path.GetExtension(path).ToLowerInvariant())
                {
                    case ".xci":
                        Logger.PrintInfo(LogClass.Application, "Loading as XCI.");
                        _device.LoadXci(path);
                        break;
                    case ".nca":
                        Logger.PrintInfo(LogClass.Application, "Loading as NCA.");
                        _device.LoadNca(path);
                        break;
                    case ".nsp":
                    case ".pfs0":
                        Logger.PrintInfo(LogClass.Application, "Loading as NSP.");
                        _device.LoadNsp(path);
                        break;
                    default:
                        Logger.PrintInfo(LogClass.Application, "Loading as homebrew.");
                        _device.LoadProgram(path);
                        break;
                }

                new Thread(new ThreadStart(CreateGameWindow)).Start();

                _GameLoaded = true;
            }
            else
            {
                Logger.PrintWarning(LogClass.Application, "Please specify a valid XCI/NCA/NSP/PFS0/NRO file");
            }

            if (DiscordIntegrationEnabled)
            {
                if (File.ReadAllLines(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RPsupported.dat")).Contains(_device.System.TitleID))
                {
                    DiscordPresence.Assets.LargeImageKey = _device.System.TitleID;
                }

                DiscordPresence.Details               = $"Playing {_device.System.TitleName}";
                DiscordPresence.State                 = string.IsNullOrWhiteSpace(_device.System.TitleID) ? string.Empty : _device.System.TitleID.ToUpper();
                DiscordPresence.Assets.LargeImageText = _device.System.TitleName;
                DiscordPresence.Assets.SmallImageKey  = "ryujinx";
                DiscordPresence.Assets.SmallImageText = "Ryujinx is an emulator for the Nintendo Switch";
                DiscordPresence.Timestamps            = new Timestamps(DateTime.UnixEpoch);

                DiscordClient.SetPresence(DiscordPresence);
            }

            string appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string savePath = System.IO.Path.Combine(appdataPath, "RyuFs", "nand", "user", "save", "0000000000000000", "savecommon", _device.System.TitleID);
            using (FileStream fs = File.OpenWrite(System.IO.Path.Combine(savePath, "LastPlayed.dat")))
            {
                using (StreamWriter sr = new StreamWriter(fs))
                {
                   sr.WriteLine(DateTime.UtcNow);
                }
            }
        }

        private static void CreateGameWindow()
        {
            using (GlScreen screen = new GlScreen(_device, _renderer))
            {
                screen.MainLoop();

                Profile.FinishProfiling();

                _device.Dispose();

                _audioOut.Dispose();
            }
        }

        private static void End()
        {
            string appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string savePath = System.IO.Path.Combine(appdataPath, "RyuFs", "nand", "user", "save", "0000000000000000", "savecommon", _device.System.TitleID);
            double currentPlayTime = 0;

            using (FileStream fs = File.OpenRead(System.IO.Path.Combine(savePath, "LastPlayed.dat")))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    DateTime startTime = DateTime.Parse(sr.ReadLine());

                    using (FileStream lpfs = File.OpenRead(System.IO.Path.Combine(savePath, "TimePlayed.dat")))
                    {
                        using (StreamReader lpsr = new StreamReader(lpfs))
                        {
                            currentPlayTime = double.Parse(lpsr.ReadLine());
                        }
                    }

                    using (FileStream tpfs = File.OpenWrite(System.IO.Path.Combine(savePath, "TimePlayed.dat")))
                    {
                        using (StreamWriter tpsr = new StreamWriter(tpfs))
                        {
                            tpsr.WriteLine(currentPlayTime + Math.Round(DateTime.UtcNow.Subtract(startTime).TotalSeconds, MidpointRounding.AwayFromZero));
                        }
                    }
                }
            }

            _audioOut.Dispose();
            DiscordClient.Dispose();
            Logger.Shutdown();
            Environment.Exit(0);
        }
        
        //Events
        private void Row_Activated(object obj, RowActivatedArgs args)
        {
            _TableStore.GetIter(out TreeIter treeiter, new TreePath(args.Path.ToString()));
            string path = (string)_TableStore.GetValue(treeiter, 5);

            LoadApplication(path);

            //Box.Remove(GameTableWindow);
            //Box.Add(GlScreen);
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

                //Box.Remove(GameTableWindow);
                //Box.Add(GlScreen);
            }

            fc.Destroy();
        }

        private void Load_Application_Folder(object o, EventArgs args)
        {
            FileChooserDialog fc = new FileChooserDialog("Choose the folder to open", this, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            if (fc.Run() == (int)ResponseType.Accept)
            {
                LoadApplication(fc.Filename);

                //Box.Remove(GameTableWindow);
                //Box.Add(GlScreen);
            }

            fc.Destroy();
        }

        private void Exit_Pressed(object o, EventArgs args) { End(); }

        private void Window_Close(object obj, DeleteEventArgs args) { End(); }

        private void ReturnMain_Pressed(object o, EventArgs args)
        {
            Box.Remove(GlScreen);
            Box.Add(GameTableWindow);
            //will also have to write logic to kill running game
        }

        private void FullScreen_Toggled(object o, EventArgs args)
        {
            if (FullScreen.Active == true) { Fullscreen(); }
            else { Unfullscreen(); }
        }

        private void Settings_Pressed(object o, EventArgs args)
        {
            SwitchSettings SettingsWin = new SwitchSettings(_device);
            _gtkapp.Register(GLib.Cancellable.Current);
            _gtkapp.AddWindow(SettingsWin);
            SettingsWin.Show();
        }

        private void Nfc_Pressed(object o, EventArgs args)
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
            AboutWindow AboutWin = new AboutWindow();
            _gtkapp.Register(GLib.Cancellable.Current);
            _gtkapp.AddWindow(AboutWin);
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
