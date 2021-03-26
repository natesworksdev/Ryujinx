using Ryujinx.Gamepad;
namespace Ryujinx.Ui.Input
{
    class KeyboardKeyAssigner : ButtonAssigner
    {
        private IKeyboard _keyboard;

        private KeyboardStateSnapshot _keyboardState;

        public KeyboardKeyAssigner(IKeyboard keyboard)
        {
            _keyboard = keyboard;
        }

        public void Init() { }

        public void ReadInput()
        {
            _keyboardState = _keyboard.GetKeyboardStateSnapshot();
        }

        public bool HasAnyButtonPressed()
        {
            return GetPressedButton().Length != 0;
        }

        public bool ShouldCancel()
        {
            return /* Mouse.GetState().IsAnyButtonDown || */ _keyboardState.IsPressed(Key.Escape);
        }

        public string GetPressedButton()
        {
            string keyPressed = "";

            for (Key key = Key.Unknown; key < Key.Count; key++)
            {
                if (_keyboardState.IsPressed(key))
                {
                    keyPressed = key.ToString();
                    break;
                }
            }

            return !ShouldCancel() ? keyPressed : "";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _keyboard?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}