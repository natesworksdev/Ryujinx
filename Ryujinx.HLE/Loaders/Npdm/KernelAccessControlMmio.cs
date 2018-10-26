namespace Ryujinx.HLE.Loaders.Npdm
{
    internal struct KernelAccessControlMmio
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
            Address  = address;
            Size     = size;
            IsRo     = isRo;
            IsNormal = isNormal;
        }
    }
}