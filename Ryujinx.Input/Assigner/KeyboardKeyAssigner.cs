using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Input.Assigner
{
    /// <summary>
    /// <see cref="IButtonAssigner"/> implementation for <see cref="IKeyboard"/>.
    /// </summary>
    public class KeyboardKeyAssigner : IButtonAssigner
    {
        private readonly IKeyboard _keyboard;
        private readonly bool _allowModifiers;

        private KeyboardStateSnapshot _keyboardState;

        public KeyboardKeyAssigner(IKeyboard keyboard, bool allowModifiers = false)
        {
            _keyboard = keyboard;
            _allowModifiers = allowModifiers;
        }

        public void Initialize() { }

        public void ReadInput()
        {
            _keyboardState = _keyboard.GetKeyboardStateSnapshot();
        }

        public bool HasAnyButtonPressed()
        {
            for (Key key = Key.Unknown; key < Key.Count; key++)
            {
                // If modifiers are allowed, we should wait until a non-modifier key
                // is pressed and return the full combo.
                if (_allowModifiers && IsModifierKey(key))
                {
                    continue;
                }

                if (_keyboardState.IsPressed(key))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsModifierKey(Key key)
        {
            switch (key)
            {
                case Key.AltLeft:
                case Key.AltRight:
                case Key.ControlLeft:
                case Key.ControlRight:
                case Key.ShiftLeft:
                case Key.ShiftRight:
                    return true;
            }

            return false;
        }

        public bool ShouldCancel()
        {
            return _keyboardState.IsPressed(Key.Escape);
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

        public IEnumerable<PressedButton> GetPressedButtons()
        {
            if (ShouldCancel())
            {
                return Enumerable.Empty<PressedButton>();
            }

            List<PressedButton> pressedKeys = new List<PressedButton>();

            for (Key key = Key.Unknown; key < Key.Count; key++)
            {
                if (_keyboardState.IsPressed(key))
                {
                    pressedKeys.Add(new PressedButton(key));
                }
            }

            return pressedKeys;
        }
    }
}