using Avalonia;
using Avalonia.Controls;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Common.Configuration.Hid.Controller;

namespace Ryujinx.Ava.UI.Views.Input
{
    public partial class InputStickView : UserControl
    {
        public static readonly DirectProperty<InputStickView, StickInputId> SideProperty =
            AvaloniaProperty.RegisterDirect<InputStickView, StickInputId>(
                nameof(Side),
                o => o.Side,
                (o, v) => o.Side = v);
        public InputStickViewModel ViewModel;

        private StickInputId _side;
        public StickInputId Side
        {
            get
            {
                return _side;
            }
            set
            {
                SetAndRaise(SideProperty, ref _side, value);
            }
        }

        public InputStickView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (VisualRoot is ControllerSettingsWindow window)
            {
                ViewModel = new InputStickViewModel(Side);
            }

            DataContext = ViewModel;
        }
    }
}