using ChocolArm64.Memory;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu
{
    public class NvGpuFifo
    {
        private const int MacrosCount    = 32;
        private const int MacroIndexMask = MacrosCount - 1;

        private NsGpu Gpu;

        private ConcurrentQueue<(AMemory, NsGpuPBEntry[])> BufferQueue;

        private NvGpuEngine[] SubChannels;

        private struct CachedMacro
        {
            public long Position { get; private set; }

            private MacroInterpreter Interpreter;

            public CachedMacro(INvGpuEngine Engine, long Position)
            {
                this.Position = Position;

                Interpreter = new MacroInterpreter(Engine);
            }

            public void PushParam(int Param)
            {
                Interpreter?.Fifo.Enqueue(Param);
            }

            public void Execute(AMemory Memory, int Param)
            {
                Interpreter?.Execute(Memory, Position, Param);
            }
        }

        private long CurrentMacroPosition;
        private int  CurrentMacroBindIndex;

        private CachedMacro[] Macros;

        private Queue<(int, int)> MacroQueue;

        public NvGpuFifo(NsGpu Gpu)
        {
            this.Gpu = Gpu;

            BufferQueue = new ConcurrentQueue<(AMemory, NsGpuPBEntry[])>();

            SubChannels = new NvGpuEngine[8];

            Macros = new CachedMacro[MacrosCount];

            MacroQueue = new Queue<(int, int)>();
        }

        public void PushBuffer(AMemory Memory, NsGpuPBEntry[] Buffer)
        {
            BufferQueue.Enqueue((Memory, Buffer));
        }

        public void DispatchCalls()
        {
            while (BufferQueue.TryDequeue(out (AMemory Memory, NsGpuPBEntry[] Buffer) Tuple))
            {
                foreach (NsGpuPBEntry PBEntry in Tuple.Buffer)
                {
                    CallMethod(Tuple.Memory, PBEntry);
                }

                ExecuteMacros(Tuple.Memory);
            }
        }

        private void ExecuteMacros(AMemory Memory)
        {
            while (MacroQueue.TryDequeue(out (int Index, int Param) Tuple))
            {
                Macros[Tuple.Index].Execute(Memory, Tuple.Param);
            }
        }

        private void CallMethod(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            if (PBEntry.Method < 0x80)
            {
                switch ((NvGpuFifoMeth)PBEntry.Method)
                {
                    case NvGpuFifoMeth.BindChannel:
                    {
                        NvGpuEngine Engine = (NvGpuEngine)PBEntry.Arguments[0];

                        SubChannels[PBEntry.SubChannel] = Engine;

                        break;
                    }

                    case NvGpuFifoMeth.SetMacroUploadAddress:
                    {
                        CurrentMacroPosition = (long)((ulong)PBEntry.Arguments[0] << 2);

                        break;
                    }

                    case NvGpuFifoMeth.SendMacroCodeData:
                    {
                        long Position = Gpu.GetCpuAddr(CurrentMacroPosition);

                        foreach (int Arg in PBEntry.Arguments)
                        {
                            Memory.WriteInt32(Position, Arg);

                            CurrentMacroPosition += 4;

                            Position += 4;
                        }
                        break;
                    }

                    case NvGpuFifoMeth.SetMacroBindingIndex:
                    {
                        CurrentMacroBindIndex = PBEntry.Arguments[0];

                        break;
                    }

                    case NvGpuFifoMeth.BindMacro:
                    {
                        long Position = (long)((ulong)PBEntry.Arguments[0] << 2);

                        Position = Gpu.GetCpuAddr(Position);

                        Macros[CurrentMacroBindIndex] = new CachedMacro(Gpu.Engine3d, Position);

                        break;
                    }
                }
            }
            else
            {
                switch (SubChannels[PBEntry.SubChannel])
                {
                    case NvGpuEngine._3d: Call3dMethod(Memory, PBEntry); break;
                }
            }
        }

        private void Call3dMethod(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            if (PBEntry.Method < 0xe00)
            {
                Gpu.Engine3d.CallMethod(Memory, PBEntry);
            }
            else
            {
                int MacroIndex = (PBEntry.Method >> 1) & MacroIndexMask;

                if ((PBEntry.Method & 1) != 0)
                {
                    foreach (int Arg in PBEntry.Arguments)
                    {
                        Macros[MacroIndex].PushParam(Arg);
                    }
                }
                else
                {
                    MacroQueue.Enqueue((MacroIndex, PBEntry.Arguments[0]));
                }
            }
        }
    }
}