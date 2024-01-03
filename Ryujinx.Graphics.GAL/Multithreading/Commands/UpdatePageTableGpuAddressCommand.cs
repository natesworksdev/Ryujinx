namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct UpdatePageTableGpuAddressCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.UpdatePageTableGpuAddress;
        private ulong _address;

        public void Set(ulong address)
        {
            _address = address;
        }

        public static void Run(ref UpdatePageTableGpuAddressCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.UpdatePageTableGpuAddress(command._address);
        }
    }
}
