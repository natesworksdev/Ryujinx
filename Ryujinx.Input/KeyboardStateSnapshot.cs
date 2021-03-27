using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Input
{
    // TODO: find a way to make this unmanaged to avoid increasing GC pressure.
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
