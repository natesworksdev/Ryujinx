using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;

namespace Ryujinx.Horizon.Sdk.Sf
{
    [AttributeUsage(AttributeTargets.Parameter)]
    sealed class BufferAttribute : Attribute
    {
        public HipcBufferFlags Flags     { get; }
        public ushort          FixedSize { get; }

        public BufferAttribute(HipcBufferFlags flags)
        {
            Flags = flags;
        }

        public BufferAttribute(HipcBufferFlags flags, ushort fixedSize)
        {
            Flags     = flags | HipcBufferFlags.FixedSize;
            FixedSize = fixedSize;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    sealed class ClientProcessIdAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    sealed class CopyHandleAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    sealed class MoveHandleAttribute : Attribute
    {
    }
}