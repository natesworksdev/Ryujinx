using JsonPrettyPrinterPlus;
using DiscordRPC;
using Gtk;
using GUI = Gtk.Builder.ObjectAttribute;
using Ryujinx.Audio;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Gal.OpenGL;
using Ryujinx.Profiler;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Utf8Json;
using Utf8Json.Resolvers;

namespace Ryujinx.UI
{
    public class MainWindow : Window
    {
        internal static HLE.Switch _device;

        private static IGalRenderer _renderer;

        private static IAalOutput _audioOut;

        private static GlScreen _screen;

        private static Application _gtkApplication;

        private static ListStore _tableStore;

        private static bool _gameLoaded = false;
        private static bool _ending     = false;

        private static TreeViewColumn favColumn;
        private static TreeViewColumn appColumn;
        private static TreeViewColumn devColumn;
        private static TreeViewColumn versionColumn;
        private static TreeViewColumn timePlayedColumn;
        private static TreeViewColumn lastPlayedColumn;
        private static TreeViewColumn fileExtColumn;
        private static TreeViewColumn fileSizeColumn;
        private static TreeViewColumn pathColumn;

        private struct ApplicationMetadata
        {
            public bool   Fav;
            public double TimePlayed;
            public string LastPlayed;
        }

        private static ApplicationMetadata AppMetadata;

        public static bool DiscordIntegrationEnabled { get; set; }

        public static DiscordRpcClient DiscordClient;

        public static RichPresence DiscordPresence;

#pragma warning disable 649
        [GUI] Window        _mainWin;
        [GUI] CheckMenuItem _fullScreen;
        [GUI] MenuItem      _stopEmulation;
        [GUI] CheckMenuItem _favToggle;
        [GUI] CheckMenuItem _iconToggle;
        [GUI] CheckMenuItem _titleToggle;
        [GUI] CheckMenuItem _developerToggle;
        [GUI] CheckMenuItem _versionToggle;
        [GUI] CheckMenuItem _timePlayedToggle;
        [GUI] CheckMenuItem _lastPlayedToggle;
        [GUI] CheckMenuItem _fileExtToggle;
        [GUI] CheckMenuItem _fileSizeToggle;
        [GUI] CheckMenuItem _pathToggle;
        [GUI] TreeView      _gameTable;
#pragma warning restore 649

        public MainWindow(string[] args, Application gtkApplication) : this(new Builder("Ryujinx.Ui.MainWindow.glade"), args, gtkApplication) { }

        private MainWindow(Builder builder, string[] args, Application gtkApplication) : base(builder.GetObject("_mainWin").Handle)
        {
            _renderer = new OglRenderer();

            _audioOut = InitializeAudioEngine();

            _device = new HLE.Switch(_renderer, _audioOut);

            Configuration.Load(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));
            Configuration.InitialConfigure(_device);

            ApplicationLibrary.Init(SwitchSettings.SwitchConfig.GameDirs, _device.System.KeySet, _device.System.State.DesiredTitleLanguage);

            _gtkApplication = gtkApplication;

            ApplyTheme();

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

            DeleteEvent += Window_Close;

            _mainWin.Icon            = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.ryujinxIcon.png");
            _stopEmulation.Sensitive = false;

            if (SwitchSettings.SwitchConfig.GuiColumns[0]) { _favToggle.Active        = true; }
            if (SwitchSettings.SwitchConfig.GuiColumns[1]) { _iconToggle.Active       = true; }
            if (SwitchSettings.SwitchConfig.GuiColumns[2]) { _titleToggle.Active      = true; }
            if (SwitchSettings.SwitchConfig.GuiColumns[3]) { _developerToggle.Active  = true; }
            if (SwitchSettings.SwitchConfig.GuiColumns[4]) { _versionToggle.Active    = true; }
            if (SwitchSettings.SwitchConfig.GuiColumns[5]) { _timePlayedToggle.Active = true; }
            if (SwitchSettings.SwitchConfig.GuiColumns[6]) { _lastPlayedToggle.Active = true; }
            if (SwitchSettings.SwitchConfig.GuiColumns[7]) { _fileExtToggle.Active    = true; }
            if (SwitchSettings.SwitchConfig.GuiColumns[8]) { _fileSizeToggle.Active   = true; }
            if (SwitchSettings.SwitchConfig.GuiColumns[9]) { _pathToggle.Active       = true; }

