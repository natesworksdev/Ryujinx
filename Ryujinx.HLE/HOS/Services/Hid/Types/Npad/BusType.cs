namespace Ryujinx.HLE.HOS.Services.Hid
{
    // nn::hidbus::BusType
    public enum BusType
    {
        LeftJoyRail = 0,
        RightJoyRail = 1,
        InternalBus = 2 // Lark microphone [6.0.0]+
    }
}