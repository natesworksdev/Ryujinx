using Avalonia.Svg.Skia;
using Ryujinx.Ava.UI.Models.Input;

namespace Ryujinx.Ava.UI.ViewModels.Input
{
    public class KeyboardInputViewModel : BaseModel
    {
        private KeyboardInputConfig _config;
        public KeyboardInputConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                OnPropertyChanged();
            }
        }

        private bool _isLeft;
        public bool IsLeft
        {
            get => _isLeft;
            set
            {
                _isLeft = value;
                OnPropertyChanged();
            }
        }

        private bool _isRight;
        public bool IsRight
        {
            get => _isRight;
            set
            {
                _isRight = value;
                OnPropertyChanged();
            }
        }

        private SvgImage _image;
        public SvgImage Image
        {
            get => _image;
            set
            {
                _image = value;
                OnPropertyChanged();
            }
        }

        public KeyboardInputViewModel(InputViewModel model, KeyboardInputConfig config)
        {
            IsLeft = model.IsLeft;
            IsRight = model.IsRight;
            Image = model.Image;
            Config = config;
        }
    }
}