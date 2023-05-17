namespace Ryujinx.Ava.UI.ViewModels
{
    public class RumbleInputViewModel : BaseModel
    {
        private float _strongRumble;
        private float _weakRumble;

        public float StrongRumble
        {
            get => _strongRumble;
            set
            {
                _strongRumble = value;
                OnPropertyChanged();
            }
        }

        public float WeakRumble
        {
            get => _weakRumble;
            set
            {
                _weakRumble = value;
                OnPropertyChanged();
            }
        }
    }
}