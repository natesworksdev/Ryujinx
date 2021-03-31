using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Input
{
    public class KeyboardStateSnapshot
    {
        private bool[] _keyPressed;

        public KeyboardStateSnapshot(bool[] keyPressed)
        {
            _keyPressed = keyPressed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPressed(Key key) => _keyPressed[(int)key];
    }
}
