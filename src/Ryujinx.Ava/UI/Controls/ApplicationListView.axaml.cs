using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ui.App.Common;
using System;
using System.Linq;

namespace Ryujinx.Ava.UI.Controls
{
    public partial class ApplicationListView : UserControl
    {
        public static readonly RoutedEvent<ApplicationOpenedEventArgs> ApplicationOpenedEvent =
            RoutedEvent.Register<ApplicationListView, ApplicationOpenedEventArgs>(nameof(ApplicationOpened), RoutingStrategies.Bubble);

        public event EventHandler<ApplicationOpenedEventArgs> ApplicationOpened
        {
            add { AddHandler(ApplicationOpenedEvent, value); }
            remove { RemoveHandler(ApplicationOpenedEvent, value); }
        }

        public ApplicationListView()
        {
            InitializeComponent();
        }

        public void GameList_DoubleTapped(object sender, TappedEventArgs args)
        {
            if (sender is ListBox listBox)
            {
                var children = listBox.GetLogicalChildren().Reverse().ToArray();
                if ((children[listBox.SelectedIndex] as ListBoxItem).IsPointerOver)
                {
                    RaiseEvent(new ApplicationOpenedEventArgs(listBox.SelectedItem as ApplicationData, ApplicationOpenedEvent));
                }
            }
        }

        public void GameList_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (sender is ListBox listBox)
            {
                (DataContext as MainWindowViewModel).ListSelectedApplication = listBox.SelectedItem as ApplicationData;
            }
        }
    }
}
