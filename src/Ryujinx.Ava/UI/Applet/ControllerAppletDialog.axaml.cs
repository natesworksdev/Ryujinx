using Avalonia.Controls;
using Avalonia.Svg.Skia;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Common;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Services.Hid;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Applet
{
    internal partial class ControllerAppletDialog : UserControl
    {
        private const string ProControllerResource = "Ryujinx.Ui.Common/Resources/Icon_Controller_ProCon.svg";
        private const string JoyConPairResource = "Ryujinx.Ui.Common/Resources/Icon_Controller_JoyConPair.svg";
        private const string JoyConLeftResource = "Ryujinx.Ui.Common/Resources/Icon_Controller_JoyConLeft.svg";
        private const string JoyConRightResource = "Ryujinx.Ui.Common/Resources/Icon_Controller_JoyConRight.svg";

        public SvgImage ProControllerImage
        {
            get
            {
                return GetResource(ProControllerResource);
            }
        }

        public SvgImage JoyconPairImage
        {
            get
            {
                return GetResource(JoyConPairResource);
            }
        }

        public SvgImage JoyconLeftImage
        {
            get
            {
                return GetResource(JoyConLeftResource);
            }
        }

        public SvgImage JoyconRightImage
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

        public ControllerAppletDialog(ControllerAppletUiArgs args)
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

            DataContext = this;
            InitializeComponent();
        }

        public ControllerAppletDialog()
        {
            DataContext = this;
            InitializeComponent();
        }

        public static async Task<UserResult> ShowInputDialog(ControllerAppletUiArgs args)
        {
            ContentDialog contentDialog = new();

            UserResult result = UserResult.Cancel;

            ControllerAppletDialog content = new(args);

            contentDialog.Title = LocaleManager.Instance[LocaleKeys.DialogControllerAppletTitle];
            contentDialog.CloseButtonText = LocaleManager.Instance[LocaleKeys.InputDialogCancel];
            contentDialog.Content = content;

            void Handler(ContentDialog sender, ContentDialogClosedEventArgs eventArgs)
            {
                if (eventArgs.Result == ContentDialogResult.Primary)
                {
                    result = UserResult.Ok;
                }
            }

            contentDialog.Closed += Handler;

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
    }
}

