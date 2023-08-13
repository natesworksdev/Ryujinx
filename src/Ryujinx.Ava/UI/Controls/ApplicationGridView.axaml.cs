using Avalonia.Controls;
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
    public partial class ApplicationGridView : UserControl
    {
        public static readonly RoutedEvent<ApplicationOpenedEventArgs> ApplicationOpenedEvent =
            RoutedEvent.Register<ApplicationGridView, ApplicationOpenedEventArgs>(nameof(ApplicationOpened), RoutingStrategies.Bubble);

        public event EventHandler<ApplicationOpenedEventArgs> ApplicationOpened
        {
            add { AddHandler(ApplicationOpenedEvent, value); }
            remove { RemoveHandler(ApplicationOpenedEvent, value); }
        }

        public ApplicationGridView()
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
                (DataContext as MainWindowViewModel).GridSelectedApplication = listBox.SelectedItem as ApplicationData;
            }
        }
    }
}
