using System.Runtime.CompilerServices;

namespace Ryujinx.Input
{
    public interface IKeyboard : IGamepad
    {
        bool IsPressed(Key key);

        KeyboardStateSnapshot GetKeyboardStateSnapshot();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyboardStateSnapshot GetStateSnapshot(IKeyboard keyboard)
        {
            bool[] keysState = new bool[(int)Key.Count];

            for (Key key = 0; key < Key.Count; key++)
            {
                keysState[(int)key] = keyboard.IsPressed(key);
            }

            return new KeyboardStateSnapshot(keysState);
        }
    }
}
