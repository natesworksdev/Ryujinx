using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Horizon.Common;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Kernel.Ipc
{
    class KClientSession : KSynchronizationObject
    {
        public KProcess CreatorProcess { get; }

        private KSession _parent;

        public ChannelState State { get; set; }

        public KClientPort ParentPort { get; }

        public KClientSession(KernelContext context, KSession parent, KClientPort parentPort) : base(context)
        {
            _parent    = parent;
            ParentPort = parentPort;

            parentPort?.IncrementReferenceCount();

            State = ChannelState.Open;

            CreatorProcess = KernelStatic.GetCurrentProcess();
            CreatorProcess.IncrementReferenceCount();
        }

        public async Task<Result> SendSyncRequest(ulong customCmdBuffAddr = 0, ulong customCmdBuffSize = 0)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();
            KSessionRequest request = new KSessionRequest(currentThread, customCmdBuffAddr, customCmdBuffSize);

            // Send request
            Result result = _parent.ServerSession.EnqueueRequest(request);
            if (result != Result.Success)
            {
                return result;
            }
                
            // Await response
            // TODO: fold in cancellation token
            return await request.Complete.Task;
        }

        public Result SendAsyncRequest(KWritableEvent asyncEvent, ulong customCmdBuffAddr = 0, ulong customCmdBuffSize = 0)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            KSessionRequest request = new KSessionRequest(currentThread, customCmdBuffAddr, customCmdBuffSize, asyncEvent);

            KernelContext.CriticalSection.Enter();

            Result result = _parent.ServerSession.EnqueueRequest(request);

            KernelContext.CriticalSection.Leave();

            return result;
        }

        public void DisconnectFromPort()
        {
            if (ParentPort != null)
            {
                ParentPort.Disconnect();
                ParentPort.DecrementReferenceCount();
            }
        }

        protected override void Destroy()
        {
            _parent.DisconnectClient();
            _parent.DecrementReferenceCount();
        }
    }
}
