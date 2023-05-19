using Avalonia.Svg.Skia;
using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Ava.UI.Views.Input;

namespace Ryujinx.Ava.UI.ViewModels.Input
{
    public class ControllerInputViewModel : BaseModel
    {
        private ControllerInputConfig _config;
        public ControllerInputConfig Config
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
                OnPropertyChanged(nameof(HasSides));
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
                OnPropertyChanged(nameof(HasSides));
            }
        }

        public bool HasSides => IsLeft ^ IsRight;

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

        public InputViewModel parentModel;

        public async void ShowMotionConfig()
        {
            await MotionInputView.Show(this);
        }

        public async void ShowRumbleConfig()
        {
            await RumbleInputView.Show(this);
        }

        public ControllerInputViewModel(InputViewModel model, ControllerInputConfig config)
        {
            parentModel = model;
            model.NotifyChangesEvent += UpdateParentModelValues;
            UpdateParentModelValues();
            Config = config;
        }

        public void UpdateParentModelValues()
        {
            IsLeft = parentModel.IsLeft;
            IsRight = parentModel.IsRight;
            Image = parentModel.Image;
        }
    }
}