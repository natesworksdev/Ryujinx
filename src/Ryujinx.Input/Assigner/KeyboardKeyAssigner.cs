namespace Ryujinx.Input.Assigner
{
    /// <summary>
    /// <see cref="IButtonAssigner"/> implementation for <see cref="IKeyboard"/>.
    /// </summary>
    public class KeyboardKeyAssigner : IButtonAssigner
    {
        private readonly IKeyboard _keyboard;

        private Key _pressedKey;
        private KeyboardStateSnapshot _keyboardState;

        public KeyboardKeyAssigner(IKeyboard keyboard)
        {
            _keyboard = keyboard;
        }

        public void Initialize()
        {
            _pressedKey = Key.Unknown;
        }

        public void ReadInput()
        {
            KeyboardStateSnapshot _newKeyboardState = _keyboard.GetKeyboardStateSnapshot();

            if (_keyboardState != null)
            {
                DetectPressedKeys(_keyboardState, _newKeyboardState);
            }

            _keyboardState = _newKeyboardState;
        }

        public bool HasAnyButtonPressed()
        {
            return _pressedKey != Key.Unknown;
        }

        public bool ShouldCancel()
        {
            return _keyboardState.IsPressed(Key.Escape);
        }

        public string GetPressedButton()
        {
            return !ShouldCancel() && HasAnyButtonPressed() ? _pressedKey.ToString() : "";
        }

        private void DetectPressedKeys(KeyboardStateSnapshot oldState, KeyboardStateSnapshot newState)
        {
            for (Key key = Key.Unknown; key < Key.Count; key++)
            {
                if (oldState.IsPressed(key) && !newState.IsPressed(key))
                {
                    _pressedKey = key;
                    break;
                }
            }
        }
    }
}