using OpenTK.Input;
using Ryujinx.HLE.Input;
using Ryujinx.Common.Logging;

namespace Ryujinx.UI.Input
{
    public struct NpadKeyboardLeft
    {
        public Key StickUp;
        public Key StickDown;
        public Key StickLeft;
        public Key StickRight;
        public Key StickButton;
        public Key DPadUp;
        public Key DPadDown;
        public Key DPadLeft;
        public Key DPadRight;
        public Key ButtonMinus;
        public Key ButtonL;
        public Key ButtonZl;
    }

    public struct NpadKeyboardRight
    {
        public Key StickUp;
        public Key StickDown;
        public Key StickLeft;
        public Key StickRight;
        public Key StickButton;
        public Key ButtonA;
        public Key ButtonB;
        public Key ButtonX;
        public Key ButtonY;
        public Key ButtonPlus;
        public Key ButtonR;
        public Key ButtonZr;
    }

    public struct KeyboardHotkeys
    {
        public Key ToggleVsync;
    }

    public class NpadKeyboard
    {
        /// <summary>
        /// Left JoyCon Keyboard Bindings
        /// </summary>
        public NpadKeyboardLeft LeftJoycon { get; private set; }

        /// <summary>
        /// Right JoyCon Keyboard Bindings
        /// </summary>
        public NpadKeyboardRight RightJoycon { get; private set; }

        /// <summary>
        /// Hotkey Keyboard Bindings
        /// </summary>
        public KeyboardHotkeys Hotkeys { get; private set; }

        public HidControllerButtons GetButtons(KeyboardState keyboard)
        {
            HidControllerButtons buttons = 0;

            if (keyboard[(Key)LeftJoycon.StickButton]) buttons |= HidControllerButtons.StickLeft;
            if (keyboard[(Key)LeftJoycon.DPadUp])      buttons |= HidControllerButtons.DpadUp;
            if (keyboard[(Key)LeftJoycon.DPadDown])    buttons |= HidControllerButtons.DpadDown;
            if (keyboard[(Key)LeftJoycon.DPadLeft])    buttons |= HidControllerButtons.DpadLeft;
            if (keyboard[(Key)LeftJoycon.DPadRight])   buttons |= HidControllerButtons.DPadRight;
            if (keyboard[(Key)LeftJoycon.ButtonMinus]) buttons |= HidControllerButtons.Minus;
            if (keyboard[(Key)LeftJoycon.ButtonL])     buttons |= HidControllerButtons.L;
            if (keyboard[(Key)LeftJoycon.ButtonZl])    buttons |= HidControllerButtons.Zl;
            
            if (keyboard[(Key)RightJoycon.StickButton]) buttons |= HidControllerButtons.StickRight;
            if (keyboard[(Key)RightJoycon.ButtonA])     buttons |= HidControllerButtons.A;
            if (keyboard[(Key)RightJoycon.ButtonB])     buttons |= HidControllerButtons.B;
            if (keyboard[(Key)RightJoycon.ButtonX])     buttons |= HidControllerButtons.X;
            if (keyboard[(Key)RightJoycon.ButtonY])     buttons |= HidControllerButtons.Y;
            if (keyboard[(Key)RightJoycon.ButtonPlus])  buttons |= HidControllerButtons.Plus;
            if (keyboard[(Key)RightJoycon.ButtonR])     buttons |= HidControllerButtons.R;
            if (keyboard[(Key)RightJoycon.ButtonZr])    buttons |= HidControllerButtons.Zr;

            return buttons;
        }

        public (short, short) GetLeftStick(KeyboardState keyboard)
        {
            short dx = 0;
            short dy = 0;
            
            if (keyboard[(Key)LeftJoycon.StickUp])    dy =  short.MaxValue;
            if (keyboard[(Key)LeftJoycon.StickDown])  dy = -short.MaxValue;
            if (keyboard[(Key)LeftJoycon.StickLeft])  dx = -short.MaxValue;
            if (keyboard[(Key)LeftJoycon.StickRight]) dx =  short.MaxValue;

            return (dx, dy);
        }

