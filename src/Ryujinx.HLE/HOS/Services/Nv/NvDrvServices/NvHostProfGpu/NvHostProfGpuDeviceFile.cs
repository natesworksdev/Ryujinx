using Ryujinx.Memory;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostProfGpu
{
    class NvHostProfGpuDeviceFile : NvDeviceFile
    {
#pragma warning disable IDE0060
        public NvHostProfGpuDeviceFile(ServiceCtx context, IVirtualMemoryManager memory, ulong owner) : base(context, owner) { }
#pragma warning restore IDE0060

        public override void Close() { }
    }
}
