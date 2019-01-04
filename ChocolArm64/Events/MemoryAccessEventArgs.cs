using System;

namespace ChocolArm64.Events
{
    public class MemoryAccessEventArgs : EventArgs
    {
        public long VirtualAddress { get; }

        public MemoryAccessEventArgs(long va)
        {
            VirtualAddress  = va;
        }
    }
}