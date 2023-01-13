using Avalonia.Controls.Primitives;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.Windows;
using System.Collections.ObjectModel;

namespace Ryujinx.Ava.UI.ViewModels
{
    internal class ConsoleWindowViewModel : BaseModel
    {
        private readonly ConsoleWindow _owner;

        public ConsoleWindowViewModel(ConsoleWindow window)
        {
            _owner = window;
        }

        public ObservableCollection<InMemoryLogTargetEntry> LogEntries => InMemoryLogTarget.Instance.Entries;

        public static void OpenLogsFolder()
        {
            MainWindowViewModel.OpenLogsFolder();
        }
    }
}