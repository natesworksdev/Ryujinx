using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.Horizon.Common;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Kernel.Ipc
{
    class KServerPort : KSynchronizationObject
    {
        private readonly object _lock = new();
        private readonly Channel<KServerSession>      _incomingConnections;
        private readonly Channel<KLightServerSession> _lightIncomingConnections;

        private readonly KPort _parent;

        public bool IsLight => _parent.IsLight;

        public KServerPort(KernelContext context, KPort parent) : base(context)
        {
            _parent = parent;

            _incomingConnections      = Channel.CreateUnbounded<KServerSession>();
            _lightIncomingConnections = Channel.CreateUnbounded<KLightServerSession>();
        }

        public void EnqueueIncomingSession(KServerSession session)
        {
            EnqueueIncomingConnection(_incomingConnections, session);
        }

        public void EnqueueIncomingLightSession(KLightServerSession session)
        {
            EnqueueIncomingConnection(_lightIncomingConnections, session);
        }

        private void EnqueueIncomingConnection<T>(Channel<T> list, T session)
        {
            lock (_lock)
            {
                if (!list.Writer.TryWrite(session))
                {
                    throw new UnreachableException("Failed to enqueue new session");
                }
            }
        }

        public KServerSession AcceptIncomingConnection()
        {
            return DequeueIncomingConnection(_incomingConnections);
        }

        public KLightServerSession AcceptIncomingLightConnection()
        {
            return DequeueIncomingConnection(_lightIncomingConnections);
        }

        private T DequeueIncomingConnection<T>(Channel<T> list)
        {
            // TODO: maybe not necessary ?
            lock (_lock)
            {
                list.Reader.TryRead(out T session);
                return session;
            }
        }

        public override bool IsSignaled()
        {
            if (_parent.IsLight)
            {
                return _lightIncomingConnections.Reader.Count != 0;
            }
            else
            {
                return _incomingConnections.Reader.Count != 0;
            }
        }

        public override async Task<Result> WaitSignaled()
        {
            var isOpen = _parent.IsLight switch {
                true => await _lightIncomingConnections.Reader.WaitToReadAsync(),
                false => await _incomingConnections.Reader.WaitToReadAsync(),
            };
            return isOpen ? Result.Success : KernelResult.PortClosed;
        } 
    }
}
