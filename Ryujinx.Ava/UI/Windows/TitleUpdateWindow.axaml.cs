using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using DynamicData;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.FileSystem;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class TitleUpdateWindow : UserControl
    {
        public TitleUpdateViewModel ViewModel;

        public TitleUpdateWindow()
        {
            DataContext = this;

            InitializeComponent();
        }

        public TitleUpdateWindow(VirtualFileSystem virtualFileSystem, ulong titleId, string titleName)
        {
            DataContext = ViewModel = new TitleUpdateViewModel(virtualFileSystem, titleId, titleName);

            InitializeComponent();
        }

        public static async Task Show(VirtualFileSystem virtualFileSystem, ulong titleId, string titleName)
        {
            ContentDialog contentDialog = new()
            {
                PrimaryButtonText   = "",
                SecondaryButtonText = "",
                CloseButtonText     = "",
                Content             = new TitleUpdateWindow(virtualFileSystem, titleId, titleName)
            };

            Style bottomBorder = new(x => x.OfType<Grid>().Name("DialogSpace").Child().OfType<Border>());
            bottomBorder.Setters.Add(new Setter(IsVisibleProperty, false));

            contentDialog.Styles.Add(bottomBorder);

            await ContentDialogHelper.ShowAsync(contentDialog);
        }
    }
}