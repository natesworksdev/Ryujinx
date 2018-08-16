namespace Ryujinx.HLE.Loaders.Npdm
{
    struct KernelAccessControlMmio
    {
        public ulong Address;
        public ulong Size;
        public bool  IsRo;
        public bool  IsNormal;

        public KernelAccessControlMmio(
            ulong Address,
            ulong Size,
            bool  IsRo,
            bool  IsNormal)
        {
            this.Address  = Address;
            this.Size     = Size;
            this.IsRo     = IsRo;
            this.IsNormal = IsNormal;
        }
    }
}