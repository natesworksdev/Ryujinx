using Avalonia;
using Avalonia.Controls;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid.Controller;

namespace Ryujinx.Ava.UI.Views.Input
{
    public partial class InputButtonView : UserControl
    {
        public static readonly DirectProperty<InputTriggerView, StickInputId> SideProperty =
            AvaloniaProperty.RegisterDirect<InputTriggerView, StickInputId>(
                nameof(Side),
                o => o.Side,
                (o, v) => o.Side = v);
        public InputButtonViewModel ViewModel;

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

        public InputButtonView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (Parent.DataContext is ControllerSettingsViewModel viewModel)
            {
                ViewModel = new InputButtonViewModel(Side, viewModel);
            }

            DataContext = ViewModel;
        }
    }
}