using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ryujinx.Ava.Ui.Controls.Settings;

public partial class SettingsNetworkView : UserControl
{
    public SettingsNetworkView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}