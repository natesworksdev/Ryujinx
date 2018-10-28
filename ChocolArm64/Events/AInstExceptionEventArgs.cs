using System;

namespace ChocolArm64.Events
{
    public class AInstExceptionEventArgs : EventArgs
    {
        public long Position { get; private set; }
        public int  Id       { get; private set; }

        public AInstExceptionEventArgs(long position, int id)
        {
            Position = position;
            Id       = id;
        }
    }
}