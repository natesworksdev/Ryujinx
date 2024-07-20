using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Applet;
using Ryujinx.Horizon.Sdk.Hid;
using Ryujinx.Horizon.Sdk.Hid.HidDevices;
using Ryujinx.Horizon.Sdk.Hid.Npad;
using Ryujinx.Horizon.Sdk.Hid.SixAxis;
using Ryujinx.Horizon.Sdk.Hid.Vibration;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;

namespace Ryujinx.Horizon.Hid
{
    partial class HidServer : IHidServer
    {
        internal const int SharedMemEntryCount = 17;

        public DebugPadDevice DebugPad;
        public TouchDevice Touchscreen;
        public MouseDevice Mouse;
        public KeyboardDevice Keyboard;
        public NpadDevices Npads;

        private bool _sixAxisSensorFusionEnabled;
        private bool _unintendedHomeButtonInputProtectionEnabled;
        private bool _npadAnalogStickCenterClampEnabled;
        private bool _vibrationPermitted;
        private bool _usbFullKeyControllerEnabled;
        private readonly bool _isFirmwareUpdateAvailableForSixAxisSensor;
        private bool _isSixAxisSensorUnalteredPassthroughEnabled;

        private NpadHandheldActivationMode _npadHandheldActivationMode;
        private GyroscopeZeroDriftMode _gyroscopeZeroDriftMode;

        private long _npadCommunicationMode;
        private uint _accelerometerPlayMode;
        private float _sevenSixAxisSensorFusionStrength;

        private SensorFusionParameters _sensorFusionParams;
        private AccelerometerParameters _accelerometerParams;

        public HidServer()
        {
            DebugPad = new DebugPadDevice(true);
            Touchscreen = new TouchDevice(true);
            Mouse = new MouseDevice(false);
            Keyboard = new KeyboardDevice(false);
            Npads = new NpadDevices(true);

            _npadHandheldActivationMode = NpadHandheldActivationMode.Dual;
            _gyroscopeZeroDriftMode = GyroscopeZeroDriftMode.Standard;

            _isFirmwareUpdateAvailableForSixAxisSensor = false;

            _sensorFusionParams = new SensorFusionParameters();
            _accelerometerParams = new AccelerometerParameters();

            _vibrationPermitted = true;
        }

        [CmifCommand(0)]
        public Result CreateAppletResource(out IAppletResource arg0, AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result ActivateDebugPad(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            // Initialize entries to avoid issues with some games.

            for (int i = 0; i < SharedMemEntryCount; i++)
            {
                DebugPad.Update();
            }

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result ActivateTouchScreen(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Touchscreen.Active = true;

            // Initialize entries to avoid issues with some games.

            for (int i = 0; i < SharedMemEntryCount; i++)
            {
                Touchscreen.Update();
            }

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(21)]
        public Result ActivateMouse(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Mouse.Active = true;

            // Initialize entries to avoid issues with some games.

            for (int i = 0; i < SharedMemEntryCount; i++)
            {
                Mouse.Update(0, 0);
            }

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(31)]
        public Result ActivateKeyboard(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Keyboard.Active = true;

            // Initialize entries to avoid issues with some games.

            KeyboardInput emptyInput = new()
            {
                Keys = new ulong[4],
            };

            for (int i = 0; i < SharedMemEntryCount; i++)
            {
                Keyboard.Update(emptyInput);
            }

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(32)]
        public Result SendKeyboardLockKeyEvent(AppletResourceUserId appletResourceUserId, KeyboardLockKeyEvent keyboardLockKeyEvent, [ClientProcessId] ulong pid)
        {
            // NOTE: This signals the keyboard driver about lock events.

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { keyboardLockKeyEvent });

            return Result.Success;
        }

        [CmifCommand(40)]
        public Result AcquireXpadIdEventHandle([CopyHandle] out int arg0, ulong xpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(41)]
        public Result ReleaseXpadIdEventHandle(ulong xpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(51)]
        public Result ActivateXpad(AppletResourceUserId appletResourceUserId, uint basicXpadId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, basicXpadId });

            return Result.Success;
        }

