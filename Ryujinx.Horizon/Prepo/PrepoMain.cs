namespace Ryujinx.Horizon.Prepo
{
    class PrepoMain : IService
    {
        public static void Main()
        {
            PrepoIpcServer ipcServer = new();

            ipcServer.Initialize();
            ipcServer.ServiceRequests();
            ipcServer.Shutdown();
        }
    }
}