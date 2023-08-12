using Gtk;
using Ryujinx.Ui.Common.Helper;
using System.Reflection;

namespace Ryujinx.Ui.Windows
{
    public partial class SwitchToAvaWindow : Window
    {
        public SwitchToAvaWindow() : base($"Switch to Avalonia")
        {
            Icon = new Gdk.Pixbuf(Assembly.GetAssembly(typeof(OpenHelper)), "Ryujinx.Ui.Common.Resources.Logo_Ryujinx.png");
            InitializeComponent();
        }
    }
}
