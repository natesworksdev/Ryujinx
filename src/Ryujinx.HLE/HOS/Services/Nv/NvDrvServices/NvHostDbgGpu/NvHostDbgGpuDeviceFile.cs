using Ryujinx.Memory;
using System;
namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostDbgGpu
{
    class NvHostDbgGpuDeviceFile : NvDeviceFile
    {
#pragma warning disable IDE0060
        public NvHostDbgGpuDeviceFile(ServiceCtx context, IVirtualMemoryManager memory, ulong owner) : base(context, owner) { }
#pragma warning restore IDE0060

        public override void Close() { }
    }
}
