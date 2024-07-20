using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Applet;
using Ryujinx.Horizon.Sdk.Hid;
using Ryujinx.Horizon.Sdk.Hid.SixAxis;
using Ryujinx.Horizon.Sdk.Hid.Vibration;
using Ryujinx.Horizon.Sdk.Sf;
using System;

namespace Ryujinx.Horizon.Hid
{
    partial class HidServer : IHidServer
    {
        [CmifCommand(0)]
        public Result CreateAppletResource(out IAppletResource arg0, AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result ActivateDebugPad(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result ActivateTouchScreen(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(21)]
        public Result ActivateMouse(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(31)]
        public Result ActivateKeyboard(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(32)]
        public Result SendKeyboardLockKeyEvent(AppletResourceUserId appletResourceUserId, KeyboardLockKeyEvent keyboardLockKeyEvent, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(40)]
        public Result AcquireXpadIdEventHandle(out int arg0, ulong arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(41)]
        public Result ReleaseXpadIdEventHandle(ulong arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(51)]
        public Result ActivateXpad(AppletResourceUserId appletResourceUserId, uint basixXpadId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(55)]
        public Result GetXpadIds(out long arg0, Span<uint> basicXpadIds)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(56)]
        public Result ActivateJoyXpad(uint joyXpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(58)]
        public Result GetJoyXpadLifoHandle(out int arg0, uint joyXpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(59)]
        public Result GetJoyXpadIds(out long arg0, Span<uint> joyXpadIds)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(60)]
        public Result ActivateSixAxisSensor(uint basixXpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(61)]
        public Result DeactivateSixAxisSensor(uint basixXpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(62)]
        public Result GetSixAxisSensorLifoHandle(out int arg0, uint basixXpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(63)]
        public Result ActivateJoySixAxisSensor(uint joyXpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(64)]
        public Result DeactivateJoySixAxisSensor(uint joyXpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(65)]
        public Result GetJoySixAxisSensorLifoHandle(out int arg0, uint joyXpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(66)]
        public Result StartSixAxisSensor(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(67)]
        public Result StopSixAxisSensor(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(68)]
        public Result IsSixAxisSensorFusionEnabled(out bool arg0, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(69)]
        public Result EnableSixAxisSensorFusion(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, bool arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(70)]
        public Result SetSixAxisSensorFusionParameters(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, float arg2, float arg3, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(71)]
        public Result GetSixAxisSensorFusionParameters(out float arg0, out float arg1, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(72)]
        public Result ResetSixAxisSensorFusionParameters(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(73)]
        public Result SetAccelerometerParameters(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, float arg2, float arg3, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(74)]
        public Result GetAccelerometerParameters(out float arg0, out float arg1, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(75)]
        public Result ResetAccelerometerParameters(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(76)]
        public Result SetAccelerometerPlayMode(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, uint arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(77)]
        public Result GetAccelerometerPlayMode(out uint arg0, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(78)]
        public Result ResetAccelerometerPlayMode(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(79)]
        public Result SetGyroscopeZeroDriftMode(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, uint arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(80)]
        public Result GetGyroscopeZeroDriftMode(out uint arg0, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(81)]
        public Result ResetGyroscopeZeroDriftMode(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(82)]
        public Result IsSixAxisSensorAtRest(out bool arg0, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(83)]
        public Result IsFirmwareUpdateAvailableForSixAxisSensor(out bool arg0, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(84)]
        public Result EnableSixAxisSensorUnalteredPassthrough(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, bool arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(85)]
        public Result IsSixAxisSensorUnalteredPassthroughEnabled(out bool arg0, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(86)]
        public Result StoreSixAxisSensorCalibrationParameter(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, in SixAxisSensorCalibrationParameter sixAxisSensorCalibrationParameter, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(87)]
        public Result LoadSixAxisSensorCalibrationParameter(AppletResourceUserId appletResourceUserId, out SixAxisSensorCalibrationParameter sixAxisSensorCalibrationParameter, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(88)]
        public Result GetSixAxisSensorIcInformation(AppletResourceUserId appletResourceUserId, out SixAxisSensorIcInformation sixAxisSensorIcInformation, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(89)]
        public Result ResetIsSixAxisSensorDeviceNewlyAssigned(AppletResourceUserId appletResourceUserId,
            SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(91)]
        public Result ActivateGesture(AppletResourceUserId appletResourceUserId, int arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result SetSupportedNpadStyleSet(AppletResourceUserId appletResourceUserId, NpadStyleTag arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(101)]
        public Result GetSupportedNpadStyleSet(AppletResourceUserId appletResourceUserId, out NpadStyleTag arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(102)]
        public Result SetSupportedNpadIdType(AppletResourceUserId appletResourceUserId, ReadOnlySpan<uint> arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(103)]
        public Result ActivateNpad(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(104)]
        public Result DeactivateNpad(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(106)]
        public Result AcquireNpadStyleSetUpdateEventHandle(AppletResourceUserId appletResourceUserId, out int arg1, uint arg2, ulong arg3, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(107)]
        public Result DisconnectNpad(AppletResourceUserId appletResourceUserId, uint arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(108)]
        public Result GetPlayerLedPattern(out ulong arg0, uint arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(109)]
        public Result ActivateNpadWithRevision(AppletResourceUserId appletResourceUserId, int arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(120)]
        public Result SetNpadJoyHoldType(AppletResourceUserId appletResourceUserId, long arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(121)]
        public Result GetNpadJoyHoldType(AppletResourceUserId appletResourceUserId, out long arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(122)]
        public Result SetNpadJoyAssignmentModeSingleByDefault(AppletResourceUserId appletResourceUserId, uint arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(123)]
        public Result SetNpadJoyAssignmentModeSingle(AppletResourceUserId appletResourceUserId, uint arg1, long arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(124)]
        public Result SetNpadJoyAssignmentModeDual(AppletResourceUserId appletResourceUserId, uint arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(125)]
        public Result MergeSingleJoyAsDualJoy(AppletResourceUserId appletResourceUserId, uint arg1, uint arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(126)]
        public Result StartLrAssignmentMode(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(127)]
        public Result StopLrAssignmentMode(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(128)]
        public Result SetNpadHandheldActivationMode(AppletResourceUserId appletResourceUserId, long arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(129)]
        public Result GetNpadHandheldActivationMode(AppletResourceUserId appletResourceUserId, out long arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(130)]
        public Result SwapNpadAssignment(AppletResourceUserId appletResourceUserId, uint arg1, uint arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(131)]
        public Result IsUnintendedHomeButtonInputProtectionEnabled(out bool arg0, AppletResourceUserId appletResourceUserId, uint arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(132)]
        public Result EnableUnintendedHomeButtonInputProtection(AppletResourceUserId appletResourceUserId, uint arg1, bool arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(133)]
        public Result SetNpadJoyAssignmentModeSingleWithDestination(out bool arg0, out uint arg1, AppletResourceUserId appletResourceUserId, uint arg3, long arg4, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(134)]
        public Result SetNpadAnalogStickUseCenterClamp(AppletResourceUserId appletResourceUserId, bool arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(135)]
        public Result SetNpadCaptureButtonAssignment(AppletResourceUserId appletResourceUserId, NpadStyleTag arg1, NpadButton arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(136)]
        public Result ClearNpadCaptureButtonAssignment(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(200)]
        public Result GetVibrationDeviceInfo(out VibrationDeviceInfoForIpc vibrationDeviceInfoForIpc, VibrationDeviceHandle vibrationDeviceHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(201)]
        public Result SendVibrationValue(AppletResourceUserId appletResourceUserId, VibrationDeviceHandle vibrationDeviceHandle, VibrationValue arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(202)]
        public Result GetActualVibrationValue(out VibrationValue arg0, AppletResourceUserId appletResourceUserId, VibrationDeviceHandle vibrationDeviceHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(203)]
        public Result CreateActiveVibrationDeviceList(out IActiveVibrationDeviceList arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(204)]
        public Result PermitVibration(bool arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(205)]
        public Result IsVibrationPermitted(out bool arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(206)]
        public Result SendVibrationValues(AppletResourceUserId appletResourceUserId, ReadOnlySpan<VibrationDeviceHandle> vibrationDeviceHandles, ReadOnlySpan<VibrationValue> vibrationValues)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(207)]
        public Result SendVibrationGcErmCommand(AppletResourceUserId appletResourceUserId, VibrationDeviceHandle vibrationDeviceHandle, VibrationGcErmCommand vibrationGcErmCommand, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(208)]
        public Result GetActualVibrationGcErmCommand(out VibrationGcErmCommand vibrationGcErmCommand, AppletResourceUserId appletResourceUserId, VibrationDeviceHandle vibrationDeviceHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(209)]
        public Result BeginPermitVibrationSession(AppletResourceUserId appletResourceUserId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(210)]
        public Result EndPermitVibrationSession()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(211)]
        public Result IsVibrationDeviceMounted(out bool arg0, AppletResourceUserId appletResourceUserId, VibrationDeviceHandle vibrationDeviceHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(212)]
        public Result SendVibrationValueInBool(AppletResourceUserId appletResourceUserId, VibrationDeviceHandle vibrationDeviceHandle, bool arg2, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(300)]
        public Result ActivateConsoleSixAxisSensor(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(301)]
        public Result StartConsoleSixAxisSensor(AppletResourceUserId appletResourceUserId, ConsoleSixAxisSensorHandle consoleSixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(302)]
        public Result StopConsoleSixAxisSensor(AppletResourceUserId appletResourceUserId, ConsoleSixAxisSensorHandle consoleSixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(303)]
        public Result ActivateSevenSixAxisSensor(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(304)]
        public Result StartSevenSixAxisSensor(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(305)]
        public Result StopSevenSixAxisSensor(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(306)]
        public Result InitializeSevenSixAxisSensor(AppletResourceUserId appletResourceUserId, int arg1, ulong arg2, int arg3, ulong arg4, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(307)]
        public Result FinalizeSevenSixAxisSensor(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(308)]
        public Result SetSevenSixAxisSensorFusionStrength(AppletResourceUserId appletResourceUserId, float arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(309)]
        public Result GetSevenSixAxisSensorFusionStrength(out float arg0, AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(310)]
        public Result ResetSevenSixAxisSensorTimestamp(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(400)]
        public Result IsUsbFullKeyControllerEnabled(out bool arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(401)]
        public Result EnableUsbFullKeyController(bool arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(402)]
        public Result IsUsbFullKeyControllerConnected(out bool arg0, uint arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(403)]
        public Result HasBattery(out bool arg0, uint arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(404)]
        public Result HasLeftRightBattery(out bool arg0, out bool arg1, uint arg2)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(405)]
        public Result GetNpadInterfaceType(out byte arg0, uint arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(406)]
        public Result GetNpadLeftRightInterfaceType(out byte arg0, out byte arg1, uint arg2)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(407)]
        public Result GetNpadOfHighestBatteryLevel(out uint arg0, ReadOnlySpan<uint> arg1, AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(500)]
        public Result GetPalmaConnectionHandle(out PalmaConnectionHandle palmaConnectionHandle, uint arg1, AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(501)]
        public Result InitializePalma(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(502)]
        public Result AcquirePalmaOperationCompleteEvent(out int arg0, PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(503)]
        public Result GetPalmaOperationInfo(out ulong arg0, Span<byte> arg1, PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(504)]
        public Result PlayPalmaActivity(PalmaConnectionHandle palmaConnectionHandle, ulong arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(505)]
        public Result SetPalmaFrModeType(PalmaConnectionHandle palmaConnectionHandle, ulong arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(506)]
        public Result ReadPalmaStep(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(507)]
        public Result EnablePalmaStep(PalmaConnectionHandle palmaConnectionHandle, bool arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(508)]
        public Result ResetPalmaStep(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(509)]
        public Result ReadPalmaApplicationSection(PalmaConnectionHandle palmaConnectionHandle, ulong arg1, ulong arg2)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(510)]
        public Result WritePalmaApplicationSection(PalmaConnectionHandle palmaConnectionHandle, ulong arg1, ulong arg2, in PalmaApplicationSectionAccessBuffer palmaApplicationSectionAccessBuffer)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(511)]
        public Result ReadPalmaUniqueCode(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(512)]
        public Result SetPalmaUniqueCodeInvalid(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(513)]
        public Result WritePalmaActivityEntry(PalmaConnectionHandle palmaConnectionHandle, ulong arg1, ulong arg2, ulong arg3, ulong arg4)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(514)]
        public Result WritePalmaRgbLedPatternEntry(PalmaConnectionHandle palmaConnectionHandle, ulong arg1, ReadOnlySpan<byte> arg2)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(515)]
        public Result WritePalmaWaveEntry(PalmaConnectionHandle palmaConnectionHandle, PalmaWaveSet palmaWaveSet, ulong arg2, int arg3, ulong arg4, ulong arg5)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(516)]
        public Result SetPalmaDataBaseIdentificationVersion(PalmaConnectionHandle palmaConnectionHandle, int arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(517)]
        public Result GetPalmaDataBaseIdentificationVersion(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(518)]
        public Result SuspendPalmaFeature(PalmaConnectionHandle palmaConnectionHandle, PalmaFeature palmaFeature)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(519)]
        public Result GetPalmaOperationResult(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(520)]
        public Result ReadPalmaPlayLog(PalmaConnectionHandle palmaConnectionHandle, ushort arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(521)]
        public Result ResetPalmaPlayLog(PalmaConnectionHandle palmaConnectionHandle, ushort arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(522)]
        public Result SetIsPalmaAllConnectable(AppletResourceUserId appletResourceUserId, bool arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(523)]
        public Result SetIsPalmaPairedConnectable(AppletResourceUserId appletResourceUserId, bool arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(524)]
        public Result PairPalma(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(525)]
        public Result SetPalmaBoostMode(bool arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(526)]
        public Result CancelWritePalmaWaveEntry(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(527)]
        public Result EnablePalmaBoostMode(AppletResourceUserId appletResourceUserId, bool arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(528)]
        public Result GetPalmaBluetoothAddress(out Address arg0, PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(529)]
        public Result SetDisallowedPalmaConnection(AppletResourceUserId appletResourceUserId, ReadOnlySpan<Address> arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(1000)]
        public Result SetNpadCommunicationMode(AppletResourceUserId appletResourceUserId, long arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(1001)]
        public Result GetNpadCommunicationMode(out long arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(1002)]
        public Result SetTouchScreenConfiguration(AppletResourceUserId appletResourceUserId, TouchScreenConfigurationForNx arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(1003)]
        public Result IsFirmwareUpdateNeededForNotification(out bool arg0, int arg1, AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(2000)]
        public Result ActivateDigitizer(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }
    }
}
