using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.Horizon.Kernel;
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

        public IProcessContext Create(MemoryBlock backingMemory, ulong addressSpaceSize)
        {
            var memory = new MemoryManager(backingMemory, addressSpaceSize, InvalidAccessHandler);

            _device.Gpu.SetVmm(memory);

            return new ArmProcessContext(memory);
        }

        private bool InvalidAccessHandler(ulong va)
        {
            Logger.Info?.Print(LogClass.Cpu, $"Guest stack trace:\n{KernelStatic.GetGuestStackTrace()}\n");
            Logger.Error?.Print(LogClass.Cpu, $"Invalid memory access at virtual address 0x{va:X16}.");

            return false;
        }
    }
}
