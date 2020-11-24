using Ryujinx.Common;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel;
using Ryujinx.Horizon.Kernel.Common;
using System.Collections.Generic;
using System;

namespace Ryujinx.Horizon.Sdk.OsTypes.Impl
{
    class WaitableManagerImpl
    {
        private const int WaitTimedOut  = -1;
        private const int WaitCancelled = -2;
        private const int WaitInvalid   = -3;

        private readonly List<WaitableHolderBase> _waitables;

        private object _lock;

        private int _waitingThreadHandle;

        private WaitableHolderBase _signaledHolder;

        public long CurrentTime { get; private set; }

        public WaitableManagerImpl()
        {
            _waitables = new List<WaitableHolderBase>();

            _lock = new object();
        }

        public void LinkWaitableHolder(WaitableHolderBase waitableHolder)
        {
            _waitables.Add(waitableHolder);
        }

        public void UnlinkWaitableHolder(WaitableHolderBase waitableHolder)
        {
            _waitables.Remove(waitableHolder);
        }

        public void MoveAllFrom(WaitableManagerImpl other)
        {
            foreach (WaitableHolderBase waitable in other._waitables)
            {
                waitable.SetManager(this);
            }

            _waitables.AddRange(other._waitables);

            other._waitables.Clear();
        }

        public WaitableHolderBase WaitAnyImpl(bool infinite, long timeout)
        {
            _waitingThreadHandle = Os.GetCurrentThreadHandle();

            _signaledHolder = null;

            WaitableHolderBase result = LinkHoldersToObjectList();

            lock (_lock)
            {
                if (_signaledHolder != null)
                {
                    result = _signaledHolder;
                }
            }

            if (result == null)
            {
                result = WaitAnyHandleImpl(infinite, timeout);
            }

            UnlinkHoldersFromObjectsList();

            return result;
        }

        private WaitableHolderBase WaitAnyHandleImpl(bool infinite, long timeout)
        {
            Span<int> objectHandles = new int[64];

            Span<WaitableHolderBase> objects = new WaitableHolderBase[64];

            int count = FillObjectsArray(objectHandles, objects);

            long endTime = infinite ? -1L : PerformanceCounter.ElapsedMilliseconds * 1000000;

            while (true)
            {
                CurrentTime = PerformanceCounter.ElapsedMilliseconds * 1000000;

                WaitableHolderBase minTimeoutObject = RecalculateNextTimepoint(endTime, out long minTimeout);

                int index;

                if (count == 0 && minTimeout == 0)
                {
                    index = WaitTimedOut;
                }
                else
                {
                    index = WaitSynchronization(objectHandles.Slice(0, count), minTimeout);

                    DebugUtil.Assert(index != WaitInvalid);
                }

                switch (index)
                {
                    case WaitTimedOut:
                        if (minTimeoutObject != null)
                        {
                            CurrentTime = PerformanceCounter.ElapsedMilliseconds * 1000000;

                            if (minTimeoutObject.Signaled == TriBool.True)
                            {
                                lock (_lock)
                                {
                                    _signaledHolder = minTimeoutObject;

                                    return _signaledHolder;
                                }
                            }
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    case WaitCancelled:
                        if (_signaledHolder != null)
                        {
                            return _signaledHolder;
                        }
                        break;
                    default:
                        lock (_lock)
                        {
                            _signaledHolder = objects[index];

                            return _signaledHolder;
                        }
                }
            }
        }

        private int FillObjectsArray(Span<int> handles, Span<WaitableHolderBase> objects)
        {
            int count = 0;

            foreach (WaitableHolderBase holder in _waitables)
            {
                int handle = holder.Handle;

                if (handle != 0)
                {
                    handles[count] = handle;
                    objects[count] = holder;

                    count++;
                }
            }

            return count;
        }

        private WaitableHolderBase RecalculateNextTimepoint(long endTime, out long minTimeout)
        {
            WaitableHolderBase minTimeHolder = null;

            long minTime = endTime;

            foreach (WaitableHolder holder in _waitables)
            {
                long currentTime = holder.GetWakeUpTime();

                if ((ulong)currentTime < (ulong)minTime)
                {
                    minTimeHolder = holder;

                    minTime = currentTime;
                }
            }

            minTimeout = (ulong)minTime < (ulong)CurrentTime ? 0 : minTime - CurrentTime;

            return minTimeHolder;
        }

        private static int WaitSynchronization(ReadOnlySpan<int> handles, long timeout)
        {
            Result result = KernelStatic.Syscall.WaitSynchronization(out int index, handles, timeout);

            if (result == KernelResult.TimedOut)
            {
                return WaitTimedOut;
            }
            else if (result == KernelResult.Cancelled)
            {
                return WaitCancelled;
            }
            else
            {
                result.AbortOnFailure();
            }

            return index;
        }

        private WaitableHolderBase LinkHoldersToObjectList()
        {
            WaitableHolderBase signaledHolder = null;

            foreach (WaitableHolderBase holder in _waitables)
            {
                TriBool isSignaled = holder.LinkToObjectList();

                if (signaledHolder == null && isSignaled == TriBool.True)
                {
                    signaledHolder = holder;
                }
            }

            return signaledHolder;
        }

        private void UnlinkHoldersFromObjectsList()
        {
            foreach (WaitableHolderBase holder in _waitables)
            {
                holder.UnlinkFromObjectList();
            }
        }
    }
}
