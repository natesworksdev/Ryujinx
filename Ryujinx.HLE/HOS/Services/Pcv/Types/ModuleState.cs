using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Pcv
{
    [StructLayout(LayoutKind.Sequential)]
    struct ModuleState
    {
        public uint ClockFrequency;
        [MarshalAs(UnmanagedType.U1)]
        public bool ClockEnabled;
        [MarshalAs(UnmanagedType.U1)]
        public bool PowerEnabled;
        [MarshalAs(UnmanagedType.U1)]
        public bool ResetAsserted;
        byte Reserved;
        public uint MinVClockRate;
    }
}