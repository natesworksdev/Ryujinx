using Ryujinx.HLE.HOS.Kernel.Common;

namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    class KDeviceAddressSpace : KAutoObject
    {
        private ulong _address;
        private ulong _size;

        private bool _hasInitialized;

        public KDeviceAddressSpace(Horizon system) : base(system) { }

        public KernelResult Initialize(ulong address, ulong size)
        {
            KernelResult result = KernelResult.Success;

            if (result == KernelResult.Success)
            {
                _address = address;
                _size    = size;

                _hasInitialized = true;
            }

            return result;
        }

        public KernelResult Attach(DeviceName name)
        {
            return KernelResult.Success;
        }
    }
}