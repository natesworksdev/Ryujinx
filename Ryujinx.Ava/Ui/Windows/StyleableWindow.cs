using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.Core.ApplicationModel;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Controls.Primitives;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Windows
{
    public class StyleableWindow : CoreWindow
    {
        public ContentDialog ContentDialog { get; private set; }
        public IBitmap IconImage { get; set; }
        public Control           WindowButtons     { get; set; }

        public StyleableWindow()
        {
            ExtendClientAreaToDecorationsHint = true;
            TransparencyLevelHint = WindowTransparencyLevel.Blur;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ryujinx.Ava.Assets.Images.Logo_Ryujinx.png");

            Icon = new WindowIcon(stream);
            stream.Position = 0;
            IconImage = new Bitmap(stream);
        }

        public void LoadDialog()
        {
            ContentDialog = this.FindControl<ContentDialog>("ContentDialog");
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            SetTitleBar(this);
            ContentDialog = this.FindControl<ContentDialog>("ContentDialog");

            foreach(var control in this.GetVisualDescendants().OfType<MinMaxCloseControl>())
            {
                WindowButtons = control;
                break;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome | ExtendClientAreaChromeHints.OSXThickTitleBar;
        }
        
        private void SetTitleBar(CoreWindow cw)
        {
            var titleBar = cw.TitleBar;
            if (titleBar != null)
            {
                titleBar.ExtendViewIntoTitleBar = true;
                if (this.FindControl<Grid>("TitleBarHost") is Grid g)
                {
                    cw.SetTitleBar(g);
                    g.Margin = new Thickness(0, 0, titleBar.SystemOverlayRightInset, 0);
                }
            }
        }
    }
}