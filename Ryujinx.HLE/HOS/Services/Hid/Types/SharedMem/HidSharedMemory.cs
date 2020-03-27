using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    // TODO: Add missing structs
    unsafe struct HidSharedMemory
    {
        public ShMemDebugPad DebugPad;
        public ShMemTouchScreen TouchScreen;
        public ShMemMouse Mouse;
        public ShMemKeyboard Keyboard;
        fixed byte BasicXpad[0x4 * 0x400];
        fixed byte HomeButton[0x200];
        fixed byte SleepButton[0x200];
        fixed byte CaptureButton[0x200];
        fixed byte InputDetector[0x10 * 0x80];
        fixed byte UniquePad[0x10 * 0x400];
        public Array10<ShMemNpad> Npads;
        fixed byte Gesture[0x800];
        fixed byte ConsoleSixAxisSensor[0x20];
        fixed byte padding[0x3de0];
    }
}
