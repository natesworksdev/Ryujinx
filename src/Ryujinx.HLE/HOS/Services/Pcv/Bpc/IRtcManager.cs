using System;

namespace Ryujinx.HLE.HOS.Services.Pcv.Bpc
{
    [Service("bpc:r")] //  1.0.0 - 8.1.0
    class IRtcManager : IpcService
    {
#pragma warning disable IDE0060
        public IRtcManager(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(0)]
        // GetRtcTime() -> u64
        public static ResultCode GetRtcTime(ServiceCtx context)
        {
            ResultCode result = GetExternalRtcValue(out ulong rtcValue);

            if (result == ResultCode.Success)
            {
                context.ResponseData.Write(rtcValue);
            }

            return result;
        }

        public static ResultCode GetExternalRtcValue(out ulong rtcValue)
        {
            // TODO: emulate MAX77620/MAX77812 RTC
            rtcValue = (ulong)(DateTime.Now.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;

            return ResultCode.Success;
        }
    }
}
