using Avalonia.Svg.Skia;
using Ryujinx.Ava.Input;
using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Ava.UI.Views.Input;
using Ryujinx.Input;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.ViewModels.Input
{
    public class ControllerInputViewModel : BaseModel
    {
        private const int DrawStickPollRate = 50; // Milliseconds per poll.
        private const int DrawStickCircumference = 5;
        private const float DrawStickScaleFactor = DrawStickCanvasCenter;

        private const int DrawStickCanvasSize = 100;
        private const int DrawStickBorderSize = DrawStickCanvasSize + 5;
        private const float DrawStickCanvasCenter = (DrawStickCanvasSize - DrawStickCircumference) / 2;

        private const float MaxVectorLength = DrawStickCanvasSize / 2;

        private IGamepad _selectedGamepad;

        private float _vectorLength;
        private float _vectorMultiplier;

        internal CancellationTokenSource _pollTokenSource = new();
        private readonly CancellationToken _pollToken;

        private GamepadInputConfig _config;
        public GamepadInputConfig Config
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

        private (float, float) _uiStickLeft;
        public (float, float) UiStickLeft
        {
            get => (_uiStickLeft.Item1 * DrawStickScaleFactor, _uiStickLeft.Item2 * DrawStickScaleFactor);
            set
            {
                _uiStickLeft = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(UiStickRightX));
                OnPropertyChanged(nameof(UiStickRightY));
                OnPropertyChanged(nameof(UiDeadzoneRight));
            }
        }

        private (float, float) _uiStickRight;
        public (float, float) UiStickRight
        {
            get => (_uiStickRight.Item1 * DrawStickScaleFactor, _uiStickRight.Item2 * DrawStickScaleFactor);
            set
            {
                _uiStickRight = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(UiStickLeftX));
                OnPropertyChanged(nameof(UiStickLeftY));
                OnPropertyChanged(nameof(UiDeadzoneLeft));
            }
        }

        public int UiStickCircumference => DrawStickCircumference;
        public int UiCanvasSize => DrawStickCanvasSize;
        public int UiStickBorderSize => DrawStickBorderSize;

        public float UiStickLeftX
        {
            get
            {
                _vectorMultiplier = 1;
                _vectorLength = GetVectorLength(UiStickLeft);

                if (_vectorLength > MaxVectorLength)
                {
                    _vectorMultiplier = MaxVectorLength / _vectorLength;
                }

                return (UiStickLeft.Item1 * _vectorMultiplier) + DrawStickCanvasCenter;
            }
        }

        public float UiStickLeftY
        {
            get
            {
                _vectorMultiplier = 1;
                _vectorLength = GetVectorLength(UiStickLeft);

                if (_vectorLength > MaxVectorLength)
                {
                    _vectorMultiplier = MaxVectorLength / _vectorLength;
                }

                return (UiStickLeft.Item2 * _vectorMultiplier) + DrawStickCanvasCenter;
            }
        }

        public float UiStickRightX
        {
            get
            {
                _vectorMultiplier = 1;
                _vectorLength = GetVectorLength(UiStickRight);

                if (_vectorLength > MaxVectorLength)
                {
                    _vectorMultiplier = MaxVectorLength / _vectorLength;
                }

                return (UiStickRight.Item1 * _vectorMultiplier) + DrawStickCanvasCenter;
            }
        }

        public float UiStickRightY
        {
            get
            {
                _vectorMultiplier = 1;
                _vectorLength = GetVectorLength(UiStickRight);

                if (_vectorLength > MaxVectorLength)
                {
                    _vectorMultiplier = MaxVectorLength / _vectorLength;
                }

                return (UiStickRight.Item2 * _vectorMultiplier) + DrawStickCanvasCenter;
            }
        }

        public float UiDeadzoneLeft => Config.DeadzoneLeft * DrawStickCanvasSize - DrawStickCircumference;
        public float UiDeadzoneRight => Config.DeadzoneRight * DrawStickCanvasSize - DrawStickCircumference;

        public readonly InputViewModel ParentModel;

        public ControllerInputViewModel(InputViewModel model, GamepadInputConfig config)
        {
            ParentModel = model;
            model.NotifyChangesEvent += OnParentModelChanged;
            OnParentModelChanged();
            Config = config;

            _pollTokenSource = new();
            _pollToken = _pollTokenSource.Token;

            Task.Run(() => PollSticks(_pollToken));
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
                    UiStickLeft = _selectedGamepad.GetStick(StickInputId.Left);
                    UiStickRight = _selectedGamepad.GetStick(StickInputId.Right);
                }

                await Task.Delay(DrawStickPollRate, token);
            }

            _pollTokenSource.Dispose();
        }

        private float GetVectorLength((float, float) raw)
        {
            return (float)Math.Sqrt((raw.Item1 * raw.Item1) + (raw.Item2 * raw.Item2));
        }

        public void OnParentModelChanged()
        {
            IsLeft = ParentModel.IsLeft;
            IsRight = ParentModel.IsRight;
            Image = ParentModel.Image;
        }
    }
}
