using Gtk;
using LibHac.FsSystem;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.IO;
using Ryujinx.Common.IO.Abstractions;
using Ryujinx.Extensions;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.Loaders.Dlc;
using Ryujinx.Ui.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using GUI = Gtk.Builder.ObjectAttribute;
using JsonHelper = Ryujinx.Common.Utilities.JsonHelper;

namespace Ryujinx.Ui.Windows
{
    public class DlcWindow : Window
    {
        private readonly VirtualFileSystem _virtualFileSystem;
        private readonly string _titleId;
        private readonly string _dlcJsonPath;
        private readonly ILocalStorageManagement _localStorageManagement;
        private readonly TreeStore _treeModel;

#pragma warning disable CS0649, IDE0044
        [GUI] Label _baseTitleInfoLabel;
        [GUI] TreeView _dlcTreeView;
        [GUI] TreeSelection _dlcTreeSelection;
#pragma warning restore CS0649, IDE0044

        private enum TreeStoreColumn
        {
            Enabled = 0,
            TitleId = 1,
            Path = 2
        }

        public DlcWindow(VirtualFileSystem virtualFileSystem, string titleId, string titleName) 
            : this(new Builder("Ryujinx.Ui.Windows.DlcWindow.glade"), virtualFileSystem, new LocalStorageManagement(), titleId, titleName) { }

        private DlcWindow(Builder builder, VirtualFileSystem virtualFileSystem, ILocalStorageManagement localStorageManagement, string titleId, string titleName) 
            : base(builder.GetObject("_dlcWindow").Handle)
        {
            builder.Autoconnect(this);

            _titleId = titleId;
            _virtualFileSystem = virtualFileSystem;
            _dlcJsonPath = System.IO.Path.Combine(AppDataManager.GamesDirPath, _titleId, "dlc.json");
            _baseTitleInfoLabel.Text = $"DLC Available for {titleName} [{titleId.ToUpper()}]";
            _localStorageManagement = localStorageManagement;

            _treeModel = new TreeStore(typeof(bool), typeof(string), typeof(string));

            LoadDlcTree();
        }

        private void LoadDlcTree()
        {
            _dlcTreeView.Model = _treeModel;

            _dlcTreeView.AppendColumn(TreeStoreColumn.Enabled.ToString(), GetEnableCellRenderToggle(), "active", (int)TreeStoreColumn.Enabled);
            _dlcTreeView.AppendColumn(TreeStoreColumn.TitleId.ToString(), new CellRendererText(), "text", (int)TreeStoreColumn.TitleId);
            _dlcTreeView.AppendColumn(TreeStoreColumn.Path.ToString(), new CellRendererText(), "text", (int)TreeStoreColumn.Path);

            var dlcContainerLoader = new DlcContainerLoader(_dlcJsonPath, _localStorageManagement);

            foreach (var dlcContainer in dlcContainerLoader.Load())
            {
                var parentIter = _treeModel.AppendValues(false, "", dlcContainer.Path);

                using FileStream containerFile = File.OpenRead(dlcContainer.Path);
                PartitionFileSystem pfs = new PartitionFileSystem(containerFile.AsStorage());
                _virtualFileSystem.ImportTickets(pfs);

                var allChildrenEnabled = true;

                var dlcNcaLoader = new DlcNcaLoader(_titleId, dlcContainer.Path, _localStorageManagement, _virtualFileSystem);

                foreach (var dlcNca in dlcNcaLoader.Load())
                {
                    var savedDlcNca = dlcContainer.DlcNcaList.FirstOrDefault(d => d.TitleId == dlcNca.TitleId);

                    if (!savedDlcNca.Enabled)
                    {
                        allChildrenEnabled = false;
                    }

                    _treeModel.AppendValues(parentIter, savedDlcNca.Enabled, dlcNca.TitleId.ToString("X16"), dlcNca.Path);
                }

                _treeModel.SetValue(parentIter, (int)TreeStoreColumn.Enabled, allChildrenEnabled);
            }
        }

