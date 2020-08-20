using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.Memory;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    class HidServerBase : ServerBase
    {
        private readonly KernelContext _context;

        public IAddressSpaceManager HidAddressSpace { get; private set; }

        private int _hidSharedMemoryHandle;
        private int _irSharedMemoryHandle;

        private ulong _hidSharedMemoryBaseAddress;
        private ulong _irSharedMemoryBaseAddress;

        public const int HidSharedMemorySize = 0x40000;
        public const int IrSharedMemorySize = 0x8000;

        public int HidSharedMemoryHandle => _hidSharedMemoryHandle;
        public int IrSharedMemoryHandle => _irSharedMemoryHandle;

        public HidServerBase(KernelContext context) : base(context, "HidServer")
        {
            _context = context;
        }

        protected override void Initialize()
        {
            HidAddressSpace = KernelStatic.AddressSpace;

            Map.LocateMappableSpace(out _hidSharedMemoryBaseAddress, HidSharedMemorySize);

            KernelStatic.Syscall.CreateSharedMemory(
                out _hidSharedMemoryHandle,
                HidSharedMemorySize,
                KMemoryPermission.ReadAndWrite,
                KMemoryPermission.Read);

            KernelStatic.Syscall.MapSharedMemory(
                _hidSharedMemoryHandle,
                _hidSharedMemoryBaseAddress,
                HidSharedMemorySize,
                KMemoryPermission.ReadAndWrite);

            Map.LocateMappableSpace(out _irSharedMemoryBaseAddress, IrSharedMemorySize);

            KernelStatic.Syscall.CreateSharedMemory(
                out _irSharedMemoryHandle,
                IrSharedMemorySize,
                KMemoryPermission.ReadAndWrite,
                KMemoryPermission.Read);

            KernelStatic.Syscall.MapSharedMemory(
                _irSharedMemoryHandle,
                _irSharedMemoryBaseAddress,
                IrSharedMemorySize,
                KMemoryPermission.ReadAndWrite);

            _context.Device.Hid.InitDevices();
        }

        public WritableRegion GetSharedMemory()
        {
            return HidAddressSpace.GetWritableRegion(_hidSharedMemoryBaseAddress, HidSharedMemorySize);
        }
    }
}
