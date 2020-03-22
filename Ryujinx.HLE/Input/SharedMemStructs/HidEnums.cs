using System;

namespace Ryujinx.HLE.Input
{
    /*
     * Reference:
     * https://github.com/switchbrew/libnx/blob/master/nx/source/services/hid.c
     * https://github.com/switchbrew/libnx/blob/master/nx/include/switch/services/hid.h
     * 
     * Some fields renamed to be more contextual
    */

    public enum HidControllerID : int
    {
        Player1 = 0,
        Player2 = 1,
        Player3 = 2,
        Player4 = 3,
        Player5 = 4,
        Player6 = 5,
        Player7 = 6,
        Player8 = 7,
        Handheld = 8,
        Unknown = 9,
        Auto = 10       // Shouldn't be used directly
    }

    [Flags]
    public enum ControllerType : int
    {
        None,
        ProController = 1 << 0,
        Handheld = 1 << 1,
        JoyconPair = 1 << 2,
        JoyconLeft = 1 << 3,
        JoyconRight = 1 << 4,
        Invalid = 1 << 5,
        Pokeball = 1 << 6,
        SystemExternal = 1 << 29,
        System = 1 << 30
    }

    internal enum ControllerLayoutType : int
    {
        ProController = 0,
        Handheld = 1,
        Dual = 2,
        Left = 3,
        Right = 4,
        DefaultDigital = 5,
        Default = 6
    }

    [Flags]
    internal enum HidControllerColorDescription : int
    {
        ColorDescriptionColorsNonexistent = (1 << 1)
    }

    public enum NpadColor : int //Thanks to CTCaer
    {
        Black = 0,

        BodyGrey = 0x828282,
        BodyNeonBlue = 0x0AB9E6,
        BodyNeonRed = 0xFF3C28,
        BodyNeonYellow = 0xE6FF00,
        BodyNeonPink = 0xFF3278,
        BodyNeonGreen = 0x1EDC00,
        BodyRed = 0xE10F00,

        ButtonsGrey = 0x0F0F0F,
        ButtonsNeonBlue = 0x001E1E,
        ButtonsNeonRed = 0x1E0A0A,
        ButtonsNeonYellow = 0x142800,
        ButtonsNeonPink = 0x28001E,
        ButtonsNeonGreen = 0x002800,
        ButtonsRed = 0x280A0A
    }


    [Flags]
    public enum ControllerKeys : long
    {
        A = 1 << 0,
        B = 1 << 1,
        X = 1 << 2,
        Y = 1 << 3,
        LStick = 1 << 4,
        RStick = 1 << 5,
        L = 1 << 6,
        R = 1 << 7,
        Zl = 1 << 8,
        Zr = 1 << 9,
        Plus = 1 << 10,
        Minus = 1 << 11,
        DpadLeft = 1 << 12,
        DpadUp = 1 << 13,
        DpadRight = 1 << 14,
        DpadDown = 1 << 15,
        LStickLeft = 1 << 16,
        LStickUp = 1 << 17,
        LStickRight = 1 << 18,
        LStickDown = 1 << 19,
        RStickLeft = 1 << 20,
        RStickUp = 1 << 21,
        RStickRight = 1 << 22,
        RStickDown = 1 << 23,
        SlLeft = 1 << 24,
        SrLeft = 1 << 25,
        SlRight = 1 << 26,
        SrRight = 1 << 27,

        // Generic Catch-all
        Up = DpadUp | LStickUp | RStickUp,
        Down = DpadDown | LStickDown | RStickDown,
        Left = DpadLeft | LStickLeft | RStickLeft,
        Right = DpadRight | LStickRight | RStickRight,
        Sl = SlLeft | SlRight,
        Sr = SrLeft | SrRight
    }


    [Flags]
    internal enum HidControllerConnectionState : long
    {
        ControllerStateConnected = (1 << 0),
        ControllerStateWired = (1 << 1),
        JoyLeftConnected = (1<<2),
        JoyRightConnected = (1<<4)
    }


    [Flags]
    internal enum DeviceType : int
    {
        FullKey = 1 << 0,
        HandheldLeft = 1 << 2,
        HandheldRight = 1 << 3,
        JoyLeft = 1 << 4,
        JoyRight = 1 << 5,
        Palma = 1 << 6, // Poké Ball Plus
        GenericExternal = 1 << 15,
        Generic = 1 << 31
    }


    [Flags]
    internal enum DeviceFlags : long
    {
        PowerInfo0Charging = 1 << 0,
        PowerInfo1Charging = 1 << 1,
        PowerInfo2Charging = 1 << 2,
        PowerInfo0Connected = 1 << 3,
        PowerInfo1Connected = 1 << 4,
        PowerInfo2Connected = 1 << 5,
        UnsupportedButtonPressedNpadSystem = 1 << 9,
        UnsupportedButtonPressedNpadSystemExt = 1 << 10,
        AbxyButtonOriented = 1 << 11,
        SlSrButtonOriented = 1 << 12,
        PlusButtonCapability = 1 << 13,
        MinusButtonCapability = 1 << 14,
        DirectionalButtonsSupported = 1 << 15
    }

    internal enum BatteryCharge : int
    {
        // TODO : Check if these are the correct states
        Percent0 = 0,
        Percent25 = 1,
        Percent50 = 2,
        Percent75 = 3,
        Percent100 = 4
    }

    internal enum HidJoyHoldType
    {
        Vertical,
        Horizontal
    }

}