using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ryujinx.Ava.Ui.ViewModels
{
    internal class BaseModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}