        public (short, short) GetRightStick(KeyboardState keyboard)
        {
            short dx = 0;
            short dy = 0;

            if (keyboard[(Key)RightJoycon.StickUp])    dy =  short.MaxValue;
            if (keyboard[(Key)RightJoycon.StickDown])  dy = -short.MaxValue;
            if (keyboard[(Key)RightJoycon.StickLeft])  dx = -short.MaxValue;
            if (keyboard[(Key)RightJoycon.StickRight]) dx =  short.MaxValue;

            return (dx, dy);
        }

        public HidHotkeyButtons GetHotkeyButtons(KeyboardState keyboard)
        {
            HidHotkeyButtons buttons = 0;

            if (keyboard[(Key)Hotkeys.ToggleVsync]) buttons |= HidHotkeyButtons.ToggleVSync;

            return buttons;
        }

        class KeyMapping
        {
            public Key TargetKey;
            public byte Target;
        }

        static KeyMapping[] KEY_MAPPING = new KeyMapping[]
        {
            new KeyMapping { TargetKey = Key.A, Target = 0x4  },
            new KeyMapping { TargetKey = Key.B, Target = 0x5  },
            new KeyMapping { TargetKey = Key.C, Target = 0x6  },
            new KeyMapping { TargetKey = Key.D, Target = 0x7  },
            new KeyMapping { TargetKey = Key.E, Target = 0x8  },
            new KeyMapping { TargetKey = Key.F, Target = 0x9  },
            new KeyMapping { TargetKey = Key.G, Target = 0xA  },
            new KeyMapping { TargetKey = Key.H, Target = 0xB  },
            new KeyMapping { TargetKey = Key.I, Target = 0xC  },
            new KeyMapping { TargetKey = Key.J, Target = 0xD  },
            new KeyMapping { TargetKey = Key.K, Target = 0xE  },
            new KeyMapping { TargetKey = Key.L, Target = 0xF  },
            new KeyMapping { TargetKey = Key.M, Target = 0x10 },
            new KeyMapping { TargetKey = Key.N, Target = 0x11 },
            new KeyMapping { TargetKey = Key.O, Target = 0x12 },
            new KeyMapping { TargetKey = Key.P, Target = 0x13 },
            new KeyMapping { TargetKey = Key.Q, Target = 0x14 },
            new KeyMapping { TargetKey = Key.R, Target = 0x15 },
            new KeyMapping { TargetKey = Key.S, Target = 0x16 },
            new KeyMapping { TargetKey = Key.T, Target = 0x17 },
            new KeyMapping { TargetKey = Key.U, Target = 0x18 },
            new KeyMapping { TargetKey = Key.V, Target = 0x19 },
            new KeyMapping { TargetKey = Key.W, Target = 0x1A },
            new KeyMapping { TargetKey = Key.X, Target = 0x1B },
            new KeyMapping { TargetKey = Key.Y, Target = 0x1C },
            new KeyMapping { TargetKey = Key.Z, Target = 0x1D },

            new KeyMapping { TargetKey = Key.Number1, Target = 0x1E },
            new KeyMapping { TargetKey = Key.Number2, Target = 0x1F },
            new KeyMapping { TargetKey = Key.Number3, Target = 0x20 },
            new KeyMapping { TargetKey = Key.Number4, Target = 0x21 },
            new KeyMapping { TargetKey = Key.Number5, Target = 0x22 },
            new KeyMapping { TargetKey = Key.Number6, Target = 0x23 },
            new KeyMapping { TargetKey = Key.Number7, Target = 0x24 },
            new KeyMapping { TargetKey = Key.Number8, Target = 0x25 },
            new KeyMapping { TargetKey = Key.Number9, Target = 0x26 },
            new KeyMapping { TargetKey = Key.Number0, Target = 0x27 },

            new KeyMapping { TargetKey = Key.Enter,        Target = 0x28 },
            new KeyMapping { TargetKey = Key.Escape,       Target = 0x29 },
            new KeyMapping { TargetKey = Key.BackSpace,    Target = 0x2A },
            new KeyMapping { TargetKey = Key.Tab,          Target = 0x2B },
            new KeyMapping { TargetKey = Key.Space,        Target = 0x2C },
            new KeyMapping { TargetKey = Key.Minus,        Target = 0x2D },
            new KeyMapping { TargetKey = Key.Plus,         Target = 0x2E },
            new KeyMapping { TargetKey = Key.BracketLeft,  Target = 0x2F },
            new KeyMapping { TargetKey = Key.BracketRight, Target = 0x30 },
            new KeyMapping { TargetKey = Key.BackSlash,    Target = 0x31 },
            new KeyMapping { TargetKey = Key.Tilde,        Target = 0x32 },
            new KeyMapping { TargetKey = Key.Semicolon,    Target = 0x33 },
            new KeyMapping { TargetKey = Key.Quote,        Target = 0x34 },
            new KeyMapping { TargetKey = Key.Grave,        Target = 0x35 },
            new KeyMapping { TargetKey = Key.Comma,        Target = 0x36 },
            new KeyMapping { TargetKey = Key.Period,       Target = 0x37 },
            new KeyMapping { TargetKey = Key.Slash,        Target = 0x38 },
            new KeyMapping { TargetKey = Key.CapsLock,     Target = 0x39 },

            new KeyMapping { TargetKey = Key.F1,  Target = 0x3a },
            new KeyMapping { TargetKey = Key.F2,  Target = 0x3b },
            new KeyMapping { TargetKey = Key.F3,  Target = 0x3c },
            new KeyMapping { TargetKey = Key.F4,  Target = 0x3d },
            new KeyMapping { TargetKey = Key.F5,  Target = 0x3e },
            new KeyMapping { TargetKey = Key.F6,  Target = 0x3f },
            new KeyMapping { TargetKey = Key.F7,  Target = 0x40 },
            new KeyMapping { TargetKey = Key.F8,  Target = 0x41 },
            new KeyMapping { TargetKey = Key.F9,  Target = 0x42 },
            new KeyMapping { TargetKey = Key.F10, Target = 0x43 },
            new KeyMapping { TargetKey = Key.F11, Target = 0x44 },
            new KeyMapping { TargetKey = Key.F12, Target = 0x45 },

            new KeyMapping { TargetKey = Key.PrintScreen, Target = 0x46 },
            new KeyMapping { TargetKey = Key.ScrollLock,  Target = 0x47 },
            new KeyMapping { TargetKey = Key.Pause,       Target = 0x48 },
            new KeyMapping { TargetKey = Key.Insert,      Target = 0x49 },
            new KeyMapping { TargetKey = Key.Home,        Target = 0x4A },
            new KeyMapping { TargetKey = Key.PageUp,      Target = 0x4B },
            new KeyMapping { TargetKey = Key.Delete,      Target = 0x4C },
            new KeyMapping { TargetKey = Key.End,         Target = 0x4D },
            new KeyMapping { TargetKey = Key.PageDown,    Target = 0x4E },
            new KeyMapping { TargetKey = Key.Right,       Target = 0x4F },
            new KeyMapping { TargetKey = Key.Left,        Target = 0x50 },
            new KeyMapping { TargetKey = Key.Down,        Target = 0x51 },
            new KeyMapping { TargetKey = Key.Up,          Target = 0x52 },

            new KeyMapping { TargetKey = Key.NumLock,        Target = 0x53 },
            new KeyMapping { TargetKey = Key.KeypadDivide,   Target = 0x54 },
            new KeyMapping { TargetKey = Key.KeypadMultiply, Target = 0x55 },
            new KeyMapping { TargetKey = Key.KeypadMinus,    Target = 0x56 },
            new KeyMapping { TargetKey = Key.KeypadPlus,     Target = 0x57 },
            new KeyMapping { TargetKey = Key.KeypadEnter,    Target = 0x58 },
            new KeyMapping { TargetKey = Key.Keypad1,        Target = 0x59 },
            new KeyMapping { TargetKey = Key.Keypad2,        Target = 0x5A },
            new KeyMapping { TargetKey = Key.Keypad3,        Target = 0x5B },
            new KeyMapping { TargetKey = Key.Keypad4,        Target = 0x5C },
            new KeyMapping { TargetKey = Key.Keypad5,        Target = 0x5D },
            new KeyMapping { TargetKey = Key.Keypad6,        Target = 0x5E },
            new KeyMapping { TargetKey = Key.Keypad7,        Target = 0x5F },
            new KeyMapping { TargetKey = Key.Keypad8,        Target = 0x60 },
            new KeyMapping { TargetKey = Key.Keypad9,        Target = 0x61 },
            new KeyMapping { TargetKey = Key.Keypad0,        Target = 0x62 },
            new KeyMapping { TargetKey = Key.KeypadPeriod,   Target = 0x63 },

            new KeyMapping { TargetKey = Key.NonUSBackSlash, Target = 0x64 },

            new KeyMapping { TargetKey = Key.F13, Target = 0x68 },
            new KeyMapping { TargetKey = Key.F14, Target = 0x69 },
            new KeyMapping { TargetKey = Key.F15, Target = 0x6A },
            new KeyMapping { TargetKey = Key.F16, Target = 0x6B },
            new KeyMapping { TargetKey = Key.F17, Target = 0x6C },
            new KeyMapping { TargetKey = Key.F18, Target = 0x6D },
            new KeyMapping { TargetKey = Key.F19, Target = 0x6E },
            new KeyMapping { TargetKey = Key.F20, Target = 0x6F },
            new KeyMapping { TargetKey = Key.F21, Target = 0x70 },
            new KeyMapping { TargetKey = Key.F22, Target = 0x71 },
            new KeyMapping { TargetKey = Key.F23, Target = 0x72 },
            new KeyMapping { TargetKey = Key.F24, Target = 0x73 },

            new KeyMapping { TargetKey = Key.ControlLeft,  Target = 0xE0 },
            new KeyMapping { TargetKey = Key.ShiftLeft,    Target = 0xE1 },
            new KeyMapping { TargetKey = Key.AltLeft,      Target = 0xE2 },
            new KeyMapping { TargetKey = Key.WinLeft,      Target = 0xE3 },
            new KeyMapping { TargetKey = Key.ControlRight, Target = 0xE4 },
            new KeyMapping { TargetKey = Key.ShiftRight,   Target = 0xE5 },
            new KeyMapping { TargetKey = Key.AltRight,     Target = 0xE6 },
            new KeyMapping { TargetKey = Key.WinRight,     Target = 0xE7 },
        };

