using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostChannel.Types;
using Ryujinx.HLE.HOS.Services.OsTypes;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostChannel
{
    internal class NvHostGpuDeviceFile : NvHostChannelDeviceFile
    {
        private SystemEventType _smExceptionBptIntReportEvent;
        private SystemEventType _smExceptionBptPauseReportEvent;
        private SystemEventType _errorNotifierEvent;

        public NvHostGpuDeviceFile(ServiceCtx context, IAddressSpaceManager memory, long owner) : base(context, memory, owner)
        {
            Os.CreateSystemEvent(out _smExceptionBptIntReportEvent, EventClearMode.AutoClear, true);
            Os.CreateSystemEvent(out _smExceptionBptPauseReportEvent, EventClearMode.AutoClear, true);
            Os.CreateSystemEvent(out _errorNotifierEvent, EventClearMode.AutoClear, true);
        }

        public override NvInternalResult Ioctl2(NvIoctl command, Span<byte> arguments, Span<byte> inlineInBuffer)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            if (command.Type == NvIoctl.NvHostMagic)
            {
                switch (command.Number)
                {
                    case 0x1b:
                        result = CallIoctlMethod<SubmitGpfifoArguments, ulong>(SubmitGpfifoEx, arguments, inlineInBuffer);
                        break;
                }
            }

            return result;
        }

        public override NvInternalResult QueryEvent(out int eventHandle, uint eventId)
        {
            // TODO: accurately represent and implement those events.
            eventHandle = 0;

            switch (eventId)
            {
                case 0x1:
                    eventHandle = Os.GetReadableHandleOfSystemEvent(ref _smExceptionBptIntReportEvent);
                    break;
                case 0x2:
                    eventHandle = Os.GetReadableHandleOfSystemEvent(ref _smExceptionBptPauseReportEvent);
                    break;
                case 0x3:
                    eventHandle = Os.GetReadableHandleOfSystemEvent(ref _errorNotifierEvent);
                    break;
            }

            return eventHandle == 0 ? NvInternalResult.InvalidInput : NvInternalResult.Success;
        }

        private NvInternalResult SubmitGpfifoEx(ref SubmitGpfifoArguments arguments, Span<ulong> inlineData)
        {
            return SubmitGpfifo(ref arguments, inlineData);
        }
    }
}
