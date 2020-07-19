using Ryujinx.Graphics.Gpu.State;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Gpu.Engine.GPFifo
{
    class GPFifoProcessor
    {
        private const int MacrosCount = 0x80;
        private const int MacroIndexMask = MacrosCount - 1;

        private readonly GpuContext _context;

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
        private readonly GPFifoClass _fifoClass;

        public GPFifoProcessor(GpuContext context)
        {
            _context = context;

            _fifoClass = new GPFifoClass(context);

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
                _fifoClass.Write(meth.Method * 4, meth.Argument);
            }
            else if (meth.Method < 0xe00)
            {
                _subChannels[meth.SubChannel].CallMethod(meth, _fifoClass.ShadowCtrl);
            }
            else
            {
                int macroIndex = (meth.Method >> 1) & MacroIndexMask;

                if ((meth.Method & 1) != 0)
                {
                    _fifoClass.MmePushArgument(macroIndex, meth.Argument);
                }
                else
                {
                    _fifoClass.MmeStart(macroIndex, meth.Argument);
                }

                if (meth.IsLastCall)
                {
                    _fifoClass.CallMme(macroIndex, _subChannels[meth.SubChannel]);

                    _context.Methods.PerformDeferredDraws();
                }
            }
        }
    }
}
