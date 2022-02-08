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
    public class StyleableWindow : Window
    {
        public ContentDialog ContentDialog { get; private set; }
        public IBitmap IconImage { get; set; }

        public StyleableWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            TransparencyLevelHint = WindowTransparencyLevel.None;

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
            ContentDialog = this.FindControl<ContentDialog>("ContentDialog");
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome | ExtendClientAreaChromeHints.OSXThickTitleBar;
        }
    }
}