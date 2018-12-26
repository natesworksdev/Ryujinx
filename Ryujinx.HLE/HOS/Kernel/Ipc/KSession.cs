using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services;
using System;

namespace Ryujinx.HLE.HOS.Kernel.Ipc
{
    class KSession : KAutoObject, IDisposable
    {
        public KServerSession ServerSession { get; }
        public KClientSession ClientSession { get; }

        private bool _hasBeenInitialized;

        public IpcService Service { get; private set; }

        public string ServiceName { get; private set; }

        public KSession(Horizon system) : base(system)
        {
            ServerSession = new KServerSession(system, this);
            ClientSession = new KClientSession(system, this);
        }

        public KSession(Horizon system, IpcService service, string serviceName) : base(system)
        {
            Service     = service;
            ServiceName = serviceName;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && Service is IDisposable disposableService)
            {
                disposableService.Dispose();
            }
        }

        protected override void Destroy()
        {
            if (_hasBeenInitialized)
            {
                KProcess creatorProcess = ClientSession.CreatorProcess;

                creatorProcess.ResourceLimit?.Release(LimitableResource.Session, 1);
            }
        }
    }
}