using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct TouchPoint
    {
        public uint X;
        public uint Y;
        public uint DiameterX;
        public uint DiameterY;
        public uint Angle;
    }

    public class TouchDevice : BaseDevice
    {
        public TouchDevice(Switch device, bool active) : base(device, active)
        {
            if (Marshal.SizeOf<HidTouchScreen>() != 0x3000)
            {
                throw new System.DataMisalignedException($"HidController struct is the wrong size! Expected:0x3000 Got:{Marshal.SizeOf<HidTouchScreen>()}");
            }
        }

        public void Update(params TouchPoint[] points)
        {
            ref HidTouchScreen touchscreen = ref _device.Hid.SharedMemory.Touchscreen;

            int prevIndex;
            int curIndex = UpdateEntriesHeader(ref touchscreen.Header, out prevIndex);

            if (!Active) return;

            ref HidTouchScreenEntry curEntry = ref touchscreen.Entries[curIndex];
            HidTouchScreenEntry prevEntry = touchscreen.Entries[prevIndex];

            curEntry.Header.SampleTimestamp = prevEntry.Header.SampleTimestamp + 1;
            curEntry.Header.SampleTimestamp2 = prevEntry.Header.SampleTimestamp2 + 1;

            curEntry.Header.NumTouches = (ulong)points.Length;

            int pointsLength = Math.Min(points.Length, curEntry.Touches.Length);

            for (int i = 0; i < pointsLength; ++i)
            {
                TouchPoint pi = points[i];
                curEntry.Touches[i] = new HidTouchScreenEntryTouch
                {
                    SampleTimestamp = curEntry.Header.SampleTimestamp,
                    X = pi.X,
                    Y = pi.Y,
                    TouchIndex = (uint)i,
                    DiameterX = pi.DiameterX,
                    DiameterY = pi.DiameterY,
                    Angle = pi.Angle
                };
            }
        }

    }
}