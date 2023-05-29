using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using System;

namespace Ryujinx.Ava.UI.Views.Main
{
    public partial class MainViewControls : UserControl
    {
        public MainWindowViewModel ViewModel;
    
        public MainViewControls()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (VisualRoot is MainWindow window)
            {
                ViewModel = window.ViewModel;
            }

            DataContext = ViewModel;
        }

        public void Sort_Checked(object sender, RoutedEventArgs args)
        {
            if (sender is RadioButton button)
            {
                ViewModel.Sort(Enum.Parse<ApplicationSort>(button.Tag.ToString()));
            }
        }
    
        public void Order_Checked(object sender, RoutedEventArgs args)
        {
            if (sender is RadioButton button)
            {
                ViewModel.Sort(button.Tag.ToString() != "Descending");
            }
        }
    
        private void SearchBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            ViewModel.SearchText = SearchBox.Text;
        }

        public void UserProfileIcon_Click(object sender, PointerPressedEventArgs e)
        {
            ViewModel.ManageProfiles();
        }

        public void UserProfileIcon_Enter(object sender, PointerEventArgs e)
        {
            /*if (sender is Image img)
            {
                img.Height *= 2;
                img.Width *= 2;
                img.ClipToBounds = false;
            }//*/
        }

        public void UserProfileIcon_Leave(object sender, PointerEventArgs e)
        {
            /*if (sender is Image img)
            {
                img.Height /= 2;
                img.Width /= 2;
                //img.ClipToBounds = true;
            }//*/
        }
    }
}