using Ryujinx.HLE.HOS.Services.Ldn.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn
{
    class NetworkChangeEventArgs : EventArgs
    {
        public NetworkInfo Info;
        public bool Connected;

        public NetworkChangeEventArgs(NetworkInfo info, bool connected)
        {
            Info = info;
            Connected = connected;
        }
    }
}
