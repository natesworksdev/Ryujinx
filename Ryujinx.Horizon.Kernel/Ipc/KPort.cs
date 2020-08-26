using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;

namespace Ryujinx.Horizon.Kernel.Ipc
{
    class KPort : KAutoObject
    {
        public KServerPort ServerPort { get; }
        public KClientPort ClientPort { get; }

        private long _nameAddress;

        private ChannelState _state;

        public bool IsLight { get; private set; }

        public KPort(KernelContextInternal context, int maxSessions, bool isLight, long nameAddress) : base(context)
        {
            ServerPort = new KServerPort(context, this);
            ClientPort = new KClientPort(context, this, maxSessions);

            IsLight = isLight;
            _nameAddress = nameAddress;

            _state = ChannelState.Open;
        }

        public Result EnqueueIncomingSession(KServerSession session)
        {
            Result result;

            KernelContext.CriticalSection.Enter();

            if (_state == ChannelState.Open)
            {
                ServerPort.EnqueueIncomingSession(session);

                result = Result.Success;
            }
            else
            {
                result = KernelResult.PortClosed;
            }

            KernelContext.CriticalSection.Leave();

            return result;
        }

        public Result EnqueueIncomingLightSession(KLightServerSession session)
        {
            Result result;

            KernelContext.CriticalSection.Enter();

            if (_state == ChannelState.Open)
            {
                ServerPort.EnqueueIncomingLightSession(session);

                result = Result.Success;
            }
            else
            {
                result = KernelResult.PortClosed;
            }

            KernelContext.CriticalSection.Leave();

            return result;
        }
    }
}