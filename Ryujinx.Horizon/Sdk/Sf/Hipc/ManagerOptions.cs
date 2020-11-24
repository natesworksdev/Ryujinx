namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    struct ManagerOptions
    {
        public static ManagerOptions Default => new ManagerOptions(0, 0, 0);

        public int PointerBufferSize { get; }
        public int MaxDomains { get; }
        public int MaxDomainObjects { get; }

        public ManagerOptions(int pointerBufferSize, int maxDomains, int maxDomainObjects)
        {
            PointerBufferSize = pointerBufferSize;
            MaxDomains = maxDomains;
            MaxDomainObjects = maxDomainObjects;
        }
    }
}
