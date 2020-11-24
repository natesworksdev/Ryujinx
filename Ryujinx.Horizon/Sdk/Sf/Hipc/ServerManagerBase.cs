using Ryujinx.Horizon.Sdk.OsTypes;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using System;
using Ryujinx.Horizon.Kernel;
using Ryujinx.Horizon.Sdk.Shims;
using Ryujinx.Horizon.Sm;

namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    class ServerManagerBase : ServerDomainSessionManager
    {
        private readonly WaitableManager _waitableManager;
        private readonly WaitableManager _waitList;

        private readonly object _waitableSelectionLocker;
        private readonly object _waitListLock;

        private readonly Event _notifyEvent;

        private readonly WaitableHolderBase _requestStopEventHolder;
        private readonly WaitableHolderBase _notifyEventHolder;

        private enum UserDataTag
        {
            Server = 1,
            Session = 2
        }

        public ServerManagerBase(ManagerOptions options) : base(options.MaxDomainObjects, options.MaxDomains)
        {
            _waitableManager = new WaitableManager();
            _waitList = new WaitableManager();

            _waitableSelectionLocker = new object();
            _waitListLock = new object();

            _notifyEvent = new Event(false);
        }

        public Result RegisterServer<T>(int portHandle, IServiceObject staticObject = null) where T : IServiceObject, new()
        {
            ServiceObjectHolder staticHolder = null;

            if (staticObject != null)
            {
                staticHolder = new ServiceObjectHolder(staticObject);
            }

            RegisterServerImpl<T>(portHandle, ServiceName.Invalid, true, staticHolder);

            return Result.Success;
        }

        public Result RegisterServer<T>(ServiceName name, int maxSessions, IServiceObject staticObject = null) where T : IServiceObject, new()
        {
            SmApi.RegisterService(out int portHandle, name, maxSessions, false);

            ServiceObjectHolder staticHolder = null;

            if (staticObject != null)
            {
                staticHolder = new ServiceObjectHolder(staticObject);
            }

            RegisterServerImpl<T>(portHandle, name, true, staticHolder);

            return Result.Success;
        }

        private void RegisterServerImpl<T>(int portHandle, ServiceName name, bool managed, ServiceObjectHolder staticHolder) where T : IServiceObject, new()
        {
            static IServiceObject DefaultFactory()
            {
                return new T();
            }

            Server server = new Server(portHandle, name, managed, staticHolder, DefaultFactory)
            {
                UserData = UserDataTag.Server
            };
            _waitableManager.LinkWaitableHolder(server);
        }

        public void ServiceRequests()
        {
            while (WaitAndProcessRequestsImpl());
        }

        public void WaitAndProcessRequests()
        {
            WaitAndProcessRequestsImpl();
        }

        private bool WaitAndProcessRequestsImpl()
        {
            WaitableHolder waitable = WaitSignaled();

            if (waitable == null)
            {
                return false;
            }

            DebugUtil.Assert(Process(waitable).IsSuccess);

            return true;
        }

        private WaitableHolder WaitSignaled()
        {
            lock (_waitableSelectionLocker)
            {
                while (true)
                {
                    ProcessWaitList();

                    WaitableHolder selected = _waitableManager.WaitAny();

                    if (selected == _requestStopEventHolder)
                    {
                        return null;
                    }
                    else if (selected == _notifyEventHolder)
                    {
                        _notifyEvent.Reset();
                    }
                    else
                    {
                        selected.UnlinkFromWaitableManager();

                        return selected;
                    }
                }
            }
        }

        protected override void RegisterSessionToWaitList(ServerSession session)
        {
            session.HasReceived = false;
            session.UserData = UserDataTag.Session;
            RegisterToWaitList(session);
        }

        private void RegisterToWaitList(WaitableHolder holder)
        {
            lock (_waitListLock)
            {
                _waitList.LinkWaitableHolder(holder);
                // _notifyEvent.Signal(); // TODO
            }
        }

        private void ProcessWaitList()
        {
            lock (_waitListLock)
            {
                _waitableManager.MoveAllFrom(_waitList);
            }
        }

        private Result Process(WaitableHolder holder)
        {
            switch ((UserDataTag)holder.UserData)
            {
                case UserDataTag.Server:
                    return ProcessForServer(holder);
                case UserDataTag.Session:
                    Result result = ProcessForSession(holder);
                    if (SfResult.RequestDeferred(result))
                    {
                        throw new NotImplementedException();
                    }
                    else if (result.IsFailure)
                    {
                        return result;
                    }
                    // TODO: Process deferred.
                    return Result.Success;
                default:
                    throw new NotImplementedException(((UserDataTag)holder.UserData).ToString());
            }
        }

        private Result ProcessForServer(WaitableHolder holder)
        {
            DebugUtil.Assert((UserDataTag)holder.UserData == UserDataTag.Server);

            ServerBase server = (ServerBase)holder;

            try
            {
                server.CreateSessionObjectHolder(out var obj);
                return AcceptSession(server.PortHandle, obj);
            }
            finally
            {
                RegisterToWaitList(server);
            }
        }

        private Result ProcessForSession(WaitableHolder holder)
        {
            ServerSession session = (ServerSession)holder;

            using var tlsMessage = KernelStatic.AddressSpace.GetWritableRegion(KernelStatic.GetThreadContext().TlsAddress, Api.TlsMessageBufferSize);

            Result result;

            if (!session.HasReceived)
            {
                result = ReceiveRequest(session, tlsMessage.Memory.Span);

                if (result.IsFailure)
                {
                    return result;
                }

                session.HasReceived = true;
                // TODO: Copy TLS to saved message
            }
            else
            {
                // TODO: Copy saved message to TLS
            }

            result = ProcessRequest(session, tlsMessage.Memory.Span);

            if (result.IsFailure && !SfResult.Invalidated(result))
            {
                return result;
            }

            return Result.Success;
        }
    }
}
