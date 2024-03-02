namespace Ryujinx.Input.Assigner
{
    /// <summary>
    /// <see cref="IButtonAssigner"/> implementation for <see cref="IKeyboard"/>.
    /// </summary>
    public class KeyboardKeyAssigner : IButtonAssigner
    {
        private readonly IKeyboard _keyboard;
        private ButtonValue? _pressedKey;
        private KeyboardStateSnapshot _keyboardState;

        public KeyboardKeyAssigner(IKeyboard keyboard)
        {
            _keyboard = keyboard;
        }

        public void Initialize()
        {
            _pressedKey = null;
        }

        public void ReadInput()
        {
            var newKeyboardState = _keyboard.GetKeyboardStateSnapshot();

            if (_keyboardState != null)
            {
                DetectPressedKeys(_keyboardState, newKeyboardState);
            }

            _keyboardState = newKeyboardState;
        }

        public bool HasAnyButtonPressed()
        {
            return _pressedKey != null;
        }

        public bool ShouldCancel()
        {
            return _keyboardState?.IsPressed(Key.Escape) == true;
        }

        public ButtonValue? GetPressedButton()
        {
            return !ShouldCancel() && HasAnyButtonPressed() ? _pressedKey : null;
        }

        public void DetectPressedKeys(KeyboardStateSnapshot oldState, KeyboardStateSnapshot newState)
        {
            for (Key key = Key.Unknown; key < Key.Count; key++)
            {
                if (oldState.IsPressed(key) && !newState.IsPressed(key))
                {
                    _pressedKey = new(key);
                    break;
                }
            }
        }
    }
}