        [CmifCommand(55)]
        public Result GetXpadIds(out long idCount, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<uint> basicXpadIds)
        {
            // There aren't any Xpads, so we return 0 and write nothing inside the buffer.
            idCount = 0;

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
        public Result GetJoyXpadLifoHandle([CopyHandle] out int arg0, uint joyXpadId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(59)]
        public Result GetJoyXpadIds(out long arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<uint> joyXpadIds)
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
        public Result GetSixAxisSensorLifoHandle([CopyHandle] out int arg0, uint basixXpadId)
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
        public Result GetJoySixAxisSensorLifoHandle([CopyHandle] out int arg0, uint joyXpadId)
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
        public Result SetAccelerometerParameters(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, float x, float y, [ClientProcessId] ulong pid)
        {
            _accelerometerParams = new AccelerometerParameters
            {
                X = x,
                Y = y,
            };

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _accelerometerParams.X, _accelerometerParams.Y });

            return Result.Success;
        }

        [CmifCommand(74)]
        public Result GetAccelerometerParameters(out float x, out float y, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            x = _accelerometerParams.X;
            y = _accelerometerParams.Y;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _accelerometerParams.X, _accelerometerParams.Y });

            return Result.Success;
        }

        [CmifCommand(75)]
        public Result ResetAccelerometerParameters(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            _accelerometerParams.X = 0;
            _accelerometerParams.Y = 0;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _accelerometerParams.X, _accelerometerParams.Y });

            return Result.Success;
        }

        [CmifCommand(76)]
        public Result SetAccelerometerPlayMode(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, uint accelerometerPlayMode, [ClientProcessId] ulong pid)
        {
            _accelerometerPlayMode = accelerometerPlayMode;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _accelerometerPlayMode });

            return Result.Success;
        }

        [CmifCommand(77)]
        public Result GetAccelerometerPlayMode(out uint accelerometerPlayMode, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            accelerometerPlayMode = _accelerometerPlayMode;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _accelerometerPlayMode });

            return Result.Success;
        }

        [CmifCommand(78)]
        public Result ResetAccelerometerPlayMode(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            _accelerometerPlayMode = 0;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _accelerometerPlayMode });

            return Result.Success;
        }

        [CmifCommand(79)]
        public Result SetGyroscopeZeroDriftMode(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, GyroscopeZeroDriftMode gyroscopeZeroDriftMode, [ClientProcessId] ulong pid)
        {
            _gyroscopeZeroDriftMode = gyroscopeZeroDriftMode;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _gyroscopeZeroDriftMode });

            return Result.Success;
        }

        [CmifCommand(80)]
        public Result GetGyroscopeZeroDriftMode(out GyroscopeZeroDriftMode gyroscopeZeroDriftMode, AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            gyroscopeZeroDriftMode = _gyroscopeZeroDriftMode;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _gyroscopeZeroDriftMode });

            return Result.Success;
        }

        [CmifCommand(81)]
        public Result ResetGyroscopeZeroDriftMode(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            _gyroscopeZeroDriftMode = GyroscopeZeroDriftMode.Standard;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _gyroscopeZeroDriftMode });

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
        public Result StoreSixAxisSensorCalibrationParameter(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias, 0x744)] in SixAxisSensorCalibrationParameter sixAxisSensorCalibrationParameter, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { sixAxisSensorHandle, sixAxisSensorCalibrationParameter });

            return Result.Success;
        }

        [CmifCommand(87)]
        public Result LoadSixAxisSensorCalibrationParameter(AppletResourceUserId appletResourceUserId, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias, 0x744)] out SixAxisSensorCalibrationParameter sixAxisSensorCalibrationParameter, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            // TODO: CalibrationParameter have to be determined.

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle });

            return Result.Success;
        }

        [CmifCommand(88)]
        public Result GetSixAxisSensorIcInformation(AppletResourceUserId appletResourceUserId, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer, 0xC8)] out SixAxisSensorIcInformation sixAxisSensorIcInformation, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            // TODO: IcInformation have to be determined.

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle });

            return Result.Success;
        }

        [CmifCommand(89)]
        public Result ResetIsSixAxisSensorDeviceNewlyAssigned(AppletResourceUserId appletResourceUserId, SixAxisSensorHandle sixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(91)]
        public Result ActivateGesture(AppletResourceUserId appletResourceUserId, int unknown, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, unknown });

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result SetSupportedNpadStyleSet(AppletResourceUserId appletResourceUserId, NpadStyleTag supportedStyleSets, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { pid, appletResourceUserId, supportedStyleSets });

            Npads.SupportedStyleSets = supportedStyleSets;

            return Result.Success;
        }

        [CmifCommand(101)]
        public Result GetSupportedNpadStyleSet(AppletResourceUserId appletResourceUserId, out NpadStyleTag supportedStyleSets, [ClientProcessId] ulong pid)
        {
            supportedStyleSets = Npads.SupportedStyleSets;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, Npads.SupportedStyleSets });

            return Result.Success;
        }

        [CmifCommand(102)]
        public Result SetSupportedNpadIdType(AppletResourceUserId appletResourceUserId, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<uint> arg1, [ClientProcessId] ulong pid)
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
        public Result AcquireNpadStyleSetUpdateEventHandle(AppletResourceUserId appletResourceUserId, [CopyHandle] out int arg1, uint arg2, ulong arg3, [ClientProcessId] ulong pid)
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
        public Result SendVibrationValues(AppletResourceUserId appletResourceUserId, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<VibrationDeviceHandle> vibrationDeviceHandles, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<VibrationValue> vibrationValues)
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
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(210)]
        public Result EndPermitVibrationSession()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(211)]
        public Result IsVibrationDeviceMounted(out bool isVibrationDeviceMounted, AppletResourceUserId appletResourceUserId, VibrationDeviceHandle vibrationDeviceHandle, [ClientProcessId] ulong pid)
        {
            // NOTE: Service use vibrationDeviceHandle to get the PlayerIndex.
            //       And return false if (npadIdType >= (NpadIdType)8 && npadIdType != NpadIdType.Handheld && npadIdType != NpadIdType.Unknown)

            isVibrationDeviceMounted = true;

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
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(301)]
        public Result StartConsoleSixAxisSensor(AppletResourceUserId appletResourceUserId, ConsoleSixAxisSensorHandle consoleSixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, consoleSixAxisSensorHandle });

            return Result.Success;
        }

        [CmifCommand(302)]
        public Result StopConsoleSixAxisSensor(AppletResourceUserId appletResourceUserId, ConsoleSixAxisSensorHandle consoleSixAxisSensorHandle, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, consoleSixAxisSensorHandle });

            return Result.Success;
        }

        [CmifCommand(303)]
        public Result ActivateSevenSixAxisSensor(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(304)]
        public Result StartSevenSixAxisSensor(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(305)]
        public Result StopSevenSixAxisSensor(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(306)]
        public Result InitializeSevenSixAxisSensor(AppletResourceUserId appletResourceUserId, [CopyHandle] int nativeHandle0, ulong counter0, [CopyHandle] int nativeHandle1, ulong counter1, [ClientProcessId] ulong pid)
        {
            // TODO: Determine if array<nn::sf::NativeHandle> is a buffer or not...

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, counter0, counter1 });

            return Result.Success;
        }

        [CmifCommand(307)]
        public Result FinalizeSevenSixAxisSensor(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(308)]
        public Result SetSevenSixAxisSensorFusionStrength(AppletResourceUserId appletResourceUserId, float sevenSixAxisSensorFusionStrength, [ClientProcessId] ulong pid)
        {
            _sevenSixAxisSensorFusionStrength = sevenSixAxisSensorFusionStrength;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, _sevenSixAxisSensorFusionStrength });

            return Result.Success;
        }

        [CmifCommand(309)]
        public Result GetSevenSixAxisSensorFusionStrength(out float sevenSixSensorFusionStrength, AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            sevenSixSensorFusionStrength = _sevenSixAxisSensorFusionStrength;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, _sevenSixAxisSensorFusionStrength });

            return Result.Success;
        }

        [CmifCommand(310)]
        public Result ResetSevenSixAxisSensorTimestamp(AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return Result.Success;
        }

        [CmifCommand(400)]
        public Result IsUsbFullKeyControllerEnabled(out bool isUsbFullKeyControllerEnabled)
        {
            isUsbFullKeyControllerEnabled = _usbFullKeyControllerEnabled;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { _usbFullKeyControllerEnabled });

            return Result.Success;
        }

        [CmifCommand(401)]
        public Result EnableUsbFullKeyController(bool usbFullKeyControllerEnabled)
        {
            _usbFullKeyControllerEnabled = usbFullKeyControllerEnabled;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { _usbFullKeyControllerEnabled });

            return Result.Success;
        }

        [CmifCommand(402)]
        public Result IsUsbFullKeyControllerConnected(out bool isConnected, uint unknown)
        {
            isConnected = true; // FullKeyController is always connected?

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { unknown, Connected = true });

            return Result.Success;
        }

        [CmifCommand(403)]
        public Result HasBattery(out bool hasBattery, uint npadId)
        {
            hasBattery = true; // Npad always has a battery?

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { npadId, HasBattery = true });

            return Result.Success;
        }

        [CmifCommand(404)]
        public Result HasLeftRightBattery(out bool hasLeftBattery, out bool hasRightBattery, uint npadId)
        {
            hasLeftBattery = true; // Npad always has a left battery?
            hasRightBattery = true; // Npad always has a right battery?

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { npadId, HasLeftBattery = true, HasRightBattery = true });

            return Result.Success;
        }

        [CmifCommand(405)]
        public Result GetNpadInterfaceType(out byte npadInterfaceType, uint npadId)
        {
            npadInterfaceType = 0;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { npadId, NpadInterfaceType = 0 });

            return Result.Success;
        }

        [CmifCommand(406)]
        public Result GetNpadLeftRightInterfaceType(out byte arg0, out byte arg1, uint arg2)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(407)]
        public Result GetNpadOfHighestBatteryLevel(out uint arg0, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<uint> arg1, AppletResourceUserId appletResourceUserId, [ClientProcessId] ulong pid)
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
        public Result AcquirePalmaOperationCompleteEvent([CopyHandle] out int arg0, PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(503)]
        public Result GetPalmaOperationInfo(out ulong arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<byte> arg1, PalmaConnectionHandle palmaConnectionHandle)
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
        public Result WritePalmaApplicationSection(PalmaConnectionHandle palmaConnectionHandle, ulong arg1, ulong arg2, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer, 0x100)] in PalmaApplicationSectionAccessBuffer palmaApplicationSectionAccessBuffer)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(511)]
        public Result ReadPalmaUniqueCode(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle });

            return Result.Success;
        }

        [CmifCommand(512)]
        public Result SetPalmaUniqueCodeInvalid(PalmaConnectionHandle palmaConnectionHandle)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle });

            return Result.Success;
        }

        [CmifCommand(513)]
        public Result WritePalmaActivityEntry(PalmaConnectionHandle palmaConnectionHandle, ulong arg1, ulong arg2, ulong arg3, ulong arg4)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(514)]
        public Result WritePalmaRgbLedPatternEntry(PalmaConnectionHandle palmaConnectionHandle, ulong arg1, [Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias)] ReadOnlySpan<byte> arg2)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(515)]
        public Result WritePalmaWaveEntry(PalmaConnectionHandle palmaConnectionHandle, PalmaWaveSet palmaWaveSet, ulong arg2, [CopyHandle] int arg3, ulong arg4, ulong arg5)
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
        public Result SetIsPalmaAllConnectable(AppletResourceUserId appletResourceUserId, bool unknownBool, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, unknownBool });

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
            // NOTE: Stubbed in system module.

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
        public Result SetDisallowedPalmaConnection(AppletResourceUserId appletResourceUserId, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<Address> arg1, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return Result.Success;
        }

        [CmifCommand(1000)]
        public Result SetNpadCommunicationMode(AppletResourceUserId appletResourceUserId, long npadCommunicationMode, [ClientProcessId] ulong pid)
        {
            _npadCommunicationMode = npadCommunicationMode;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, _npadCommunicationMode });

            return Result.Success;
        }

        [CmifCommand(1001)]
        public Result GetNpadCommunicationMode(out long npadCommunicationMode)
        {
            npadCommunicationMode = _npadCommunicationMode;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { _npadCommunicationMode });

            return Result.Success;
        }

        [CmifCommand(1002)]
        public Result SetTouchScreenConfiguration(AppletResourceUserId appletResourceUserId, TouchScreenConfigurationForNx touchScreenConfigurationForNx, [ClientProcessId] ulong pid)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, touchScreenConfigurationForNx });

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
