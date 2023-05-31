namespace Ryujinx.Ava.UI.ViewModels
{
    internal sealed class UserProfileImageSelectorViewModel : BaseModel
    {
        private bool _firmwareFound;

        public bool FirmwareFound
        {
            get => _firmwareFound;

            set
            {
                _firmwareFound = value;
                OnPropertyChanged();
            }
        }
    }
}