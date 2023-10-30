using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Models;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.Ui.App.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class CheatWindow : UserControl
    {
        private readonly string _enabledCheatsPath;
        public bool NoCheatsFound { get; }

        public AvaloniaList<CheatNode> LoadedCheats { get; }
        public string BuildId { get; }

        public CheatWindow()
        {
            DataContext = this;

            InitializeComponent();
        }

        public CheatWindow(VirtualFileSystem virtualFileSystem, string titleId, string titleName, string titlePath)
        {
            LoadedCheats = new AvaloniaList<CheatNode>();

            BuildId = ApplicationData.GetApplicationBuildId(virtualFileSystem, titlePath);

            InitializeComponent();

            string modsBasePath = ModLoader.GetModsBasePath();
            string titleModsPath = ModLoader.GetTitleDir(modsBasePath, titleId);
            ulong titleIdValue = ulong.Parse(titleId, NumberStyles.HexNumber);

            _enabledCheatsPath = Path.Combine(titleModsPath, "cheats", "enabled.txt");

            string[] enabled = Array.Empty<string>();

            if (File.Exists(_enabledCheatsPath))
            {
                enabled = File.ReadAllLines(_enabledCheatsPath);
            }

            int cheatAdded = 0;

            var mods = new ModLoader.ModCache();

            ModLoader.QueryContentsDir(mods, new DirectoryInfo(Path.Combine(modsBasePath, "contents")), titleIdValue);

            string currentCheatFile = string.Empty;
            string buildId = string.Empty;

            CheatNode currentGroup = null;

            foreach (var cheat in mods.Cheats)
            {
                if (cheat.Path.FullName != currentCheatFile)
                {
                    currentCheatFile = cheat.Path.FullName;
                    string parentPath = currentCheatFile.Replace(titleModsPath, "");

                    buildId = Path.GetFileNameWithoutExtension(currentCheatFile).ToUpper();
                    currentGroup = new CheatNode("", buildId, parentPath, true);

                    LoadedCheats.Add(currentGroup);
                }

                var model = new CheatNode(cheat.Name, buildId, "", false, enabled.Contains($"{buildId}-{cheat.Name}"));
                currentGroup?.SubNodes.Add(model);

                cheatAdded++;
            }

            if (cheatAdded == 0)
            {
                NoCheatsFound = true;
            }

            DataContext = this;
        }

        public static async Task Show(VirtualFileSystem virtualFileSystem, string titleId, string titleName, string titlePath)
        {
            ContentDialog contentDialog = new()
            {
                PrimaryButtonText = "",
                SecondaryButtonText = "",
                CloseButtonText = "",
                Content = new CheatWindow(virtualFileSystem, titleId, titleName, titlePath),
                Title = string.Format(LocaleManager.Instance[LocaleKeys.CheatWindowHeading], titleName, titleId.ToUpper()),
            };

            Style bottomBorder = new(x => x.OfType<Grid>().Name("DialogSpace").Child().OfType<Border>());
            bottomBorder.Setters.Add(new Setter(IsVisibleProperty, false));

            contentDialog.Styles.Add(bottomBorder);

            await ContentDialogHelper.ShowAsync(contentDialog);
        }

        public void Save()
        {
            if (NoCheatsFound)
            {
                return;
            }

            List<string> enabledCheats = new();

            foreach (var cheats in LoadedCheats)
            {
                foreach (var cheat in cheats.SubNodes)
                {
                    if (cheat.IsEnabled)
                    {
                        enabledCheats.Add(cheat.BuildIdKey);
                    }
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_enabledCheatsPath));

            File.WriteAllLines(_enabledCheatsPath, enabledCheats);

            ((ContentDialog)Parent).Hide();
        }

        public void Close()
        {
            ((ContentDialog)Parent).Hide();
        }
    }
}
