using ChocolArm64.Memory;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu
{
    class NvGpuEngine3d : INvGpuEngine
    {
        public int[] Registers { get; private set; }

        private NsGpu Gpu;

        private Dictionary<int, NvGpuMethod> Methods;

        public NvGpuEngine3d(NsGpu Gpu)
        {
            this.Gpu = Gpu;

            Registers = new int[0xe00];

            Methods = new Dictionary<int, NvGpuMethod>()
            {
                { 0x6c3, QueryControl }
            };
        }

        public void CallMethod(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            if (Methods.TryGetValue(PBEntry.Method, out NvGpuMethod Methd))
            {
                Methd(Memory, PBEntry);
            }

            if (PBEntry.Arguments.Count == 1)
            {
                Registers[PBEntry.Method] = PBEntry.Arguments[0];
            }
        }

        private void QueryControl(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            long Position = MakeAddress(NvGpuEngine3dReg.QueryAddr);

            int Seq  = Registers[(int)NvGpuEngine3dReg.QuerySequence];
            int Ctrl = Registers[(int)NvGpuEngine3dReg.QueryControl];

            int Mode = Ctrl & 3;

            if (Mode == 0)
            {
                //Write.
                Position = Gpu.MemoryMgr.GetCpuAddr(Position);

                if (Position != -1)
                {
                    Memory.WriteInt32(Position, Seq);
                }
            }
        }

        private long MakeAddress(NvGpuEngine3dReg Reg)
        {
            return
                (long)Registers[(int)Reg + 0] << 32 |
                (uint)Registers[(int)Reg + 1];
        }
    }
}