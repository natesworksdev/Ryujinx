using Ryujinx.Graphics.Memory;

namespace Ryujinx.Graphics
{
    internal interface INvGpuEngine
    {
        int[] Registers { get; }

        void CallMethod(NvGpuVmm vmm, NvGpuPBEntry pbEntry);
    }
}