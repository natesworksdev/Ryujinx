namespace Ryujinx.HLE.HOS.Kernel
{
    struct KPageNode
    {
        public long Address;
        public long PagesCount;

        public KPageNode(long Address, long PagesCount)
        {
            this.Address    = Address;
            this.PagesCount = PagesCount;
        }
    }
}