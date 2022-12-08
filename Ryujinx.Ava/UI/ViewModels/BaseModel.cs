using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class BaseModel : INotifyPropertyChanged
    {
        private readonly object _lock = new();
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            lock (_lock)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}