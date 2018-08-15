namespace Ryujinx.HLE.OsHle.Kernel
{
    class KTransferMemory
    {
        public long Position { get; private set; }
        public long Size     { get; private set; }

        public KTransferMemory(long Position, long Size)
        {
            this.Position = Position;
            this.Size     = Size;
        }
    }
}