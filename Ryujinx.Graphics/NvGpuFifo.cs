using Ryujinx.Graphics.Memory;
using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.Graphics
{
    public class NvGpuFifo
    {
        private const int MacrosCount    = 0x80;
        private const int MacroIndexMask = MacrosCount - 1;

        //Note: The size of the macro memory is unknown, we just make
        //a guess here and use 256kb as the size. Increase if needed.
        private const int MmeWords = 256 * 256;

        private NvGpu _gpu;

        private ConcurrentQueue<(NvGpuVmm, NvGpuPbEntry[])> _bufferQueue;

        private NvGpuEngine[] _subChannels;

        public AutoResetEvent Event { get; private set; }

        private struct CachedMacro
        {
            public int Position { get; private set; }

            private MacroInterpreter _interpreter;

            public CachedMacro(NvGpuFifo pFifo, INvGpuEngine engine, int position)
            {
                Position = position;

                _interpreter = new MacroInterpreter(pFifo, engine);
            }

            public void PushParam(int param)
            {
                _interpreter?.Fifo.Enqueue(param);
            }

            public void Execute(NvGpuVmm vmm, int[] mme, int param)
            {
                _interpreter?.Execute(vmm, mme, Position, param);
            }
        }

        private int _currMacroPosition;
        private int _currMacroBindIndex;

        private CachedMacro[] _macros;

        private int[] _mme;

        public NvGpuFifo(NvGpu gpu)
        {
            _gpu = gpu;

            _bufferQueue = new ConcurrentQueue<(NvGpuVmm, NvGpuPbEntry[])>();

            _subChannels = new NvGpuEngine[8];

            _macros = new CachedMacro[MacrosCount];

            _mme = new int[MmeWords];

            Event = new AutoResetEvent(false);
        }

        public void PushBuffer(NvGpuVmm vmm, NvGpuPbEntry[] buffer)
        {
            _bufferQueue.Enqueue((vmm, buffer));

            Event.Set();
        }

        public void DispatchCalls()
        {
            while (Step());
        }

        private (NvGpuVmm Vmm, NvGpuPbEntry[] Pb) _curr;

        private int _currPbEntryIndex;

        public bool Step()
        {
            while (_curr.Pb == null || _curr.Pb.Length <= _currPbEntryIndex)
            {
                if (!_bufferQueue.TryDequeue(out _curr))
                {
                    return false;
                }

                _gpu.Engine3D.ResetCache();

                _gpu.ResourceManager.ClearPbCache();

                _currPbEntryIndex = 0;
            }

            CallMethod(_curr.Vmm, _curr.Pb[_currPbEntryIndex++]);

            return true;
        }

        private void CallMethod(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            if ((NvGpuFifoMeth)pbEntry.Method == NvGpuFifoMeth.BindChannel)
            {
                NvGpuEngine engine = (NvGpuEngine)pbEntry.Arguments[0];

                _subChannels[pbEntry.SubChannel] = engine;
            }
            else
            {
                switch (_subChannels[pbEntry.SubChannel])
                {
                    case NvGpuEngine._2D:  Call2DMethod  (vmm, pbEntry); break;
                    case NvGpuEngine._3D:  Call3DMethod  (vmm, pbEntry); break;
                    case NvGpuEngine.P2Mf: CallP2MfMethod(vmm, pbEntry); break;
                    case NvGpuEngine.M2Mf: CallM2MfMethod(vmm, pbEntry); break;
                }
            }
        }

        private void Call2DMethod(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            _gpu.Engine2D.CallMethod(vmm, pbEntry);
        }

        private void Call3DMethod(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            if (pbEntry.Method < 0x80)
            {
                switch ((NvGpuFifoMeth)pbEntry.Method)
                {
                    case NvGpuFifoMeth.SetMacroUploadAddress:
                    {
                        _currMacroPosition = pbEntry.Arguments[0];

                        break;
                    }

                    case NvGpuFifoMeth.SendMacroCodeData:
                    {
                        foreach (int arg in pbEntry.Arguments)
                        {
                            _mme[_currMacroPosition++] = arg;
                        }
                        break;
                    }

                    case NvGpuFifoMeth.SetMacroBindingIndex:
                    {
                        _currMacroBindIndex = pbEntry.Arguments[0];

                        break;
                    }

                    case NvGpuFifoMeth.BindMacro:
                    {
                        int position = pbEntry.Arguments[0];

                        _macros[_currMacroBindIndex] = new CachedMacro(this, _gpu.Engine3D, position);

                        break;
                    }
                }
            }
            else if (pbEntry.Method < 0xe00)
            {
                _gpu.Engine3D.CallMethod(vmm, pbEntry);
            }
            else
            {
                int macroIndex = (pbEntry.Method >> 1) & MacroIndexMask;

                if ((pbEntry.Method & 1) != 0)
                {
                    foreach (int arg in pbEntry.Arguments)
                    {
                        _macros[macroIndex].PushParam(arg);
                    }
                }
                else
                {
                    _macros[macroIndex].Execute(vmm, _mme, pbEntry.Arguments[0]);
                }
            }
        }

        private void CallP2MfMethod(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            _gpu.EngineP2Mf.CallMethod(vmm, pbEntry);
        }

        private void CallM2MfMethod(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            _gpu.EngineM2Mf.CallMethod(vmm, pbEntry);
        }
    }
}