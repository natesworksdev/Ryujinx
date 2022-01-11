using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using FluentAvalonia.Styling;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.Common;
using Ryujinx.Configuration;
using System;
using System.Linq;
using System.Diagnostics;
using Ryujinx.Common.Logging;
using Avalonia.Rendering;

namespace Ryujinx.Ava
{
    public class App : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();

            if (Program.PreviewerDetached)
            {
                ApplyConfiguredTheme();

                ConfigurationState.Instance.Ui.BaseStyle.Event += ThemeChanged_Event;
                ConfigurationState.Instance.Ui.CustomThemePath.Event += ThemeChanged_Event;
                ConfigurationState.Instance.Ui.EnableCustomTheme.Event += CustomThemeChanged_Event;
            }
        }

        private void CustomThemeChanged_Event(object sender, ReactiveEventArgs<bool> e)
        {
            try
            {
                ApplyConfiguredTheme();
            }
            catch (Exception)
            {
                Logger.Warning?.Print(LogClass.Application, "Failed to Apply Theme. A restart is needed to apply the selected theme");

                ShowRestartDialog();
            }
        }

        private async void ShowRestartDialog()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var result = await ContentDialogHelper.CreateConfirmationDialog(
                        (desktop.MainWindow as MainWindow).SettingsWindow,
                        LocaleManager.Instance["DialogThemeRestartMessage"],
                        LocaleManager.Instance["DialogThemeRestartSubMessage"], LocaleManager.Instance["InputDialogYes"], LocaleManager.Instance["InputDialogNo"], LocaleManager.Instance["DialogRestartRequiredMessage"]);

                    if (result == UserResult.Yes)
                    {
                        var path = Process.GetCurrentProcess().MainModule.FileName;
                        var info = new ProcessStartInfo() {FileName = path, UseShellExecute = false};
                        var proc = Process.Start(info);
                        desktop.Shutdown();
                        Environment.Exit(0);
                    }
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private void ThemeChanged_Event(object sender, ReactiveEventArgs<string> e)
        {
            try
            {
                ApplyConfiguredTheme();
            }
            catch (Exception)
            {
                Logger.Warning?.Print(LogClass.Application, "Failed to Apply Theme. A restart is needed to apply the selected theme");

                ShowRestartDialog();
            }
        }

        private void ApplyConfiguredTheme()
        {
            string baseStyle = ConfigurationState.Instance.Ui.BaseStyle;
            string themePath = ConfigurationState.Instance.Ui.CustomThemePath;
            bool enableCustomTheme = ConfigurationState.Instance.Ui.EnableCustomTheme;

            if (string.IsNullOrWhiteSpace(baseStyle))
            {
                ConfigurationState.Instance.Ui.BaseStyle.Value = "Dark";

                baseStyle = ConfigurationState.Instance.Ui.BaseStyle;
            }

            var theme = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>();
            
            theme.RequestedTheme = baseStyle;
            
            var currentStyles = this.Styles;

            if (currentStyles.Count > 2)
            {
                currentStyles.RemoveRange(2, currentStyles.Count - 1);
            }

            IStyle newStyles = null;

            newStyles = (Styles)AvaloniaXamlLoader.Load(new Uri($"avares://Ryujinx.Ava/Assets/Styles/Base{baseStyle}.xaml", UriKind.Absolute));

            if (currentStyles.Count == 3)
            {
                currentStyles[2] = newStyles;
            }
            else
            {
                currentStyles.Add(newStyles);
            }

            if (enableCustomTheme)
            {
                if (!string.IsNullOrWhiteSpace(themePath))
                {
                    try
                    {
                        var themeContent = System.IO.File.ReadAllText(themePath);
                        var customStyle = AvaloniaRuntimeXamlLoader.Parse<IStyle>(themeContent);

                        if (currentStyles.Count == 4)
                        {
                            currentStyles[3] = customStyle;
                        }
                        else
                        {
                            currentStyles.Add(customStyle);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error?.Print(LogClass.Application, $"Failed to Apply Custom Theme. Error: {ex.Message}");
                    }
                }
            }
        }
    }
}