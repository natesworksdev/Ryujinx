using Ryujinx.Common.Configuration.Hid;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class CycleController : BaseModel
    {
        private string _player;
        private Key _hotkey;

        public string Player
        {
            get => _player;
            set
            {
                _player = value;
                OnPropertyChanged(nameof(Player));
            }
        }

        public Key Hotkey
        {
            get => _hotkey;
            set
            {
                _hotkey = value;
                OnPropertyChanged(nameof(Hotkey));
            }
        }

        public CycleController(int v, Key x)
        {
            Player = $"Player {v}";
            Hotkey = x;
        }
    }
}
