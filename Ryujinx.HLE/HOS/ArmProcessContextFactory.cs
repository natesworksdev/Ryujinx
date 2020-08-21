using Ryujinx.Cpu;
using Ryujinx.Horizon.Kernel.Process;
using Ryujinx.Memory;

namespace Ryujinx.HLE.HOS
{
    class ArmProcessContextFactory : IProcessContextFactory
    {
        private readonly Switch _device;

        public ArmProcessContextFactory(Switch device)
        {
            _device = device;
        }

        public IProcessContext Create(MemoryBlock backingMemory, ulong addressSpaceSize, InvalidAccessHandler invalidAccessHandler)
        {
            var memory = new MemoryManager(backingMemory, addressSpaceSize, invalidAccessHandler);

            _device.Gpu.SetVmm(memory);

            return new ArmProcessContext(memory);
        }
    }
}
