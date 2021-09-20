using Gtk;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.Ui.Helper;
using Ryujinx.Ui.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using GUI        = Gtk.Builder.ObjectAttribute;
using JsonHelper = Ryujinx.Common.Utilities.JsonHelper;

namespace Ryujinx.Ui.Windows
{
    public class ModsWindow : Window
    {
        private readonly string             _modsInfoLabelFormat;
        private readonly VirtualFileSystem  _virtualFileSystem;
        private readonly ulong              _titleId;
        private readonly string             _titleIdText;
        private readonly string             _titleName;
        private readonly string             _modsJsonPath;

        private int _selectedMods = 0;

#pragma warning disable CS0649, IDE0044
        [GUI] Label         _modsInfoLabel;
        [GUI] TreeView      _modsTreeView;
        [GUI] TreeSelection _modsTreeSelection;
#pragma warning restore CS0649, IDE0044

        public ModsWindow(VirtualFileSystem virtualFileSystem, ulong titleId, string titleIdText, string titleName) : this(new Builder("Ryujinx.Ui.Windows.ModsWindow.glade"), virtualFileSystem, titleId, titleIdText, titleName) { }

        private ModsWindow(Builder builder, VirtualFileSystem virtualFileSystem, ulong titleId, string titleIdText, string titleName) : base(builder.GetObject("_modsWindow").Handle)
        {
            builder.Autoconnect(this);

            _titleId             = titleId;
            _titleIdText         = titleIdText;
            _titleName           = titleName;
            _virtualFileSystem   = virtualFileSystem;
            _modsJsonPath        = GetModsJsonPath(_titleIdText);
            _modsInfoLabelFormat = _modsInfoLabel.Text;

            _modsTreeView.Model = new TreeStore(typeof(bool), typeof(string), typeof(string), typeof(string));

            CellRendererToggle enableToggle = new CellRendererToggle();
            enableToggle.Toggled += (sender, args) =>
            {
                _modsTreeView.Model.GetIter(out TreeIter treeIter, new TreePath(args.Path));
                bool newValue = !(bool)_modsTreeView.Model.GetValue(treeIter, 0);
                _selectedMods += newValue ? 1 : -1;
                _modsTreeView.Model.SetValue(treeIter, 0, newValue);
                UpdateInfoLabel();
            };

            _modsTreeView.AppendColumn("Enabled", enableToggle,           "active", 0);
            _modsTreeView.AppendColumn("Name",    new CellRendererText(), "text",   1);
            _modsTreeView.AppendColumn("Type",    new CellRendererText(), "text",   2);
            _modsTreeView.AppendColumn("Path",    new CellRendererText(), "text",   3);
            _modsTreeView.ButtonReleaseEvent += Row_Clicked;

            Refresh();
        }

        private static string GetModsJsonPath(string titleId)
        {
            return System.IO.Path.Combine(AppDataManager.GamesDirPath, titleId, "enabled_mods.json");
        }

        private void Clear()
        {
            List<TreeIter> toRemove = new List<TreeIter>();

            if (_modsTreeView.Model.GetIterFirst(out TreeIter iter))
            {
                do
                {
                    toRemove.Add(iter);
                }
                while (_modsTreeView.Model.IterNext(ref iter));
            }

            foreach (TreeIter i in toRemove)
            {
                TreeIter j = i;
                ((TreeStore)_modsTreeView.Model).Remove(ref j);
            }

            _selectedMods = 0;
        }

        private void Save()
        {
            // Save the list of enabled mods

            List<ModEntry> enabledMods = new List<ModEntry>();

            if (_modsTreeView.Model.GetIterFirst(out TreeIter parentIter))
            {
                do
                {
                    if ((bool)_modsTreeView.Model.GetValue(parentIter, 0))
                    {
                        string name = (string)_modsTreeView.Model.GetValue(parentIter, 1);
                        string path = (string)_modsTreeView.Model.GetValue(parentIter, 3);
                        enabledMods.Add(new ModEntry(name, path));
                    }
                }
                while (_modsTreeView.Model.IterNext(ref parentIter));
            }

            using (FileStream modsJsonStream = File.Create(_modsJsonPath, 4096, FileOptions.WriteThrough))
            {
                modsJsonStream.Write(Encoding.UTF8.GetBytes(JsonHelper.Serialize(enabledMods, true)));
            }
        }

