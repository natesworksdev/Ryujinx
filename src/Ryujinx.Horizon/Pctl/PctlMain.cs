namespace Ryujinx.Horizon.Pctl
{
    internal class PctlMain : IService
    {
        public static void Main(ServiceTable serviceTable)
        {
            PctlIpcServer ipcServer = new();

            ipcServer.Initialize();

            serviceTable.SignalServiceReady();

            ipcServer.ServiceRequests();
            ipcServer.Shutdown();
        }
    }
}
