using Avalonia.Collections;
using Avalonia.Interactivity;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class CheatWindow : StyleableWindow
    {
        internal CheatWindowViewModel ViewModel { get; private set; }

        public CheatWindow()
        {
            DataContext = this;

            InitializeComponent();

            Title = $"Ryujinx {Program.Version} - " + LocaleManager.Instance[LocaleKeys.CheatWindowTitle];
        }

        public CheatWindow(VirtualFileSystem virtualFileSystem, string titleId, string titleName)
        {
            DataContext = ViewModel = new CheatWindowViewModel();

            ViewModel.CloseAction += Close;

            ViewModel.LoadedCheats = new AvaloniaList<CheatsList>();

            ViewModel.Heading = string.Format(LocaleManager.Instance[LocaleKeys.CheatWindowHeading], titleName, titleId.ToUpper());

            InitializeComponent();

            string modsBasePath = virtualFileSystem.ModLoader.GetModsBasePath();
            string titleModsPath = virtualFileSystem.ModLoader.GetTitleDir(modsBasePath, titleId);
            ulong titleIdValue = ulong.Parse(titleId, System.Globalization.NumberStyles.HexNumber);

            ViewModel.EnabledCheatsPath = Path.Combine(titleModsPath, "cheats", "enabled.txt");

            string[] enabled = { };

            if (File.Exists(ViewModel.EnabledCheatsPath))
            {
                enabled = File.ReadAllLines(ViewModel.EnabledCheatsPath);
            }

            int cheatAdded = 0;

            var mods = new ModLoader.ModCache();

            ModLoader.QueryContentsDir(mods, new DirectoryInfo(Path.Combine(modsBasePath, "contents")), titleIdValue);

            string currentCheatFile = string.Empty;
            string buildId = string.Empty;
            string parentPath = string.Empty;

            CheatsList currentGroup = null;

            foreach (var cheat in mods.Cheats)
            {
                if (cheat.Path.FullName != currentCheatFile)
                {
                    currentCheatFile = cheat.Path.FullName;
                    parentPath = currentCheatFile.Replace(titleModsPath, "");

                    buildId = Path.GetFileNameWithoutExtension(currentCheatFile).ToUpper();
                    currentGroup = new CheatsList(buildId, parentPath);

                    ViewModel.LoadedCheats.Add(currentGroup);
                }

                var model = new CheatModel(cheat.Name, buildId, enabled.Contains($"{buildId}-{cheat.Name}"));
                currentGroup?.Add(model);

                cheatAdded++;
            }

            if (cheatAdded == 0)
            {
                ViewModel.NoCheatsFound = true;
            }

            Title = $"Ryujinx {Program.Version} - " + LocaleManager.Instance[LocaleKeys.CheatWindowTitle];
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}