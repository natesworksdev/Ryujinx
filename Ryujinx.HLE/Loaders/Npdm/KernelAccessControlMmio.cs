namespace Ryujinx.HLE.Loaders.Npdm
{
    struct KernelAccessControlMmio
    {
        public ulong Address  { get; private set; }
        public ulong Size     { get; private set; }
        public bool  IsRo     { get; private set; }
        public bool  IsNormal { get; private set; }

        public KernelAccessControlMmio(
            ulong address,
            ulong size,
            bool  isRo,
            bool  isNormal)
        {
            this.Address  = address;
            this.Size     = size;
            this.IsRo     = isRo;
            this.IsNormal = isNormal;
        }
    }
}