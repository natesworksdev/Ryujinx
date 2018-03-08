namespace Ryujinx.Core.OsHle.Handles
{
    class HSharedMem
    {
        public long PA { get; private set; }

        public HSharedMem(long PA)
        {
            this.PA = PA;
        }
    }
}