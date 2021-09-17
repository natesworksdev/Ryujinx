using Gtk;
using Ryujinx.Ui.Helper;
using System;

namespace Ryujinx.Ui.Widgets
{
    public partial class ModsTreeViewContextMenu : Menu
    {
        private readonly string _modFolder;

        public ModsTreeViewContextMenu(string modFolder)
        {
            InitializeComponent();

            _modFolder = modFolder;

            PopupAtPointer(null);
        }

        private void OpenModFolderMenuItem_Activated(object sender, EventArgs args)
        {
            OpenHelper.OpenFolder(_modFolder);
        }
    }
}
