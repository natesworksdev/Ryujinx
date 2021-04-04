namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    readonly struct KPageNode
    {
        public readonly ulong Address;
        public readonly ulong PagesCount;

        public KPageNode(ulong address, ulong pagesCount)
        {
            Address    = address;
            PagesCount = pagesCount;
        }
    }
}