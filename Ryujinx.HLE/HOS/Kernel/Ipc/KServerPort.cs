using Ryujinx.HLE.HOS.Kernel.Common;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel.Ipc
{
    class KServerPort : KSynchronizationObject
    {
        private LinkedList<KServerSession> _incomingConnections;
        private LinkedList<KServerSession> _lightIncomingConnections;

        private KPort _parent;

        public bool IsLight => _parent.IsLight;

        public KServerPort(Horizon system, KPort parent) : base(system)
        {
            _parent = parent;

            _incomingConnections      = new LinkedList<KServerSession>();
            _lightIncomingConnections = new LinkedList<KServerSession>();
        }

        public void EnqueueIncomingSession(KServerSession session)
        {
            System.CriticalSection.Enter();

            _incomingConnections.AddLast(session);

            if (_incomingConnections.Count == 1)
            {
                Signal();
            }

            System.CriticalSection.Leave();
        }

        public KServerSession AcceptIncomingConnection()
        {
            return AcceptIncomingConnection(_incomingConnections);
        }

        public KServerSession AcceptLightIncomingConnection()
        {
            return AcceptIncomingConnection(_lightIncomingConnections);
        }

        private KServerSession AcceptIncomingConnection(LinkedList<KServerSession> list)
        {
            KServerSession session = null;

            System.CriticalSection.Enter();

            if (list.Count != 0)
            {
                session = list.First.Value;

                list.RemoveFirst();
            }

            System.CriticalSection.Leave();

            return session;
        }

        public override bool IsSignaled()
        {
            if (_parent.IsLight)
            {
                return _lightIncomingConnections.Count != 0;
            }
            else
            {
                return _incomingConnections.Count != 0;
            }
        }
    }
}