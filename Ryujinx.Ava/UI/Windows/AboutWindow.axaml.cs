using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using Avalonia.Threading;
using DynamicData;
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
                PrimaryButtonText = "",
                SecondaryButtonText = "",
                CloseButtonText = LocaleManager.Instance["UserProfilesClose"],
                Content = content
            };
            
            Style closeButton = new(x => x.Name("CloseButton"));
            closeButton.Setters.Add(new Setter(WidthProperty, 70d));

            Style closeButtonParent = new(x => x.Name("CommandSpace"));
            closeButtonParent.Setters.Add(new Setter(HorizontalAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Right));

            contentDialog.Styles.Add(closeButton);
            contentDialog.Styles.Add(closeButtonParent);
            
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