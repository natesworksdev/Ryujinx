using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;

namespace Ryujinx.Ava.Ui.Controls.Settings;

public partial class SettingsHotkeysView : UserControl
{
    private ButtonKeyAssigner _currentAssigner;
    private SettingsViewModel _viewModel;
    
    public SettingsHotkeysView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _viewModel = DataContext as SettingsViewModel;
    }
    
    private void MouseClick(object sender, PointerPressedEventArgs e)
    {
        bool shouldUnbind = e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed;

        _currentAssigner?.Cancel(shouldUnbind);

        PointerPressed -= MouseClick;
    }
    
    private void Button_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton button)
        {
            if (_currentAssigner != null && button == _currentAssigner.ToggledButton)
            {
                return;
            }

            if (_currentAssigner == null && (bool)button.IsChecked)
            {
                _currentAssigner = new ButtonKeyAssigner(button);

                FocusManager.Instance.Focus(this, NavigationMethod.Pointer);

                PointerPressed += MouseClick;

                IKeyboard       keyboard = (IKeyboard)_viewModel.AvaloniaKeyboardDriver.GetGamepad(_viewModel.AvaloniaKeyboardDriver.GamepadsIds[0]);
                IButtonAssigner assigner = new KeyboardKeyAssigner(keyboard);

                _currentAssigner.GetInputAndAssign(assigner);
            }
            else
            {
                if (_currentAssigner != null)
                {
                    ToggleButton oldButton = _currentAssigner.ToggledButton;

                    _currentAssigner.Cancel();
                    _currentAssigner = null;

                    button.IsChecked = false;
                }
            }
        }
    }

    private void Button_Unchecked(object sender, RoutedEventArgs e)
    {
        _currentAssigner?.Cancel();
        _currentAssigner = null;
    }

    protected override void OnUnloaded()
    {
        _currentAssigner?.Cancel();
        _currentAssigner = null;
        
        base.OnUnloaded();
    }
}