namespace Ryujinx.Ava.UI.ViewModels
{
    public class MotionInputViewModel : BaseModel
    {
        private int _slot;
        private int _altSlot;
        private string _dsuServerHost;
        private int _dsuServerPort;
        private bool _mirrorInput;
        private bool _enableMotion;
        private int _sensitivity;
        private double _gryoDeadzone;
        private bool _enableCemuHookMotion;

        public int Slot
        {
            get => _slot;
            set
            {
                _slot = value;
                OnPropertyChanged();
            }
        }

        public int AltSlot
        {
            get => _altSlot;
            set
            {
                _altSlot = value;
                OnPropertyChanged();
            }
        }

        public string DsuServerHost
        {
            get => _dsuServerHost;
            set
            {
                _dsuServerHost = value;
                OnPropertyChanged();
            }
        }

        public int DsuServerPort
        {
            get => _dsuServerPort;
            set
            {
                _dsuServerPort = value;
                OnPropertyChanged();
            }
        }

        public bool MirrorInput
        {
            get => _mirrorInput;
            set
            {
                _mirrorInput = value;
                OnPropertyChanged();
            }
        }

        public bool EnableMotion
        {
            get => _enableMotion;
            set
            {
                _enableMotion = value;
                OnPropertyChanged();
            }
        }

        public int Sensitivity
        {
            get => _sensitivity;
            set
            {
                _sensitivity = value;
                OnPropertyChanged();
            }
        }

        public double GyroDeadzone
        {
            get => _gryoDeadzone;
            set
            {
                _gryoDeadzone = value;
                OnPropertyChanged();
            }
        }

        public bool EnableCemuHookMotion
        {
            get => _enableCemuHookMotion;
            set
            {
                _enableCemuHookMotion = value;
                OnPropertyChanged();
            }
        }
    }
}