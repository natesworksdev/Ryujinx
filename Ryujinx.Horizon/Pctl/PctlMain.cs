namespace Ryujinx.Horizon.Pctl
{
    class PctlMain : IService
    {
        public static void Main()
        {
            PctlIpcServer ipcServer = new PctlIpcServer();

            ipcServer.Initialize();
            ipcServer.ServiceRequests();
            ipcServer.Shutdown();
        }
    }
}