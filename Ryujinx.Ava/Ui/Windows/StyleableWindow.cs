using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using FluentAvalonia.Core.ApplicationModel;
using FluentAvalonia.UI.Controls;
using System;
using System.IO;
using System.Reflection;

namespace Ryujinx.Ava.Ui.Windows
{
    public class StyleableWindow : Window
    {
        public ContentDialog ContentDialog { get; private set; }

        public StyleableWindow()
        {
            ExtendClientAreaToDecorationsHint = false;
            TransparencyLevelHint             = WindowTransparencyLevel.None;

            this.GetObservable(WindowStateProperty)
                .Subscribe(x =>
                {
                    PseudoClasses.Set(":maximized",  x == WindowState.Maximized);
                    PseudoClasses.Set(":fullscreen", x == WindowState.FullScreen);
                });

            this.GetObservable(IsExtendedIntoWindowDecorationsProperty)
                .Subscribe(x =>
                {
                    if (!x)
                    {
                        TransparencyLevelHint = WindowTransparencyLevel.None;
                        SystemDecorations     = SystemDecorations.Full;
                    }
                });

            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ryujinx.Ava.Assets.Images.Logo_Ryujinx.png");

            Icon = new WindowIcon(stream);
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