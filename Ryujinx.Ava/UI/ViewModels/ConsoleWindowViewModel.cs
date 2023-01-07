using Avalonia.Controls.Primitives;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.Windows;
using System.Collections.ObjectModel;

namespace Ryujinx.Ava.UI.ViewModels
{
    internal class ConsoleWindowViewModel : BaseModel
    {
        private readonly ConsoleWindow _owner;
        private bool _autoScroll = true;

        public ConsoleWindowViewModel(ConsoleWindow window)
        {
            _owner = window;
        }

        public ObservableCollection<InMemoryLogTarget.Entry> LogEntries => InMemoryLogTarget.Instance.Entries;

        public ScrollBarVisibility ScrollBarVisible
        {
            get { return _autoScroll ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Visible; }
        }

        public bool AutoScroll
        {
            get { return _autoScroll; }
            set
            {
                _autoScroll = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScrollBarVisible));
            }
        }

        public static void OpenLogsFolder()
        {
            MainWindowViewModel.OpenLogsFolder();
        }
    }
}