using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gpu.Synchronization;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl
{
    class NvHostSyncpt
    {
        public const int EventsCount = 64;

        private int[]  _counterMin;
        private int[]  _counterMax;
        private bool[] _clientManaged;
        private bool[] _assigned;

        private Switch _device;

        public NvHostEvent[] Events { get; private set; }

        private object SyncpointAllocatorLock = new object();

        public NvHostSyncpt(Switch device)
        {
            _device        = device;
            Events         = new NvHostEvent[EventsCount];
            _counterMin    = new int[Synchronization.MaxHarwareSyncpoints];
            _counterMax    = new int[Synchronization.MaxHarwareSyncpoints];
            _clientManaged = new bool[Synchronization.MaxHarwareSyncpoints];
            _assigned      = new bool[Synchronization.MaxHarwareSyncpoints];
        }

        private void ReserveSyncpointLocked(uint id, bool isClientManaged)
        {
            if (id >= Synchronization.MaxHarwareSyncpoints || _assigned[id])
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            _assigned[id]      = true;
            _clientManaged[id] = isClientManaged;
        }

        public uint AllocateSyncpoint(bool isClientManaged)
        {
            lock (SyncpointAllocatorLock)
            {
                for (uint i = 1; i < Synchronization.MaxHarwareSyncpoints; i++)
                {
                    if (!_assigned[i])
                    {
                        ReserveSyncpointLocked(i, isClientManaged);
                        return i;
                    }
                }
            }

            Logger.PrintError(LogClass.ServiceNv, "Cannot allocate a new syncpoint!");
            return 0;
        }

        public void ReleaseSyncpoint(uint id)
        {
            if (id == 0)
            {
                return;
            }

            lock (SyncpointAllocatorLock)
            {
                if (id >= Synchronization.MaxHarwareSyncpoints || !_assigned[id])
                {
                    throw new ArgumentOutOfRangeException(nameof(id));
                }

                _assigned[id]      = false;
                _clientManaged[id] = false;

                SetSyncpointMinEqualSyncpointMax(id);
            }
        }

        public void SetSyncpointMinEqualSyncpointMax(uint id)
        {
            if (id >= Synchronization.MaxHarwareSyncpoints)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            int value = (int)ReadSyncpointValue(id);

            Interlocked.Exchange(ref _counterMax[id], value);
        }

        public NvHostEvent GetFreeEvent(uint id, out uint eventIndex)
        {
            eventIndex = EventsCount;

            uint nullIndex = EventsCount;

            for (uint index = 0; index < EventsCount; index++)
            {
                NvHostEvent Event = Events[index];

                if (Event != null)
                {
                    if (Event.State == NvHostEventState.Availaible ||
                        Event.State == NvHostEventState.Signaled   ||
                        Event.State == NvHostEventState.Cancelled)
                    {
                        eventIndex = index;

                        if (Event.Fence.Id == id)
                        {
                            return Event;
                        }
                    }
                }
                else if (nullIndex == EventsCount)
                {
                    nullIndex = index;
                }
            }

            if (nullIndex < EventsCount)
            {
                eventIndex = nullIndex;

                RegisterEvent(eventIndex);

                return Events[nullIndex];
            }

            if (eventIndex < EventsCount)
            {
                return Events[eventIndex];
            }

            return null;
        }

        public NvInternalResult RegisterEvent(uint eventId)
        {
            NvInternalResult result = UnregisterEvent(eventId);

            if (result == NvInternalResult.Success)
            {
                Events[eventId] = new NvHostEvent(this, eventId, _device.System);
            }

            return result;
        }

        public NvInternalResult UnregisterEvent(uint eventId)
        {
            if (eventId >= EventsCount)
            {
                return NvInternalResult.InvalidInput;
            }

            NvHostEvent evnt = Events[eventId];

            if (evnt == null)
            {
                return NvInternalResult.Success;
            }

            if (evnt.State == NvHostEventState.Availaible || evnt.State == NvHostEventState.Cancelled || evnt.State == NvHostEventState.Signaled)
            {
                Events[eventId] = null;

                return NvInternalResult.Success;
            }

            return NvInternalResult.Busy;
        }

        public NvInternalResult KillEvent(ulong eventMask)
        {
            NvInternalResult result = NvInternalResult.Success;

            for (uint eventId = 0; eventId < EventsCount; eventId++)
            {
                if ((eventMask & (1UL << (int)eventId)) != 0)
                {
                    NvInternalResult tmp = UnregisterEvent(eventId);

                    if (tmp != NvInternalResult.Success)
                    {
                        result = tmp;
                    }
                }
            }

            return result;
        }

        public NvInternalResult SignalEvent(uint eventId)
        {
            if (eventId >= EventsCount)
            {
                return NvInternalResult.InvalidInput;
            }

            NvHostEvent evnt = Events[eventId];

            if (evnt == null)
            {
                return NvInternalResult.InvalidInput;
            }

            NvHostEventState oldState = evnt.State;

            if (oldState == NvHostEventState.Waiting)
            {
                evnt.State = NvHostEventState.Cancelling;

                evnt.Cancel(_device.Gpu);
            }

            evnt.State = NvHostEventState.Cancelled;

            return NvInternalResult.Success;
        }

        public uint ReadSyncpointValue(uint id)
        {
            return UpdateMin(id);
        }

        public uint ReadSyncpointMinValue(uint id)
        {
            return (uint)_counterMin[id];
        }

        public uint ReadSyncpointMaxValue(uint id)
        {
            return (uint)_counterMax[id];
        }

        public KEvent QueryEvent(uint eventId)
        {
            uint eventSlot;

            if ((eventId >> 28) == 1)
            {
                eventSlot = eventId & 0xFFFF;
            }
            else
            {
                eventSlot = eventId & 0xFF;
            }

            if (eventSlot >= EventsCount)
            {
                return null;
            }

            return Events[eventSlot].Event;
        }

        private bool IsClientManaged(uint id)
        {
            if (id >= Synchronization.MaxHarwareSyncpoints)
            {
                return false;
            }

            return _clientManaged[id];
        }

        public void Increment(uint id)
        {
            if (IsClientManaged(id))
            {
                IncrementSyncpointMax(id);
            }

            IncrementSyncpointGPU(id);
        }

        public uint UpdateMin(uint id)
        {
            uint newValue = _device.Gpu.Synchronization.GetSyncpointValue(id);

            Interlocked.Exchange(ref _counterMin[id], (int)newValue);

            return newValue;
        }

        private void IncrementSyncpointGPU(uint id)
        {
            _device.Gpu.Synchronization.IncrementSyncpoint(id);
        }

        public void IncrementSyncpointMin(uint id)
        {
            Interlocked.Increment(ref _counterMin[id]);
        }

        public uint IncrementSyncpointMaxExt(uint id, int count)
        {
            if (count == 0)
            {
                return ReadSyncpointMaxValue(id);
            }

            uint result = 0;

            for (int i = 0; i < count; i++)
            {
                result = IncrementSyncpointMax(id);
            }

            return result;
        }

        private uint IncrementSyncpointMax(uint id)
        {
            return (uint)Interlocked.Increment(ref _counterMax[id]);
        }

        public bool IsSyncpointExpired(uint id, uint threshold)
        {
            return MinCompare(id, _counterMin[id], _counterMax[id], (int)threshold);
        }

        private bool MinCompare(uint id, int min, int max, int threshold)
        {
            int minDiff = min - threshold;
            int maxDiff = max - threshold;

            if (IsClientManaged(id))
            {
                return minDiff >= 0;
            }
            else
            {
                return (uint)maxDiff >= (uint)minDiff;
            }
        }
    }
}