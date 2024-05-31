using Avalonia.Svg.Skia;
using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Input;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.ViewModels.Input
{
    public class KeyboardInputViewModel : BaseModel
    {
        private (float, float) _leftBuffer = (0, 0);
        private (float, float) _rightBuffer = (0, 0);
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

        private KeyboardInputConfig _config;
        public KeyboardInputConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                StickVisualizer.UpdateConfig(_config);

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

        public KeyboardInputViewModel(InputViewModel model, KeyboardInputConfig config)
        {
            ParentModel = model;
            model.NotifyChangesEvent += OnParentModelChanged;
            OnParentModelChanged();
            _stickVisualizer = new();
            Config = config;

            StickVisualizer.PollToken = StickVisualizer.PollTokenSource.Token;

            Task.Run(() => PollKeyboard(StickVisualizer.PollToken));
        }

        private async Task PollKeyboard(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (ParentModel.IsKeyboard)
                {
                    IKeyboard keyboard = (IKeyboard)ParentModel.AvaloniaKeyboardDriver.GetGamepad("0");
                    var snap = keyboard.GetKeyboardStateSnapshot();

                    if (snap.IsPressed((Key)Config.LeftStickRight))
                    {
                        _leftBuffer.Item1 += 1;
                    }
                    if (snap.IsPressed((Key)Config.LeftStickLeft))
                    {
                        _leftBuffer.Item1 -= 1;
                    }
                    if (snap.IsPressed((Key)Config.LeftStickUp))
                    {
                        _leftBuffer.Item2 += 1;
                    }
                    if (snap.IsPressed((Key)Config.LeftStickDown))
                    {
                        _leftBuffer.Item2 -= 1;
                    }

                    if (snap.IsPressed((Key)Config.RightStickRight))
                    {
                        _rightBuffer.Item1 += 1;
                    }
                    if (snap.IsPressed((Key)Config.RightStickLeft))
                    {
                        _rightBuffer.Item1 -= 1;
                    }
                    if (snap.IsPressed((Key)Config.RightStickUp))
                    {
                        _rightBuffer.Item2 += 1;
                    }
                    if (snap.IsPressed((Key)Config.RightStickDown))
                    {
                        _rightBuffer.Item2 -= 1;
                    }

                    StickVisualizer.UiStickLeft = _leftBuffer;
                    StickVisualizer.UiStickRight = _rightBuffer;
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
