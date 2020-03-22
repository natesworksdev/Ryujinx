using System;
using static Ryujinx.HLE.Input.Hid;

namespace Ryujinx.HLE.Input
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
        public TouchDevice(Switch device, bool active) : base(device, active) { }

        public void Update(params TouchPoint[] points)
        {
            ref var touchscreen = ref _device.Hid.SharedMemory.Touchscreen;

            int curIndex = UpdateEntriesHeader(ref touchscreen.Header, out _);
            var sampleCounter = ++touchscreen.SequenceNumber;

            if (!Active) return;

            ref var curEntry = ref touchscreen.Entries[curIndex];
            curEntry.Header.SequenceNumber = sampleCounter;
            curEntry.Header.NumTouches = (ulong)points.Length;

            var pointsLength = Math.Min(points.Length, curEntry.Touches.Length);

            for (int i = 0; i < pointsLength; ++i)
            {
                var pi = points[i];
                curEntry.Touches[i] = new HidTouchScreenEntryTouch
                {
                    SequenceNumber = sampleCounter,
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