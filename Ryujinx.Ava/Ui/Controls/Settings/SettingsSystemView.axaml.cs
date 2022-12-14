using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Ryujinx.Ava.Ui.Models;
using Avalonia.Data.Converters;
using Avalonia.Data;
using Ryujinx.Ava.Ui.Windows;
using System.Linq;

namespace Ryujinx.Ava.Ui.Controls.Settings;

public partial class SettingsSystemView : UserControl
{
    private SettingsWindow _parent;

    public SettingsSystemView()
    {
        InitializeComponent();
        
        FuncMultiValueConverter<string, string> converter = new(parts => string.Format("{0}  {1}   {2}", parts.ToArray()).Trim());
        MultiBinding tzMultiBinding = new() { Converter = converter };
        tzMultiBinding.Bindings.Add(new Binding("UtcDifference"));
        tzMultiBinding.Bindings.Add(new Binding("Location"));
        tzMultiBinding.Bindings.Add(new Binding("Abbreviation"));

        TimeZoneBox.ValueMemberBinding = tzMultiBinding;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void TimeZoneBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems != null && e.AddedItems.Count > 0)
        {
            if (e.AddedItems[0] is TimeZone timeZone)
            {
                e.Handled = true;

                _parent.ViewModel.ValidateAndSetTimeZone(timeZone.Location);
            }
        }
    }

    private void TimeZoneBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is AutoCompleteBox box)
        {
            if (box.SelectedItem != null && box.SelectedItem is TimeZone timeZone)
            {
                _parent.ViewModel.ValidateAndSetTimeZone(timeZone.Location);
            }
        }
    }
}