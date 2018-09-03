using Ryujinx.Graphics.Memory;

namespace Ryujinx.Graphics.Engines
{
    interface INvGpuEngine
    {
        int[] Registers { get; }

        void CallMethod(NvGpuVmm Vmm, NvGpuPBEntry PBEntry);
    }
}