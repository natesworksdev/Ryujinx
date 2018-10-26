using Ryujinx.HLE.HOS;
using System;

namespace Ryujinx.HLE.Input
{
    public class Hid
    {
        /*
         * Reference:
         * https://github.com/reswitched/libtransistor/blob/development/lib/hid.c
         * https://github.com/reswitched/libtransistor/blob/development/include/libtransistor/hid.h
         * https://github.com/switchbrew/libnx/blob/master/nx/source/services/hid.c
         * https://github.com/switchbrew/libnx/blob/master/nx/include/switch/services/hid.h
         */

        private const int HidHeaderSize            = 0x400;
        private const int HidTouchScreenSize       = 0x3000;
        private const int HidMouseSize             = 0x400;
        private const int HidKeyboardSize          = 0x400;
        private const int HidUnkSection1Size       = 0x400;
        private const int HidUnkSection2Size       = 0x400;
        private const int HidUnkSection3Size       = 0x400;
        private const int HidUnkSection4Size       = 0x400;
        private const int HidUnkSection5Size       = 0x200;
        private const int HidUnkSection6Size       = 0x200;
        private const int HidUnkSection7Size       = 0x200;
        private const int HidUnkSection8Size       = 0x800;
        private const int HidControllerSerialsSize = 0x4000;
        private const int HidControllersSize       = 0x32000;
        private const int HidUnkSection9Size       = 0x800;

        private const int HidTouchHeaderSize = 0x28;
        private const int HidTouchEntrySize  = 0x298;

        private const int HidTouchEntryHeaderSize = 0x10;
        private const int HidTouchEntryTouchSize  = 0x28;

        private const int HidControllerSize        = 0x5000;
        private const int HidControllerHeaderSize  = 0x28;
        private const int HidControllerLayoutsSize = 0x350;

        private const int HidControllersLayoutHeaderSize = 0x20;
        private const int HidControllersInputEntrySize   = 0x30;

        private const int HidHeaderOffset            = 0;
        private const int HidTouchScreenOffset       = HidHeaderOffset            + HidHeaderSize;
        private const int HidMouseOffset             = HidTouchScreenOffset       + HidTouchScreenSize;
        private const int HidKeyboardOffset          = HidMouseOffset             + HidMouseSize;
        private const int HidUnkSection1Offset       = HidKeyboardOffset          + HidKeyboardSize;
        private const int HidUnkSection2Offset       = HidUnkSection1Offset       + HidUnkSection1Size;
        private const int HidUnkSection3Offset       = HidUnkSection2Offset       + HidUnkSection2Size;
        private const int HidUnkSection4Offset       = HidUnkSection3Offset       + HidUnkSection3Size;
        private const int HidUnkSection5Offset       = HidUnkSection4Offset       + HidUnkSection4Size;
        private const int HidUnkSection6Offset       = HidUnkSection5Offset       + HidUnkSection5Size;
        private const int HidUnkSection7Offset       = HidUnkSection6Offset       + HidUnkSection6Size;
        private const int HidUnkSection8Offset       = HidUnkSection7Offset       + HidUnkSection7Size;
        private const int HidControllerSerialsOffset = HidUnkSection8Offset       + HidUnkSection8Size;
        private const int HidControllersOffset       = HidControllerSerialsOffset + HidControllerSerialsSize;
        private const int HidUnkSection9Offset       = HidControllersOffset       + HidControllersSize;

        private const int HidEntryCount = 17;

        private Switch _device;

        private long _hidPosition;

        public Hid(Switch device, long hidPosition)
        {
            _device      = device;
            _hidPosition = hidPosition;

            device.Memory.FillWithZeros(hidPosition, Horizon.HidSize);

            InitializeJoyconPair(
                JoyConColor.BodyNeonRed,
                JoyConColor.ButtonsNeonRed,
                JoyConColor.BodyNeonBlue,
                JoyConColor.ButtonsNeonBlue);
        }

        private void InitializeJoyconPair(
            JoyConColor leftColorBody,
            JoyConColor leftColorButtons,
            JoyConColor rightColorBody,
            JoyConColor rightColorButtons)
        {
            long baseControllerOffset = _hidPosition + HidControllersOffset + 8 * HidControllerSize;

            HidControllerType type = HidControllerType.ControllerTypeHandheld;

            bool isHalf = false;

            HidControllerColorDesc singleColorDesc =
                HidControllerColorDesc.ColorDescColorsNonexistent;

            JoyConColor singleColorBody    = JoyConColor.Black;
            JoyConColor singleColorButtons = JoyConColor.Black;

            HidControllerColorDesc splitColorDesc = 0;

            _device.Memory.WriteInt32(baseControllerOffset + 0x0,  (int)type);

            _device.Memory.WriteInt32(baseControllerOffset + 0x4,  isHalf ? 1 : 0);

            _device.Memory.WriteInt32(baseControllerOffset + 0x8,  (int)singleColorDesc);
            _device.Memory.WriteInt32(baseControllerOffset + 0xc,  (int)singleColorBody);
            _device.Memory.WriteInt32(baseControllerOffset + 0x10, (int)singleColorButtons);
            _device.Memory.WriteInt32(baseControllerOffset + 0x14, (int)splitColorDesc);

            _device.Memory.WriteInt32(baseControllerOffset + 0x18, (int)leftColorBody);
            _device.Memory.WriteInt32(baseControllerOffset + 0x1c, (int)leftColorButtons);

            _device.Memory.WriteInt32(baseControllerOffset + 0x20, (int)rightColorBody);
            _device.Memory.WriteInt32(baseControllerOffset + 0x24, (int)rightColorButtons);
        }

