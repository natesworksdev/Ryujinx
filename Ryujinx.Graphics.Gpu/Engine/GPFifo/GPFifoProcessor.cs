using Ryujinx.Graphics.Gpu.Engine.MME;
using Ryujinx.Graphics.Gpu.State;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Gpu.Engine.GPFifo
{
    class GPFifoProcessor
    {
        private const int MacrosCount = 0x80;
        private const int MacroIndexMask = MacrosCount - 1;

        // Note: The size of the macro memory is unknown, we just make
        // a guess here and use 256kb as the size. Increase if needed.
        private const int MmeWords = 256 * 256;

        private readonly GpuContext _context;

        private readonly Macro[] _macros;
        private readonly int[] _mme;

        /// <summary>
        /// Internal GPFIFO state.
        /// </summary>
        private struct DmaState
        {
            public int Method;
            public int SubChannel;
            public int MethodCount;
            public bool NonIncrementing;
            public bool IncrementOnce;
        }

        private DmaState _state;

        private readonly GpuState[] _subChannels;
        private readonly GpuState _fifoChannel;

        private ShadowRamControl _shadowCtrl;

        public GPFifoProcessor(GpuContext context)
        {
            _context = context;

            _macros = new Macro[MacrosCount];
            _mme = new int[MmeWords];

            _fifoChannel = new GpuState();

            _context.Methods.RegisterCallbacksForFifo(_fifoChannel);

            _subChannels = new GpuState[8];

            for (int index = 0; index < _subChannels.Length; index++)
            {
                _subChannels[index] = new GpuState();

                _context.Methods.RegisterCallbacks(_subChannels[index]);
            }
        }

        public void Process(ReadOnlySpan<int> commandBuffer)
        {
            for (int index = 0; index < commandBuffer.Length; index++)
            {
                int command = commandBuffer[index];

                if (_state.MethodCount != 0)
                {
                    Send(new MethodParams(_state.Method, command, _state.SubChannel, _state.MethodCount));

                    if (!_state.NonIncrementing)
                    {
                        _state.Method++;
                    }

                    if (_state.IncrementOnce)
                    {
                        _state.NonIncrementing = true;
                    }

                    _state.MethodCount--;
                }
                else
                {
                    CompressedMethod meth = Unsafe.As<int, CompressedMethod>(ref command);

                    if (TryFastUniformBufferUpdate(meth, commandBuffer, index))
                    {
                        index += meth.MethodCount;
                        continue;
                    }

                    switch (meth.SecOp)
                    {
                        case SecOp.IncMethod:
                        case SecOp.NonIncMethod:
                        case SecOp.OneInc:
                            _state.Method = meth.MethodAddress;
                            _state.SubChannel = meth.MethodSubchannel;
                            _state.MethodCount = meth.MethodCount;
                            _state.IncrementOnce = meth.SecOp == SecOp.OneInc;
                            _state.NonIncrementing = meth.SecOp == SecOp.NonIncMethod;
                            break;
                        case SecOp.ImmdDataMethod:
                            Send(new MethodParams(meth.MethodAddress, meth.ImmdData, meth.MethodSubchannel, 1));
                            break;
                    }
                }
            }
        }

        private bool TryFastUniformBufferUpdate(CompressedMethod meth, ReadOnlySpan<int> commandBuffer, int offset)
        {
            int availableCount = commandBuffer.Length - offset;

            if (meth.MethodCount < availableCount &&
                meth.SecOp == SecOp.NonIncMethod &&
                meth.MethodAddress == (int)MethodOffset.UniformBufferUpdateData)
            {
                GpuState state = _subChannels[meth.MethodSubchannel];

                _context.Methods.UniformBufferUpdate(state, commandBuffer.Slice(offset + 1, meth.MethodCount));

                return true;
            }

            return false;
        }

        private void Send(MethodParams meth)
        {
            if ((MethodOffset)meth.Method == MethodOffset.BindChannel)
            {
                _subChannels[meth.SubChannel] = new GpuState();

                _context.Methods.RegisterCallbacks(_subChannels[meth.SubChannel]);
            }
            else if (meth.Method < 0x60)
            {
                // TODO: check if macros are shared between subchannels or not. For now let's assume they are.
                _fifoChannel.CallMethod(meth, _shadowCtrl);
            }
            else if (meth.Method < 0xe00)
            {
                _subChannels[meth.SubChannel].CallMethod(meth, _shadowCtrl);
            }
            else
            {
                int macroIndex = (meth.Method >> 1) & MacroIndexMask;

                if ((meth.Method & 1) != 0)
                {
                    _macros[macroIndex].PushArgument(meth.Argument);
                }
                else
                {
                    _macros[macroIndex].StartExecution(meth.Argument);
                }

                if (meth.IsLastCall)
                {
                    _macros[macroIndex].Execute(_mme, _shadowCtrl, _subChannels[meth.SubChannel]);

                    _context.Methods.PerformDeferredDraws();
                }
            }
        }

        /// <summary>
        /// Send macro code/data to the MME
        /// </summary>
        /// <param name="index">The index in the MME</param>
        /// <param name="data">The data to use</param>
        public void SendMacroCodeData(int index, int data)
        {
            _mme[index] = data;
        }

        /// <summary>
        /// Bind a macro index to a position for the MME
        /// </summary>
        /// <param name="index">The macro index</param>
        /// <param name="position">The position of the macro</param>
        public void BindMacro(int index, int position)
        {
            _macros[index] = new Macro(position);
        }

        /// <summary>
        /// Change the shadow RAM setting
        /// </summary>
        /// <param name="shadowCtrl">The new Shadow RAM setting</param>
        public void SetMmeShadowRamControl(ShadowRamControl shadowCtrl)
        {
            _shadowCtrl = shadowCtrl;
        }
    }
}
