using Gtk;

namespace Ryujinx.Ui.Windows
{
    public partial class SwitchToAvaWindow : Window
    {
        private void InitializeComponent()
        {
            //
            // AboutWindow
            //
            CanFocus = false;
            Resizable = false;
            Modal = true;
            WindowPosition = WindowPosition.Center;
            DefaultWidth = 800;
            DefaultHeight = 450;
            TypeHint = Gdk.WindowTypeHint.Dialog;
            
            ShowComponent();
        }

        private void ShowComponent()
        {
            
        }
    }
}
