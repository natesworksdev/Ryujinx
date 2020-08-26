using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Process;
using System.Threading;

namespace Ryujinx.Horizon.Kernel.Ipc
{
    class KClientPort : KSynchronizationObject
    {
        private int _sessionsCount;
        private readonly int _maxSessions;

        private readonly KPort _parent;

        public bool IsLight => _parent.IsLight;

        public KClientPort(KernelContextInternal context, KPort parent, int maxSessions) : base(context)
        {
            _maxSessions = maxSessions;
            _parent = parent;
        }

        public Result Connect(out KClientSession clientSession)
        {
            clientSession = null;

            KProcess currentProcess = KernelContext.Scheduler.GetCurrentProcess();

            if (currentProcess.ResourceLimit != null &&
               !currentProcess.ResourceLimit.Reserve(LimitableResource.Session, 1))
            {
                return KernelResult.ResLimitExceeded;
            }

            if (!IncrementSessionsCount())
            {
                currentProcess.ResourceLimit?.Release(LimitableResource.Session, 1);

                return KernelResult.SessionCountExceeded;
            }

            KSession session = new KSession(KernelContext, this);

            Result result = _parent.EnqueueIncomingSession(session.ServerSession);

            if (result != Result.Success)
            {
                session.ClientSession.DecrementReferenceCount();
                session.ServerSession.DecrementReferenceCount();

                return result;
            }

            clientSession = session.ClientSession;

            return result;
        }

        public Result ConnectLight(out KLightClientSession clientSession)
        {
            clientSession = null;

            KProcess currentProcess = KernelContext.Scheduler.GetCurrentProcess();

            if (currentProcess.ResourceLimit != null &&
               !currentProcess.ResourceLimit.Reserve(LimitableResource.Session, 1))
            {
                return KernelResult.ResLimitExceeded;
            }

            if (!IncrementSessionsCount())
            {
                currentProcess.ResourceLimit?.Release(LimitableResource.Session, 1);

                return KernelResult.SessionCountExceeded;
            }

            KLightSession session = new KLightSession(KernelContext);

            Result result = _parent.EnqueueIncomingLightSession(session.ServerSession);

            if (result != Result.Success)
            {
                session.ClientSession.DecrementReferenceCount();
                session.ServerSession.DecrementReferenceCount();

                return result;
            }

            clientSession = session.ClientSession;

            return result;
        }

        private bool IncrementSessionsCount()
        {
            while (true)
            {
                int currentCount = _sessionsCount;

                if (currentCount < _maxSessions)
                {
                    if (Interlocked.CompareExchange(ref _sessionsCount, currentCount + 1, currentCount) == currentCount)
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public void Disconnect()
        {
            KernelContext.CriticalSection.Enter();

            SignalIfMaximumReached(Interlocked.Decrement(ref _sessionsCount));

            KernelContext.CriticalSection.Leave();
        }

        private void SignalIfMaximumReached(int value)
        {
            if (value == _maxSessions)
            {
                Signal();
            }
        }

        public static new Result RemoveName(KernelContextInternal context, string name)
        {
            KAutoObject foundObj = FindNamedObject(context, name);

            if (!(foundObj is KClientPort))
            {
                return KernelResult.NotFound;
            }

            return KAutoObject.RemoveName(context, name);
        }
    }
}