using Ryujinx.Graphics.Memory;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics
{
    public class NvGpuEngineP2mf : INvGpuEngine
    {
        public int[] Registers { get; private set; }

        private NvGpu _gpu;

        private Dictionary<int, NvGpuMethod> _methods;

        private ReadOnlyCollection<int> _dataBuffer;

        public NvGpuEngineP2mf(NvGpu gpu)
        {
            _gpu = gpu;

            Registers = new int[0x80];

            _methods = new Dictionary<int, NvGpuMethod>();

            void AddMethod(int meth, int count, int stride, NvGpuMethod method)
            {
                while (count-- > 0)
                {
                    _methods.Add(meth, method);

                    meth += stride;
                }
            }

            AddMethod(0x6c, 1, 1, Execute);
            AddMethod(0x6d, 1, 1, PushData);
        }

        public void CallMethod(NvGpuVmm vmm, NvGpuPBEntry pbEntry)
        {
            if (_methods.TryGetValue(pbEntry.Method, out NvGpuMethod method))
            {
                method(vmm, pbEntry);
            }
            else
            {
                WriteRegister(pbEntry);
            }
        }

        private void Execute(NvGpuVmm vmm, NvGpuPBEntry pbEntry)
        {
            //TODO: Some registers and copy modes are still not implemented.
            int control = pbEntry.Arguments[0];

            long dstAddress = MakeInt64From2XInt32(NvGpuEngineP2mfReg.DstAddress);

            int lineLengthIn = ReadRegister(NvGpuEngineP2mfReg.LineLengthIn);

            _dataBuffer = null;

            _gpu.Fifo.Step();

            for (int offset = 0; offset < lineLengthIn; offset += 4)
            {
                vmm.WriteInt32(dstAddress + offset, _dataBuffer[offset >> 2]);
            }
        }

        private void PushData(NvGpuVmm vmm, NvGpuPBEntry pbEntry)
        {
            _dataBuffer = pbEntry.Arguments;
        }

        private long MakeInt64From2XInt32(NvGpuEngineP2mfReg reg)
        {
            return
                ((long)Registers[(int)reg + 0] << 32) |
                (uint)Registers[(int)reg + 1];
        }

        private void WriteRegister(NvGpuPBEntry pbEntry)
        {
            int argsCount = pbEntry.Arguments.Count;

            if (argsCount > 0)
            {
                Registers[pbEntry.Method] = pbEntry.Arguments[argsCount - 1];
            }
        }

        private int ReadRegister(NvGpuEngineP2mfReg reg)
        {
            return Registers[(int)reg];
        }

        private void WriteRegister(NvGpuEngineP2mfReg reg, int value)
        {
            Registers[(int)reg] = value;
        }
    }
}