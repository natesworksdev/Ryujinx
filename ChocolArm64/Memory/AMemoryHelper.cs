using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ChocolArm64.Memory
{
    public static class AMemoryHelper
    {
        public static void FillWithZeros(AMemory memory, long position, int size)
        {
            int size8 = size & ~(8 - 1);

            for (int offs = 0; offs < size8; offs += 8) memory.WriteInt64(position + offs, 0);

            for (int offs = size8; offs < size - size8; offs++) memory.WriteByte(position + offs, 0);
        }

        public static unsafe T Read<T>(AMemory memory, long position) where T : struct
        {
            long size = Marshal.SizeOf<T>();

            memory.EnsureRangeIsValid(position, size);

            IntPtr ptr = (IntPtr)memory.Translate(position);

            return Marshal.PtrToStructure<T>(ptr);
        }

        public static unsafe void Write<T>(AMemory memory, long position, T value) where T : struct
        {
            long size = Marshal.SizeOf<T>();

            memory.EnsureRangeIsValid(position, size);

            IntPtr ptr = (IntPtr)memory.TranslateWrite(position);

            Marshal.StructureToPtr<T>(value, ptr, false);
        }

        public static string ReadAsciiString(AMemory memory, long position, long maxSize = -1)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                for (long offs = 0; offs < maxSize || maxSize == -1; offs++)
                {
                    byte value = (byte)memory.ReadByte(position + offs);

                    if (value == 0) break;

                    ms.WriteByte(value);
                }

                return Encoding.ASCII.GetString(ms.ToArray());
            }
        }
    }
}