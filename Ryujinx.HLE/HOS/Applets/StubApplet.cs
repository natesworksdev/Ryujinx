using System;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;

namespace Ryujinx.HLE.HOS.Applets
{
    class StubApplet : IApplet
    {
        public AppletId AppletId { get; }

        public event EventHandler AppletStateChanged;

        public StubApplet(AppletId appletId)
        {
            AppletId = appletId;
        }

        public ResultCode GetResult()
        {
            Logger.PrintWarning(LogClass.ServiceAm, $"Stub {AppletId} applet called.");

            return ResultCode.Success;
        }

        public ResultCode Start(AppletSession normalSession, AppletSession interactiveSession)
        {
            Logger.PrintWarning(LogClass.ServiceAm, $"Stub {AppletId} applet called.");

            byte[] normalData;
            byte[] interactiveData;

            normalSession.TryPop(out normalData);
            interactiveSession.TryPop(out interactiveData);

            normalSession.Push(BuildResponse());
            interactiveSession.Push(BuildResponse());

            AppletStateChanged?.Invoke(this, null);

            return ResultCode.Success;
        }

        private byte[] BuildResponse()
        {
            return new byte[0x1000];
        }
    }
}
