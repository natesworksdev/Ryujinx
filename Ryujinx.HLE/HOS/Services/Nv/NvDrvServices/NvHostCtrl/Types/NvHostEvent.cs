using Ryujinx.Graphics.Gpu;
using Ryujinx.Graphics.Gpu.Synchronization;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Nv.Types;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl
{
    internal class NvHostEvent
    {
        public NvFence          Fence;
        public NvHostEventState State;
        public KEvent           Event;

        private uint                  _eventId;
        private NvHostSyncpt          _syncpointManager;
        private SyncpointWaiterHandle _waiterInformation;

        public NvHostEvent(NvHostSyncpt syncpointManager, uint eventId, Horizon system)
        {
            Fence.Id = NvFence.InvalidSyncPointId;

            State = NvHostEventState.Registered;

            Event = new KEvent(system);

            _eventId = eventId;

            _syncpointManager = syncpointManager;
        }

        public void Reset()
        {
            Fence.Id    = NvFence.InvalidSyncPointId;
            Fence.Value = 0;
            State       = NvHostEventState.Registered;
        }

        private void Signal()
        {
            State = NvHostEventState.Free;

            Event.WritableEvent.Signal();
        }

        private void GpuSignaled()
        {
            Signal();
        }

        public void Cancel(GpuContext gpuContext)
        {
            if (_waiterInformation != null)
            {
                gpuContext.Synchronization.UnregisterCallback(Fence.Id, _waiterInformation);

                Signal();
            }
        }

        public void Wait(GpuContext gpuContext, NvFence fence)
        {
            Fence = fence;
            State = NvHostEventState.Waiting;

            _waiterInformation = gpuContext.Synchronization.RegisterCallbackOnSyncpoint(Fence.Id, Fence.Value, GpuSignaled);
        }
    }
}