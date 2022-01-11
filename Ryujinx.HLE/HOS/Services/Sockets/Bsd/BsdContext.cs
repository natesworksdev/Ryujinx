using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    class BsdContext
    {
        private static Dictionary<long, BsdContext> _registry = new Dictionary<long, BsdContext>();

        private readonly object _lock = new object();

        private List<IBsdSocket> _sockets;

        private BsdContext()
        {
            _sockets = new List<IBsdSocket>();
        }

        public BsdSocket RetrieveBsdSocket(int socketFd)
        {
            IBsdSocket socket = RetrieveSocket(socketFd);

            if (socket is BsdSocket bsdSocket)
            {
                return bsdSocket;
            }

            return null;
        }

        public IBsdSocket RetrieveSocket(int socketFd)
        {
            lock (_lock)
            {
                if (socketFd >= 0 && _sockets.Count > socketFd)
                {
                    return _sockets[socketFd];
                }
            }

            return null;
        }

        public int RegisterSocket(IBsdSocket socket)
        {
            lock (_lock)
            {
                for (int fd = 0; fd < _sockets.Count; fd++)
                {
                    if (_sockets[fd] == null)
                    {
                        _sockets[fd] = socket;

                        return fd;
                    }
                }

                _sockets.Add(socket);

                return _sockets.Count - 1;
            }
        }

        public int DuplicateSocket(int socketFd)
        {
            IBsdSocket oldSocket = RetrieveSocket(socketFd);

            if (oldSocket != null)
            {
                lock (_lock)
                {
                    oldSocket.Refcount++;

                    return RegisterSocket(oldSocket);
                }
            }

            return -1;
        }

        public bool Close(int socketFd)
        {
            IBsdSocket socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                socket.Refcount--;

                if (socket.Refcount <= 0)
                {
                    socket.Dispose();
                }

                lock (_lock)
                {
                    _sockets[socketFd] = null;
                }

                return true;
            }

            return false;
        }

        public LinuxError ShutdownAll(BsdSocketShutdownFlags how)
        {
            lock (_lock)
            {
                foreach (BsdSocket socket in _sockets)
                {
                    if (socket != null)
                    {
                        LinuxError errno = socket.Handle.Shutdown(how);

                        if (errno != LinuxError.SUCCESS)
                        {
                            return errno;
                        }
                    }
                }
            }

            return LinuxError.SUCCESS;
        }

        public static BsdContext GetOrRegister(long processId)
        {
            BsdContext context = GetContext(processId);

            if (context == null)
            {
                context = new BsdContext();

                lock (_registry)
                {
                    _registry.TryAdd(processId, context);
                }
            }

            return context;
        }

        public static BsdContext GetContext(long processId)
        {
            lock (_registry)
            {
                if (!_registry.TryGetValue(processId, out BsdContext processContext))
                {
                    return null;
                }

                return processContext;
            }
        }
    }
}
