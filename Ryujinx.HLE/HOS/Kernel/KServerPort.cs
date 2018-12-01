namespace Ryujinx.HLE.HOS.Kernel
{
    internal class KServerPort : KSynchronizationObject
    {
        private KPort _parent;

        public KServerPort(Horizon system) : base(system) { }

        public void Initialize(KPort parent)
        {
            _parent = parent;
        }
    }
}