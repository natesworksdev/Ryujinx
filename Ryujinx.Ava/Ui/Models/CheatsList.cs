using Avalonia.Collections;
using DynamicData.Binding;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Ryujinx.Ava.Ui.Models
{
    public class CheatsList : ObservableCollection<CheatModel>
    {
        public CheatsList(string buildId, string path)
        {
            BuildId = buildId;
            Path = path;
            this.CollectionChanged += CheatsList_CollectionChanged;
        }

        private void CheatsList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                (e.NewItems[0] as CheatModel).EnableToggled += Item_EnableToggled;
            }
        }

        private void Item_EnableToggled(object sender, bool e)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsEnabled)));
        }

        public string BuildId { get; }
        public string Path { get; }

        public bool IsEnabled
        {
            get
            {
                return this.ToList().TrueForAll(x => x.IsEnabled);
            }
            set
            {
                foreach (var cheat in this)
                {
                    cheat.IsEnabled = value;
                }
                
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsEnabled)));
            }
        }
    }
}