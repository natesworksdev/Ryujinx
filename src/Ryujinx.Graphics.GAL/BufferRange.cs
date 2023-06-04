namespace Ryujinx.Graphics.GAL
{
    public readonly struct BufferRange
    {
        public static BufferRange Empty { get; } = new(BufferHandle.Null, 0, 0);

        public BufferHandle Handle { get; }

        public int Offset { get; }
        public int Size { get; }

        public BufferRange(BufferHandle handle, int offset, int size)
        {
            Handle = handle;
            Offset = offset;
            Size = size;
        }
    }
}