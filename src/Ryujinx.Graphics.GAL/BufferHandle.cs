using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.GAL
{
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public readonly record struct BufferHandle
    {
        public readonly ulong Value;

        public static BufferHandle Null => new BufferHandle(0);
        public static BufferHandle Undefined => new BufferHandle(ulong.MaxValue);

        private BufferHandle(ulong value) => Value = value;
    }
}
