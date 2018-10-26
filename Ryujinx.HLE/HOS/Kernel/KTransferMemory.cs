namespace Ryujinx.HLE.HOS.Kernel
{
    internal class KTransferMemory
    {
        public long Position { get; private set; }
        public long Size     { get; private set; }

        public KTransferMemory(long position, long size)
        {
            this.Position = position;
            this.Size     = size;
        }
    }
}