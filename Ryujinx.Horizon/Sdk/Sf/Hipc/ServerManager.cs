namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    class ServerManager : ServerManagerBase
    {
        private readonly object _resourceLock;

        public ServerManager() : this(ManagerOptions.Default)
        {
        }

        public ServerManager(ManagerOptions options) : base(options)
        {
            _resourceLock = new object();
        }
    }
}
