using System.Runtime.InteropServices;
 
namespace Ryujinx
{
    public enum HidControllerKeys
    {
        KEY_A = (1 << 0),       //< A
        KEY_B = (1 << 1),       //< B
        KEY_X = (1 << 2),       //< X
        KEY_Y = (1 << 3),       //< Y
        KEY_LSTICK = (1 << 4),       //< Left Stick Button
        KEY_RSTICK = (1 << 5),       //< Right Stick Button
        KEY_L = (1 << 6),       //< L
        KEY_R = (1 << 7),       //< R
        KEY_ZL = (1 << 8),       //< ZL
        KEY_ZR = (1 << 9),       //< ZR
        KEY_PLUS = (1 << 10),      //< Plus
        KEY_MINUS = (1 << 11),      //< Minus
        KEY_DLEFT = (1 << 12),      //< D-Pad Left
        KEY_DUP = (1 << 13),      //< D-Pad Up
        KEY_DRIGHT = (1 << 14),      //< D-Pad Right
        KEY_DDOWN = (1 << 15),      //< D-Pad Down
        KEY_LSTICK_LEFT = (1 << 16),      //< Left Stick Left
        KEY_LSTICK_UP = (1 << 17),      //< Left Stick Up
        KEY_LSTICK_RIGHT = (1 << 18),      //< Left Stick Right
        KEY_LSTICK_DOWN = (1 << 19),      //< Left Stick Down
        KEY_RSTICK_LEFT = (1 << 20),      //< Right Stick Left
        KEY_RSTICK_UP = (1 << 21),      //< Right Stick Up
        KEY_RSTICK_RIGHT = (1 << 22),      //< Right Stick Right
        KEY_RSTICK_DOWN = (1 << 23),      //< Right Stick Down
        KEY_SL = (1 << 24),      //< SL
        KEY_SR = (1 << 25),      //< SR

        // Pseudo-key for at least one finger on the touch screen
        KEY_TOUCH = (1 << 26),

        // Buttons by orientation (for single Joy-Con), also works with Joy-Con pairs, Pro Controller
        KEY_JOYCON_RIGHT = (1 << 0),
        KEY_JOYCON_DOWN = (1 << 1),
        KEY_JOYCON_UP = (1 << 2),
        KEY_JOYCON_LEFT = (1 << 3),

        // Generic catch-all directions, also works for single Joy-Con
        KEY_UP = KEY_DUP | KEY_LSTICK_UP | KEY_RSTICK_UP,    //< D-Pad Up or Sticks Up
        KEY_DOWN = KEY_DDOWN | KEY_LSTICK_DOWN | KEY_RSTICK_DOWN,  //< D-Pad Down or Sticks Down
        KEY_LEFT = KEY_DLEFT | KEY_LSTICK_LEFT | KEY_RSTICK_LEFT,  //< D-Pad Left or Sticks Left
        KEY_RIGHT = KEY_DRIGHT | KEY_LSTICK_RIGHT | KEY_RSTICK_RIGHT, //< D-Pad Right or Sticks Right
    }

    public enum HidControllerID
    {
        CONTROLLER_PLAYER_1 = 0,
        CONTROLLER_PLAYER_2 = 1,
        CONTROLLER_PLAYER_3 = 2,
        CONTROLLER_PLAYER_4 = 3,
        CONTROLLER_PLAYER_5 = 4,
        CONTROLLER_PLAYER_6 = 5,
        CONTROLLER_PLAYER_7 = 6,
        CONTROLLER_PLAYER_8 = 7,
        CONTROLLER_HANDHELD = 8,
        CONTROLLER_UNKNOWN = 9,
        CONTROLLER_P1_AUTO = 10, //Not an actual HID-sysmodule ID. Only for hidKeys*(). Automatically uses CONTROLLER_PLAYER_1 when connected, otherwise uses CONTROLLER_HANDHELD.
    }

    public enum HidControllerJoystick
    {
        Joystick_Left = 0,
        Joystick_Right = 1,
        Joystick_Num_Sticks = 2
    }

    public enum HidControllerLayouts
    {
        Pro_Controller,
        Handheld_Joined,
        Joined,
        Left,
        Right,
        Main_No_Analog,
        Main
    }

    public enum HidControllerConnectionState
    {
        Controller_State_Connected = 1 << 0,
        Controller_State_Wired = 1 << 1
    }

    public enum HidControllerType
    {
        ControllerType_ProController = 1 << 0,
        ControllerType_Handheld = 1 << 1,
        ControllerType_JoyconPair = 1 << 2,
        ControllerType_JoyconLeft = 1 << 3,
        ControllerType_JoyconRight = 1 << 4
    }

    public enum HidControllerColorDescription
    {
        ColorDesc_ColorsNonexistent = 1 << 1,
    }

    public struct JoystickPosition //Size: 0x8
    {
        public int DX;
        public int DY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidControllerMAC //Size: 0x20
    {
        public ulong Timestamp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] MAC;
        public ulong Unknown;
        public ulong Timestamp_2;
    }

    public struct HidControllerHeader //Size: 0x28
    {
        public uint Type;
        public uint IsHalf;
        public uint SingleColorsDescriptor;
        public uint SingleColorBody;
        public uint SingleColorButtons;
        public uint SplitColorsDescriptor;
        public uint LeftColorBody;
        public uint LeftColorButtons;
        public uint RightColorBody;
        public uint RightColorButtons;
    }

    public struct HidControllerLayoutHeader //Size: 0x20
    {
        public ulong TimestampTicks;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidControllerInputEntry //Size: 0x30
    {
        public ulong Timestamp;
        public ulong Timestamp_2;
        public ulong Buttons;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)HidControllerJoystick.Joystick_Num_Sticks)]
        public JoystickPosition[] Joysticks;
        public ulong ConnectionState;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidControllerLayout //Size: 0x350
    {
        public HidControllerLayoutHeader Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public HidControllerInputEntry[] Entries;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidController //Size: 0x5000
    {
        public HidControllerHeader Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public HidControllerLayout[] Layouts;
        /*
            pro_controller
            handheld_joined
            joined
            left
            right
            main_no_analog
            main
        */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x2A70)]
        public byte[] Unknown_1;
        public HidControllerMAC MacLeft;
        public HidControllerMAC MacRight;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xDF8)]
        public byte[] Unknown_2;
    }
}
