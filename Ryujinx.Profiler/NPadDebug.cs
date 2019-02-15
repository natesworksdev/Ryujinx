using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Input;

namespace Ryujinx.Profiler
{
    public struct NpadDebugButtons
    {
        public Key ToggleProfiler;
    }

    public class NpadDebug
    {
        public NpadDebugButtons Buttons;

        private KeyboardState _prevKeyboard;

        public NpadDebug(NpadDebugButtons buttons)
        {
            Buttons = buttons;
        }

        public bool TogglePressed(KeyboardState keyboard) => !keyboard[Buttons.ToggleProfiler] && _prevKeyboard[Buttons.ToggleProfiler];

        public void SetPrevKeyboardState(KeyboardState keyboard)
        {
            _prevKeyboard = keyboard;
        }
    }
}
