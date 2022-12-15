using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Views.Main;

public partial class MainMenuBarView : UserControl
{
    public MainWindow Window;
    public MainWindowViewModel ViewModel;
    
    public MainMenuBarView()
    {
        InitializeComponent();
    }

    private async void StopEmulation_Click(object sender, RoutedEventArgs e)
    {
        await Task.Run(() =>
        {
            Window.ViewModel.AppHost?.ShowExitPrompt();
        });
    }

    private async void PauseEmulation_Click(object sender, RoutedEventArgs e)
    {
        await Task.Run(() =>
        {
            Window.ViewModel.AppHost?.Pause();
        });
    }

    private async void ResumeEmulation_Click(object sender, RoutedEventArgs e)
    {
        await Task.Run(() =>
        {
            Window.ViewModel.AppHost?.Resume();
        });
    }

    private void ScanAmiiboMenuItem_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is MenuItem)
        {
            ViewModel.IsAmiiboRequested = Window.ViewModel.AppHost.Device.System.SearchingForAmiibo(out _);
        }
    }
}