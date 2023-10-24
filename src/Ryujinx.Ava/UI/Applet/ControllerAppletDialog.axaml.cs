using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Common;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Services.Hid;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Applet
{
    internal partial class ControllerAppletDialog : UserControl
    {
        private const string ProControllerResource = "Ryujinx.Ui.Common/Resources/Icon_Controller_ProCon.svg";
        private const string JoyConPairResource = "Ryujinx.Ui.Common/Resources/Icon_Controller_JoyConPair.svg";
        private const string JoyConLeftResource = "Ryujinx.Ui.Common/Resources/Icon_Controller_JoyConLeft.svg";
        private const string JoyConRightResource = "Ryujinx.Ui.Common/Resources/Icon_Controller_JoyConRight.svg";

        public static SvgImage ProControllerImage
        {
            get
            {
                return GetResource(ProControllerResource);
            }
        }

        public static SvgImage JoyconPairImage
        {
            get
            {
                return GetResource(JoyConPairResource);
            }
        }

        public static SvgImage JoyconLeftImage
        {
            get
            {
                return GetResource(JoyConLeftResource);
            }
        }

        public static SvgImage JoyconRightImage
        {
            get
            {
                return GetResource(JoyConRightResource);
            }
        }

        public string PlayerCount { get; set; } = "";
        public bool SupportsProController { get; set; }
        public bool SupportsLeftJoycon { get; set; }
        public bool SupportsRightJoycon { get; set; }
        public bool SupportsJoyconPair { get; set; }
        public bool IsDocked { get; set; }

        private readonly MainWindow _mainWindow;

        public ControllerAppletDialog(ControllerAppletUiArgs args, MainWindow mainWindow)
        {
            if (args.PlayerCountMin == args.PlayerCountMax)
            {
                PlayerCount = args.PlayerCountMin.ToString();
            }
            else
            {
                PlayerCount = $"{args.PlayerCountMin} - {args.PlayerCountMax}";
            }

            SupportsProController = (args.SupportedStyles & ControllerType.ProController) != 0;
            SupportsLeftJoycon = (args.SupportedStyles & ControllerType.JoyconLeft) != 0;
            SupportsRightJoycon = (args.SupportedStyles & ControllerType.JoyconRight) != 0;
            SupportsJoyconPair = (args.SupportedStyles & ControllerType.JoyconPair) != 0;

            IsDocked = args.IsDocked;

            _mainWindow = mainWindow;

            DataContext = this;

            InitializeComponent();
        }

        public ControllerAppletDialog(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            DataContext = this;

            InitializeComponent();
        }

        public static async Task<UserResult> ShowControllerAppletDialog(ControllerAppletUiArgs args, MainWindow window)
        {
            ContentDialog contentDialog = new();
            UserResult result = UserResult.Cancel;
            ControllerAppletDialog content = new(args, window);

            contentDialog.Title = LocaleManager.Instance[LocaleKeys.DialogControllerAppletTitle];
            contentDialog.Content = content;

            void Handler(ContentDialog sender, ContentDialogClosedEventArgs eventArgs)
            {
                if (eventArgs.Result == ContentDialogResult.Primary)
                {
                    result = UserResult.Ok;
                }
            }

            contentDialog.Closed += Handler;

            Style bottomBorder = new(x => x.OfType<Grid>().Name("DialogSpace").Child().OfType<Border>());
            bottomBorder.Setters.Add(new Setter(IsVisibleProperty, false));

            contentDialog.Styles.Add(bottomBorder);

            await ContentDialogHelper.ShowAsync(contentDialog);

            return result;
        }

        private static SvgImage GetResource(string path)
        {
            SvgImage image = new();

            if (!string.IsNullOrWhiteSpace(path))
            {
                SvgSource source = new();

                source.Load(EmbeddedResources.GetStream(path));

                image.Source = source;
            }

            return image;
        }

        public void OpenSettingsWindow()
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                _mainWindow.SettingsWindow = new SettingsWindow(_mainWindow.VirtualFileSystem, _mainWindow.ContentManager);
                _mainWindow.SettingsWindow.NavPanel.Content = _mainWindow.SettingsWindow.InputPage;
                _mainWindow.SettingsWindow.NavPanel.SelectedItem = _mainWindow.SettingsWindow.NavPanel.MenuItems.ElementAt(1);

                await _mainWindow.SettingsWindow.ShowDialog(_mainWindow);
            });
        }

        public void Close()
        {
            ((ContentDialog)Parent).Hide();
        }
    }
}