            _gameTable.Model = _tableStore = new ListStore(typeof(bool), typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

            UpdateColumns();
            UpdateGameTable();
        }

        public static void CreateErrorDialog(string errorMessage)
        {
            MessageDialog errorDialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, errorMessage)
            {
                Title          = "Ryujinx - Error",
                Icon           = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.ryujinxIcon.png"),
                WindowPosition = WindowPosition.Center
            };
            errorDialog.SetSizeRequest(100, 20);
            errorDialog.Run();
            errorDialog.Dispose();
        }

        public static void ApplyTheme()
        {
            CssProvider cssProvider = new CssProvider();

            if (SwitchSettings.SwitchConfig.EnableCustomTheme)
            {
                if (File.Exists(SwitchSettings.SwitchConfig.CustomThemePath) && (System.IO.Path.GetExtension(SwitchSettings.SwitchConfig.CustomThemePath) == ".css"))
                {
                    cssProvider.LoadFromPath(SwitchSettings.SwitchConfig.CustomThemePath);
                }
                else
                {
                    Logger.PrintWarning(LogClass.Application, $"The \"custom_theme_path\" section in \"Config.json\" contains an invalid path: \"{SwitchSettings.SwitchConfig.CustomThemePath}\"");
                }
            }

            StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, 800);
        }

        private void UpdateColumns()
        {
            foreach (TreeViewColumn column in _gameTable.Columns)
            {
                _gameTable.RemoveColumn(column);
            }

            CellRendererToggle favToggle = new CellRendererToggle();
            favToggle.Toggled += FavToggle_Toggled;

            if (SwitchSettings.SwitchConfig.GuiColumns[0]) { _gameTable.AppendColumn("Fav",         favToggle,                "active", 0); }
            if (SwitchSettings.SwitchConfig.GuiColumns[1]) { _gameTable.AppendColumn("Icon",        new CellRendererPixbuf(), "pixbuf", 1); }
            if (SwitchSettings.SwitchConfig.GuiColumns[2]) { _gameTable.AppendColumn("Application", new CellRendererText(),   "text",   2); }
            if (SwitchSettings.SwitchConfig.GuiColumns[3]) { _gameTable.AppendColumn("Developer",   new CellRendererText(),   "text",   3); }
            if (SwitchSettings.SwitchConfig.GuiColumns[4]) { _gameTable.AppendColumn("Version",     new CellRendererText(),   "text",   4); }
            if (SwitchSettings.SwitchConfig.GuiColumns[5]) { _gameTable.AppendColumn("Time Played", new CellRendererText(),   "text",   5); }
            if (SwitchSettings.SwitchConfig.GuiColumns[6]) { _gameTable.AppendColumn("Last Played", new CellRendererText(),   "text",   6); }
            if (SwitchSettings.SwitchConfig.GuiColumns[7]) { _gameTable.AppendColumn("File Ext",    new CellRendererText(),   "text",   7); }
            if (SwitchSettings.SwitchConfig.GuiColumns[8]) { _gameTable.AppendColumn("File Size",   new CellRendererText(),   "text",   8); }
            if (SwitchSettings.SwitchConfig.GuiColumns[9]) { _gameTable.AppendColumn("Path",        new CellRendererText(),   "text",   9); }

            foreach (TreeViewColumn column in _gameTable.Columns)
            {
                     if (column.Title == "Fav")         { favColumn        = column; }
                else if (column.Title == "Application") { appColumn        = column; }
                else if (column.Title == "Developer")   { devColumn        = column; }
                else if (column.Title == "Version")     { versionColumn    = column; }
                else if (column.Title == "Time Played") { timePlayedColumn = column; }
                else if (column.Title == "Last Played") { lastPlayedColumn = column; }
                else if (column.Title == "File Ext")    { fileExtColumn    = column; }
                else if (column.Title == "File Size")   { fileSizeColumn   = column; }
                else if (column.Title == "Path")        { pathColumn       = column; }
            }

            if (SwitchSettings.SwitchConfig.GuiColumns[0]) { favColumn.SortColumnId        = 0; }
            if (SwitchSettings.SwitchConfig.GuiColumns[2]) { appColumn.SortColumnId        = 2; }
            if (SwitchSettings.SwitchConfig.GuiColumns[3]) { devColumn.SortColumnId        = 3; }
            if (SwitchSettings.SwitchConfig.GuiColumns[4]) { versionColumn.SortColumnId    = 4; }
            if (SwitchSettings.SwitchConfig.GuiColumns[5]) { timePlayedColumn.SortColumnId = 5; }
            if (SwitchSettings.SwitchConfig.GuiColumns[6]) { lastPlayedColumn.SortColumnId = 6; }
            if (SwitchSettings.SwitchConfig.GuiColumns[7]) { fileExtColumn.SortColumnId    = 7; }
            if (SwitchSettings.SwitchConfig.GuiColumns[8]) { fileSizeColumn.SortColumnId   = 8; }
            if (SwitchSettings.SwitchConfig.GuiColumns[9]) { pathColumn.SortColumnId       = 9; }
        }

        public static void UpdateGameTable()
        {
            _tableStore.Clear();
            ApplicationLibrary.Init(SwitchSettings.SwitchConfig.GameDirs, _device.System.KeySet, _device.System.State.DesiredTitleLanguage);

            foreach (ApplicationLibrary.ApplicationData AppData in ApplicationLibrary.ApplicationLibraryData)
            {
                _tableStore.AppendValues(AppData.Fav, new Gdk.Pixbuf(AppData.Icon, 75, 75), $"{AppData.TitleName}\n{AppData.TitleId.ToUpper()}", AppData.Developer, AppData.Version, AppData.TimePlayed, AppData.LastPlayed, AppData.FileExt, AppData.FileSize, AppData.Path);
            }

            _tableStore.SetSortFunc(5, TimePlayedSort);
            _tableStore.SetSortFunc(6, LastPlayedSort);
            _tableStore.SetSortFunc(8, FileSizeSort);
        }

        internal void LoadApplication(string path)
        {
            if (_gameLoaded)
            {
                CreateErrorDialog("A game has already been loaded. Please close the emulator and try again");
            }
            else
            {
                Logger.RestartTime();

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
                        _device.LoadCart(path, romFsFiles[0]);
                    }
                    else
                    {
                        Logger.PrintInfo(LogClass.Application, "Loading as cart WITHOUT RomFS.");
                        _device.LoadCart(path);
                    }
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
                            try
                            {
                                _device.LoadProgram(path);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                Logger.PrintError(LogClass.Application, "The file which you have specified is unsupported by Ryujinx.");
                            }
                            break;
                    }
                }
                else
                {
                    Logger.PrintWarning(LogClass.Application, "Please specify a valid XCI/NCA/NSP/PFS0/NRO file.");
                    End();
                }

