using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostAsGpu.Types
{
    [StructLayout(LayoutKind.Sequential)]
    struct AllocSpaceArguments
    {
        public uint Pages;
        public uint PageSize;
        public uint Flags;
        public uint Padding;
        public long Offset;
    }
}
