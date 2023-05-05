using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.IO;
using Ryujinx.Common.Memory;
using Ryujinx.Memory;

namespace Ryujinx.Cpu
{
    public static class MemoryHelper
    {
        private static readonly RecyclableMemoryStreamManager s_memoryStreamManager = new RecyclableMemoryStreamManager();

        public static void FillWithZeros(IVirtualMemoryManager memory, ulong position, int size)
        {
            int size8 = size & ~(8 - 1);

            for (int offs = 0; offs < size8; offs += 8)
            {
                memory.Write<long>(position + (ulong)offs, 0);
            }

            for (int offs = size8; offs < (size - size8); offs++)
            {
                memory.Write<byte>(position + (ulong)offs, 0);
            }
        }

        public unsafe static T Read<T>(IVirtualMemoryManager memory, ulong position) where T : unmanaged
        {
            ReadOnlySpan<byte> span = memory.GetSpan(position, Unsafe.SizeOf<T>());
            return MemoryMarshal.Read<T>(span);
        }

        public unsafe static ulong Write<T>(IVirtualMemoryManager memory, ulong position, T value) where T : unmanaged
        {
            Span<byte> span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
            memory.Write(position, span);
            return (ulong)span.Length;
        }

        public static string ReadAsciiString(IVirtualMemoryManager memory, ulong position, long maxSize = -1)
        {
            using (MemoryStream ms = s_memoryStreamManager.GetStream())
            {
                for (long offs = 0; offs < maxSize || maxSize == -1; offs++)
                {
                    byte value = memory.Read<byte>(position + (ulong)offs);

                    if (value == 0)
                    {
                        break;
                    }

                    ms.WriteByte(value);
                }

                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(ms.GetBuffer(), 0, (int)ms.Length);
                return Encoding.ASCII.GetString(span);
            }
        }
    }
}
