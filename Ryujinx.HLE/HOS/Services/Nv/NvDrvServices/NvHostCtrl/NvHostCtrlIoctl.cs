using ARMeilleure.Memory;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services.Settings;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl
{
    class NvHostCtrlIoctl
    {
        private static ConcurrentDictionary<KProcess, NvHostCtrlUserCtx> _userCtxs;

        private static bool _isProductionMode = true;

        static NvHostCtrlIoctl()
        {
            _userCtxs = new ConcurrentDictionary<KProcess, NvHostCtrlUserCtx>();

            if (NxSettings.Settings.TryGetValue("nv!rmos_set_production_mode", out object productionModeSetting))
            {
                _isProductionMode = ((string)productionModeSetting) != "0"; // Default value is ""
            }
        }

        public static NvInternalResult ProcessIoctl(ServiceCtx context, int cmd)
        {
            switch (cmd & 0xffff)
            {
                case 0x0014: return SyncptRead    (context);
                case 0x0015: return SyncptIncr    (context);
                case 0x0016: return SyncptWait    (context);
                case 0x0019: return SyncptWaitEx  (context);
                case 0x001a: return SyncptReadMax (context);
                case 0x001b: return GetConfig     (context);
                case 0x001d: return EventWait     (context);
                case 0x001e: return EventWaitAsync(context);
                case 0x001f: return EventRegister (context);
            }

            throw new NotImplementedException(cmd.ToString("x8"));
        }

        private static NvInternalResult SyncptRead(ServiceCtx context)
        {
            return SyncptReadMinOrMax(context, max: false);
        }

        private static NvInternalResult SyncptIncr(ServiceCtx context)
        {
            long inputPosition = context.Request.GetBufferType0x21().Position;

            int id = context.Memory.ReadInt32(inputPosition);

            if ((uint)id >= NvHostSyncpt.SyncptsCount)
            {
                return NvInternalResult.InvalidInput;
            }

            GetUserCtx(context).Syncpt.Increment(id);

            return NvInternalResult.Success;
        }

        private static NvInternalResult SyncptWait(ServiceCtx context)
        {
            return SyncptWait(context, extended: false);
        }

        private static NvInternalResult SyncptWaitEx(ServiceCtx context)
        {
            return SyncptWait(context, extended: true);
        }

        private static NvInternalResult SyncptReadMax(ServiceCtx context)
        {
            return SyncptReadMinOrMax(context, max: true);
        }

        private static NvInternalResult GetConfig(ServiceCtx context)
        {
            if (!_isProductionMode)
            {
                long inputPosition  = context.Request.GetBufferType0x21().Position;
                long outputPosition = context.Request.GetBufferType0x22().Position;

                string domain = MemoryHelper.ReadAsciiString(context.Memory, inputPosition + 0, 0x41);
                string name   = MemoryHelper.ReadAsciiString(context.Memory, inputPosition + 0x41, 0x41);

                if (NxSettings.Settings.TryGetValue($"{domain}!{name}", out object nvSetting))
                {
                    byte[] settingBuffer = new byte[0x101];

                    if (nvSetting is string stringValue)
                    {
                        if (stringValue.Length > 0x100)
                        {
                            Logger.PrintError(LogClass.ServiceNv, $"{domain}!{name} String value size is too big!");
                        }
                        else
                        {
                            settingBuffer = Encoding.ASCII.GetBytes(stringValue + "\0");
                        }
                    }

                    if (nvSetting is int intValue)
                    {
                        settingBuffer = BitConverter.GetBytes(intValue);
                    }
                    else if (nvSetting is bool boolValue)
                    {
                        settingBuffer[0] = boolValue ? (byte)1 : (byte)0;
                    }
                    else
                    {
                        throw new NotImplementedException(nvSetting.GetType().Name);
                    }

                    context.Memory.WriteBytes(outputPosition + 0x82, settingBuffer);

                    Logger.PrintDebug(LogClass.ServiceNv, $"Got setting {domain}!{name}");
                }

                return NvInternalResult.Success;
            }

            // NOTE: This actually return NotAvailableInProduction but this is directly translated as a InvalidInput before returning the ioctl.
            //return NvInternalResult.NotAvailableInProduction;
            return NvInternalResult.InvalidInput;
        }

        private static NvInternalResult EventWait(ServiceCtx context)
        {
            return EventWait(context, async: false);
        }

        private static NvInternalResult EventWaitAsync(ServiceCtx context)
        {
            return EventWait(context, async: true);
        }

        private static NvInternalResult EventRegister(ServiceCtx context)
        {
            long inputPosition  = context.Request.GetBufferType0x21().Position;
            long outputPosition = context.Request.GetBufferType0x22().Position;

            int eventId = context.Memory.ReadInt32(inputPosition);

            Logger.PrintStub(LogClass.ServiceNv);

            return NvInternalResult.Success;
        }

        private static NvInternalResult SyncptReadMinOrMax(ServiceCtx context, bool max)
        {
            long inputPosition  = context.Request.GetBufferType0x21().Position;
            long outputPosition = context.Request.GetBufferType0x22().Position;

            NvHostCtrlSyncptRead args = MemoryHelper.Read<NvHostCtrlSyncptRead>(context.Memory, inputPosition);

            if ((uint)args.Id >= NvHostSyncpt.SyncptsCount)
            {
                return NvInternalResult.InvalidInput;
            }

            if (max)
            {
                args.Value = GetUserCtx(context).Syncpt.GetMax(args.Id);
            }
            else
            {
                args.Value = GetUserCtx(context).Syncpt.GetMin(args.Id);
            }

            MemoryHelper.Write(context.Memory, outputPosition, args);

            return NvInternalResult.Success;
        }

        private static NvInternalResult SyncptWait(ServiceCtx context, bool extended)
        {
            long inputPosition  = context.Request.GetBufferType0x21().Position;
            long outputPosition = context.Request.GetBufferType0x22().Position;

            NvHostCtrlSyncptWait args = MemoryHelper.Read<NvHostCtrlSyncptWait>(context.Memory, inputPosition);

            NvHostSyncpt syncpt = GetUserCtx(context).Syncpt;

            if ((uint)args.Id >= NvHostSyncpt.SyncptsCount)
            {
                return NvInternalResult.InvalidInput;
            }

            NvInternalResult result;

            if (syncpt.MinCompare(args.Id, args.Thresh))
            {
                result = NvInternalResult.Success;
            }
            else if (args.Timeout == 0)
            {
                result = NvInternalResult.TryAgain;
            }
            else
            {
                Logger.PrintDebug(LogClass.ServiceNv, "Waiting syncpt with timeout of " + args.Timeout + "ms...");

                using (ManualResetEvent waitEvent = new ManualResetEvent(false))
                {
                    syncpt.AddWaiter(args.Thresh, waitEvent);

                    // Note: Negative (> INT_MAX) timeouts aren't valid on .NET,
                    // in this case we just use the maximum timeout possible.
                    int timeout = args.Timeout;

                    if (timeout < -1)
                    {
                        timeout = int.MaxValue;
                    }

                    if (timeout == -1)
                    {
                        waitEvent.WaitOne();

                        result = NvInternalResult.Success;
                    }
                    else if (waitEvent.WaitOne(timeout))
                    {
                        result = NvInternalResult.Success;
                    }
                    else
                    {
                        result = NvInternalResult.TimedOut;
                    }
                }

                Logger.PrintDebug(LogClass.ServiceNv, "Resuming...");
            }

            if (extended)
            {
                context.Memory.WriteInt32(outputPosition + 0xc, syncpt.GetMin(args.Id));
            }

            return result;
        }

        private static NvInternalResult EventWait(ServiceCtx context, bool async)
        {
            long inputPosition  = context.Request.GetBufferType0x21().Position;
            long outputPosition = context.Request.GetBufferType0x22().Position;

            NvHostCtrlSyncptWaitEx args = MemoryHelper.Read<NvHostCtrlSyncptWaitEx>(context.Memory, inputPosition);

            if ((uint)args.Id >= NvHostSyncpt.SyncptsCount)
            {
                return NvInternalResult.InvalidInput;
            }

            void WriteArgs()
            {
                MemoryHelper.Write(context.Memory, outputPosition, args);
            }

            NvHostSyncpt syncpt = GetUserCtx(context).Syncpt;

            if (syncpt.MinCompare(args.Id, args.Thresh))
            {
                args.Value = syncpt.GetMin(args.Id);

                WriteArgs();

                return NvInternalResult.Success;
            }

            if (!async)
            {
                args.Value = 0;
            }

            if (args.Timeout == 0)
            {
                WriteArgs();

                return NvInternalResult.TryAgain;
            }

            NvHostEvent Event;

            NvInternalResult result;

            int eventIndex;

            if (async)
            {
                eventIndex = args.Value;

                if ((uint)eventIndex >= NvHostCtrlUserCtx.EventsCount)
                {
                    return NvInternalResult.InvalidInput;
                }

                Event = GetUserCtx(context).Events[eventIndex];
            }
            else
            {
                Event = GetFreeEvent(context, syncpt, args.Id, out eventIndex);
            }

            if (Event != null &&
               (Event.State == NvHostEventState.Registered ||
                Event.State == NvHostEventState.Free))
            {
                Event.Id     = args.Id;
                Event.Thresh = args.Thresh;

                Event.State = NvHostEventState.Waiting;

                if (!async)
                {
                    args.Value = ((args.Id & 0xfff) << 16) | 0x10000000;
                }
                else
                {
                    args.Value = args.Id << 4;
                }

                args.Value |= eventIndex;

                result = NvInternalResult.TryAgain;
            }
            else
            {
                result = NvInternalResult.InvalidInput;
            }

            WriteArgs();

            return result;
        }

        private static NvHostEvent GetFreeEvent(
            ServiceCtx   context,
            NvHostSyncpt syncpt,
            int          id,
            out int      eventIndex)
        {
            NvHostEvent[] events = GetUserCtx(context).Events;

            eventIndex = NvHostCtrlUserCtx.EventsCount;

            int nullIndex = NvHostCtrlUserCtx.EventsCount;

            for (int index = 0; index < NvHostCtrlUserCtx.EventsCount; index++)
            {
                NvHostEvent Event = events[index];

                if (Event != null)
                {
                    if (Event.State == NvHostEventState.Registered ||
                        Event.State == NvHostEventState.Free)
                    {
                        eventIndex = index;

                        if (Event.Id == id)
                        {
                            return Event;
                        }
                    }
                }
                else if (nullIndex == NvHostCtrlUserCtx.EventsCount)
                {
                    nullIndex = index;
                }
            }

            if (nullIndex < NvHostCtrlUserCtx.EventsCount)
            {
                eventIndex = nullIndex;

                return events[nullIndex] = new NvHostEvent();
            }

            if (eventIndex < NvHostCtrlUserCtx.EventsCount)
            {
                return events[eventIndex];
            }

            return null;
        }

        public static NvHostCtrlUserCtx GetUserCtx(ServiceCtx context)
        {
            return _userCtxs.GetOrAdd(context.Process, (key) => new NvHostCtrlUserCtx());
        }

        public static void UnloadProcess(KProcess process)
        {
            _userCtxs.TryRemove(process, out _);
        }
    }
}
