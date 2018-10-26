using ChocolArm64.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.Utilities
{
    internal class StructWriter
    {
        private AMemory _memory;

        public long Position { get; private set; }

        public StructWriter(AMemory memory, long position)
        {
            _memory   = memory;
            Position = position;
        }

        public void Write<T>(T value) where T : struct
        {
            AMemoryHelper.Write(_memory, Position, value);

            Position += Marshal.SizeOf<T>();
        }
    }
}
