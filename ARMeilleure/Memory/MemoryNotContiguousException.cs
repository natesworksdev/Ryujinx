using System;

namespace ARMeilleure.Memory
{
    class MemoryNotContiguousException : Exception
    {
        public MemoryNotContiguousException() : base("The specified memory region is not contiguous.") { }
    }
}