#if MACOS_BUILD
                CreateGameWindow();
#else
                new Thread(new ThreadStart(CreateGameWindow)).Start();
#endif

                _gameLoaded              = true;
                _stopEmulation.Sensitive = true;

                if (DiscordIntegrationEnabled)
                {
                    if (File.ReadAllLines(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RPsupported.dat")).Contains(_device.System.TitleID))
                    {
                        DiscordPresence.Assets.LargeImageKey = _device.System.TitleID;
                    }

                    string state = _device.System.TitleID;

                    if (state == null)
                    {
                        state = "Ryujinx";
                    }
                    else
                    {
                        state = state.ToUpper();
                    }

                    string details = "Idling";

                    if (_device.System.TitleName != null)
                    {
                        details = $"Playing {_device.System.TitleName}";
                    }

                    DiscordPresence.Details               = details;
                    DiscordPresence.State                 = state;
                    DiscordPresence.Assets.LargeImageText = _device.System.TitleName;
                    DiscordPresence.Assets.SmallImageKey  = "ryujinx";
                    DiscordPresence.Assets.SmallImageText = "Ryujinx is an emulator for the Nintendo Switch";
                    DiscordPresence.Timestamps            = new Timestamps(DateTime.UtcNow);

                    DiscordClient.SetPresence(DiscordPresence);
                }

                string metadataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RyuFs", "games", _device.System.TitleID, "gui");
                string metadataFile   = System.IO.Path.Combine(metadataFolder, "metadata.json");

                IJsonFormatterResolver resolver = CompositeResolver.Create(new[] { StandardResolver.AllowPrivateSnakeCase });

                if (!File.Exists(metadataFile))
                {
                    Directory.CreateDirectory(metadataFolder);

                    AppMetadata = new ApplicationMetadata
                    {
                        Fav        = false,
                        TimePlayed = 0,
                        LastPlayed = "Never"
                    };

                    byte[] data = JsonSerializer.Serialize(AppMetadata, resolver);
                    File.WriteAllText(metadataFile, Encoding.UTF8.GetString(data, 0, data.Length).PrettyPrintJson());
                }

                using (Stream stream = File.OpenRead(metadataFile))
                {
                    AppMetadata = JsonSerializer.Deserialize<ApplicationMetadata>(stream, resolver);
                }

                AppMetadata.LastPlayed = DateTime.UtcNow.ToString();

                byte[] saveData = JsonSerializer.Serialize(AppMetadata, resolver);
                File.WriteAllText(metadataFile, Encoding.UTF8.GetString(saveData, 0, saveData.Length).PrettyPrintJson());
            }
        }

        private static void CreateGameWindow()
        {
            Configuration.ConfigureHid(_device, SwitchSettings.SwitchConfig);
            
            using (_screen = new GlScreen(_device, _renderer))
            {
                _screen.MainLoop();

                End();
            }
        }

        private static void End()
        {
            if (!_ending)
            {
                _ending = true;

                if (_gameLoaded)
                {
                    string metadataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RyuFs", "games", _device.System.TitleID, "gui");
                    string metadataFile   = System.IO.Path.Combine(metadataFolder, "metadata.json");

                    IJsonFormatterResolver resolver = CompositeResolver.Create(new[] { StandardResolver.AllowPrivateSnakeCase });

                    if (!File.Exists(metadataFile))
                    {
                        Directory.CreateDirectory(metadataFolder);

                        AppMetadata = new ApplicationMetadata
                        {
                            Fav        = false,
                            TimePlayed = 0,
                            LastPlayed = "Never"
                        };

                        byte[] data = JsonSerializer.Serialize(AppMetadata, resolver);
                        File.WriteAllText(metadataFile, Encoding.UTF8.GetString(data, 0, data.Length).PrettyPrintJson());
                    }

                    using (Stream stream = File.OpenRead(metadataFile))
                    {
                        AppMetadata = JsonSerializer.Deserialize<ApplicationMetadata>(stream, resolver);
                    }

                    AppMetadata.TimePlayed += Math.Round(DateTime.UtcNow.Subtract(DateTime.Parse(AppMetadata.LastPlayed)).TotalSeconds, MidpointRounding.AwayFromZero);

                    byte[] saveData = JsonSerializer.Serialize(AppMetadata, resolver);
                    File.WriteAllText(metadataFile, Encoding.UTF8.GetString(saveData, 0, saveData.Length).PrettyPrintJson());
                }

                Profile.FinishProfiling();
                _device.Dispose();
                _audioOut.Dispose();
                DiscordClient?.Dispose();
                Logger.Shutdown();
                Environment.Exit(0);
            }
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

        //Events
        private void FavToggle_Toggled(object o, ToggledArgs args)
        {
            _tableStore.GetIter(out TreeIter treeIter, new TreePath(args.Path));
            string titleid      = _tableStore.GetValue(treeIter, 2).ToString().Split("\n")[1].ToLower();
            string metadataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RyuFs", "games", titleid, "gui", "metadata.json");

            IJsonFormatterResolver resolver = CompositeResolver.Create(new[] { StandardResolver.AllowPrivateSnakeCase });

            using (Stream stream = File.OpenRead(metadataPath))
            {
                AppMetadata = JsonSerializer.Deserialize<ApplicationMetadata>(stream, resolver);
            }

            if ((bool)_tableStore.GetValue(treeIter, 0))
            {
                _tableStore.SetValue(treeIter, 0, false);

                AppMetadata.Fav = false;
            }
            else
            {
                _tableStore.SetValue(treeIter, 0, true);

                AppMetadata.Fav = true;
            }

            byte[] saveData = JsonSerializer.Serialize(AppMetadata, resolver);
            File.WriteAllText(metadataPath, Encoding.UTF8.GetString(saveData, 0, saveData.Length).PrettyPrintJson());
        }

        private void Row_Activated(object o, RowActivatedArgs args)
        {
            _tableStore.GetIter(out TreeIter treeIter, new TreePath(args.Path.ToString()));
            string path = (string)_tableStore.GetValue(treeIter, 9);

            LoadApplication(path);
        }

        private void Load_Application_File(object o, EventArgs args)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Choose the file to open", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            fileChooser.Filter = new FileFilter();
            fileChooser.Filter.AddPattern("*.nsp" );
            fileChooser.Filter.AddPattern("*.pfs0");
            fileChooser.Filter.AddPattern("*.xci" );
            fileChooser.Filter.AddPattern("*.nca" );
            fileChooser.Filter.AddPattern("*.nro" );
            fileChooser.Filter.AddPattern("*.nso" );

            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                LoadApplication(fileChooser.Filename);
            }

            fileChooser.Dispose();
        }

        private void Load_Application_Folder(object o, EventArgs args)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Choose the folder to open", this, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                LoadApplication(fileChooser.Filename);
            }

            fileChooser.Dispose();
        }

        private void Open_Ryu_Folder(object o, EventArgs args)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName        = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RyuFs"),
                UseShellExecute = true,
                Verb            = "open"
            });
        }

        private void Exit_Pressed(object o, EventArgs args)
        {
            _screen?.Exit();
            End();
        }

        private void Window_Close(object o, DeleteEventArgs args)
        {
            _screen?.Exit();
            End();
        }

        private void StopEmulation_Pressed(object o, EventArgs args)
        {
            // TODO: Write logic to kill running game
        }

        private void FullScreen_Toggled(object o, EventArgs args)
        {
            if (_fullScreen.Active)
            {
                Fullscreen();
            }
            else
            {
                Unfullscreen();
            }
        }

        private void Settings_Pressed(object o, EventArgs args)
        {
            SwitchSettings SettingsWin = new SwitchSettings(_device);

            _gtkApplication.Register(GLib.Cancellable.Current);
            _gtkApplication.AddWindow(SettingsWin);

            SettingsWin.Show();
        }

        private void Update_Pressed(object o, EventArgs args)
        {
            string ryuUpdater = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RyuFs", "RyuUpdater.exe");

            try
            {
                Process.Start(new ProcessStartInfo(ryuUpdater, "/U") { UseShellExecute = true });
            }
            catch(System.ComponentModel.Win32Exception)
            {
                CreateErrorDialog("Update canceled by user or updater was not found");
            }
        }

        private void About_Pressed(object o, EventArgs args)
        {
            AboutWindow AboutWin = new AboutWindow();

            _gtkApplication.Register(GLib.Cancellable.Current);
            _gtkApplication.AddWindow(AboutWin);

            AboutWin.Show();
        }

        private void Fav_Toggled(object o, EventArgs args)
        {
            SwitchSettings.SwitchConfig.GuiColumns[0] = _favToggle.Active;

            Configuration.SaveConfig(SwitchSettings.SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));

            UpdateColumns();
        }

        private void Icon_Toggled(object o, EventArgs args)
        {
            SwitchSettings.SwitchConfig.GuiColumns[1] = _iconToggle.Active;

            Configuration.SaveConfig(SwitchSettings.SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));

            UpdateColumns();
        }

        private void Title_Toggled(object o, EventArgs args)
        {
            SwitchSettings.SwitchConfig.GuiColumns[2] = _titleToggle.Active;

            Configuration.SaveConfig(SwitchSettings.SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));

            UpdateColumns();
        }

        private void Developer_Toggled(object o, EventArgs args)
        {
            SwitchSettings.SwitchConfig.GuiColumns[3] = _developerToggle.Active;

            Configuration.SaveConfig(SwitchSettings.SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));

            UpdateColumns();
        }

        private void Version_Toggled(object o, EventArgs args)
        {
            SwitchSettings.SwitchConfig.GuiColumns[4] = _versionToggle.Active;

            Configuration.SaveConfig(SwitchSettings.SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));

            UpdateColumns();
        }

        private void TimePlayed_Toggled(object o, EventArgs args)
        {
            SwitchSettings.SwitchConfig.GuiColumns[5] = _timePlayedToggle.Active;

            Configuration.SaveConfig(SwitchSettings.SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));

            UpdateColumns();
        }

        private void LastPlayed_Toggled(object o, EventArgs args)
        {
            SwitchSettings.SwitchConfig.GuiColumns[6] = _lastPlayedToggle.Active;

            Configuration.SaveConfig(SwitchSettings.SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));

            UpdateColumns();
        }

        private void FileExt_Toggled(object o, EventArgs args)
        {
            SwitchSettings.SwitchConfig.GuiColumns[7] = _fileExtToggle.Active;

            Configuration.SaveConfig(SwitchSettings.SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));

            UpdateColumns();
        }

        private void FileSize_Toggled(object o, EventArgs args)
        {
            SwitchSettings.SwitchConfig.GuiColumns[8] = _fileSizeToggle.Active;

            Configuration.SaveConfig(SwitchSettings.SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));

            UpdateColumns();
        }

        private void Path_Toggled(object o, EventArgs args)
        {
            SwitchSettings.SwitchConfig.GuiColumns[9] = _pathToggle.Active;

            Configuration.SaveConfig(SwitchSettings.SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));

            UpdateColumns();
        }
        
        private static int TimePlayedSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            string aValue = model.GetValue(a, 5).ToString();
            string bValue = model.GetValue(b, 5).ToString();

            if (aValue.Length > 4 && aValue.Substring(aValue.Length - 4) == "mins")
            {
                aValue = (float.Parse(aValue.Substring(0, aValue.Length - 5)) * 60).ToString();
            }
            else if (aValue.Length > 3 && aValue.Substring(aValue.Length - 3) == "hrs")
            {
                aValue = (float.Parse(aValue.Substring(0, aValue.Length - 4)) * 3600).ToString();
            }
            else if (aValue.Length > 4 && aValue.Substring(aValue.Length - 4) == "days")
            {
                aValue = (float.Parse(aValue.Substring(0, aValue.Length - 5)) * 86400).ToString();
            }
            else
            {
                aValue = aValue.Substring(0, aValue.Length - 1);
            }

            if (bValue.Length > 4 && bValue.Substring(bValue.Length - 4) == "mins")
            {
                bValue = (float.Parse(bValue.Substring(0, bValue.Length - 5)) * 60).ToString();
            }
            else if (bValue.Length > 3 && bValue.Substring(bValue.Length - 3) == "hrs")
            {
                bValue = (float.Parse(bValue.Substring(0, bValue.Length - 4)) * 3600).ToString();
            }
            else if (bValue.Length > 4 && bValue.Substring(bValue.Length - 4) == "days")
            {
                bValue = (float.Parse(bValue.Substring(0, bValue.Length - 5)) * 86400).ToString();
            }
            else
            {
                bValue = bValue.Substring(0, bValue.Length - 1);
            }

                 if (float.Parse(aValue) > float.Parse(bValue)) return -1;
            else if (float.Parse(bValue) > float.Parse(aValue)) return 1;
            else return 0;
        }

        private static int LastPlayedSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            string aValue = model.GetValue(a, 6).ToString();
            string bValue = model.GetValue(b, 6).ToString();

            if (aValue == "Never") aValue = DateTime.UnixEpoch.ToString();
            if (bValue == "Never") bValue = DateTime.UnixEpoch.ToString();

            return DateTime.Compare(DateTime.Parse(bValue), DateTime.Parse(aValue));
        }

        private static int FileSizeSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            string aValue = model.GetValue(a, 8).ToString();
            string bValue = model.GetValue(b, 8).ToString();

            if (aValue.Substring(aValue.Length - 2) == "GB")
            {
                aValue = (float.Parse(aValue.Substring(0, aValue.Length - 2)) * 1024).ToString();
            }
            else aValue = aValue.Substring(0, aValue.Length - 2);

            if (bValue.Substring(bValue.Length - 2) == "GB")
            {
                bValue = (float.Parse(bValue.Substring(0, bValue.Length - 2)) * 1024).ToString();
            }
            else bValue = bValue.Substring(0, bValue.Length - 2);

                 if (float.Parse(aValue) > float.Parse(bValue)) return -1;
            else if (float.Parse(bValue) > float.Parse(aValue)) return 1;
            else return 0;
        }
    }
}
