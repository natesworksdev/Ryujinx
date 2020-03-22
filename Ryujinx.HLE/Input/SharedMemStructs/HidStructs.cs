using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.Input
{

    /*
     * Reference:
     * https://github.com/switchbrew/libnx/blob/master/nx/source/services/hid.c
     * https://github.com/switchbrew/libnx/blob/master/nx/include/switch/services/hid.h
     * 
     * Some fields renamed to be more contextual
    */

    struct HidBool
    {
        private uint _value;
        public static implicit operator bool(HidBool value) => (value._value & 1) != 0;
        public static implicit operator HidBool(bool value) => new HidBool() { _value = value ? 1u : 0u };
    }

    struct HidCommonEntriesHeader
    {
        public ulong TimestampTicks;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
    }

    struct MousePosition
    {
        public int X;
        public int Y;
        public int VelocityX;
        public int VelocityY;
        public int ScrollVelocityX;
        public int ScrollVelocityY;
    }

    struct HidVector
    {
        public float X;
        public float Y;
        public float Z;
    }

    struct SixAxisSensorValues
    {
        public HidVector Accelerometer;
        public HidVector Gyroscope;
        public HidVector _Unk;
        public Array3<HidVector> Orientation;
    }

    struct HidTouchScreenEntryHeader
    {
        public ulong SequenceNumber;
        public ulong NumTouches;
    }

    struct HidTouchScreenEntryTouch
    {
        public ulong SequenceNumber;
        public uint _Padding;
        public uint TouchIndex;
        public uint X;
        public uint Y;
        public uint DiameterX;
        public uint DiameterY;
        public uint Angle;
        public uint _Padding2;
    }

    struct HidTouchScreenEntry
    {
        public HidTouchScreenEntryHeader Header;
        public Array16<HidTouchScreenEntryTouch> Touches;
        public ulong _Unk;
    }

    // HidTouchScreen's header has an extra field compared to other entry fields.
    // So, this struct was modified from libnx a bit to reuse headers.
    unsafe struct HidTouchScreen
    {
        public HidCommonEntriesHeader Header;
        public ulong SequenceNumber;
        public Array17<HidTouchScreenEntry> Entries;
        public fixed byte _Padding[0x3c0];
    }


    struct HidMouseEntry
    {
        public ulong SequenceNumber;
        public ulong SequenceNumber2;
        public MousePosition Position;
        public ulong Buttons;
    }

    unsafe struct HidMouse
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidMouseEntry> Entries;
        public fixed byte _Padding[0xB0];
    }

    unsafe struct HidKeyboardEntry
    {
        public ulong SequenceNumber;
        public ulong SequenceNumber2;
        public ulong Modifier;
        public fixed uint Keys[8];
    }

    unsafe struct HidKeyboard
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidKeyboardEntry> Entries;
        public fixed byte _Padding[0x28];
    }

    unsafe struct HidControllerMAC
    {
        public ulong Timestamp;
        public fixed byte Mac[0x8];
        public ulong _Unk;
        public ulong Timestamp2;
    }

    struct HidControllerHeader
    {
        public ControllerType Type;
        public HidBool IsHalf;
        public HidControllerColorDescription SingleColorsDescriptor;
        public NpadColor SingleColorBody;
        public NpadColor SingleColorButtons;
        public HidControllerColorDescription SplitColorsDescriptor;
        public NpadColor LeftColorBody;
        public NpadColor LeftColorButtons;
        public NpadColor RightColorBody;
        public NpadColor RightColorButtons;
    }

    public struct JoystickPosition
    {
        public int Dx;
        public int Dy;
    }

    struct HidControllerInputEntry
    {
        public ulong SequenceNumber;
        public ulong SequenceNumber2;
        public ControllerKeys Buttons;
        public Array2<JoystickPosition> Joysticks;
        public HidControllerConnectionState ConnectionState;
    }

    struct HidControllerLayout
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidControllerInputEntry> Entries;
    }

    struct HidControllerSixAxisEntry
    {
        public ulong SequenceNumber;
        public ulong _Unk1;
        public ulong SequenceNumber2;
        public SixAxisSensorValues Values;
        public ulong _Unk3;
    }

    struct HidControllerSixAxisLayout
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidControllerSixAxisEntry> Entries;
    }

    unsafe struct HidControllerMisc
    {
        public DeviceType DeviceType;
        public uint _Padding;
        public DeviceFlags DeviceFlags;
        public uint UnintendedHomeButtonInputProtectionEnabled;
        public Array3<BatteryCharge> BatteryCharge;
        public fixed byte _Unk1[0x20];
        HidControllerMAC MacLeft;
        HidControllerMAC MacRight;
    }

    unsafe struct HidController
    {
        public HidControllerHeader Header;
        public Array7<HidControllerLayout> Layouts;         // One for each ControllerLayoutType?
        public Array6<HidControllerSixAxisLayout> Sixaxis;  // Unknown layout mapping
        public HidControllerMisc Misc;
        public fixed byte _Unk2[0xDF8];
    }

    unsafe struct HidDebugPadEntry
    {
        public ulong SequenceNumber;
        public fixed byte _Unk[0x20];
    }

    struct HidDebugPad
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidDebugPadEntry> Entries;
    }

    unsafe struct HidSharedMemory
    {
        public HidDebugPad DebugPad;
        public fixed byte _Pad[0x138];
        public HidTouchScreen Touchscreen;
        public HidMouse Mouse;
        public HidKeyboard Keyboard;
        public fixed byte _UnkSection1[0x400];
        public fixed byte _UnkSection2[0x400];
        public fixed byte _UnkSection3[0x400];
        public fixed byte _UnkSection4[0x400];
        public fixed byte _UnkSection5[0x200];
        public fixed byte _UnkSection6[0x200];
        public fixed byte _UnkSection7[0x200];
        public fixed byte _UnkSection8[0x800];
        public fixed byte ControllerSerials[0x4000];
        public Array10<HidController> Controllers;
        public fixed byte _UnkSection9[0x4600];
    }
}
