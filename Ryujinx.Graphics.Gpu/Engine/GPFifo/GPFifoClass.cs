using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.Gpu.Engine.MME;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Graphics.Gpu.Engine.GPFifo
{
    class GPFifoClass : IDeviceState
    {
        private readonly GpuContext _context;
        private readonly DeviceState<GPFifoClassState> _state;

        private const int MacrosCount = 0x80;

        // Note: The size of the macro memory is unknown, we just make
        // a guess here and use 256kb as the size. Increase if needed.
        private const int MacroCodeSize = 256 * 256;

        private readonly Macro[] _macros;
        private readonly int[] _macroCode;
        private ShadowRamControl _shadowCtrl;

        public GPFifoClass(GpuContext context)
        {
            _context = context;
            _state = new DeviceState<GPFifoClassState>(new Dictionary<string, RwCallback>
            {
                { nameof(GPFifoClassState.Semaphored), new RwCallback(Semaphored, null) },
                { nameof(GPFifoClassState.Syncpointb), new RwCallback(Syncpointb, null) },
                { nameof(GPFifoClassState.WaitForIdle), new RwCallback(WaitForIdle, null) },
                { nameof(GPFifoClassState.LoadMmeInstructionRam), new RwCallback(LoadMmeInstructionRam, null) },
                { nameof(GPFifoClassState.LoadMmeStartAddressRam), new RwCallback(LoadMmeStartAddressRam, null) },
                { nameof(GPFifoClassState.SetMmeShadowRamControl), new RwCallback(SetMmeShadowRamControl, null) }
            });

            _macros = new Macro[MacrosCount];
            _macroCode = new int[MacroCodeSize];
        }

        public int Read(int offset) => _state.Read(offset);
        public void Write(int offset, int data) => _state.Write(offset, data);

        /// <summary>
        /// Writes a GPU counter to guest memory.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void Semaphored(int argument)
        {
            ulong address = ((ulong)_state.State.SemaphorebOffsetLower << 2) |
                            ((ulong)_state.State.SemaphoreaOffsetUpper << 32);

            int value = _state.State.SemaphorecPayload;

            _context.MemoryAccessor.Write(address, value);

            _context.AdvanceSequence();
        }

        /// <summary>
        /// Apply a fence operation on a syncpoint.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void Syncpointb(int argument)
        {
            SyncpointbOperation operation = _state.State.SyncpointbOperation;

            uint syncpointId = (uint)_state.State.SyncpointbSyncptIndex;

            if (operation == SyncpointbOperation.Wait)
            {
                uint threshold = (uint)_state.State.SyncpointaPayload;

                _context.Synchronization.WaitOnSyncpoint(syncpointId, threshold, Timeout.InfiniteTimeSpan);
            }
            else if (operation == SyncpointbOperation.Incr)
            {
                _context.Synchronization.IncrementSyncpoint(syncpointId);
            }
        }

        /// <summary>
        /// Waits for the GPU to be idle.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void WaitForIdle(int argument)
        {
            _context.Methods.PerformDeferredDraws();
            _context.Renderer.Pipeline.Barrier();
        }

        /// <summary>
        /// Send macro code/data to the MME
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void LoadMmeInstructionRam(int argument)
        {
            _macroCode[_state.State.LoadMmeInstructionRamPointer++] = argument;
        }

        /// <summary>
        /// Bind a macro index to a position for the MME
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void LoadMmeStartAddressRam(int argument)
        {
            _macros[_state.State.LoadMmeStartAddressRamPointer++] = new Macro(argument);
        }

        /// <summary>
        /// Change the shadow RAM setting
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void SetMmeShadowRamControl(int argument)
        {
            _shadowCtrl = (ShadowRamControl)argument;
        }
    }
}
