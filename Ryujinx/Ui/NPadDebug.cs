using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Input;

namespace Ryujinx.Ui
{
    public struct NPadDebugButtons
    {
        public int ToggleProfiler;
    }

    public class NPadDebug
    {
        public NPadDebugButtons Buttons;
        private KeyboardState prevKeyboard;

        public NPadDebug(NPadDebugButtons buttons)
        {
            Buttons = buttons;
        }

        public bool TogglePressed(KeyboardState keyboard) => !keyboard[(Key) Buttons.ToggleProfiler] && prevKeyboard[(Key) Buttons.ToggleProfiler];

        public void SetPrevKeyboardState(KeyboardState keyboard)
        {
            prevKeyboard = keyboard;
        }
    }
}
