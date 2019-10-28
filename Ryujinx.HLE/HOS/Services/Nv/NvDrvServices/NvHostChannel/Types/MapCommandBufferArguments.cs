using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostChannel.Types
{
    [StructLayout(LayoutKind.Sequential)]
    struct CommandBufferHandle
    {
        public int MapHandle;
        public int MapAddress;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MapCommandBufferArguments
    {
        public int   NumEntries;
        public int   DataAddress; // Ignored by the driver.
        [MarshalAs(UnmanagedType.I1)]
        public bool AttachHostChDas;
        public byte  Padding1;
        public short Padding2;
    }
}
