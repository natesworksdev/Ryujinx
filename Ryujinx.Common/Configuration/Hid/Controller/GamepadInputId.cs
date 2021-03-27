namespace Ryujinx.Common.Configuration.Hid.Controller
{
    public enum GamepadInputId : byte
    {
        Unbound,
        A,
        B,
        X,
        Y,
        LeftStick,
        RightStick,
        LeftShoulder,
        RightShoulder,

        // Likely axis
        LeftTrigger,
        // Likely axis
        RightTrigger,

        DpadUp,
        DpadDown,
        DpadLeft,
        DpadRight,

        // Special buttons

        Minus,
        Plus,

        Back = Minus,
        Start = Plus,

        // Virtual buttons for single joycon
        SingleLeftTrigger0,
        SingleRightTrigger0,

        SingleLeftTrigger1,
        SingleRightTrigger1,

        Count
    }
}
