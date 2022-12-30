using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Common.Utilities;
using Ryujinx.Ui.Common.Helper;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Button = Avalonia.Controls.Button;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class AboutWindow : UserControl
    {
        public AboutWindow()
        {
            Version = Program.Version;

            DataContext = this;

            InitializeComponent();

            _ = DownloadPatronsJson();
        }

        public string Supporters { get; set; }
        public string Version { get; set; }

        public string Developers => string.Format(LocaleManager.Instance["AboutPageDeveloperListMore"], "gdkchan, Ac_K, Thog, rip in peri peri, LDj3SNuD, emmaus, Thealexbarney, Xpl0itR, GoffyDude, »jD«");

        public static async Task Show()
        {
            var content = new AboutWindow();
            ContentDialog contentDialog = new ContentDialog
            {
                Title = LocaleManager.Instance["MenuBarHelpAbout"],
                PrimaryButtonText = "",
                SecondaryButtonText = "",
                CloseButtonText = LocaleManager.Instance["UserProfilesClose"],
                Content = content,
                Padding = new Thickness(0)
            };

            await contentDialog.ShowAsync();
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
                Supporters = LocaleManager.Instance["ConnectionError"];

                return;
            }

            HttpClient httpClient = new();

            try
            {
                string patreonJsonString = await httpClient.GetStringAsync("https://patreon.ryujinx.org/");

                Supporters = string.Join(", ", JsonHelper.Deserialize<string[]>(patreonJsonString));
            }
            catch
            {
                Supporters = LocaleManager.Instance["ApiError"];
            }

            await Dispatcher.UIThread.InvokeAsync(() => SupportersTextBlock.Text = Supporters);
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