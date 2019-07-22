using Ryujinx.Common;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Time.Clock;
using Ryujinx.HLE.HOS.Services.Time.TimeZone;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Time
{
    [Service("time:a", TimePermissions.Applet)]
    [Service("time:s", TimePermissions.System)]
    [Service("time:u", TimePermissions.User)]
    class IStaticService : IpcService
    {
        private TimePermissions _permissions;

        private int _timeSharedMemoryNativeHandle = 0;

        private static readonly DateTime StartupDate = DateTime.UtcNow;

        public IStaticService(ServiceCtx context, TimePermissions permissions)
        {
            _permissions = permissions;
        }

        [Command(0)]
        // GetStandardUserSystemClock() -> object<nn::timesrv::detail::service::ISystemClock>
        public ResultCode GetStandardUserSystemClock(ServiceCtx context)
        {
            MakeObject(context, new ISystemClock(StandardUserSystemClockCore.Instance, (_permissions & TimePermissions.UserSystemClockWritableMask) != 0));

            return ResultCode.Success;
        }

        [Command(1)]
        // GetStandardNetworkSystemClock() -> object<nn::timesrv::detail::service::ISystemClock>
        public ResultCode GetStandardNetworkSystemClock(ServiceCtx context)
        {
            MakeObject(context, new ISystemClock(StandardNetworkSystemClockCore.Instance, (_permissions & TimePermissions.NetworkSystemClockWritableMask) != 0));

            return ResultCode.Success;
        }

        [Command(2)]
        // GetStandardSteadyClock() -> object<nn::timesrv::detail::service::ISteadyClock>
        public ResultCode GetStandardSteadyClock(ServiceCtx context)
        {
            MakeObject(context, new ISteadyClock());

            return ResultCode.Success;
        }

        [Command(3)]
        // GetTimeZoneService() -> object<nn::timesrv::detail::service::ITimeZoneService>
        public ResultCode GetTimeZoneService(ServiceCtx context)
        {
            MakeObject(context, new ITimeZoneService());

            return ResultCode.Success;
        }

        [Command(4)]
        // GetStandardLocalSystemClock() -> object<nn::timesrv::detail::service::ISystemClock>
        public ResultCode GetStandardLocalSystemClock(ServiceCtx context)
        {
            MakeObject(context, new ISystemClock(StandardLocalSystemClockCore.Instance, (_permissions & TimePermissions.LocalSystemClockWritableMask) != 0));

            return ResultCode.Success;
        }

        [Command(5)] // 4.0.0+
        // GetEphemeralNetworkSystemClock() -> object<nn::timesrv::detail::service::ISystemClock>
        public ResultCode GetEphemeralNetworkSystemClock(ServiceCtx context)
        {
            MakeObject(context, new ISystemClock(StandardNetworkSystemClockCore.Instance, false));

            return ResultCode.Success;
        }

        [Command(20)] // 6.0.0+
        // GetSharedMemoryNativeHandle() -> handle<copy>
        public ResultCode GetSharedMemoryNativeHandle(ServiceCtx context)
        {
            if (_timeSharedMemoryNativeHandle == 0)
            {
                if (context.Process.HandleTable.GenerateHandle(context.Device.System.TimeSharedMem, out _timeSharedMemoryNativeHandle) != KernelResult.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_timeSharedMemoryNativeHandle);

            return ResultCode.Success;
        }

        [Command(100)]
        // IsStandardUserSystemClockAutomaticCorrectionEnabled() -> bool
        public ResultCode IsStandardUserSystemClockAutomaticCorrectionEnabled(ServiceCtx context)
        {
            context.ResponseData.Write(StandardUserSystemClockCore.Instance.IsAutomaticCorrectionEnabled());

            return ResultCode.Success;
        }

        [Command(101)]
        // SetStandardUserSystemClockAutomaticCorrectionEnabled(b8)
        public ResultCode SetStandardUserSystemClockAutomaticCorrectionEnabled(ServiceCtx context)
        {
            if ((_permissions & TimePermissions.UserSystemClockWritableMask) == 0)
            {
                return ResultCode.PermissionDenied;
            }

            bool autoCorrectionEnabled = context.RequestData.ReadBoolean();

            return StandardUserSystemClockCore.Instance.SetAutomaticCorrectionEnabled(context.Thread, autoCorrectionEnabled);
        }

        [Command(200)] // 3.0.0+
        // IsStandardNetworkSystemClockAccuracySufficient() -> bool
        public ResultCode IsStandardNetworkSystemClockAccuracySufficient(ServiceCtx context)
        {
            context.ResponseData.Write(StandardNetworkSystemClockCore.Instance.IsStandardNetworkSystemClockAccuracySufficient(context.Thread));

            return ResultCode.Success;
        }

        [Command(300)] // 4.0.0+
        // CalculateMonotonicSystemClockBaseTimePoint(nn::time::SystemClockContext) -> s64
        public ResultCode CalculateMonotonicSystemClockBaseTimePoint(ServiceCtx context)
        {
            SystemClockContext otherContext = context.RequestData.ReadStruct<SystemClockContext>();

            SteadyClockTimePoint currentTimePoint = StandardSteadyClockCore.Instance.GetCurrentTimePoint(context.Thread);

            ResultCode result = ResultCode.TimeMismatch;

            if (currentTimePoint.ClockSourceId == otherContext.SteadyTimePoint.ClockSourceId)
            {
                TimeSpanType ticksTimeSpan = TimeSpanType.FromTicks(context.Thread.Context.ThreadState.CntpctEl0, context.Thread.Context.ThreadState.CntfrqEl0);

                long baseTimePoint = otherContext.Offset + currentTimePoint.TimePoint - ticksTimeSpan.ToSeconds();

                context.ResponseData.Write(baseTimePoint);

                result = 0;
            }

            return result;
        }

        [Command(400)] // 4.0.0+
        // GetClockSnapshot(u8) -> buffer<nn::time::sf::ClockSnapshot, 0x1a>
        public ResultCode GetClockSnapshot(ServiceCtx context)
        {
            byte type = context.RequestData.ReadByte();

            long bufferPosition = context.Request.RecvListBuff[0].Position;
            long bufferSize     = context.Request.RecvListBuff[0].Size;

            ResultCode result = StandardUserSystemClockCore.Instance.GetSystemClockContext(context.Thread, out SystemClockContext userContext);

            if (result == ResultCode.Success)
            {
                result = StandardNetworkSystemClockCore.Instance.GetSystemClockContext(context.Thread, out SystemClockContext networkContext);

                if (result == ResultCode.Success)
                {
                    result = GetClockSnapshotFromSystemClockContextInternal(context.Thread, userContext, networkContext, type, out ClockSnapshot clockSnapshot);

                    if (result == ResultCode.Success)
                    {
                        context.Memory.WriteStruct(bufferPosition, clockSnapshot);
                    }
                }
            }

            return result;
        }

        [Command(401)] // 4.0.0+
        // GetClockSnapshotFromSystemClockContext(u8, nn::time::SystemClockContext, nn::time::SystemClockContext) -> buffer<nn::time::sf::-*, 0x1a>
        public ResultCode GetClockSnapshotFromSystemClockContext(ServiceCtx context)
        {
            byte type = context.RequestData.ReadByte();
            context.RequestData.BaseStream.Position += 7;

            SystemClockContext userContext    = context.RequestData.ReadStruct<SystemClockContext>();
            SystemClockContext networkContext = context.RequestData.ReadStruct<SystemClockContext>();
            long               bufferPosition = context.Request.RecvListBuff[0].Position;
            long               bufferSize     = context.Request.RecvListBuff[0].Size;


            ResultCode result = GetClockSnapshotFromSystemClockContextInternal(context.Thread, userContext, networkContext, type, out ClockSnapshot clockSnapshot);

            if (result == ResultCode.Success)
            {
                context.Memory.WriteStruct(bufferPosition, clockSnapshot);
            }

            return result;
        }

        private ResultCode GetClockSnapshotFromSystemClockContextInternal(KThread thread, SystemClockContext userContext, SystemClockContext networkContext, byte type, out ClockSnapshot clockSnapshot)
        {
            clockSnapshot = new ClockSnapshot();

            SteadyClockCore steadyClockCore = StandardSteadyClockCore.Instance;

            SteadyClockTimePoint currentTimePoint = steadyClockCore.GetCurrentTimePoint(thread);

            clockSnapshot.IsAutomaticCorrectionEnabled = StandardUserSystemClockCore.Instance.IsAutomaticCorrectionEnabled();
            clockSnapshot.UserContext                  = userContext;
            clockSnapshot.NetworkContext               = networkContext;

            char[] tzName       = TimeZoneManager.Instance.GetDeviceLocationName().ToCharArray();
            char[] locationName = new char[0x24];

            Array.Copy(tzName, locationName, locationName.Length);

            clockSnapshot.LocationName = locationName;

            ResultCode result = ClockSnapshot.GetCurrentTime(out clockSnapshot.UserTime, currentTimePoint, clockSnapshot.UserContext);

            if (result == ResultCode.Success)
            {
                result = TimeZoneManager.Instance.ToCalendarTimeWithMyRules(clockSnapshot.UserTime, out CalendarInfo userCalendarInfo);

                if (result == ResultCode.Success)
                {
                    clockSnapshot.UserCalendarTime           = userCalendarInfo.Time;
                    clockSnapshot.UserCalendarAdditionalTime = userCalendarInfo.AdditionalInfo;

                    if (ClockSnapshot.GetCurrentTime(out clockSnapshot.NetworkTime, currentTimePoint, clockSnapshot.NetworkContext) != ResultCode.Success)
                    {
                        clockSnapshot.NetworkTime = 0;
                    }

                    result = TimeZoneManager.Instance.ToCalendarTimeWithMyRules(clockSnapshot.NetworkTime, out CalendarInfo networkCalendarInfo);

                    if (result == ResultCode.Success)
                    {
                        clockSnapshot.NetworkCalendarTime           = networkCalendarInfo.Time;
                        clockSnapshot.NetworkCalendarAdditionalTime = networkCalendarInfo.AdditionalInfo;
                        clockSnapshot.Type                          = type;

                        // Probably a version field?
                        clockSnapshot.Unknown = 0;
                    }
                }
            }

            return result;
        }
    }
}