        public void SetJoyconButton(
            HidControllerId      controllerId,
            HidControllerLayouts controllerLayout,
            HidControllerButtons buttons,
            HidJoystickPosition  leftStick,
            HidJoystickPosition  rightStick)
        {
            long controllerOffset = _hidPosition + HidControllersOffset;

            controllerOffset += (int)controllerId * HidControllerSize;

            controllerOffset += HidControllerHeaderSize;

            controllerOffset += (int)controllerLayout * HidControllerLayoutsSize;

            long lastEntry = _device.Memory.ReadInt64(controllerOffset + 0x10);

            long currEntry = (lastEntry + 1) % HidEntryCount;

            long timestamp = GetTimestamp();

            _device.Memory.WriteInt64(controllerOffset + 0x0,  timestamp);
            _device.Memory.WriteInt64(controllerOffset + 0x8,  HidEntryCount);
            _device.Memory.WriteInt64(controllerOffset + 0x10, currEntry);
            _device.Memory.WriteInt64(controllerOffset + 0x18, HidEntryCount - 1);

            controllerOffset += HidControllersLayoutHeaderSize;

            long lastEntryOffset = controllerOffset + lastEntry * HidControllersInputEntrySize;

            controllerOffset += currEntry * HidControllersInputEntrySize;

            long sampleCounter = _device.Memory.ReadInt64(lastEntryOffset) + 1;

            _device.Memory.WriteInt64(controllerOffset + 0x0, sampleCounter);
            _device.Memory.WriteInt64(controllerOffset + 0x8, sampleCounter);

            _device.Memory.WriteInt64(controllerOffset + 0x10, (uint)buttons);

            _device.Memory.WriteInt32(controllerOffset + 0x18, leftStick.Dx);
            _device.Memory.WriteInt32(controllerOffset + 0x1c, leftStick.Dy);

            _device.Memory.WriteInt32(controllerOffset + 0x20, rightStick.Dx);
            _device.Memory.WriteInt32(controllerOffset + 0x24, rightStick.Dy);

            _device.Memory.WriteInt64(controllerOffset + 0x28,
                (uint)HidControllerConnState.ControllerStateConnected |
                (uint)HidControllerConnState.ControllerStateWired);
        }

        public void SetTouchPoints(params HidTouchPoint[] points)
        {
            long touchScreenOffset = _hidPosition + HidTouchScreenOffset;

            long lastEntry = _device.Memory.ReadInt64(touchScreenOffset + 0x10);

            long currEntry = (lastEntry + 1) % HidEntryCount;

            long timestamp = GetTimestamp();

            _device.Memory.WriteInt64(touchScreenOffset + 0x0,  timestamp);
            _device.Memory.WriteInt64(touchScreenOffset + 0x8,  HidEntryCount);
            _device.Memory.WriteInt64(touchScreenOffset + 0x10, currEntry);
            _device.Memory.WriteInt64(touchScreenOffset + 0x18, HidEntryCount - 1);
            _device.Memory.WriteInt64(touchScreenOffset + 0x20, timestamp);

            long touchEntryOffset = touchScreenOffset + HidTouchHeaderSize;

            long lastEntryOffset = touchEntryOffset + lastEntry * HidTouchEntrySize;

            long sampleCounter = _device.Memory.ReadInt64(lastEntryOffset) + 1;

            touchEntryOffset += currEntry * HidTouchEntrySize;

            _device.Memory.WriteInt64(touchEntryOffset + 0x0, sampleCounter);
            _device.Memory.WriteInt64(touchEntryOffset + 0x8, points.Length);

            touchEntryOffset += HidTouchEntryHeaderSize;

            const int padding = 0;

            int index = 0;

            foreach (HidTouchPoint point in points)
            {
                _device.Memory.WriteInt64(touchEntryOffset + 0x0,  timestamp);
                _device.Memory.WriteInt32(touchEntryOffset + 0x8,  padding);
                _device.Memory.WriteInt32(touchEntryOffset + 0xc,  index++);
                _device.Memory.WriteInt32(touchEntryOffset + 0x10, point.X);
                _device.Memory.WriteInt32(touchEntryOffset + 0x14, point.Y);
                _device.Memory.WriteInt32(touchEntryOffset + 0x18, point.DiameterX);
                _device.Memory.WriteInt32(touchEntryOffset + 0x1c, point.DiameterY);
                _device.Memory.WriteInt32(touchEntryOffset + 0x20, point.Angle);
                _device.Memory.WriteInt32(touchEntryOffset + 0x24, padding);

                touchEntryOffset += HidTouchEntryTouchSize;
            }
        }

        private static long GetTimestamp()
        {
            return (long)((ulong)Environment.TickCount * 19_200);
        }
    }
}
