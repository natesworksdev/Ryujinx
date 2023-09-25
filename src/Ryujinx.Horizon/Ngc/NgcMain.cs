namespace Ryujinx.Horizon.Ngc
{
    class NgcMain : IService
    {
        public static void Main(ServiceTable serviceTable)
        {
            NgcIpcServer ipcServer = new();

            ipcServer.Initialize(HorizonStatic.Options.FsClient);

            // TODO: Notification thread, requires implementing OpenSystemDataUpdateEventNotifier on FS.

            serviceTable.SignalServiceReady();

            ipcServer.ServiceRequests();
            ipcServer.Shutdown();
        }
    }
}
