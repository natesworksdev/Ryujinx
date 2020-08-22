namespace Ryujinx.Horizon.Kernel.Memory
{
    struct KPageNode
    {
        public ulong Address;
        public ulong PagesCount;

        public KPageNode(ulong address, ulong pagesCount)
        {
            Address = address;
            PagesCount = pagesCount;
        }
    }
}