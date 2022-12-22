using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Utilities;
using Ryujinx.Ui.Common.Helper;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class AboutWindow : StyleableWindow
    {
        internal AboutWindowViewModel ViewModel { get; private set; }

        public AboutWindow()
        {
            if (Program.PreviewerDetached)
            {
                Title = $"Ryujinx {Program.Version} - " + LocaleManager.Instance["MenuBarHelpAbout"];
            }
            
            DataContext = ViewModel = new AboutWindowViewModel();
            
            ViewModel.Version = Program.Version;
            ViewModel.Developers = string.Format(LocaleManager.Instance["AboutPageDeveloperListMore"], "gdkchan, Ac_K, Thog, rip in peri peri, LDj3SNuD, emmaus, Thealexbarney, Xpl0itR, GoffyDude, »jD«");

            InitializeComponent();

            _ = DownloadPatronsJson();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                OpenHelper.OpenUrl(button.Tag.ToString());
            }
        }

        private async Task DownloadPatronsJson()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                ViewModel.Supporters = LocaleManager.Instance["ConnectionError"];

                return;
            }

            HttpClient httpClient = new();

            try
            {
                string patreonJsonString = await httpClient.GetStringAsync("https://patreon.ryujinx.org/");

                ViewModel.Supporters = string.Join(", ", JsonHelper.Deserialize<string[]>(patreonJsonString));
            }
            catch
            {
                ViewModel.Supporters = LocaleManager.Instance["ApiError"];
            }
        }

        private void AmiiboLabel_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is TextBlock)
            {
                OpenHelper.OpenUrl("https://amiiboapi.com");
            }
        }
    }
}