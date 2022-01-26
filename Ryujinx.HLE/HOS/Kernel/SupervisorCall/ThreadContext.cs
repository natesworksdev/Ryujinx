using ARMeilleure.State;
using Ryujinx.Common.Memory;

namespace Ryujinx.HLE.HOS.Kernel.SupervisorCall
{
    public struct ThreadContext
    {
        public Array29<ulong> Registers;
        public ulong Fp;
        public ulong Lr;
        public ulong Sp;
        public ulong Pc;
        public uint Pstate;
        private uint _padding;
        public Array32<V128> FpuRegisters;
        public uint Fpcr;
        public uint Fpsr;
        public ulong Tpidr;
    }
}
