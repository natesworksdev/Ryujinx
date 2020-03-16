using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gpu.Synchronization;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl.Types;
using Ryujinx.HLE.HOS.Services.Nv.Types;
using Ryujinx.HLE.HOS.Services.Settings;

using System;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl
{
    internal class NvHostCtrlDeviceFile : NvDeviceFile
    {
        private bool          _isProductionMode;
        private Switch        _device;

        public NvHostCtrlDeviceFile(ServiceCtx context) : base(context)
        {
            if (NxSettings.Settings.TryGetValue("nv!rmos_set_production_mode", out object productionModeSetting))
            {
                _isProductionMode = ((string)productionModeSetting) != "0"; // Default value is ""
            }
            else
            {
                _isProductionMode = true;
            }

            _device = context.Device;
        }

        public override NvInternalResult Ioctl(NvIoctl command, Span<byte> arguments)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            if (command.Type == NvIoctl.NvHostCustomMagic)
            {
                switch (command.Number)
                {
                    case 0x14:
                        result = CallIoctlMethod<NvFence>(SyncptRead, arguments);
                        break;
                    case 0x15:
                        result = CallIoctlMethod<uint>(SyncptIncr, arguments);
                        break;
                    case 0x16:
                        result = CallIoctlMethod<SyncptWaitArguments>(SyncptWait, arguments);
                        break;
                    case 0x19:
                        result = CallIoctlMethod<SyncptWaitExArguments>(SyncptWaitEx, arguments);
                        break;
                    case 0x1a:
                        result = CallIoctlMethod<NvFence>(SyncptReadMax, arguments);
                        break;
                    case 0x1b:
                        // As Marshal cannot handle unaligned arrays, we do everything by hand here.
                        GetConfigurationArguments configArgument = GetConfigurationArguments.FromSpan(arguments);
                        result = GetConfig(configArgument);

                        if (result == NvInternalResult.Success)
                        {
                            configArgument.CopyTo(arguments);
                        }
                        break;
                    case 0x1c:
                        result = CallIoctlMethod<uint>(EventSignal, arguments);
                        break;
                    case 0x1d:
                        result = CallIoctlMethod<EventWaitArguments>(EventWait, arguments);
                        break;
                    case 0x1e:
                        result = CallIoctlMethod<EventWaitArguments>(EventWaitAsync, arguments);
                        break;
                    case 0x1f:
                        result = CallIoctlMethod<uint>(EventRegister, arguments);
                        break;
                    case 0x20:
                        result = CallIoctlMethod<uint>(EventUnregister, arguments);
                        break;
                    case 0x21:
                        result = CallIoctlMethod<ulong>(EventKill, arguments);
                        break;
                }
            }

            return result;
        }

        public override NvInternalResult QueryEvent(out int eventHandle, uint eventId)
        {
            KEvent targetEvent = _device.System.HostSyncpoint.QueryEvent(eventId);

            if (targetEvent != null)
            {
                if (Owner.HandleTable.GenerateHandle(targetEvent.ReadableEvent, out eventHandle) != KernelResult.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }
            else
            {
                eventHandle = 0;

                return NvInternalResult.InvalidInput;
            }

            return NvInternalResult.Success;
        }

        private NvInternalResult SyncptRead(ref NvFence arguments)
        {
            return SyncptReadMinOrMax(ref arguments, max: false);
        }

        private NvInternalResult SyncptIncr(ref uint id)
        {
            if (id >= Synchronization.MaxHarwareSyncpoints)
            {
                return NvInternalResult.InvalidInput;
            }

            _device.System.HostSyncpoint.Increment(id);

            return NvInternalResult.Success;
        }

        private NvInternalResult SyncptWait(ref SyncptWaitArguments arguments)
        {
            uint dummyValue = 0;

            return EventWait(ref arguments.Fence, ref dummyValue, arguments.Timeout, async: false);
        }

        private NvInternalResult SyncptWaitEx(ref SyncptWaitExArguments arguments)
        {
            return EventWait(ref arguments.Input.Fence, ref arguments.Value, arguments.Input.Timeout, async: false);
        }

        private NvInternalResult SyncptReadMax(ref NvFence arguments)
        {
            return SyncptReadMinOrMax(ref arguments, max: true);
        }

        private NvInternalResult GetConfig(GetConfigurationArguments arguments)
        {
            if (!_isProductionMode && NxSettings.Settings.TryGetValue($"{arguments.Domain}!{arguments.Parameter}".ToLower(), out object nvSetting))
            {
                byte[] settingBuffer = new byte[0x101];

                if (nvSetting is string stringValue)
                {
                    if (stringValue.Length > 0x100)
                    {
                        Logger.PrintError(LogClass.ServiceNv, $"{arguments.Domain}!{arguments.Parameter} String value size is too big!");
                    }
                    else
                    {
                        settingBuffer = Encoding.ASCII.GetBytes(stringValue + "\0");
                    }
                }
                else if (nvSetting is int intValue)
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

                Logger.PrintDebug(LogClass.ServiceNv, $"Got setting {arguments.Domain}!{arguments.Parameter}");

                arguments.Configuration = settingBuffer;

                return NvInternalResult.Success;
            }

            // NOTE: This actually return NotAvailableInProduction but this is directly translated as a InvalidInput before returning the ioctl.
            //return NvInternalResult.NotAvailableInProduction;
            return NvInternalResult.InvalidInput;
        }

        private NvInternalResult EventWait(ref EventWaitArguments arguments)
        {
            return EventWait(ref arguments.Fence, ref arguments.Value, arguments.Timeout, async: false);
        }

        private NvInternalResult EventWaitAsync(ref EventWaitArguments arguments)
        {
            return EventWait(ref arguments.Fence, ref arguments.Value, arguments.Timeout, async: true);
        }

        private NvInternalResult EventRegister(ref uint userEventId)
        {
            return _device.System.HostSyncpoint.RegisterEvent(userEventId);
        }

        private NvInternalResult EventUnregister(ref uint userEventId)
        {
            return _device.System.HostSyncpoint.UnregisterEvent(userEventId);
        }

        private NvInternalResult EventKill(ref ulong eventMask)
        {
            return _device.System.HostSyncpoint.KillEvent(eventMask);
        }

        private NvInternalResult EventSignal(ref uint userEventId)
        {
            return _device.System.HostSyncpoint.SignalEvent(userEventId & ushort.MaxValue);
        }

        private NvInternalResult SyncptReadMinOrMax(ref NvFence arguments, bool max)
        {
            if (arguments.Id >= Synchronization.MaxHarwareSyncpoints)
            {
                return NvInternalResult.InvalidInput;
            }

            if (max)
            {
                arguments.Value = _device.System.HostSyncpoint.ReadSyncpointMaxValue(arguments.Id);
            }
            else
            {
                arguments.Value = _device.System.HostSyncpoint.ReadSyncpointValue(arguments.Id);
            }

            return NvInternalResult.Success;
        }

        private NvInternalResult EventWait(ref NvFence fence, ref uint value, int timeout, bool async)
        {
            if (fence.Id >= Synchronization.MaxHarwareSyncpoints)
            {
                return NvInternalResult.InvalidInput;
            }

            // First try to check if the syncpoint is already expired on the CPU side
            if (_device.System.HostSyncpoint.IsSyncpointExpired(fence.Id, fence.Value))
            {
                value = _device.System.HostSyncpoint.ReadSyncpointMinValue(fence.Id);

                return NvInternalResult.Success;
            }

            // Try to invalidate the CPU cache and check for expiration again.
            uint newCachedSyncpointValue = _device.System.HostSyncpoint.UpdateMin(fence.Id);

            // Has the fence already expired?
            if (_device.System.HostSyncpoint.IsSyncpointExpired(fence.Id, fence.Value))
            {
                value = newCachedSyncpointValue;

                return NvInternalResult.Success;
            }

            // If the timeout is 0, directly return.
            if (timeout == 0)
            {
                return NvInternalResult.TryAgain;
            }

            // The syncpoint value isn't at the fence yet, we need to wait.

            if (!async)
            {
                value = 0;
            }

            NvHostEvent Event;

            NvInternalResult result;

            uint eventIndex;

            if (async)
            {
                eventIndex = value;

                if (eventIndex >= NvHostSyncpt.EventsCount)
                {
                    return NvInternalResult.InvalidInput;
                }

                Event = _device.System.HostSyncpoint.Events[eventIndex];
            }
            else
            {
                Event = _device.System.HostSyncpoint.GetFreeEvent(fence.Id, out eventIndex);
            }

            if (Event != null &&
               (Event.State == NvHostEventState.Availaible ||
                Event.State == NvHostEventState.Signaled   ||
                Event.State == NvHostEventState.Cancelled))
            {
                Event.Wait(_device.Gpu, fence);

                if (!async)
                {
                    value = ((fence.Id & 0xfff) << 16) | 0x10000000;
                }
                else
                {
                    value = fence.Id << 4;
                }

                value |= eventIndex;

                result = NvInternalResult.TryAgain;
            }
            else
            {
                Logger.PrintError(LogClass.ServiceNv, $"Invalid Event at index {eventIndex} (async: {async})");

                if (Event != null)
                {
                    Logger.PrintError(LogClass.ServiceNv, Event.DumpState(_device.Gpu));
                }

                result = NvInternalResult.InvalidInput;
            }

            return result;
        }

        public override void Close() { }
    }
}
