using Avalonia.Controls;

namespace Ryujinx.Ava.UI.Views.Settings;

public partial class SettingsInputView : UserControl
{
    public SettingsInputView()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded()
    {
        ControllerSettings.Dispose();
        
        base.OnUnloaded();
    }
}