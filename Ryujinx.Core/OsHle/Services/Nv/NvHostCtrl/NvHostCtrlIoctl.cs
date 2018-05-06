using ChocolArm64.Memory;
using Ryujinx.Core.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.Core.OsHle.Services.Nv.NvHostCtrl
{
    class NvHostCtrlIoctl
    {
        private const int LocksCount = 16;

        private const int EventsCount = 64;

        private static ConcurrentDictionary<Process, NvHostEvent[]> EventArrays;

        private static ConcurrentDictionary<Process, NvHostSyncpt> NvSyncPts;

        static NvHostCtrlIoctl()
        {
            EventArrays = new ConcurrentDictionary<Process, NvHostEvent[]>();

            NvSyncPts = new ConcurrentDictionary<Process, NvHostSyncpt>();
        }

        public static int ProcessIoctl(ServiceCtx Context, int Cmd)
        {
            switch (Cmd & 0xffff)
            {
                case 0x0014: return SyncptRead    (Context);
                case 0x0015: return SyncptIncr    (Context);
                case 0x0016: return SyncptWait    (Context);
                case 0x0019: return SyncptWaitEx  (Context);
                case 0x001a: return SyncptReadMax (Context);
                case 0x001b: return GetConfig     (Context);
                case 0x001d: return EventWait     (Context);
                case 0x001e: return EventWaitAsync(Context);
                case 0x001f: return EventRegister (Context);
            }

            throw new NotImplementedException(Cmd.ToString("x8"));
        }

        private static int SyncptRead(ServiceCtx Context)
        {
            return SyncptReadMinOrMax(Context, Max: false);
        }

        private static int SyncptIncr(ServiceCtx Context)
        {
            long InputPosition = Context.Request.GetBufferType0x21Position();

            int Id = Context.Memory.ReadInt32(InputPosition);

            if ((uint)Id >= NvHostSyncpt.SyncPtsCount)
            {
                return NvResult.InvalidInput;
            }

            GetSyncPt(Context).Increment(Id);

            return NvResult.Success;
        }

        private static int SyncptWait(ServiceCtx Context)
        {
            return SyncptWait(Context, Extended: false);
        }

        private static int SyncptWaitEx(ServiceCtx Context)
        {
            return SyncptWait(Context, Extended: true);
        }

        private static int SyncptReadMax(ServiceCtx Context)
        {
            return SyncptReadMinOrMax(Context, Max: true);
        }

        private static int GetConfig(ServiceCtx Context)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            string Nv   = AMemoryHelper.ReadAsciiString(Context.Memory, InputPosition + 0,    0x41);
            string Name = AMemoryHelper.ReadAsciiString(Context.Memory, InputPosition + 0x41, 0x41);

            Context.Memory.WriteByte(OutputPosition + 0x82, 0);

            Context.Ns.Log.PrintStub(LogClass.ServiceNv, "Stubbed.");

            return NvResult.Success;
        }

        private static int EventWait(ServiceCtx Context)
        {
            return EventWait(Context, Async: false);
        }

        private static int EventWaitAsync(ServiceCtx Context)
        {
            return EventWait(Context, Async: true);
        }

        private static int EventRegister(ServiceCtx Context)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            int EventId = Context.Memory.ReadInt32(InputPosition);

            Context.Ns.Log.PrintInfo(LogClass.ServiceNv, EventId.ToString());

            Context.Ns.Log.PrintStub(LogClass.ServiceNv, "Stubbed.");

            return NvResult.Success;
        }

        private static int SyncptReadMinOrMax(ServiceCtx Context, bool Max)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvHostCtrlSyncptRead Args = AMemoryHelper.Read<NvHostCtrlSyncptRead>(Context.Memory, InputPosition);

            if ((uint)Args.Id >= NvHostSyncpt.SyncPtsCount)
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

        private static int SyncptWait(ServiceCtx Context, bool Extended)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvHostCtrlSyncptWait Args = AMemoryHelper.Read<NvHostCtrlSyncptWait>(Context.Memory, InputPosition);

            NvHostSyncpt SyncPt = GetSyncPt(Context);

            if ((uint)Args.Id >= NvHostSyncpt.SyncPtsCount)
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
                }

                Context.Ns.Log.PrintDebug(LogClass.ServiceNv, "Resuming...");
            }

            if (Extended)
            {
                Context.Memory.WriteInt32(OutputPosition + 0xc, SyncPt.GetMin(Args.Id));
            }

            return Result;
        }

        private static int EventWait(ServiceCtx Context, bool Async)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvHostCtrlSyncptWaitEx Args = AMemoryHelper.Read<NvHostCtrlSyncptWaitEx>(Context.Memory, InputPosition);

            if ((uint)Args.Id >= NvHostSyncpt.SyncPtsCount)
            {
                return NvResult.InvalidInput;
            }

            void WriteArgs()
            {
                AMemoryHelper.Write(Context.Memory, OutputPosition, Args);
            }

            NvHostSyncpt SyncPt = GetSyncPt(Context);

            Context.Ns.Log.PrintInfo(LogClass.ServiceNv, Args.Id + " " + Args.Thresh + " " + Args.Timeout + " " + Args.Value + " " + Async + " " + SyncPt.GetMin(Args.Id));

            if (SyncPt.MinCompare(Args.Id, Args.Thresh))
            {
                Args.Value = SyncPt.GetMin(Args.Id);

                WriteArgs();

                return NvResult.Success;
            }

            if (!Async)
            {
                Args.Value = 0;
            }

            if (Args.Timeout == 0)
            {
                WriteArgs();

                return NvResult.TryAgain;
            }

            NvHostEvent Event;

            int Result, EventIndex;

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

            if (Event != null &&
               (Event.State == NvHostEventState.Registered ||
                Event.State == NvHostEventState.Free))
            {
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
            }
            else
            {
                Result = NvResult.InvalidInput;
            }

            WriteArgs();

            return Result;
        }

        private static NvHostEvent GetFreeEvent(
            ServiceCtx   Context,
            NvHostSyncpt SyncPt,
            int          Id,
            out int      EventIndex)
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

        public static NvHostSyncpt GetSyncPt(ServiceCtx Context)
        {
            return NvSyncPts.GetOrAdd(Context.Process, (Key) => new NvHostSyncpt());
        }

        private static NvHostEvent[] GetEvents(ServiceCtx Context)
        {
            return EventArrays.GetOrAdd(Context.Process, (Key) => new NvHostEvent[EventsCount]);
        }
    }
}