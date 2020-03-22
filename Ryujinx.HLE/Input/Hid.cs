using Ryujinx.Common;
using System.Runtime.InteropServices;
using Ryujinx.HLE.HOS;

namespace Ryujinx.HLE.Input
{

    public class Hid
    {
        private readonly Switch _device;
        private long _hidMemoryAddress;

        internal ref HidSharedMemory SharedMemory => ref _device.Memory.GetStructRef<HidSharedMemory>(_hidMemoryAddress);
        internal const int SharedMemEntryCount = 17;

        public TouchDevice Touchscreen;
        public MouseDevice Mouse;
        public KeyboardDevice Keyboard;
        public NpadDevices Npads;

        public Hid(in Switch device, long sharedHidMemoryAddress)
        {
            _device = device;
            _hidMemoryAddress = sharedHidMemoryAddress;

            if (Marshal.SizeOf<HidSharedMemory>() != Horizon.HidSize)
            {
                // use a better exception here?
                throw new System.DataMisalignedException("HidSharedMemory struct is the wrong size!");
            }

            device.Memory.FillWithZeros(sharedHidMemoryAddress, Horizon.HidSize);
        }

        public void InitDevices()
        {
            Touchscreen = new TouchDevice(_device, true);
            Mouse = new MouseDevice(_device, false);
            Keyboard = new KeyboardDevice(_device, false);
            Npads = new NpadDevices(_device, true);
        }

        public ControllerKeys UpdateStickButtons(JoystickPosition leftStick, JoystickPosition rightStick)
        {
            ControllerKeys result = 0;

            result |= (leftStick.Dx < 0) ? ControllerKeys.LStickLeft : result;
            result |= (leftStick.Dx > 0) ? ControllerKeys.LStickRight : result;
            result |= (leftStick.Dy < 0) ? ControllerKeys.LStickDown : result;
            result |= (leftStick.Dy > 0) ? ControllerKeys.LStickUp : result;

            result |= (rightStick.Dx < 0) ? ControllerKeys.RStickLeft : result;
            result |= (rightStick.Dx > 0) ? ControllerKeys.RStickRight : result;
            result |= (rightStick.Dy < 0) ? ControllerKeys.RStickDown : result;
            result |= (rightStick.Dy > 0) ? ControllerKeys.RStickUp : result;

            return result;
        }

        internal static ulong GetTimestampTicks()
        {
            return (ulong)PerformanceCounter.ElapsedMilliseconds * 19200;
        }

    }
}
