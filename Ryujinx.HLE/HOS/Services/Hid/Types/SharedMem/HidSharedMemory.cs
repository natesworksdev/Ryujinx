using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    unsafe struct HidSharedMemory
    {
        public ShMemDebugPad DebugPad;
        public ShMemTouchScreen TouchScreen;
        public ShMemMouse Mouse;
        public ShMemKeyboard Keyboard;
        fixed byte _BasicXpad[0x4 * 0x400];
        fixed byte _HomeButton[0x200];
        fixed byte _SleepButton[0x200];
        fixed byte _CaptureButton[0x200];
        fixed byte _InputDetector[0x10 * 0x80];
        fixed byte _UniquePad[0x10 * 0x400];
        public Array10<ShMemNpad> Npads;
        fixed byte _Gesture[0x800];
        fixed byte _ConsoleSixAxisSensor[0x20];
        fixed byte _padding[0x3de0];
    }
}
