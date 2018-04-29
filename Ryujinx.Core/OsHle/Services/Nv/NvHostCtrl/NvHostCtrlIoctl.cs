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

        private static ConcurrentDictionary<Process, IdDictionary> NvSyncPts;

        static NvHostCtrlIoctl()
        {
            EventArrays = new ConcurrentDictionary<Process, NvHostEvent[]>();

            NvSyncPts = new ConcurrentDictionary<Process, IdDictionary>();
        }

        private int SyncPtRead(ServiceCtx Context)
        {
            return SyncPtReadMinOrMax(Context, Max: false);
        }

        private int SyncPtIncr(ServiceCtx Context)
        {
            long InputPosition = Context.Request.GetBufferType0x21Position();

            int Id = Context.Memory.ReadInt32(InputPosition);

            GetOrAddNvSyncPt(Context, Id).Increment();

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

        private int SyncPtReadMinOrMax(ServiceCtx Context, bool Max)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvHostCtrlSyncPtRead Args = AMemoryHelper.Read<NvHostCtrlSyncPtRead>(Context.Memory, InputPosition);

            if (Max)
            {
                Args.Value = GetOrAddNvSyncPt(Context, Args.Id).CounterMax;
            }
            else
            {
                Args.Value = GetOrAddNvSyncPt(Context, Args.Id).CounterMin;
            }

            AMemoryHelper.Write(Context.Memory, OutputPosition, Args);

            return NvResult.Success;
        }

        private int SyncPtWait(ServiceCtx Context, bool Extended)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvHostCtrlSyncPtWait Args = AMemoryHelper.Read<NvHostCtrlSyncPtWait>(Context.Memory, InputPosition);

            NvSyncPt SyncPt = GetOrAddNvSyncPt(Context, Args.Id);

            void WriteSyncPtMin()
            {
                if (Extended)
                {
                    Context.Memory.WriteInt32(OutputPosition + 0xc, SyncPt.CounterMin);
                }
            }

            if (SyncPt.MinCompare(Args.Thresh))
            {
                WriteSyncPtMin();

                return NvResult.Success;
            }

            if (Args.Timeout == 0)
            {
                return NvResult.TryAgain;
            }

            Context.Ns.Log.PrintDebug(LogClass.ServiceNv, "Waiting syncpt with timeout of " + Args.Timeout + "ms...");

            bool Signaled = true;

            using (ManualResetEvent WaitEvent = new ManualResetEvent(false))
            {
                SyncPt.AddWaiter(Args.Thresh, WaitEvent);

                if (Args.Timeout != -1)
                {
                    //Note: Negative (> INT_MAX) timeouts aren't valid on .NET,
                    //in this case we just use the maximum timeout possible.
                    int Timeout = Args.Timeout;

                    if (Timeout < 0)
                    {
                        Timeout = int.MaxValue;
                    }

                    Signaled = WaitEvent.WaitOne(Timeout);
                }
                else
                {
                    WaitEvent.WaitOne();
                }
            }

            WriteSyncPtMin();

            if (Signaled)
            {
                Context.Ns.Log.PrintDebug(LogClass.ServiceNv, "Resuming...");
            }
            else
            {
                Context.Ns.Log.PrintDebug(LogClass.ServiceNv, "Timed out, resuming...");
            }

            return Signaled
                ? NvResult.Success
                : NvResult.TimedOut;
        }

        private int EventWait(ServiceCtx Context, bool IsEvent)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            int Result, Value;

            NvHostCtrlSyncPtWait Args = AMemoryHelper.Read<NvHostCtrlSyncPtWait>(Context.Memory, InputPosition);

            if (Args.Timeout == 0)
            {
                return NvResult.TryAgain;
            }

            if (IsEvent || Args.Timeout == -1)
            {
                int EventIndex = GetFreeEvent(Context, Args.Id, out NvHostEvent Event);

                if (EventIndex == EventsCount)
                {
                    return NvResult.OutOfMemory;
                }

                Event.Id     = Args.Id;
                Event.Thresh = Args.Thresh;

                Event.Free = false;

                if (IsEvent)
                {
                    Value = ((Args.Id & 0xfff) << 16) | 0x10000000;
                }
                else
                {
                    Value = Args.Id << 4;
                }

                Value |= EventIndex;

                Result = NvResult.TryAgain;
            }
            else
            {
                Context.Ns.Log.PrintDebug(LogClass.ServiceNv, "Waiting syncpt with timeout of " + Args.Timeout + "ms...");

                bool Signaled = true;

                NvSyncPt SyncPt = GetOrAddNvSyncPt(Context, Args.Id);

                using (ManualResetEvent WaitEvent = new ManualResetEvent(false))
                {
                    SyncPt.AddWaiter(Args.Thresh, WaitEvent);

                    if (Args.Timeout != -1)
                    {
                        //Note: Negative (> INT_MAX) timeouts aren't valid on .NET,
                        //in this case we just use the maximum timeout possible.
                        int Timeout = Args.Timeout;

                        if (Timeout < 0)
                        {
                            Timeout = int.MaxValue;
                        }

                        Signaled = WaitEvent.WaitOne(Timeout);
                    }
                    else
                    {
                        WaitEvent.WaitOne();
                    }
                }

                if (Signaled)
                {
                    Context.Ns.Log.PrintDebug(LogClass.ServiceNv, "Resuming...");
                }
                else
                {
                    Context.Ns.Log.PrintDebug(LogClass.ServiceNv, "Timed out, resuming...");
                }

                Value = SyncPt.CounterMin;

                Result = Signaled
                    ? NvResult.Success
                    : NvResult.TimedOut;
            }

            Context.Memory.WriteInt32(OutputPosition + 0xc, Value);

            return Result;
        }

        private NvSyncPt GetOrAddNvSyncPt(ServiceCtx Context, int Id)
        {
            NvSyncPt SyncPt = GetNvSyncPt(Context, Id);

            if (SyncPt == null)
            {
                IdDictionary SyncPts = NvSyncPts.GetOrAdd(Context.Process, (Key) => new IdDictionary());

                SyncPt = new NvSyncPt();

                SyncPts.Add(Id, SyncPt);
            }

            return SyncPt;
        }

        public NvSyncPt GetNvSyncPt(ServiceCtx Context, int Id)
        {
            if (NvSyncPts.TryGetValue(Context.Process, out IdDictionary SyncPts))
            {
                return SyncPts.GetData<NvSyncPt>(Id);
            }

            return null;
        }

        private int GetFreeEvent(ServiceCtx Context, int Id, out NvHostEvent Event)
        {
            NvHostEvent[] Events = GetEvents(Context);

            int NullIndex = EventsCount;
            int FreeIndex = EventsCount;

            for (int Index = 0; Index < EventsCount; Index++)
            {
                Event = Events[Index];

                if (Event != null)
                {
                    if (Event.Free && MinCompare(Context,
                                                 Event.Id,
                                                 Event.Thresh))
                    {
                        if (Event.Id == Id)
                        {
                            return Index;
                        }

                        FreeIndex = Index;
                    }
                }
                else if (NullIndex == EventsCount)
                {
                    NullIndex = Index;
                }
            }

            if (NullIndex < EventsCount)
            {
                Events[NullIndex] = Event = new NvHostEvent();

                return NullIndex;
            }

            Event = FreeIndex < EventsCount ? Events[FreeIndex] : null;

            return FreeIndex;
        }

        private bool MinCompare(ServiceCtx Context, int Id, int Threshold)
        {
            //TODO
            return false;
        }

        private NvHostEvent[] GetEvents(ServiceCtx Context)
        {
            return EventArrays.GetOrAdd(Context.Process, (Key) => new NvHostEvent[EventsCount]);
        }
    }
}