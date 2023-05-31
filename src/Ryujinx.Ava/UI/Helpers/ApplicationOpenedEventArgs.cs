using Avalonia.Interactivity;
using Ryujinx.Ui.App.Common;

namespace Ryujinx.Ava.UI.Helpers
{
    public sealed class ApplicationOpenedEventArgs : RoutedEventArgs
    {
        public ApplicationData Application { get; }

        public ApplicationOpenedEventArgs(ApplicationData application, RoutedEvent routedEvent)
        {
            Application = application;
            RoutedEvent = routedEvent;
        }
    }
}