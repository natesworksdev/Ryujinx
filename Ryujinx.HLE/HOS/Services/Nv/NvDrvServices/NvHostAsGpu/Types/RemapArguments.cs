using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostAsGpu.Types
{
    [StructLayout(LayoutKind.Sequential)]
    struct RemapArguments
    {
        public ushort Flags;
        public ushort Kind;
        public int    NvMapHandle;
        public int    MapOffset;
        public uint   GpuOffset;
        public uint   Pages;
    }
}
