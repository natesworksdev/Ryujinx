namespace Ryujinx.HLE.HOS.Kernel
{
    class KTransferMemory
    {
        public long Position { get; private set; }
        public long Size     { get; private set; }

        public KTransferMemory(long position, long size)
        {
            Position = position;
            Size     = size;
        }
    }
}