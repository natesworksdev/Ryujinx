using Ryujinx.Common;
using Ryujinx.HLE.HOS.Services.Time.Clock;

namespace Ryujinx.HLE.HOS.Services.Time
{
    class ISteadyClock : IpcService
    {

        [Command(0)]
        // GetCurrentTimePoint() -> nn::time::SteadyClockTimePoint
        public ResultCode GetCurrentTimePoint(ServiceCtx context)
        {
            SteadyClockTimePoint currentTimePoint = SteadyClockCore.Instance.GetCurrentTimePoint(context.Thread);

            context.ResponseData.WriteStruct(currentTimePoint);

            return ResultCode.Success;
        }

        [Command(1)]
        // GetTestOffset() -> nn::TimeSpanType
        public ResultCode GetTestOffset(ServiceCtx context)
        {
            context.ResponseData.WriteStruct(SteadyClockCore.Instance.GetTestOffset());

            return ResultCode.Success;
        }

        [Command(2)]
        // SetTestOffset(nn::TimeSpanType)
        public ResultCode SetTestOffset(ServiceCtx context)
        {
            TimeSpanType testOffset = context.RequestData.ReadStruct<TimeSpanType>();

            SteadyClockCore.Instance.SetTestOffset(testOffset);

            return 0;
        }

        [Command(200)] // 3.0.0+
        // GetInternalOffset() -> nn::TimeSpanType
        public long GetInternalOffset(ServiceCtx context)
        {
            context.ResponseData.WriteStruct(SteadyClockCore.Instance.GetInternalOffset());

            return 0;
        }

        [Command(201)] // 3.0.0-3.0.2
        // SetInternalOffset(nn::TimeSpanType)
        public long SetInternalOffset(ServiceCtx context)
        {
            TimeSpanType internalOffset = context.RequestData.ReadStruct<TimeSpanType>();

            SteadyClockCore.Instance.SetInternalOffset(internalOffset);

            return ResultCode.Success;
        }
    }
}