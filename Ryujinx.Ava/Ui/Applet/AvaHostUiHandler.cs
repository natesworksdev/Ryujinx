using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.HLE;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Services.Am.AppletOE.ApplicationProxyService.ApplicationProxy.Types;
using Ryujinx.HLE.Ui;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace Ryujinx.Ava.Ui.Applet
{
    internal class AvaHostUiHandler : IHostUiHandler
    {
        private readonly MainWindow _parent;

        public IHostUiTheme HostUiTheme { get; }

        public AvaHostUiHandler(MainWindow parent)
        {
            _parent = parent;

            HostUiTheme = new AvaloniaHostUiTheme(parent);
        }

        public bool DisplayMessageDialog(ControllerAppletUiArgs args)
        {
            string playerCount = args.PlayerCountMin == args.PlayerCountMax
                ? $"exactly {args.PlayerCountMin}"
                : $"{args.PlayerCountMin}-{args.PlayerCountMax}";

            string message = $"Application requests {playerCount} player(s) with:\n\n"
                             + $"TYPES: {args.SupportedStyles}\n\n"
                             + $"PLAYERS: {string.Join(", ", args.SupportedPlayers)}\n\n"
                             + (args.IsDocked ? "Docked mode set. Handheld is also invalid.\n\n" : "")
                             + "Please open Settings and reconfigure Input now or press Close.";

            return DisplayMessageDialog(LocaleManager.Instance["DialogControllerAppletTitle"], message);
        }

        public bool DisplayMessageDialog(string title, string message)
        {
            ManualResetEvent dialogCloseEvent = new(false);

            bool okPressed = false;

            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    ManualResetEvent deferEvent = new(false);

                    bool opened = false;

                    UserResult response = await ContentDialogHelper.ShowDeferredContentDialog(_parent, title, message, "", 
                        LocaleManager.Instance["DialogOpenSettingsWindow"], "", "Close", 0xF4A3, deferEvent,
                       async  (window) =>
                        {
                            if (opened)
                            {
                                return;
                            }

                            opened = true;
                            
                            _parent.SettingsWindow = new SettingsWindow(_parent.VirtualFileSystem, _parent.ContentManager);

                            await _parent.SettingsWindow.ShowDialog(window);

                            opened = false;
                        });

                    if (response == UserResult.Ok)
                    {
                        okPressed = true;
                    }

                    dialogCloseEvent.Set();
                }
                catch (Exception ex)
                {
                    ContentDialogHelper.CreateErrorDialog(_parent, string.Format(LocaleManager.Instance["DialogMessageDialogErrorExceptionMessage"], ex));

                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();

            return okPressed;
        }

        public bool DisplayInputDialog(SoftwareKeyboardUiArgs args, out string userText)
        {
            ManualResetEvent dialogCloseEvent = new(false);

            bool okPressed = false;
            bool error = false;
            string inputText = args.InitialText ?? "";

            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    var response = await SwkbdAppletDialog.ShowInputDialog(_parent, "Software Keyboard", args);

                    if (response.Result == UserResult.Ok)
                    {
                        inputText = response.Input;
                        okPressed = true;
                    }
                }
                catch (Exception ex)
                {
                    error = true;
                    ContentDialogHelper.CreateErrorDialog(_parent, string.Format(LocaleManager.Instance["DialogSoftwareKeyboardErrorExceptionMessage"], ex));
                }
                finally
                {
                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();

            userText = error ? null : inputText;

            return error || okPressed;
        }

        public void ExecuteProgram(Switch device, ProgramSpecifyKind kind, ulong value)
        {
            device.Configuration.UserChannelPersistence.ExecuteProgram(kind, value);
            if (((MainWindow)_parent).AppHost != null)
            {
                Task.Run(((MainWindow)_parent).AppHost.Dispose);
            }
        }

        public bool DisplayErrorAppletDialog(string title, string message, string[] buttons)
        {
            ManualResetEvent dialogCloseEvent = new(false);

            bool showDetails = false;

            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    ErrorAppletWindow msgDialog = new(_parent, buttons, message)
                    {
                        Title = title, WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    msgDialog.Width = 400;

                    object response = await msgDialog.Run();

                    if (response != null)
                    {
                        if (buttons.Length > 1)
                        {
                            if ((int)response != buttons.Length - 1)
                            {
                                showDetails = true;
                            }
                        }
                    }

                    dialogCloseEvent.Set();

                    msgDialog.Close();
                }
                catch (Exception ex)
                {
                    dialogCloseEvent.Set();
                    ContentDialogHelper.CreateErrorDialog(_parent, string.Format(LocaleManager.Instance["DialogErrorAppletErrorExceptionMessage"], ex));
                }
            });

            dialogCloseEvent.WaitOne();

            return showDetails;
        }

        public IDynamicTextInputHandler CreateDynamicTextInputHandler()
        {
            return new AvaloniaDynamicTextInputHandler(_parent);
        }
    }
}