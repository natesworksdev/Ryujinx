using Avalonia.Svg.Skia;
using Ryujinx.Ava.UI.Models;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class KeyboardInputViewModel : BaseModel
    {
        private KeyboardInputConfig _config;
        private bool _isLeft;
        private bool _isRight;
        private bool _showSettings;
        private SvgImage _image;

        public KeyboardInputConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                OnPropertyChanged();
            }
        }

        public bool IsLeft
        {
            get => _isLeft;
            set
            {
                _isLeft = value;
                OnPropertyChanged();
            }
        }

        public bool IsRight
        {
            get => _isRight;
            set
            {
                _isRight = value;
                OnPropertyChanged();
            }
        }

        public bool ShowSettings
        {
            get => _showSettings;
            set
            {
                _showSettings = value;
                OnPropertyChanged();
            }
        }

        public SvgImage Image
        {
            get => _image;
            set
            {
                _image = value;
                OnPropertyChanged();
            }
        }
    }
}