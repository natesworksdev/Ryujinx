using System.Runtime.CompilerServices;

namespace Ryujinx.Gamepad
{
    // TODO: find a way to make this unmanaged to avoid increasing GC pressure.
    public class KeyboardStateSnaphot
    {
        private bool[] _keyPressed;

        public KeyboardStateSnaphot(bool[] keyPressed)
        {
            _keyPressed = keyPressed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPressed(Key key) => _keyPressed[(int)key];
    }
}
