using ChocolArm64.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.Utilities
{
    class StructReader
    {
        private AMemory _memory;

        public long Position { get; private set; }

        public StructReader(AMemory memory, long position)
        {
            this._memory   = memory;
            this.Position = position;
        }

        public T Read<T>() where T : struct
        {
            T value = AMemoryHelper.Read<T>(_memory, Position);

            Position += Marshal.SizeOf<T>();

            return value;
        }

        public T[] Read<T>(int size) where T : struct
        {
            int structSize = Marshal.SizeOf<T>();

            int count = size / structSize;

            T[] output = new T[count];

            for (int index = 0; index < count; index++)
            {
                output[index] = AMemoryHelper.Read<T>(_memory, Position);

                Position += structSize;
            }

            return output;
        }
    }
}
