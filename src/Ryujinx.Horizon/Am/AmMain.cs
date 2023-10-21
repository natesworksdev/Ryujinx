namespace Ryujinx.Horizon.Am
{
    class AmMain : IService
    {
        public static void Main(ServiceTable serviceTable)
        {
            AmIpcServer ipcServer = new();

            ipcServer.Initialize();

            serviceTable.SignalServiceReady();

            ipcServer.ServiceRequests();
            ipcServer.Shutdown();
        }
    }
}
