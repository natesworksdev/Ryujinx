using Ryujinx.HLE.HOS.Kernel.Common;

namespace Ryujinx.HLE.HOS.Kernel.Ipc
{
    class KPort : KAutoObject
    {
        public KServerPort ServerPort { get; }
        public KClientPort ClientPort { get; }

        private long _nameAddress;
        private int  _resourceStatus;

        public bool IsLight { get; private set; }

        public KPort(Horizon system) : base(system)
        {
            ServerPort = new KServerPort(system);
            ClientPort = new KClientPort(system);
        }

        public void Initialize(int maxSessions, bool isLight, long nameAddress)
        {
            ServerPort.Initialize(this);
            ClientPort.Initialize(this, maxSessions);

            IsLight      = isLight;
            _nameAddress = nameAddress;

            _resourceStatus = 1;
        }

        public KernelResult EnqueueIncomingSession(KServerSession session)
        {
            KernelResult result;

            System.CriticalSection.Enter();

            if (_resourceStatus == 1)
            {
                ServerPort.EnqueueIncomingSession(session);

                result = KernelResult.Success;
            }
            else
            {
                result = KernelResult.PortClosed;
            }

            System.CriticalSection.Leave();

            return result;
        }
    }
}