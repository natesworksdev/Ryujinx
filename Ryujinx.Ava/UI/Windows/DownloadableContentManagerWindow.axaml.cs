using Avalonia.Controls;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.FileSystem;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class DownloadableContentManagerWindow : UserControl
    {
        public DownloadableContentManagerViewModel ViewModel;

        public DownloadableContentManagerWindow()
        {
            DataContext = this;

            InitializeComponent();
        }

        public DownloadableContentManagerWindow(VirtualFileSystem virtualFileSystem, ulong titleId, string titleName)
        {
            DataContext = ViewModel = new DownloadableContentManagerViewModel(virtualFileSystem, titleId, titleName);

            InitializeComponent();

            RemoveButton.IsEnabled = false;

            DlcDataGrid.SelectionChanged += DlcDataGrid_SelectionChanged;
        }

        public static async Task Show(VirtualFileSystem virtualFileSystem, ulong titleId, string titleName)
        {
            ContentDialog contentDialog = new()
            {
                PrimaryButtonText   = "",
                SecondaryButtonText = "",
                CloseButtonText     = "",
                Content             = new DownloadableContentManagerViewModel(virtualFileSystem, titleId, titleName),
                Title               = string.Format(LocaleManager.Instance[LocaleKeys.DlcWindowTitle], titleName, titleId.ToString("X16"))
            };

            Style bottomBorder = new(x => x.OfType<Grid>().Name("DialogSpace").Child().OfType<Border>());
            bottomBorder.Setters.Add(new Setter(IsVisibleProperty, false));

            contentDialog.Styles.Add(bottomBorder);

            await ContentDialogHelper.ShowAsync(contentDialog);
        }

        private void DlcDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveButton.IsEnabled = (DlcDataGrid.SelectedItems.Count > 0);
        }

        public void SaveAndClose()
        {
            ViewModel.Save();
            ((ContentDialog)Parent).Hide();
        }
    }
}