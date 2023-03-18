using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Common
{
    /// <summary>
    /// This is a "marker interface" to add some compile-time safety to a convention-based optimization.
    /// Any struct implementing this interface must use its first 8 bytes as a "Sampling Number".
    /// This is most often accomplished by using <c>StructLayoutAttribute</c> and careful member ordering,
    /// such that the first member is a <c>ulong</c> (.NET type: <c>System.UInt64</c>) field.
    /// 
    /// Example:
    /// 
    /// <c>
    ///         [StructLayout(LayoutKind.Sequential, Pack = 1)]
    ///         struct DebugPadState : ISampledDataStruct
    ///         {
    ///             public ulong SamplingNumber;
    ///             
    ///             // other members...
    ///         }
    /// </c>
    /// </summary>
    internal interface ISampledDataStruct
    {
        // No Instance Members - marker interface only

        public static ulong GetSamplingNumber<T>(ref T sampledDataStruct) where T : unmanaged, ISampledDataStruct
        {
            ReadOnlySpan<T> structSpan = MemoryMarshal.CreateReadOnlySpan(ref sampledDataStruct, 1);

            ReadOnlySpan<byte> byteSpan = MemoryMarshal.Cast<T, byte>(structSpan);

            ulong value = BitConverter.IsLittleEndian
                ? BinaryPrimitives.ReadUInt64LittleEndian(byteSpan)
                : BinaryPrimitives.ReadUInt64BigEndian(byteSpan);

            return value;
        }
    }
}
