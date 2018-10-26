using System;

namespace ChocolArm64.Events
{
    public class ACpuTraceEventArgs : EventArgs
    {
        public long Position { get; private set; }

        public ACpuTraceEventArgs(long position)
        {
            Position = position;
        }
    }
}