using System.Runtime.InteropServices;

namespace Ryujinx.HLE.Input
{
    struct Array2<T> where T : unmanaged  { T _e0; T _e1;          public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 2)[index]; }
    struct Array3<T> where T : unmanaged  { T _e0; Array2<T> _e1;  public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 3)[index]; }
    struct Array4<T> where T : unmanaged  { T _e0; Array3<T> _e1;  public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 4)[index]; }
    struct Array5<T> where T : unmanaged  { T _e0; Array4<T> _e1;  public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 5)[index]; }
    struct Array6<T> where T : unmanaged  { T _e0; Array5<T> _e1;  public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 6)[index]; }
    struct Array7<T> where T : unmanaged  { T _e0; Array6<T> _e1;  public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 7)[index]; }
    struct Array8<T> where T : unmanaged  { T _e0; Array7<T> _e1;  public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 8)[index]; }
    struct Array9<T> where T : unmanaged  { T _e0; Array8<T> _e1;  public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 9)[index]; }
    struct Array10<T> where T : unmanaged { T _e0; Array9<T> _e1;  public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 10)[index]; }
    struct Array11<T> where T : unmanaged { T _e0; Array10<T> _e1; public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 11)[index]; }
    struct Array12<T> where T : unmanaged { T _e0; Array11<T> _e1; public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 12)[index]; }
    struct Array13<T> where T : unmanaged { T _e0; Array12<T> _e1; public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 13)[index]; }
    struct Array14<T> where T : unmanaged { T _e0; Array13<T> _e1; public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 14)[index]; }
    struct Array15<T> where T : unmanaged { T _e0; Array14<T> _e1; public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 15)[index]; }
    struct Array16<T> where T : unmanaged { T _e0; Array15<T> _e1; public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 16)[index]; }
    struct Array17<T> where T : unmanaged { T _e0; Array16<T> _e1; public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref _e0, 17)[index]; }

    struct HidBool
    {
        private uint _value;

        public static implicit operator bool(HidBool value)
        {
            return (value._value & 1) != 0;
        }

        public static implicit operator HidBool(bool value)
        {
            return new HidBool() { _value = value ? 1u : 0u };
        }
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
        public HidVector Unk;
        public Array3<HidVector> Orientation;
    }

    struct HidTouchScreenHeader
    {
        public ulong TimestampTicks;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
        public ulong Timestamp;
    }

    struct HidTouchScreenEntryHeader
    {
        public ulong Timestamp;
        public ulong NumTouches;
    }

    struct HidTouchScreenEntryTouch
    {
        public ulong Timestamp;
        public uint Padding;
        public uint TouchIndex;
        public uint X;
        public uint Y;
        public uint DiameterX;
        public uint DiameterY;
        public uint Angle;
        public uint Padding2;
    }

    struct HidTouchScreenEntry
    {
        public HidTouchScreenEntryHeader Header;
        public Array16<HidTouchScreenEntryTouch> Touches;
        public ulong Unk;
    }

    unsafe struct HidTouchScreen
    {
        public HidTouchScreenHeader Header;
        public Array17<HidTouchScreenEntry> Entries;
        public fixed byte Padding[0x3c0];
    }

    struct HidMouseHeader
    {
        public ulong TimestampTicks;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
    }

    struct HidMouseEntry
    {
        public ulong Timestamp;
        public ulong Timestamp_2;
        public MousePosition Position;
        public ulong Buttons;
    }

    unsafe struct HidMouse
    {
        public HidMouseHeader Header;
        public Array17<HidMouseEntry> Entries;
        public fixed byte Padding[0xB0];
    }

    struct HidKeyboardHeader
    {
        public ulong TimestampTicks;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
    }

    unsafe struct HidKeyboardEntry
    {
        public ulong Timestamp;
        public ulong Timestamp2;
        public ulong Modifier;
        public fixed uint Keys[8];
    }

    unsafe struct HidKeyboard
    {
        public HidKeyboardHeader Header;
        public Array17<HidKeyboardEntry> Entries;
        public fixed byte Padding[0x28];
    }

    unsafe struct HidControllerMAC
    {
        public ulong Timestamp;
        public fixed byte Mac[0x8];
        public ulong Unk;
        public ulong Timestamp2;
    }

    struct HidControllerHeader
    {
        public ControllerStatus Type;
        public HidBool IsHalf;
        public ControllerColorDescription SingleColorsDescriptor;
        public NpadColor SingleColorBody;
        public NpadColor SingleColorButtons;
        public ControllerColorDescription SplitColorsDescriptor;
        public NpadColor LeftColorBody;
        public NpadColor LeftColorButtons;
        public NpadColor RightColorBody;
        public NpadColor RightColorButtons;
    }

    struct HidControllerLayoutHeader
    {
        public ulong TimestampTicks;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
    }

    struct HidControllerInputEntry
    {
        public ulong Timestamp;
        public ulong Timestamp2;
        public ControllerButtons Buttons;
        public Array2<JoystickPosition> Joysticks;
        public ControllerConnectionState ConnectionState;
    }

    struct HidControllerLayout
    {
        public HidControllerLayoutHeader Header;
        public Array17<HidControllerInputEntry> Entries;
    }

    struct HidControllerSixAxisHeader
    {
        public ulong Timestamp;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
    }

    struct HidControllerSixAxisEntry
    {
        public ulong Timestamp;
        public ulong Unk1;
        public ulong Timestamp2;
        public SixAxisSensorValues Values;
        public ulong Unk3;
    }

    struct HidControllerSixAxisLayout
    {
        public HidControllerSixAxisHeader Header;
        public Array17<HidControllerSixAxisEntry> Entries;
    }

    unsafe struct HidControllerMisc
    {
        public ControllerDeviceType DeviceType;
        public uint Padding;
        public DeviceFlags DeviceFlags;
        public uint UnintendedHomeButtonInputProtectionEnabled;
        public BatteryState PowerInfo0BatteryState;
        public BatteryState PowerInfo1BatteryState;
        public BatteryState PowerInfo2BatteryState;
        public fixed byte Unk1[0x20];
        HidControllerMAC MacLeft;
        HidControllerMAC MacRight;
    }

    unsafe struct HidController
    {
        public HidControllerHeader Header;
        public Array7<HidControllerLayout> Layouts;
        public Array6<HidControllerSixAxisLayout> Sixaxis;
        public HidControllerMisc Misc;
        public fixed byte Unk2[0xDF8];
    }

    unsafe struct HidSharedMemory
    {
        public fixed byte Header[0x400];
        public HidTouchScreen Touchscreen;
        public HidMouse Mouse;
        public HidKeyboard Keyboard;
        public fixed byte UnkSection1[0x400];
        public fixed byte UnkSection2[0x400];
        public fixed byte UnkSection3[0x400];
        public fixed byte UnkSection4[0x400];
        public fixed byte UnkSection5[0x200];
        public fixed byte UnkSection6[0x200];
        public fixed byte UnkSection7[0x200];
        public fixed byte UnkSection8[0x800];
        public fixed byte ControllerSerials[0x4000];
        public Array10<HidController> Controllers;
        public fixed byte UnkSection9[0x4600];
    }
}
