using Ryujinx.Common;
using Ryujinx.Configuration.Hid;
using Ryujinx.HLE.HOS;
using System;

namespace Ryujinx.HLE.Input
{
    public partial class Hid
    {
        private readonly Switch _device;

        private ulong _sharedMemoryAddress;

        internal ref HidSharedMemory SharedMemory => ref _device.Memory.GetRef<HidSharedMemory>(_sharedMemoryAddress);

        public BaseController PrimaryController { get; private set; }

        public Hid(Switch device, ulong sharedMemoryAddress)
        {
            _device              = device;
            _sharedMemoryAddress = sharedMemoryAddress;

            device.Memory.ZeroFill(sharedMemoryAddress, Horizon.HidSize);
        }

        private static ControllerStatus ConvertControllerTypeToState(ControllerType controllerType)
        {
            return controllerType switch
            {
                ControllerType.Handheld => ControllerStatus.Handheld,
                ControllerType.NpadLeft => ControllerStatus.NpadLeft,
                ControllerType.NpadRight => ControllerStatus.NpadRight,
                ControllerType.NpadPair => ControllerStatus.NpadPair,
                ControllerType.ProController => ControllerStatus.ProController,
                _ => throw new NotImplementedException(),
            };
        }

        public void InitializePrimaryController(ControllerType controllerType)
        {
            ControllerId controllerId = controllerType == ControllerType.Handheld ?
                ControllerId.ControllerHandheld : ControllerId.ControllerPlayer1;

            if (controllerType == ControllerType.ProController)
            {
                PrimaryController = new ProController(_device, NpadColor.Black, NpadColor.Black);
            }
            else
            {
                PrimaryController = new NpadController(
                    ConvertControllerTypeToState(controllerType),
                    _device,
                    (NpadColor.BodyNeonRed,     NpadColor.BodyNeonRed),
                    (NpadColor.ButtonsNeonBlue, NpadColor.ButtonsNeonBlue));
            }

            PrimaryController.Connect(controllerId);
        }

        public static ControllerButtons UpdateStickButtons(JoystickPosition leftStick, JoystickPosition rightStick)
        {
            ControllerButtons result = 0;

            if (rightStick.Dx < 0)
            {
                result |= ControllerButtons.RStickLeft;
            }

            if (rightStick.Dx > 0)
            {
                result |= ControllerButtons.RStickRight;
            }

            if (rightStick.Dy < 0)
            {
                result |= ControllerButtons.RStickDown;
            }

            if (rightStick.Dy > 0)
            {
                result |= ControllerButtons.RStickUp;
            }

            if (leftStick.Dx < 0)
            {
                result |= ControllerButtons.LStickLeft;
            }

            if (leftStick.Dx > 0)
            {
                result |= ControllerButtons.LStickRight;
            }

            if (leftStick.Dy < 0)
            {
                result |= ControllerButtons.LStickDown;
            }

            if (leftStick.Dy > 0)
            {
                result |= ControllerButtons.LStickUp;
            }

            return result;
        }

        public void SetTouchPoints(params TouchPoint[] points)
        {
            ref HidSharedMemory sharedMemory = ref SharedMemory;

            ref HidTouchScreen touchScreen = ref sharedMemory.Touchscreen;

            touchScreen.Header.NumEntries    = 17;
            touchScreen.Header.MaxEntryIndex = 16;

            touchScreen.Header.LatestEntry = (touchScreen.Header.LatestEntry + 1) % 17;

            touchScreen.Header.TimestampTicks = GetTimestamp();

            ulong timestamp = touchScreen.Header.Timestamp + 1;

            touchScreen.Header.Timestamp = timestamp;

            ref HidTouchScreenEntry entry = ref touchScreen.Entries[(int)touchScreen.Header.LatestEntry];

            entry.Header.Timestamp  = timestamp;
            entry.Header.NumTouches = (ulong)points.Length;

            for (int index = 0; index < points.Length; index++)
            {
                entry.Touches[index] = new HidTouchScreenEntryTouch()
                {
                    Timestamp  = timestamp,
                    TouchIndex = (uint)index,
                    X          = points[index].X,
                    Y          = points[index].Y,
                    DiameterX  = points[index].DiameterX,
                    DiameterY  = points[index].DiameterY,
                    Angle      = points[index].Angle
                };
            }
        }

        public unsafe void WriteKeyboard(Keyboard keyboard)
        {
            ref HidSharedMemory sharedMemory = ref SharedMemory;

            ref HidKeyboard kbd = ref sharedMemory.Keyboard;

            kbd.Header.NumEntries    = 17;
            kbd.Header.MaxEntryIndex = 16;

            kbd.Header.LatestEntry = (kbd.Header.LatestEntry + 1) % 17;

            kbd.Header.TimestampTicks = GetTimestamp();

            kbd.Entries[(int)kbd.Header.LatestEntry].Timestamp++;
            kbd.Entries[(int)kbd.Header.LatestEntry].Timestamp2++;

            for (int index = 0; index < keyboard.Keys.Length; index++)
            {
                kbd.Entries[(int)kbd.Header.LatestEntry].Keys[index] = keyboard.Keys[index];
            }
            
            kbd.Entries[(int)kbd.Header.LatestEntry].Modifier = keyboard.Modifier;
        }

        internal static ulong GetTimestamp()
        {
            return (ulong)PerformanceCounter.ElapsedMilliseconds * 19200;
        }
    }
}