        private void Refresh()
        {
            // Clear the tree and reload the enabled mods.

            Clear();

            HashSet<ModEntry> enabledMods = new HashSet<ModEntry>();

            try
            {
                enabledMods.UnionWith(JsonHelper.DeserializeFromFile<List<ModEntry>>(_modsJsonPath));
            }
            catch
            {
                Logger.Error?.Print(LogClass.Application, "Failed to load list of enabled mods for title");
            }

            // Create a list of ids which mods can be applied when loading the game, this includes the game itself and its DLCs.

            List<ulong> titleIds = new List<ulong>();

            titleIds.Add(_titleId);

            try
            {
                string dlcJsonPath = DlcWindow.GetDLCJsonPath(_titleIdText);
                var dlcContainerList = JsonHelper.DeserializeFromFile<List<DlcContainer>>(dlcJsonPath);

                // Consider even DLCs that are not enabled.
                foreach (var dlc in dlcContainerList)
                {
                    foreach (var nca in dlc.DlcNcaList)
                    {
                        titleIds.Add(nca.TitleId);
                    }
                }
            }
            catch
            {
                Logger.Error?.Print(LogClass.Application, "Failed to load DLCs for title");
            }

            Logger.Info?.Print(LogClass.Application, $"Loaded {titleIds.Count} title ids for title");

            Dictionary<ulong, ModLoader.ModCache> modCaches  = new Dictionary<ulong, ModLoader.ModCache>();
            ModLoader.PatchCache                  patchCache = new ModLoader.PatchCache();

            foreach (ulong titleId in titleIds)
            {
                modCaches[titleId] = new ModLoader.ModCache();
            }

            // Todo instantiate ModLoader
            ModLoader.CollectMods(modCaches, patchCache, _virtualFileSystem.ModLoader.GetModsBasePath());

            AddModsToTree(patchCache.KipPatches, "Patch (global)", enabledMods, ref _selectedMods);
            AddModsToTree(patchCache.NroPatches, "Patch (global)", enabledMods, ref _selectedMods);
            AddModsToTree(patchCache.NsoPatches, "Patch (global)", enabledMods, ref _selectedMods);

            foreach (var modCache in modCaches)
            {
                AddModsToTree(modCache.Value.Cheats         , "Cheat", enabledMods, ref _selectedMods);
                AddModsToTree(modCache.Value.ExefsContainers, "Exefs", enabledMods, ref _selectedMods);
                AddModsToTree(modCache.Value.ExefsDirs      , "Exefs", enabledMods, ref _selectedMods);
                AddModsToTree(modCache.Value.RomfsContainers, "Romfs", enabledMods, ref _selectedMods);
                AddModsToTree(modCache.Value.RomfsDirs      , "Romfs", enabledMods, ref _selectedMods);
            }

            Logger.Info?.Print(LogClass.Application, $"Loaded {titleIds.Count} title ids for title");

            UpdateInfoLabel();
        }

        private void UpdateInfoLabel()
        {
            _modsInfoLabel.Text = string.Format(_modsInfoLabelFormat, _titleName, _selectedMods);
        }

        private string GetRelativeModPath(string absolutePath)
        {
            return System.IO.Path.GetRelativePath(_virtualFileSystem.ModLoader.GetModsBasePath(), absolutePath);
        }

        private void AddModsToTree(IEnumerable<ModLoader.Cheat> mods, string type, HashSet<ModEntry> enabledMods, ref int enabledModsFound)
        {
            foreach (var mod in mods)
            {
                AddModToTree(mod.Path, mod.Name, type, enabledMods, ref enabledModsFound);
            }
        }

        private void AddModsToTree<T>(IEnumerable<ModLoader.Mod<T>> mods, string type, HashSet<ModEntry> enabledMods, ref int enabledModsFound) where T : FileSystemInfo
        {
            foreach (var mod in mods)
            {
                AddModToTree(mod.Path, mod.Name, type, enabledMods, ref enabledModsFound);
            }
        }

        private void AddModToTree(FileSystemInfo path, string name, string type, HashSet<ModEntry> enabledMods, ref int enabledModsFound)
        {
            string relativePath = GetRelativeModPath(path.FullName);
            var    key          = new ModEntry(name, relativePath);
            bool   enabled      = enabledMods.Contains(key);

            ((TreeStore)_modsTreeView.Model).AppendValues(enabled, name, type, relativePath);

            if (enabled)
            {
                enabledModsFound++;
            }
        }
        private void ModsWindow_DeleteEvent(object sender, DeleteEventArgs args)
        {
            Save();
        }

        private void RefreshButton_Clicked(object sender, EventArgs args)
        {
            Save();
            Refresh();
        }

        private void EnableAllButton_Clicked(object sender, EventArgs args)
        {
            _selectedMods = 0;

            if (_modsTreeView.Model.GetIterFirst(out TreeIter parentIter))
            {
                do
                {
                    _modsTreeView.Model.SetValue(parentIter, 0, true);
                    _selectedMods++;
                }
                while (_modsTreeView.Model.IterNext(ref parentIter));
            }

            UpdateInfoLabel();
        }

        private void DisableAllButton_Clicked(object sender, EventArgs args)
        {
            List<ModEntry> enabledMods = new List<ModEntry>();

            if (_modsTreeView.Model.GetIterFirst(out TreeIter parentIter))
            {
                do
                {
                    _modsTreeView.Model.SetValue(parentIter, 0, false);
                    _selectedMods++;
                }
                while (_modsTreeView.Model.IterNext(ref parentIter));
            }

            _selectedMods = 0;
            UpdateInfoLabel();
        }

        private void OpenGlobalFolderButton_Clicked(object sender, EventArgs args)
        {
            OpenHelper.OpenFolder(_virtualFileSystem.ModLoader.GetModsBasePath());
        }

        private void OpenGameFolderButton_Clicked(object sender, EventArgs args)
        {
            string modsBasePath  = _virtualFileSystem.ModLoader.GetModsBasePath();
            string titleModsPath = _virtualFileSystem.ModLoader.GetTitleDir(modsBasePath, _titleIdText);

            OpenHelper.OpenFolder(titleModsPath);
        }

        private void Row_Clicked(object sender, ButtonReleaseEventArgs args)
        {
            if (args.Event.Button != 3 /* Right Click */)
            {
                return;
            }

            _modsTreeSelection.GetSelected(out TreeIter treeIter);

            if (treeIter.UserData == IntPtr.Zero)
            {
                return;
            }

            string path = System.IO.Path.Combine(_virtualFileSystem.ModLoader.GetModsBasePath(), (string)_modsTreeView.Model.GetValue(treeIter, 3));

            if (!Directory.Exists(path))
            {
                // Open the containing directory if the mod is a single file.
                path = new DirectoryInfo(path).Parent.FullName;
            }

            _ = new ModsTreeViewContextMenu(path);
        }

        private void OpenModFolderMenuItem_Activated(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
