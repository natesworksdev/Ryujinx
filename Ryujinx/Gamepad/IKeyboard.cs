using Ryujinx.Configuration.Hid;
using System.Runtime.CompilerServices;

namespace Ryujinx.Gamepad
{
    public interface IKeyboard : IGamepad
    {
        // TODO: normal keyboard api and all
        bool IsPressed(Key key);

        KeyboardStateSnaphot GetKeyboardStateSnapshot();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyboardStateSnaphot GetStateSnapshot(IKeyboard keyboard)
        {
            bool[] keysState = new bool[(int)Key.Count];

            for (Key key = 0; key < Key.Count; key++)
            {
                keysState[(int)key] = keyboard.IsPressed(key);
            }

            return new KeyboardStateSnaphot(keysState);
        }
    }
}