        static KeyMapping[] KEY_MODIFIER_MAPPING = new KeyMapping[]
        {
            new KeyMapping { TargetKey = Key.ControlLeft,  Target = 0 },
            new KeyMapping { TargetKey = Key.ShiftLeft,    Target = 1 },
            new KeyMapping { TargetKey = Key.AltLeft,      Target = 2 },
            new KeyMapping { TargetKey = Key.WinLeft,      Target = 3 },
            new KeyMapping { TargetKey = Key.ControlRight, Target = 4 },
            new KeyMapping { TargetKey = Key.ShiftRight,   Target = 5 },
            new KeyMapping { TargetKey = Key.AltRight,     Target = 6 },
            new KeyMapping { TargetKey = Key.WinRight,     Target = 7 },
            new KeyMapping { TargetKey = Key.CapsLock,     Target = 8 },
            new KeyMapping { TargetKey = Key.ScrollLock,   Target = 9 },
            new KeyMapping { TargetKey = Key.NumLock,      Target = 10 },
        };

        public HidKeyboard GetKeysDown(KeyboardState keyboard)
        {
            HidKeyboard hidKeyboard = new HidKeyboard
            {
                    Modifier = 0,
                    Keys = new int[0x8]
            };

            foreach (KeyMapping keyMapping in KEY_MAPPING)
            {
                int value = keyboard[keyMapping.TargetKey] ? 1 : 0;

                hidKeyboard.Keys[keyMapping.Target / 0x20] |= (value << (keyMapping.Target % 0x20));
            }

            foreach (KeyMapping keyMapping in KEY_MODIFIER_MAPPING)
            {
                int value = keyboard[keyMapping.TargetKey] ? 1 : 0;

                hidKeyboard.Modifier |= value << keyMapping.Target;
            }

            return hidKeyboard;
        }
    }
}
