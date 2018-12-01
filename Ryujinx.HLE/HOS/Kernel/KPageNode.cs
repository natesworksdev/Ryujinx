namespace Ryujinx.HLE.HOS.Kernel
{
    struct KPageNode
    {
        public ulong Address;
        public ulong PagesCount;

        public KPageNode(ulong address, ulong pagesCount)
        {
            this.Address    = address;
            this.PagesCount = pagesCount;
        }
    }
}