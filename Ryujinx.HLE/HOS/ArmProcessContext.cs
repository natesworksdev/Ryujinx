using Ryujinx.Cpu;
using Ryujinx.Horizon.Kernel;
using Ryujinx.Memory;

namespace Ryujinx.HLE.HOS
{
    class ArmProcessContext : IProcessContext
    {
        private readonly MemoryManager _memoryManager;
        private readonly CpuContext _cpuContext;

        public IAddressSpaceManager AddressSpace => _memoryManager;

        public ArmProcessContext(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
            _cpuContext = new CpuContext(memoryManager);
        }

        public IThreadContext CreateThreadContext(ulong timerFrequency, ulong tlsAddress, bool is32Bit)
        {
            return new ArmThreadContext(timerFrequency, tlsAddress, is32Bit);
        }

        public void Execute(IThreadContext context, ulong codeAddress)
        {
            _cpuContext.Execute(((ArmThreadContext)context).Internal, codeAddress);
        }

        public void Dispose()
        {
            _memoryManager.Dispose();
        }
    }
}
