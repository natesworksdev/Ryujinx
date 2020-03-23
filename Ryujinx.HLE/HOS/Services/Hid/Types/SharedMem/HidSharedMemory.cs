using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    public unsafe struct HidSharedMemory
    {
        public HidDebugPad DebugPad;
        public HidTouchScreen Touchscreen;
        public HidMouse Mouse;
        public HidKeyboard Keyboard;
        public fixed byte _BasicXpad[0x4 * 0x400];
        public fixed byte _HomeButton[0x200];
        public fixed byte _SleepButton[0x200];
        public fixed byte _CaptureButton[0x200];
        public fixed byte _InputDetector[0x10 * 0x80];
        public fixed byte _UniquePad[0x10 * 0x400];
        public Array10<HidController> Controllers;
        public fixed byte _Gesture[0x800];
        public fixed byte _ConsoleSixAxisSensor[0x20];
        public fixed byte _Padding[0x3de0];
    }
}
