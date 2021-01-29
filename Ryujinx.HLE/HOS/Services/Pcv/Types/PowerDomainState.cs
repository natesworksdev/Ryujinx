using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Pcv
{
    [StructLayout(LayoutKind.Sequential)]
    struct PowerDomainState
    {
        [MarshalAs(UnmanagedType.U1)]
        public bool Enabled;
        // The below 3 bytes could be merged into a single byte[3] buffer,
        // but doing this will block `GetPowerDomainStateTable` to call `context.Memory.Write`.
        byte Reserved1;
        byte Reserved2;
        byte Reserved3;
        public int Voltage;
    }
}