        private CellRendererToggle GetEnableCellRenderToggle()
        {
            var enableToggle = new CellRendererToggle();

            enableToggle.Toggled += (sender, args) =>
            {
                _treeModel.GetIter(out TreeIter treeIter, new TreePath(args.Path));
                var newValue = !(bool)_treeModel.GetValue(treeIter, (int)TreeStoreColumn.Enabled);
                _treeModel.SetValue(treeIter, (int)TreeStoreColumn.Enabled, newValue);

                if (_treeModel.IterHasChild(treeIter))
                {
                    _treeModel.ForEachChildren(treeIter, c => _treeModel.SetValue(c, (int)TreeStoreColumn.Enabled, newValue));
                }                    
                else
                {
                    if (_treeModel.IterParent(out var parentIter, treeIter))
                    {
                        var totalChildren = 0;
                        var totalEnabled = 0;

                        _treeModel.ForEachChildren(parentIter, c =>
                        {
                            totalChildren++;

                            if ((bool)_treeModel.GetValue(c, (int)TreeStoreColumn.Enabled))
                            {
                                totalEnabled++;
                            }                                
                        });

                        _treeModel.SetValue(parentIter, (int)TreeStoreColumn.Enabled, totalEnabled == totalChildren);
                    }
                }
            };

            return enableToggle;
        }

        private void AddButton_Clicked(object sender, EventArgs args)
        {
            using var fileChooser = new FileChooserDialog("Select DLC files", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Add", ResponseType.Accept)
            {
                SelectMultiple = true,
                Filter = new FileFilter()
            };
            fileChooser.SetPosition(WindowPosition.Center);
            fileChooser.Filter.AddPattern("*.nsp");

            if (fileChooser.Run() != (int)ResponseType.Accept)
            {
                return;
            }
            
            foreach (string containerPath in fileChooser.Filenames.Where(f => _localStorageManagement.Exists(f)))
            {
                var dlcLoader = new DlcNcaLoader(_titleId, containerPath, _localStorageManagement, _virtualFileSystem);

                var dlcNcas = dlcLoader.Load();
                if (!dlcNcas.Any())
                {
                    GtkDialog.CreateErrorDialog($"The file {containerPath} does not contain DLC for the selected title!");
                    break;
                }

                TreeIter? parentIter = null;

                foreach (var nca in dlcNcas)
                {
                    parentIter ??= _treeModel.AppendValues(true, "", containerPath);
                    _treeModel.AppendValues(parentIter.Value, true, nca.TitleId.ToString("X16"), nca.Path);
                }
            }
        }

        private void RemoveButton_Clicked(object sender, EventArgs args)
        {
            if (_dlcTreeSelection.GetSelected(out _, out TreeIter treeIter))
            {
                if (_dlcTreeView.Model.IterParent(out TreeIter parentIter, treeIter) && _dlcTreeView.Model.IterNChildren(parentIter) <= 1)
                {
                    _treeModel.Remove(ref parentIter);
                }
                else
                {
                    _treeModel.Remove(ref treeIter);
                }
            }
        }

        private void RemoveAllButton_Clicked(object sender, EventArgs args) => _treeModel.Clear();

        private void SaveButton_Clicked(object sender, EventArgs args)
        {
            var dlcContainerList = new List<DlcContainer>();

            _treeModel.ForEach((parentIter) =>
            {
                var dlcContainer = new DlcContainer
                {
                    Path = (string)_treeModel.GetValue(parentIter, (int)TreeStoreColumn.Path),
                    DlcNcaList = new List<DlcNca>()
                };

                _treeModel.ForEachChildren(parentIter, (ncaIter) =>
                {
                    dlcContainer.DlcNcaList.Add(new DlcNca(
                        enabled: (bool)_treeModel.GetValue(ncaIter, (int)TreeStoreColumn.Enabled),
                        titleId: Convert.ToUInt64(_treeModel.GetValue(ncaIter, (int)TreeStoreColumn.TitleId).ToString(), 16),
                        path: (string)_treeModel.GetValue(ncaIter, (int)TreeStoreColumn.Path)
                    ));
                });

                dlcContainerList.Add(dlcContainer);
            });

            using (FileStream dlcJsonStream = File.Create(_dlcJsonPath, 4096, FileOptions.WriteThrough))
            {
                dlcJsonStream.Write(Encoding.UTF8.GetBytes(JsonHelper.Serialize(dlcContainerList, true)));
            }

            Dispose();
        }

        private void CancelButton_Clicked(object sender, EventArgs args)
        {
            Dispose();
        }
    }
}