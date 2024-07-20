using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Hid.HidServer;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Hid;
using Ryujinx.Horizon.Sdk.Hid.HidDevices;
using Ryujinx.Horizon.Sdk.Hid.Npad;
using Ryujinx.Horizon.Sdk.Hid.SixAxis;
using Ryujinx.Horizon.Sdk.Hid.Vibration;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    [Service("hid")]
    class IHidServer : IpcService
    {
        private readonly KEvent _xpadIdEvent;
        private readonly KEvent _palmaOperationCompleteEvent;

        private int _xpadIdEventHandle;

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
#pragma warning disable CS0649 // Field is never assigned to
        private readonly long _vibrationGcErmCommand;
#pragma warning restore CS0649
        private float _sevenSixAxisSensorFusionStrength;

        private SensorFusionParameters _sensorFusionParams;
        private AccelerometerParameters _accelerometerParams;

        public IHidServer(ServiceCtx context) : base(context.Device.System.HidServer)
        {
            _xpadIdEvent = new KEvent(context.Device.System.KernelContext);
            _palmaOperationCompleteEvent = new KEvent(context.Device.System.KernelContext);

            _npadHandheldActivationMode = NpadHandheldActivationMode.Dual;
            _gyroscopeZeroDriftMode = GyroscopeZeroDriftMode.Standard;

            _isFirmwareUpdateAvailableForSixAxisSensor = false;

            _sensorFusionParams = new SensorFusionParameters();
            _accelerometerParams = new AccelerometerParameters();

            // TODO: signal event at right place
            _xpadIdEvent.ReadableEvent.Signal();

            _vibrationPermitted = true;
        }

        [CommandCmif(0)]
        // CreateAppletResource(nn::applet::AppletResourceUserId) -> object<nn::hid::IAppletResource>
        public ResultCode CreateAppletResource(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long appletResourceUserId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059

            MakeObject(context, new IAppletResource(context.Device.System.HidSharedMem));

            return ResultCode.Success;
        }

        [CommandCmif(32)]
        // SendKeyboardLockKeyEvent(uint flags, pid)
        public ResultCode SendKeyboardLockKeyEvent(ServiceCtx context)
        {
            uint flags = context.RequestData.ReadUInt32();

            // NOTE: This signal the keyboard driver about lock events.

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { flags });

            return ResultCode.Success;
        }

        [CommandCmif(40)]
        // AcquireXpadIdEventHandle(ulong XpadId) -> nn::sf::NativeHandle
        public ResultCode AcquireXpadIdEventHandle(ServiceCtx context)
        {
            long xpadId = context.RequestData.ReadInt64();

            if (context.Process.HandleTable.GenerateHandle(_xpadIdEvent.ReadableEvent, out _xpadIdEventHandle) != Result.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_xpadIdEventHandle);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { xpadId });

            return ResultCode.Success;
        }

        [CommandCmif(41)]
        // ReleaseXpadIdEventHandle(ulong XpadId)
        public ResultCode ReleaseXpadIdEventHandle(ServiceCtx context)
        {
            long xpadId = context.RequestData.ReadInt64();

            context.Process.HandleTable.CloseHandle(_xpadIdEventHandle);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { xpadId });

            return ResultCode.Success;
        }

        [CommandCmif(55)]
        // GetXpadIds() -> long IdsCount, buffer<array<nn::hid::BasicXpadId>, type: 0xa>
        public ResultCode GetXpadIds(ServiceCtx context)
        {
            // There is any Xpad, so we return 0 and write nothing inside the type-0xa buffer.
            context.ResponseData.Write(0L);

            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return ResultCode.Success;
        }

        [CommandCmif(56)]
        // ActivateJoyXpad(nn::hid::JoyXpadId)
        public ResultCode ActivateJoyXpad(ServiceCtx context)
        {
            int joyXpadId = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { joyXpadId });

            return ResultCode.Success;
        }

        [CommandCmif(58)]
        // GetJoyXpadLifoHandle(nn::hid::JoyXpadId) -> nn::sf::NativeHandle
        public ResultCode GetJoyXpadLifoHandle(ServiceCtx context)
        {
            int joyXpadId = context.RequestData.ReadInt32();

            int handle = 0;

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { joyXpadId });

            return ResultCode.Success;
        }

        [CommandCmif(59)]
        // GetJoyXpadIds() -> long IdsCount, buffer<array<nn::hid::JoyXpadId>, type: 0xa>
        public ResultCode GetJoyXpadIds(ServiceCtx context)
        {
            // There is any JoyXpad, so we return 0 and write nothing inside the type-0xa buffer.
            context.ResponseData.Write(0L);

            Logger.Stub?.PrintStub(LogClass.ServiceHid);

            return ResultCode.Success;
        }

        [CommandCmif(60)]
        // ActivateSixAxisSensor(nn::hid::BasicXpadId)
        public ResultCode ActivateSixAxisSensor(ServiceCtx context)
        {
            int basicXpadId = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { basicXpadId });

            return ResultCode.Success;
        }

        [CommandCmif(61)]
        // DeactivateSixAxisSensor(nn::hid::BasicXpadId)
        public ResultCode DeactivateSixAxisSensor(ServiceCtx context)
        {
            int basicXpadId = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { basicXpadId });

            return ResultCode.Success;
        }

        [CommandCmif(62)]
        // GetSixAxisSensorLifoHandle(nn::hid::BasicXpadId) -> nn::sf::NativeHandle
        public ResultCode GetSixAxisSensorLifoHandle(ServiceCtx context)
        {
            int basicXpadId = context.RequestData.ReadInt32();

            int handle = 0;

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { basicXpadId });

            return ResultCode.Success;
        }

        [CommandCmif(63)]
        // ActivateJoySixAxisSensor(nn::hid::JoyXpadId)
        public ResultCode ActivateJoySixAxisSensor(ServiceCtx context)
        {
            int joyXpadId = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { joyXpadId });

            return ResultCode.Success;
        }

        [CommandCmif(64)]
        // DeactivateJoySixAxisSensor(nn::hid::JoyXpadId)
        public ResultCode DeactivateJoySixAxisSensor(ServiceCtx context)
        {
            int joyXpadId = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { joyXpadId });

            return ResultCode.Success;
        }

        [CommandCmif(65)]
        // GetJoySixAxisSensorLifoHandle(nn::hid::JoyXpadId) -> nn::sf::NativeHandle
        public ResultCode GetJoySixAxisSensorLifoHandle(ServiceCtx context)
        {
            int joyXpadId = context.RequestData.ReadInt32();

            int handle = 0;

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { joyXpadId });

            return ResultCode.Success;
        }

        [CommandCmif(66)]
        // StartSixAxisSensor(nn::hid::SixAxisSensorHandle, nn::applet::AppletResourceUserId)
        public ResultCode StartSixAxisSensor(ServiceCtx context)
        {
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle });

            return ResultCode.Success;
        }

        [CommandCmif(67)]
        // StopSixAxisSensor(nn::hid::SixAxisSensorHandle, nn::applet::AppletResourceUserId)
        public ResultCode StopSixAxisSensor(ServiceCtx context)
        {
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle });

            return ResultCode.Success;
        }

        [CommandCmif(68)]
        // IsSixAxisSensorFusionEnabled(nn::hid::SixAxisSensorHandle, nn::applet::AppletResourceUserId) -> bool IsEnabled
        public ResultCode IsSixAxisSensorFusionEnabled(ServiceCtx context)
        {
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            long appletResourceUserId = context.RequestData.ReadInt64();

            context.ResponseData.Write(_sixAxisSensorFusionEnabled);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _sixAxisSensorFusionEnabled });

            return ResultCode.Success;
        }

        [CommandCmif(69)]
        // EnableSixAxisSensorFusion(bool Enabled, nn::hid::SixAxisSensorHandle, nn::applet::AppletResourceUserId)
        public ResultCode EnableSixAxisSensorFusion(ServiceCtx context)
        {
            _sixAxisSensorFusionEnabled = context.RequestData.ReadUInt32() != 0;
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _sixAxisSensorFusionEnabled });

            return ResultCode.Success;
        }

        [CommandCmif(70)]
        // SetSixAxisSensorFusionParameters(nn::hid::SixAxisSensorHandle, float RevisePower, float ReviseRange, nn::applet::AppletResourceUserId)
        public ResultCode SetSixAxisSensorFusionParameters(ServiceCtx context)
        {
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding

            _sensorFusionParams = new SensorFusionParameters
            {
                RevisePower = context.RequestData.ReadInt32(),
                ReviseRange = context.RequestData.ReadInt32(),
            };

            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _sensorFusionParams.RevisePower, _sensorFusionParams.ReviseRange });

            return ResultCode.Success;
        }

        [CommandCmif(71)]
        // GetSixAxisSensorFusionParameters(nn::hid::SixAxisSensorHandle, nn::applet::AppletResourceUserId) -> float RevisePower, float ReviseRange)
        public ResultCode GetSixAxisSensorFusionParameters(ServiceCtx context)
        {
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            long appletResourceUserId = context.RequestData.ReadInt64();

            context.ResponseData.Write(_sensorFusionParams.RevisePower);
            context.ResponseData.Write(_sensorFusionParams.ReviseRange);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _sensorFusionParams.RevisePower, _sensorFusionParams.ReviseRange });

            return ResultCode.Success;
        }

        [CommandCmif(72)]
        // ResetSixAxisSensorFusionParameters(nn::hid::SixAxisSensorHandle, nn::applet::AppletResourceUserId)
        public ResultCode ResetSixAxisSensorFusionParameters(ServiceCtx context)
        {
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            long appletResourceUserId = context.RequestData.ReadInt64();

            _sensorFusionParams.RevisePower = 0;
            _sensorFusionParams.ReviseRange = 0;

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _sensorFusionParams.RevisePower, _sensorFusionParams.ReviseRange });

            return ResultCode.Success;
        }

        [CommandCmif(82)]
        // IsSixAxisSensorAtRest(nn::hid::SixAxisSensorHandle, nn::applet::AppletResourceUserId) -> bool IsAsRest
        public ResultCode IsSixAxisSensorAtRest(ServiceCtx context)
        {
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            long appletResourceUserId = context.RequestData.ReadInt64();

            bool isAtRest = true;

            context.ResponseData.Write(isAtRest);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, isAtRest });

            return ResultCode.Success;
        }

        [CommandCmif(83)] // 6.0.0+
        // IsFirmwareUpdateAvailableForSixAxisSensor(nn::hid::AppletResourceUserId, nn::hid::SixAxisSensorHandle, pid) -> bool UpdateAvailable
        public ResultCode IsFirmwareUpdateAvailableForSixAxisSensor(ServiceCtx context)
        {
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            long appletResourceUserId = context.RequestData.ReadInt64();

            context.ResponseData.Write(_isFirmwareUpdateAvailableForSixAxisSensor);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _isFirmwareUpdateAvailableForSixAxisSensor });

            return ResultCode.Success;
        }

        [CommandCmif(84)] // 13.0.0+
        // EnableSixAxisSensorUnalteredPassthrough(nn::applet::AppletResourceUserId, nn::hid::SixAxisSensorHandle, u8 enabled)
        public ResultCode EnableSixAxisSensorUnalteredPassthrough(ServiceCtx context)
        {
            _isSixAxisSensorUnalteredPassthroughEnabled = context.RequestData.ReadUInt32() != 0;
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle, _isSixAxisSensorUnalteredPassthroughEnabled });

            return ResultCode.Success;
        }

        [CommandCmif(85)] // 13.0.0+
        // IsSixAxisSensorUnalteredPassthroughEnabled(nn::applet::AppletResourceUserId, nn::hid::SixAxisSensorHandle) -> u8 enabled
        public ResultCode IsSixAxisSensorUnalteredPassthroughEnabled(ServiceCtx context)
        {
            int sixAxisSensorHandle = context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            long appletResourceUserId = context.RequestData.ReadInt64();

            context.ResponseData.Write(_isSixAxisSensorUnalteredPassthroughEnabled);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, sixAxisSensorHandle });

            return ResultCode.Success;
        }

        [CommandCmif(102)]
        // SetSupportedNpadIdType(nn::applet::AppletResourceUserId, array<NpadIdType, 9>)
        public ResultCode SetSupportedNpadIdType(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long appletResourceUserId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059
            ulong arrayPosition = context.Request.PtrBuff[0].Position;
            ulong arraySize = context.Request.PtrBuff[0].Size;

            ReadOnlySpan<NpadIdType> supportedPlayerIds = MemoryMarshal.Cast<byte, NpadIdType>(context.Memory.GetSpan(arrayPosition, (int)arraySize));

            context.Device.Hid.Npads.ClearSupportedPlayers();

            for (int i = 0; i < supportedPlayerIds.Length; ++i)
            {
                if (HidUtils.IsValidNpadIdType(supportedPlayerIds[i]))
                {
                    context.Device.Hid.Npads.SetSupportedPlayer(HidUtils.GetIndexFromNpadIdType(supportedPlayerIds[i]));
                }
            }

            Logger.Stub?.PrintStub(LogClass.ServiceHid, $"{supportedPlayerIds.Length} Players: " + string.Join(",", supportedPlayerIds.ToArray()));

            return ResultCode.Success;
        }

        [CommandCmif(103)]
        // ActivateNpad(nn::applet::AppletResourceUserId)
        public ResultCode ActivateNpad(ServiceCtx context)
        {
            return ActiveNpadImpl(context);
        }

        [CommandCmif(104)]
        // DeactivateNpad(nn::applet::AppletResourceUserId)
        public ResultCode DeactivateNpad(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();

            context.Device.Hid.Npads.Active = false;
            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return ResultCode.Success;
        }

        [CommandCmif(106)]
        // AcquireNpadStyleSetUpdateEventHandle(nn::applet::AppletResourceUserId, uint, ulong) -> nn::sf::NativeHandle
        public ResultCode AcquireNpadStyleSetUpdateEventHandle(ServiceCtx context)
        {
            PlayerIndex npadId = HidUtils.GetIndexFromNpadIdType((NpadIdType)context.RequestData.ReadInt32());
            long appletResourceUserId = context.RequestData.ReadInt64();
            long npadStyleSet = context.RequestData.ReadInt64();

            KEvent evnt = context.Device.Hid.Npads.GetStyleSetUpdateEvent(npadId);
            if (context.Process.HandleTable.GenerateHandle(evnt.ReadableEvent, out int handle) != Result.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            // Games expect this event to be signaled after calling this function
            evnt.ReadableEvent.Signal();

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, npadId, npadStyleSet });

            return ResultCode.Success;
        }

        [CommandCmif(107)]
        // DisconnectNpad(nn::applet::AppletResourceUserId, uint NpadIdType)
        public ResultCode DisconnectNpad(ServiceCtx context)
        {
            NpadIdType npadIdType = (NpadIdType)context.RequestData.ReadInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, npadIdType });

            return ResultCode.Success;
        }

        [CommandCmif(108)]
        // GetPlayerLedPattern(u32 npad_id) -> u64 led_pattern
        public ResultCode GetPlayerLedPattern(ServiceCtx context)
        {
            NpadIdType npadId = (NpadIdType)context.RequestData.ReadUInt32();

            ulong ledPattern = npadId switch
            {
                NpadIdType.Player1 => 0b0001,
                NpadIdType.Player2 => 0b0011,
                NpadIdType.Player3 => 0b0111,
                NpadIdType.Player4 => 0b1111,
                NpadIdType.Player5 => 0b1001,
                NpadIdType.Player6 => 0b0101,
                NpadIdType.Player7 => 0b1101,
                NpadIdType.Player8 => 0b0110,
                NpadIdType.Unknown => 0b0000,
                NpadIdType.Handheld => 0b0000,
                _ => throw new InvalidOperationException($"{nameof(npadId)} contains an invalid value: {npadId}"),
            };

            context.ResponseData.Write(ledPattern);

            return ResultCode.Success;
        }

        [CommandCmif(109)] // 5.0.0+
        // ActivateNpadWithRevision(nn::applet::AppletResourceUserId, ulong revision)
        public ResultCode ActivateNpadWithRevision(ServiceCtx context)
        {
            ulong revision = context.RequestData.ReadUInt64();

            return ActiveNpadImpl(context, revision);
        }

        private ResultCode ActiveNpadImpl(ServiceCtx context, ulong revision = 0)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();

            context.Device.Hid.Npads.Active = true;

            // Initialize entries to avoid issues with some games.

            List<GamepadInput> emptyGamepadInputs = new();
            List<SixAxisInput> emptySixAxisInputs = new();

            for (int player = 0; player < NpadDevices.MaxControllers; player++)
            {
                GamepadInput gamepadInput = new();
                SixAxisInput sixaxisInput = new();

                gamepadInput.PlayerId = (PlayerIndex)player;
                sixaxisInput.PlayerId = (PlayerIndex)player;

                sixaxisInput.Orientation = new float[9];

                emptyGamepadInputs.Add(gamepadInput);
                emptySixAxisInputs.Add(sixaxisInput);
            }

            for (int entry = 0; entry < Ryujinx.Horizon.Sdk.Hid.Hid.SharedMemEntryCount; entry++)
            {
                context.Device.Hid.Npads.Update(emptyGamepadInputs);
                context.Device.Hid.Npads.UpdateSixAxis(emptySixAxisInputs);
            }

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, revision });

            return ResultCode.Success;
        }

        [CommandCmif(120)]
        // SetNpadJoyHoldType(nn::applet::AppletResourceUserId, ulong NpadJoyHoldType)
        public ResultCode SetNpadJoyHoldType(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long appletResourceUserId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059

            NpadJoyHoldType npadJoyHoldType = (NpadJoyHoldType)context.RequestData.ReadUInt64();

            if (npadJoyHoldType > NpadJoyHoldType.Horizontal)
            {
                throw new InvalidOperationException($"{nameof(npadJoyHoldType)} contains an invalid value: {npadJoyHoldType}");
            }

            foreach (PlayerIndex playerIndex in context.Device.Hid.Npads.GetSupportedPlayers())
            {
                if (HidUtils.GetNpadIdTypeFromIndex(playerIndex) > NpadIdType.Handheld)
                {
                    return ResultCode.InvalidNpadIdType;
                }
            }

            context.Device.Hid.Npads.JoyHold = npadJoyHoldType;

            return ResultCode.Success;
        }

        [CommandCmif(121)]
        // GetNpadJoyHoldType(nn::applet::AppletResourceUserId) -> ulong NpadJoyHoldType
        public ResultCode GetNpadJoyHoldType(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long appletResourceUserId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059

            foreach (PlayerIndex playerIndex in context.Device.Hid.Npads.GetSupportedPlayers())
            {
                if (HidUtils.GetNpadIdTypeFromIndex(playerIndex) > NpadIdType.Handheld)
                {
                    return ResultCode.InvalidNpadIdType;
                }
            }

            context.ResponseData.Write((ulong)context.Device.Hid.Npads.JoyHold);

            return ResultCode.Success;
        }

        [CommandCmif(122)]
        // SetNpadJoyAssignmentModeSingleByDefault(uint HidControllerId, nn::applet::AppletResourceUserId)
        public ResultCode SetNpadJoyAssignmentModeSingleByDefault(ServiceCtx context)
        {
            NpadIdType npadIdType = (NpadIdType)context.RequestData.ReadUInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long appletResourceUserId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059

            if (HidUtils.IsValidNpadIdType(npadIdType))
            {
                context.Device.Hid.SharedMemory.Npads[(int)HidUtils.GetIndexFromNpadIdType(npadIdType)].InternalState.JoyAssignmentMode = NpadJoyAssignmentMode.Single;
            }

            return ResultCode.Success;
        }

        [CommandCmif(123)]
        // SetNpadJoyAssignmentModeSingle(uint npadIdType, nn::applet::AppletResourceUserId, uint npadJoyDeviceType)
        public ResultCode SetNpadJoyAssignmentModeSingle(ServiceCtx context)
        {
            NpadIdType npadIdType = (NpadIdType)context.RequestData.ReadUInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            long appletResourceUserId = context.RequestData.ReadInt64();
            NpadJoyDeviceType npadJoyDeviceType = (NpadJoyDeviceType)context.RequestData.ReadUInt32();

            if (HidUtils.IsValidNpadIdType(npadIdType))
            {
                SetNpadJoyAssignmentModeSingleWithDestinationImpl(context, npadIdType, appletResourceUserId, npadJoyDeviceType, out _, out _);
            }

            return ResultCode.Success;
        }

        [CommandCmif(124)]
        // SetNpadJoyAssignmentModeDual(uint npadIdType, nn::applet::AppletResourceUserId)
        public ResultCode SetNpadJoyAssignmentModeDual(ServiceCtx context)
        {
            NpadIdType npadIdType = (NpadIdType)context.RequestData.ReadUInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long appletResourceUserId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059

            if (HidUtils.IsValidNpadIdType(npadIdType))
            {
                context.Device.Hid.SharedMemory.Npads[(int)HidUtils.GetIndexFromNpadIdType(npadIdType)].InternalState.JoyAssignmentMode = NpadJoyAssignmentMode.Dual;
            }

            return ResultCode.Success;
        }

        [CommandCmif(125)]
        // MergeSingleJoyAsDualJoy(uint npadIdType0, uint npadIdType1, nn::applet::AppletResourceUserId)
        public ResultCode MergeSingleJoyAsDualJoy(ServiceCtx context)
        {
            NpadIdType npadIdType0 = (NpadIdType)context.RequestData.ReadUInt32();
            NpadIdType npadIdType1 = (NpadIdType)context.RequestData.ReadUInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            if (HidUtils.IsValidNpadIdType(npadIdType0) && HidUtils.IsValidNpadIdType(npadIdType1))
            {
                Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, npadIdType0, npadIdType1 });
            }

            return ResultCode.Success;
        }

        [CommandCmif(126)]
        // StartLrAssignmentMode(nn::applet::AppletResourceUserId)
        public ResultCode StartLrAssignmentMode(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return ResultCode.Success;
        }

        [CommandCmif(127)]
        // StopLrAssignmentMode(nn::applet::AppletResourceUserId)
        public ResultCode StopLrAssignmentMode(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId });

            return ResultCode.Success;
        }

        [CommandCmif(128)]
        // SetNpadHandheldActivationMode(nn::applet::AppletResourceUserId, long HidNpadHandheldActivationMode)
        public ResultCode SetNpadHandheldActivationMode(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();
            _npadHandheldActivationMode = (NpadHandheldActivationMode)context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, _npadHandheldActivationMode });

            return ResultCode.Success;
        }

        [CommandCmif(129)]
        // GetNpadHandheldActivationMode(nn::applet::AppletResourceUserId) -> long HidNpadHandheldActivationMode
        public ResultCode GetNpadHandheldActivationMode(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();

            context.ResponseData.Write((long)_npadHandheldActivationMode);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, _npadHandheldActivationMode });

            return ResultCode.Success;
        }

        [CommandCmif(130)]
        // SwapNpadAssignment(uint OldNpadAssignment, uint NewNpadAssignment, nn::applet::AppletResourceUserId)
        public ResultCode SwapNpadAssignment(ServiceCtx context)
        {
            int oldNpadAssignment = context.RequestData.ReadInt32();
            int newNpadAssignment = context.RequestData.ReadInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, oldNpadAssignment, newNpadAssignment });

            return ResultCode.Success;
        }

        [CommandCmif(131)]
        // IsUnintendedHomeButtonInputProtectionEnabled(uint Unknown0, nn::applet::AppletResourceUserId) ->  bool IsEnabled
        public ResultCode IsUnintendedHomeButtonInputProtectionEnabled(ServiceCtx context)
        {
            uint unknown0 = context.RequestData.ReadUInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            context.ResponseData.Write(_unintendedHomeButtonInputProtectionEnabled);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, unknown0, _unintendedHomeButtonInputProtectionEnabled });

            return ResultCode.Success;
        }

        [CommandCmif(132)]
        // EnableUnintendedHomeButtonInputProtection(bool Enable, uint Unknown0, nn::applet::AppletResourceUserId)
        public ResultCode EnableUnintendedHomeButtonInputProtection(ServiceCtx context)
        {
            _unintendedHomeButtonInputProtectionEnabled = context.RequestData.ReadBoolean();
            uint unknown0 = context.RequestData.ReadUInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, unknown0, _unintendedHomeButtonInputProtectionEnabled });

            return ResultCode.Success;
        }

        [CommandCmif(133)] // 5.0.0+
        // SetNpadJoyAssignmentModeSingleWithDestination(uint npadIdType, uint npadJoyDeviceType, nn::applet::AppletResourceUserId) -> bool npadIdTypeIsSet, uint npadIdTypeSet
        public ResultCode SetNpadJoyAssignmentModeSingleWithDestination(ServiceCtx context)
        {
            NpadIdType npadIdType = (NpadIdType)context.RequestData.ReadInt32();
            NpadJoyDeviceType npadJoyDeviceType = (NpadJoyDeviceType)context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            long appletResourceUserId = context.RequestData.ReadInt64();

            if (HidUtils.IsValidNpadIdType(npadIdType))
            {
                SetNpadJoyAssignmentModeSingleWithDestinationImpl(context, npadIdType, appletResourceUserId, npadJoyDeviceType, out NpadIdType npadIdTypeSet, out bool npadIdTypeIsSet);

                if (npadIdTypeIsSet)
                {
                    context.ResponseData.Write(npadIdTypeIsSet);
                    context.ResponseData.Write((uint)npadIdTypeSet);
                }
            }

            return ResultCode.Success;
        }

        private void SetNpadJoyAssignmentModeSingleWithDestinationImpl(ServiceCtx context, NpadIdType npadIdType, long appletResourceUserId, NpadJoyDeviceType npadJoyDeviceType, out NpadIdType npadIdTypeSet, out bool npadIdTypeIsSet)
        {
            npadIdTypeSet = default;
            npadIdTypeIsSet = false;

            context.Device.Hid.SharedMemory.Npads[(int)HidUtils.GetIndexFromNpadIdType(npadIdType)].InternalState.JoyAssignmentMode = NpadJoyAssignmentMode.Single;

            // TODO: Service seems to use the npadJoyDeviceType to find the nearest other Npad available and merge them to dual.
            //       If one is found, it returns the npadIdType of the other Npad and a bool.
            //       If not, it returns nothing.
        }

        [CommandCmif(134)] // 6.1.0+
        // SetNpadUseAnalogStickUseCenterClamp(bool Enable, nn::applet::AppletResourceUserId)
        public ResultCode SetNpadUseAnalogStickUseCenterClamp(ServiceCtx context)
        {
            ulong pid = context.RequestData.ReadUInt64();
            _npadAnalogStickCenterClampEnabled = context.RequestData.ReadUInt32() != 0;
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { pid, appletResourceUserId, _npadAnalogStickCenterClampEnabled });

            return ResultCode.Success;
        }

        [CommandCmif(200)]
        // GetVibrationDeviceInfo(nn::hid::VibrationDeviceHandle) -> nn::hid::VibrationDeviceInfo
        public ResultCode GetVibrationDeviceInfo(ServiceCtx context)
        {
            VibrationDeviceHandle deviceHandle = context.RequestData.ReadStruct<VibrationDeviceHandle>();
            NpadStyleIndex deviceType = (NpadStyleIndex)deviceHandle.DeviceType;
            NpadIdType npadIdType = (NpadIdType)deviceHandle.PlayerId;

            if (deviceType < NpadStyleIndex.System || deviceType >= NpadStyleIndex.FullKey)
            {
                if (!HidUtils.IsValidNpadIdType(npadIdType))
                {
                    return ResultCode.InvalidNpadIdType;
                }

                if (deviceHandle.Position > 1)
                {
                    return ResultCode.InvalidDeviceIndex;
                }

                VibrationDeviceType vibrationDeviceType = VibrationDeviceType.None;

                if (Enum.IsDefined(deviceType))
                {
                    vibrationDeviceType = VibrationDeviceType.LinearResonantActuator;
                }
                else if ((uint)deviceType == 8)
                {
                    vibrationDeviceType = VibrationDeviceType.GcErm;
                }

                VibrationDevicePosition vibrationDevicePosition = VibrationDevicePosition.None;

                if (vibrationDeviceType == VibrationDeviceType.LinearResonantActuator)
                {
                    if (deviceHandle.Position == 0)
                    {
                        vibrationDevicePosition = VibrationDevicePosition.Left;
                    }
                    else if (deviceHandle.Position == 1)
                    {
                        vibrationDevicePosition = VibrationDevicePosition.Right;
                    }
                    else
                    {
                        throw new InvalidOperationException($"{nameof(deviceHandle.Position)} contains an invalid value: {deviceHandle.Position}");
                    }
                }

                VibrationDeviceValue deviceInfo = new()
                {
                    DeviceType = vibrationDeviceType,
                    Position = vibrationDevicePosition,
                };

                context.ResponseData.WriteStruct(deviceInfo);

                return ResultCode.Success;
            }

            return ResultCode.InvalidNpadDeviceType;
        }

        [CommandCmif(201)]
        // SendVibrationValue(nn::hid::VibrationDeviceHandle, nn::hid::VibrationValue, nn::applet::AppletResourceUserId)
        public ResultCode SendVibrationValue(ServiceCtx context)
        {
            VibrationDeviceHandle deviceHandle = new()
            {
                DeviceType = context.RequestData.ReadByte(),
                PlayerId = context.RequestData.ReadByte(),
                Position = context.RequestData.ReadByte(),
                Reserved = context.RequestData.ReadByte(),
            };

            VibrationValue vibrationValue = new()
            {
                AmplitudeLow = context.RequestData.ReadSingle(),
                FrequencyLow = context.RequestData.ReadSingle(),
                AmplitudeHigh = context.RequestData.ReadSingle(),
                FrequencyHigh = context.RequestData.ReadSingle(),
            };

#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long appletResourceUserId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059

            Dictionary<byte, VibrationValue> dualVibrationValues = new()
            {
                [deviceHandle.Position] = vibrationValue,
            };

            context.Device.Hid.Npads.UpdateRumbleQueue((PlayerIndex)deviceHandle.PlayerId, dualVibrationValues);

            return ResultCode.Success;
        }

        [CommandCmif(202)]
        // GetActualVibrationValue(nn::hid::VibrationDeviceHandle, nn::applet::AppletResourceUserId) -> nn::hid::VibrationValue
        public ResultCode GetActualVibrationValue(ServiceCtx context)
        {
            VibrationDeviceHandle deviceHandle = new()
            {
                DeviceType = context.RequestData.ReadByte(),
                PlayerId = context.RequestData.ReadByte(),
                Position = context.RequestData.ReadByte(),
                Reserved = context.RequestData.ReadByte(),
            };

#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long appletResourceUserId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059

            VibrationValue vibrationValue = context.Device.Hid.Npads.GetLastVibrationValue((PlayerIndex)deviceHandle.PlayerId, deviceHandle.Position);

            context.ResponseData.Write(vibrationValue.AmplitudeLow);
            context.ResponseData.Write(vibrationValue.FrequencyLow);
            context.ResponseData.Write(vibrationValue.AmplitudeHigh);
            context.ResponseData.Write(vibrationValue.FrequencyHigh);

            return ResultCode.Success;
        }

        [CommandCmif(203)]
        // CreateActiveVibrationDeviceList() -> object<nn::hid::IActiveVibrationDeviceList>
        public ResultCode CreateActiveVibrationDeviceList(ServiceCtx context)
        {
            MakeObject(context, new IActiveApplicationDeviceList());

            return ResultCode.Success;
        }

        [CommandCmif(204)]
        // PermitVibration(bool Enable)
        public ResultCode PermitVibration(ServiceCtx context)
        {
            _vibrationPermitted = context.RequestData.ReadBoolean();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { _vibrationPermitted });

            return ResultCode.Success;
        }

        [CommandCmif(205)]
        // IsVibrationPermitted() -> bool IsEnabled
        public ResultCode IsVibrationPermitted(ServiceCtx context)
        {
            context.ResponseData.Write(_vibrationPermitted);

            return ResultCode.Success;
        }

        [CommandCmif(206)]
        // SendVibrationValues(nn::applet::AppletResourceUserId, buffer<array<nn::hid::VibrationDeviceHandle>, type: 9>, buffer<array<nn::hid::VibrationValue>, type: 9>)
        public ResultCode SendVibrationValues(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long appletResourceUserId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059

            byte[] vibrationDeviceHandleBuffer = new byte[context.Request.PtrBuff[0].Size];

            context.Memory.Read(context.Request.PtrBuff[0].Position, vibrationDeviceHandleBuffer);

            byte[] vibrationValueBuffer = new byte[context.Request.PtrBuff[1].Size];

            context.Memory.Read(context.Request.PtrBuff[1].Position, vibrationValueBuffer);

            Span<VibrationDeviceHandle> deviceHandles = MemoryMarshal.Cast<byte, VibrationDeviceHandle>(vibrationDeviceHandleBuffer);
            Span<VibrationValue> vibrationValues = MemoryMarshal.Cast<byte, VibrationValue>(vibrationValueBuffer);

            if (!deviceHandles.IsEmpty && vibrationValues.Length == deviceHandles.Length)
            {
                Dictionary<byte, VibrationValue> dualVibrationValues = new();
                PlayerIndex currentIndex = (PlayerIndex)deviceHandles[0].PlayerId;

                for (int deviceCounter = 0; deviceCounter < deviceHandles.Length; deviceCounter++)
                {
                    PlayerIndex index = (PlayerIndex)deviceHandles[deviceCounter].PlayerId;
                    byte position = deviceHandles[deviceCounter].Position;

                    if (index != currentIndex || dualVibrationValues.Count == 2)
                    {
                        context.Device.Hid.Npads.UpdateRumbleQueue(currentIndex, dualVibrationValues);
                        dualVibrationValues = new Dictionary<byte, VibrationValue>();
                    }

                    dualVibrationValues[position] = vibrationValues[deviceCounter];
                    currentIndex = index;
                }

                context.Device.Hid.Npads.UpdateRumbleQueue(currentIndex, dualVibrationValues);
            }

            return ResultCode.Success;
        }

        [CommandCmif(207)] // 4.0.0+
        // SendVibrationGcErmCommand(nn::hid::VibrationDeviceHandle, nn::hid::VibrationGcErmCommand, nn::applet::AppletResourceUserId)
        public ResultCode SendVibrationGcErmCommand(ServiceCtx context)
        {
            int vibrationDeviceHandle = context.RequestData.ReadInt32();
            long vibrationGcErmCommand = context.RequestData.ReadInt64();
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, vibrationDeviceHandle, vibrationGcErmCommand });

            return ResultCode.Success;
        }

        [CommandCmif(208)] // 4.0.0+
        // GetActualVibrationGcErmCommand(nn::hid::VibrationDeviceHandle, nn::applet::AppletResourceUserId) -> nn::hid::VibrationGcErmCommand
        public ResultCode GetActualVibrationGcErmCommand(ServiceCtx context)
        {
            int vibrationDeviceHandle = context.RequestData.ReadInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            context.ResponseData.Write(_vibrationGcErmCommand);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, vibrationDeviceHandle, _vibrationGcErmCommand });

            return ResultCode.Success;
        }

        [CommandCmif(406)] // 4.0.0+
        // GetNpadLeftRightInterfaceType(uint NpadId) -> uchar LeftInterfaceType, uchar RightInterfaceType
        public ResultCode GetNpadLeftRightInterfaceType(ServiceCtx context)
        {
            int npadId = context.RequestData.ReadInt32();

            context.ResponseData.Write((byte)0);
            context.ResponseData.Write((byte)0);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { npadId, LeftInterfaceType = 0, RightInterfaceType = 0 });

            return ResultCode.Success;
        }

        [CommandCmif(500)] // 5.0.0+
        // GetPalmaConnectionHandle(uint Unknown0, nn::applet::AppletResourceUserId) -> nn::hid::PalmaConnectionHandle
        public ResultCode GetPalmaConnectionHandle(ServiceCtx context)
        {
            int unknown0 = context.RequestData.ReadInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            int palmaConnectionHandle = 0;

            context.ResponseData.Write(palmaConnectionHandle);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { appletResourceUserId, unknown0, palmaConnectionHandle });

            return ResultCode.Success;
        }

        [CommandCmif(501)] // 5.0.0+
        // InitializePalma(nn::hid::PalmaConnectionHandle)
        public ResultCode InitializePalma(ServiceCtx context)
        {
            int palmaConnectionHandle = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle });

            _palmaOperationCompleteEvent.ReadableEvent.Signal();

            return ResultCode.Success;
        }

        [CommandCmif(502)] // 5.0.0+
        // AcquirePalmaOperationCompleteEvent(nn::hid::PalmaConnectionHandle) -> nn::sf::NativeHandle
        public ResultCode AcquirePalmaOperationCompleteEvent(ServiceCtx context)
        {
            int palmaConnectionHandle = context.RequestData.ReadInt32();

            if (context.Process.HandleTable.GenerateHandle(_palmaOperationCompleteEvent.ReadableEvent, out int handle) != Result.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle });

            return ResultCode.Success;
        }

        [CommandCmif(503)] // 5.0.0+
        // GetPalmaOperationInfo(nn::hid::PalmaConnectionHandle) -> long Unknown0, buffer<Unknown>
        public ResultCode GetPalmaOperationInfo(ServiceCtx context)
        {
            int palmaConnectionHandle = context.RequestData.ReadInt32();

            long unknown0 = 0; //Counter?

            context.ResponseData.Write(unknown0);

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle, unknown0 });

            return ResultCode.Success;
        }

        [CommandCmif(504)] // 5.0.0+
        // PlayPalmaActivity(nn::hid::PalmaConnectionHandle, ulong Unknown0)
        public ResultCode PlayPalmaActivity(ServiceCtx context)
        {
            int palmaConnectionHandle = context.RequestData.ReadInt32();
            long unknown0 = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle, unknown0 });

            _palmaOperationCompleteEvent.ReadableEvent.Signal();

            return ResultCode.Success;
        }

        [CommandCmif(505)] // 5.0.0+
        // SetPalmaFrModeType(nn::hid::PalmaConnectionHandle, ulong FrModeType)
        public ResultCode SetPalmaFrModeType(ServiceCtx context)
        {
            int palmaConnectionHandle = context.RequestData.ReadInt32();
            long frModeType = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle, frModeType });

            _palmaOperationCompleteEvent.ReadableEvent.Signal();

            return ResultCode.Success;
        }

        [CommandCmif(506)] // 5.0.0+
        // ReadPalmaStep(nn::hid::PalmaConnectionHandle)
        public ResultCode ReadPalmaStep(ServiceCtx context)
        {
            int palmaConnectionHandle = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle });

            return ResultCode.Success;
        }

        [CommandCmif(507)] // 5.0.0+
        // EnablePalmaStep(nn::hid::PalmaConnectionHandle, bool Enable)
        public ResultCode EnablePalmaStep(ServiceCtx context)
        {
            int palmaConnectionHandle = context.RequestData.ReadInt32();
            bool enabledPalmaStep = context.RequestData.ReadBoolean();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle, enabledPalmaStep });

            _palmaOperationCompleteEvent.ReadableEvent.Signal();

            return ResultCode.Success;
        }

        [CommandCmif(508)] // 5.0.0+
        // ResetPalmaStep(nn::hid::PalmaConnectionHandle)
        public ResultCode ResetPalmaStep(ServiceCtx context)
        {
            int palmaConnectionHandle = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle });

            _palmaOperationCompleteEvent.ReadableEvent.Signal();

            return ResultCode.Success;
        }

        [CommandCmif(509)] // 5.0.0+
        // ReadPalmaApplicationSection(nn::hid::PalmaConnectionHandle, ulong Unknown0, ulong Unknown1)
        public ResultCode ReadPalmaApplicationSection(ServiceCtx context)
        {
            int palmaConnectionHandle = context.RequestData.ReadInt32();
            long unknown0 = context.RequestData.ReadInt64();
            long unknown1 = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle, unknown0, unknown1 });

            return ResultCode.Success;
        }

        [CommandCmif(510)] // 5.0.0+
        // WritePalmaApplicationSection(nn::hid::PalmaConnectionHandle, ulong Unknown0, ulong Unknown1, nn::hid::PalmaApplicationSectionAccessBuffer)
        public ResultCode WritePalmaApplicationSection(ServiceCtx context)
        {
            int palmaConnectionHandle = context.RequestData.ReadInt32();
            long unknown0 = context.RequestData.ReadInt64();
            long unknown1 = context.RequestData.ReadInt64();
            // nn::hid::PalmaApplicationSectionAccessBuffer cast is unknown

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { palmaConnectionHandle, unknown0, unknown1 });

            _palmaOperationCompleteEvent.ReadableEvent.Signal();

            return ResultCode.Success;
        }

        [CommandCmif(1004)] // 17.0.0+
        // SetTouchScreenResolution(int width, int height, nn::applet::AppletResourceUserId)
        public ResultCode SetTouchScreenResolution(ServiceCtx context)
        {
            int width = context.RequestData.ReadInt32();
            int height = context.RequestData.ReadInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new { width, height, appletResourceUserId });

            return ResultCode.Success;
        }
    }
}
