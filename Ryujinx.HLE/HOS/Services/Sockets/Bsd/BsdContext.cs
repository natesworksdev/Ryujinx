using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    class BsdContext
    {
        private static object RegistryLock = new object();
        private static Dictionary<long, BsdContext> Registry = new Dictionary<long, BsdContext>();

        private object Lock = new object();

        private List<IBsdSocket> _sockets;

        private BsdContext()
        {
            _sockets = new List<IBsdSocket>();
        }

        public BsdSocket RetrieveBsdSocket(int socketFd)
        {
            IBsdSocket socket = RetrieveSocket(socketFd);

            if (socket != null && socket is BsdSocket bsdSocket)
            {
                return bsdSocket;
            }

            return null;
        }

        public IBsdSocket RetrieveSocket(int socketFd)
        {
            lock (Lock)
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
            lock (Lock)
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
                lock (Lock)
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

                lock (Lock)
                {
                    _sockets[socketFd] = null;
                }

                return true;
            }

            return false;
        }

        public LinuxError ShutdownAll(BsdSocketShutdownFlags how)
        {
            lock (Lock)
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

        public static bool TryRegister(long processId, out BsdContext processContext)
        {
            processContext = new BsdContext();

            lock (RegistryLock)
            {
                return Registry.TryAdd(processId, processContext);
            }
        }

        public static BsdContext GetContext(long processId)
        {
            lock (RegistryLock)
            {
                if (!Registry.TryGetValue(processId, out BsdContext processContext))
                {
                    return null;
                }

                return processContext;
            }
        }
    }
}
