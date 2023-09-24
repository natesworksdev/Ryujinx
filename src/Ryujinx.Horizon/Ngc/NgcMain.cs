namespace Ryujinx.Horizon.Ngc
{
    class NgcMain : IService
    {
        public static void Main(ServiceTable serviceTable)
        {
            NgcIpcServer ipcServer = new();

            ipcServer.Initialize(HorizonStatic.Options.FsClient);

            serviceTable.SignalServiceReady();

            ipcServer.ServiceRequests();
            ipcServer.Shutdown();
        }
    }
}
