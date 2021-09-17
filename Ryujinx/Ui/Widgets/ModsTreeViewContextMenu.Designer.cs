using Gtk;

namespace Ryujinx.Ui.Widgets
{
    public partial class ModsTreeViewContextMenu : Menu
    {
        private MenuItem _openModFolderMenuItem;

        private void InitializeComponent()
        {
            //
            // _openModFolderMenuItem
            //
            _openModFolderMenuItem = new MenuItem("Open mod folder")
            {
                TooltipText = "Open the directory which contains the mod.",
            };
            _openModFolderMenuItem.Activated += OpenModFolderMenuItem_Activated;

            ShowComponent();
        }

        private void ShowComponent()
        {
            Add(_openModFolderMenuItem);

            ShowAll();
        }
    }
}