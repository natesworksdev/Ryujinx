using Avalonia.Svg.Skia;
using Ryujinx.Ava.Input;
using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Ava.UI.Views.Input;
using Ryujinx.Input;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.ViewModels.Input
{
    public class ControllerInputViewModel : BaseModel
    {
        private IGamepad _selectedGamepad;

        private StickVisualizer _stickVisualizer;
        public StickVisualizer StickVisualizer
        {
            get => _stickVisualizer;
            set
            {
                _stickVisualizer = value;

                OnPropertyChanged();
            }
        }

        private GamepadInputConfig _config;
        public GamepadInputConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                StickVisualizer.UpdateConfig(Config);

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

        public readonly InputViewModel ParentModel;

        public ControllerInputViewModel(InputViewModel model, GamepadInputConfig config)
        {
            ParentModel = model;
            model.NotifyChangesEvent += OnParentModelChanged;
            OnParentModelChanged();
            _stickVisualizer = new();
            Config = config;

            StickVisualizer.PollToken = StickVisualizer.PollTokenSource.Token;

            Task.Run(() => PollSticks(StickVisualizer.PollToken));
        }

        public async void ShowMotionConfig()
        {
            await MotionInputView.Show(this);
        }

        public async void ShowRumbleConfig()
        {
            await RumbleInputView.Show(this);
        }

        private async Task PollSticks(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _selectedGamepad = ParentModel.SelectedGamepad;

                if (_selectedGamepad != null && _selectedGamepad is not AvaloniaKeyboard)
                {
                    StickVisualizer.UiStickLeft = _selectedGamepad.GetStick(StickInputId.Left);
                    StickVisualizer.UiStickRight = _selectedGamepad.GetStick(StickInputId.Right);
                }

                await Task.Delay(StickVisualizer.DrawStickPollRate, token);
            }

            StickVisualizer.PollTokenSource.Dispose();
        }

        public void OnParentModelChanged()
        {
            IsLeft = ParentModel.IsLeft;
            IsRight = ParentModel.IsRight;
            Image = ParentModel.Image;
        }
    }
}
