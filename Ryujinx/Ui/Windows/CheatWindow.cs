using Gtk;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using Ryujinx.Common.Configuration;
using Ryujinx.HLE.FileSystem;
using Ryujinx.Ui.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using GUI        = Gtk.Builder.ObjectAttribute;
using JsonHelper = Ryujinx.Common.Utilities.JsonHelper;

namespace Ryujinx.Ui.Windows
{
    public class CheatWindow : Window
    {
        private readonly string _enabledCheatsPath;

#pragma warning disable CS0649, IDE0044
        [GUI] Label    _baseTitleInfoLabel;
        [GUI] TreeView _cheatTreeView;
#pragma warning restore CS0649, IDE0044

        public CheatWindow(VirtualFileSystem virtualFileSystem, string titleId, string titleName) : this(new Builder("Ryujinx.Ui.Windows.CheatWindow.glade"), virtualFileSystem, titleId, titleName) { }

        private CheatWindow(Builder builder, VirtualFileSystem virtualFileSystem, string titleId, string titleName) : base(builder.GetObject("_cheatWindow").Handle)
        {
            builder.Autoconnect(this);
            _baseTitleInfoLabel.Text = $"Cheats Available for {titleName} [{titleId.ToUpper()}]";

            string modsBasePath  = virtualFileSystem.ModLoader.GetModsBasePath();
            string titleModsPath = virtualFileSystem.ModLoader.GetTitleDir(modsBasePath, titleId);

            var cheatsPath     = System.IO.Path.Combine(titleModsPath, "cheats");
            _enabledCheatsPath = System.IO.Path.Combine(cheatsPath, "enabled.txt");

            _cheatTreeView.Model = new TreeStore(typeof(bool), typeof(string), typeof(string));

            CellRendererToggle enableToggle = new CellRendererToggle();
            enableToggle.Toggled += (sender, args) =>
            {
                _cheatTreeView.Model.GetIter(out TreeIter treeIter, new TreePath(args.Path));
                bool newValue = !(bool)_cheatTreeView.Model.GetValue(treeIter, 0);
                _cheatTreeView.Model.SetValue(treeIter, 0, newValue);

                if (_cheatTreeView.Model.IterChildren(out TreeIter childIter, treeIter))
                {
                    do
                    {
                        _cheatTreeView.Model.SetValue(childIter, 0, newValue);
                    }
                    while (_cheatTreeView.Model.IterNext(ref childIter));
                }
            };

            _cheatTreeView.AppendColumn("Enabled", enableToggle, "active", 0);
            _cheatTreeView.AppendColumn("Name", new CellRendererText(), "text", 1);

            var buildIdColumn = _cheatTreeView.AppendColumn("Build Id", new CellRendererText(), "text", 2);
            buildIdColumn.Visible = false;

            string[] enabled = { };

            if (File.Exists(_enabledCheatsPath))
            {
                enabled = File.ReadAllLines(_enabledCheatsPath);
            }

            foreach (var cheatFile in Directory.EnumerateFiles(cheatsPath, "*.txt"))
            {
                if (cheatFile == _enabledCheatsPath)
                {
                    continue;
                }

                IEnumerable<string> cheatNames = File.ReadAllLines(cheatFile).Where(x => x.StartsWith("[") && x.EndsWith("]"));
                string buildId = System.IO.Path.GetFileNameWithoutExtension(cheatFile);

                bool allEnabled = cheatNames.ToList().TrueForAll(x => enabled.Contains($"{buildId}-<{x.Substring(1, x.Length - 2)} Cheat>"));
                bool anyEnabled = cheatNames.Any(x => enabled.Contains($"{buildId}-<{x.Substring(1, x.Length - 2)} Cheat>"));

                TreeIter parentIter = ((TreeStore)_cheatTreeView.Model).AppendValues(allEnabled, buildId, "");

                foreach (var cheat in cheatNames)
                {
                    string cleanName = $"{cheat.Substring(1, cheat.Length - 2)}";
                    ((TreeStore)_cheatTreeView.Model).AppendValues(parentIter, enabled.Contains($"{buildId}-<{cleanName} Cheat>"), cleanName, buildId);
                }
            }

            _cheatTreeView.ExpandAll();
        }

        private void SaveButton_Clicked(object sender, EventArgs args)
        {
            List<string> enabledCheats = new List<string>();

            if (_cheatTreeView.Model.GetIterFirst(out TreeIter parentIter))
            {
                do
                {
                    if (_cheatTreeView.Model.IterChildren(out TreeIter childIter, parentIter))
                    {
                        do
                        {
                            var enabled = (bool)_cheatTreeView.Model.GetValue(childIter, 0);

                            if (enabled)
                            {
                                var name = _cheatTreeView.Model.GetValue(childIter, 1).ToString();
                                var buildId = _cheatTreeView.Model.GetValue(childIter, 2).ToString();

                                enabledCheats.Add($"{buildId}-<{name} Cheat>");
                            }
                        }
                        while (_cheatTreeView.Model.IterNext(ref childIter));
                    }
                }
                while (_cheatTreeView.Model.IterNext(ref parentIter));
            }

            File.WriteAllLines(_enabledCheatsPath, enabledCheats);

            Dispose();
        }

        private void CancelButton_Clicked(object sender, EventArgs args)
        {
            Dispose();
        }
    }
}
