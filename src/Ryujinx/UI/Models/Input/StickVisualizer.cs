using Ryujinx.Ava.UI.ViewModels;
using System;
using System.Threading;

namespace Ryujinx.Ava.UI.Models.Input
{
    public class StickVisualizer : BaseModel
    {
        public const int DrawStickPollRate = 50; // Milliseconds per poll.
        public const int DrawStickCircumference = 5;
        public const float DrawStickScaleFactor = DrawStickCanvasCenter;
        public const int DrawStickCanvasSize = 100;
        public const int DrawStickBorderSize = DrawStickCanvasSize + 5;
        public const float DrawStickCanvasCenter = (DrawStickCanvasSize - DrawStickCircumference) / 2;
        public const float MaxVectorLength = DrawStickCanvasSize / 2;

        public CancellationTokenSource PollTokenSource = new();
        public CancellationToken PollToken;

        private static float _vectorLength;
        private static float _vectorMultiplier;

        private GamepadInputConfig _gamepadConfig;
        public GamepadInputConfig GamepadConfig
        {
            get => _gamepadConfig;
            set
            {
                _gamepadConfig = value;

                OnPropertyChanged();
            }
        }

        private KeyboardInputConfig _keyboardConfig;
        public KeyboardInputConfig KeyboardConfig
        {
            get => _keyboardConfig;
            set
            {
                _keyboardConfig = value;

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

        public float UiStickLeftX => ClampVector(UiStickLeft).Item1;
        public float UiStickLeftY => ClampVector(UiStickLeft).Item2;
        public float UiStickRightX => ClampVector(UiStickRight).Item1;
        public float UiStickRightY => ClampVector(UiStickRight).Item2;

        public int UiStickCircumference => DrawStickCircumference;
        public int UiCanvasSize => DrawStickCanvasSize;
        public int UiStickBorderSize => DrawStickBorderSize;

        public float? UiDeadzoneLeft => _gamepadConfig?.DeadzoneLeft * DrawStickCanvasSize - DrawStickCircumference;
        public float? UiDeadzoneRight => _gamepadConfig?.DeadzoneRight * DrawStickCanvasSize - DrawStickCircumference;

        public void UpdateConfig(object config)
        {
            if (config is GamepadInputConfig padConfig)
            {
                GamepadConfig = padConfig;

                return;
            }
            else if (config is KeyboardInputConfig keyConfig)
            {
                KeyboardConfig = keyConfig;

                return;
            }

            throw new ArgumentException($"Invalid configuration: {config}");
        }

        public static (float, float) ClampVector((float, float) vect)
        {
            _vectorMultiplier = 1;
            _vectorLength = MathF.Sqrt((vect.Item1 * vect.Item1) + (vect.Item2 * vect.Item2));

            if (_vectorLength > MaxVectorLength)
            {
                _vectorMultiplier = MaxVectorLength / _vectorLength;
            }

            vect.Item1 = vect.Item1 * _vectorMultiplier + DrawStickCanvasCenter;
            vect.Item2 = vect.Item2 * _vectorMultiplier + DrawStickCanvasCenter;

            return vect;
        }
    }
}
