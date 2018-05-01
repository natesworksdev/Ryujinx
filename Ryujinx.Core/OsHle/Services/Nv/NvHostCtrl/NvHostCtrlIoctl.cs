using ChocolArm64.Memory;
using Ryujinx.Core.Logging;
using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.Core.OsHle.Services.Nv
{
    class NvHostCtrlIoctl
    {
        private const int EventsCount = 64;

        private static ConcurrentDictionary<Process, NvHostEvent[]> EventArrays;

        private static ConcurrentDictionary<Process, NvHostSyncPt> NvSyncPts;

        static NvHostCtrlIoctl()
        {
            EventArrays = new ConcurrentDictionary<Process, NvHostEvent[]>();

            NvSyncPts = new ConcurrentDictionary<Process, NvHostSyncPt>();
        }

        private int SyncPtRead(ServiceCtx Context)
        {
            return SyncPtReadMinOrMax(Context, Max: false);
        }

        private int SyncPtIncr(ServiceCtx Context)
        {
            long InputPosition = Context.Request.GetBufferType0x21Position();

            int Id = Context.Memory.ReadInt32(InputPosition);

            if ((uint)Id >= NvHostSyncPt.SyncPtsCount)
            {
                return NvResult.InvalidInput;
            }

            GetSyncPt(Context).Increment(Id);

            return NvResult.Success;
        }

        private int SyncPtWait(ServiceCtx Context)
        {
            return SyncPtWait(Context, Extended: false);
        }

        private int SyncPtWaitEx(ServiceCtx Context)
        {
            return SyncPtWait(Context, Extended: true);
        }

        private int SyncPtReadMax(ServiceCtx Context)
        {
            return SyncPtReadMinOrMax(Context, Max: true);
        }

        private int EventWait(ServiceCtx Context)
        {
            return EventWait(Context, Async: false);
        }

        private int EventWaitAsync(ServiceCtx Context)
        {
            return EventWait(Context, Async: true);
        }

        private int SyncPtReadMinOrMax(ServiceCtx Context, bool Max)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvHostCtrlSyncPtRead Args = AMemoryHelper.Read<NvHostCtrlSyncPtRead>(Context.Memory, InputPosition);

            if ((uint)Args.Id >= NvHostSyncPt.SyncPtsCount)
            {
                return NvResult.InvalidInput;
            }

            if (Max)
            {
                Args.Value = GetSyncPt(Context).GetMax(Args.Id);
            }
            else
            {
                Args.Value = GetSyncPt(Context).GetMin(Args.Id);
            }

            AMemoryHelper.Write(Context.Memory, OutputPosition, Args);

            return NvResult.Success;
        }

        private int SyncPtWait(ServiceCtx Context, bool Extended)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvHostCtrlSyncPtWait Args = AMemoryHelper.Read<NvHostCtrlSyncPtWait>(Context.Memory, InputPosition);

            NvHostSyncPt SyncPt = GetSyncPt(Context);

            if ((uint)Args.Id >= NvHostSyncPt.SyncPtsCount)
            {
                return NvResult.InvalidInput;
            }

            int Result;

            if (SyncPt.MinCompare(Args.Id, Args.Thresh))
            {
                Result = NvResult.Success;
            }
            else if (Args.Timeout == 0)
            {
                Result = NvResult.TryAgain;
            }
            else
            {
                Context.Ns.Log.PrintDebug(LogClass.ServiceNv, "Waiting syncpt with timeout of " + Args.Timeout + "ms...");

                using (ManualResetEvent WaitEvent = new ManualResetEvent(false))
                {
                    SyncPt.AddWaiter(Args.Thresh, WaitEvent);

                    //Note: Negative (> INT_MAX) timeouts aren't valid on .NET,
                    //in this case we just use the maximum timeout possible.
                    int Timeout = Args.Timeout;

                    if (Timeout < -1)
                    {
                        Timeout = int.MaxValue;
                    }

                    if (Timeout == -1)
                    {
                        WaitEvent.WaitOne();

                        Result = NvResult.Success;
                    }
                    else if (WaitEvent.WaitOne(Timeout))
                    {
                        Result = NvResult.Success;
                    }
                    else
                    {
                        Result = NvResult.TimedOut;
                    }

                    Context.Ns.Log.PrintDebug(LogClass.ServiceNv, "Resuming...");
                }
            }

            if (Extended)
            {
                Context.Memory.WriteInt32(OutputPosition + 0xc, SyncPt.GetMin(Args.Id));
            }

            return Result;
        }

        private int EventWait(ServiceCtx Context, bool Async)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            int Result;

            NvHostCtrlSyncPtWaitEx Args = AMemoryHelper.Read<NvHostCtrlSyncPtWaitEx>(Context.Memory, InputPosition);

            if ((uint)Args.Id >= NvHostSyncPt.SyncPtsCount)
            {
                return NvResult.InvalidInput;
            }

            NvHostSyncPt SyncPt = GetSyncPt(Context);

            if (SyncPt.MinCompare(Args.Id, Args.Thresh))
            {
                Args.Value = SyncPt.GetMin(Args.Id);

                return NvResult.Success;
            }

            if (Args.Timeout == 0)
            {
                if (!Async)
                {
                    Args.Value = 0;
                }

                return NvResult.TryAgain;
            }

            NvHostEvent Event;

            int EventIndex;

            if (Async)
            {
                EventIndex = Args.Value;

                if ((uint)EventIndex >= EventsCount)
                {
                    return NvResult.InvalidInput;
                }

                Event = GetEvents(Context)[EventIndex];
            }
            else
            {
                Event = GetFreeEvent(Context, SyncPt, Args.Id, out EventIndex);
            }

            if (Event == null)
            {
                return NvResult.InvalidInput;
            }

            Event.Id     = Args.Id;
            Event.Thresh = Args.Thresh;

            Event.State = NvHostEventState.Waiting;

            if (!Async)
            {
                Args.Value = ((Args.Id & 0xfff) << 16) | 0x10000000;
            }
            else
            {
                Args.Value = Args.Id << 4;
            }

            Args.Value |= EventIndex;

            Result = NvResult.TryAgain;

            AMemoryHelper.Write(Context.Memory, OutputPosition, Args);

            return Result;
        }

        private NvHostEvent GetFreeEvent(ServiceCtx Context, NvHostSyncPt SyncPt, int Id, out int EventIndex)
        {
            NvHostEvent[] Events = GetEvents(Context);

            EventIndex = EventsCount;

            int NullIndex = EventsCount;

            for (int Index = 0; Index < EventsCount; Index++)
            {
                NvHostEvent Event = Events[Index];

                if (Event != null)
                {
                    if (Event.State == NvHostEventState.Registered ||
                        Event.State == NvHostEventState.Free)
                    {
                        EventIndex = Index;

                        if (Event.Id == Id)
                        {
                            return Event;
                        }
                    }
                }
                else if (NullIndex == EventsCount)
                {
                    NullIndex = Index;
                }
            }

            if (NullIndex < EventsCount)
            {
                EventIndex = NullIndex;

                return Events[NullIndex] = new NvHostEvent();
            }

            if (EventIndex < EventsCount)
            {
                return Events[EventIndex];
            }

            return null;
        }

        public static NvHostSyncPt GetSyncPt(ServiceCtx Context)
        {
            return NvSyncPts.GetOrAdd(Context.Process, (Key) => new NvHostSyncPt());
        }

        private static NvHostEvent[] GetEvents(ServiceCtx Context)
        {
            return EventArrays.GetOrAdd(Context.Process, (Key) => new NvHostEvent[EventsCount]);
        }
    }
}