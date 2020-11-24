using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sm;
using System;

namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    class Server : ServerBase
    {
        public Server(int portHandle, ServiceName name, bool managed, ServiceObjectHolder staticHoder, Func<IServiceObject> factory)
            : base(portHandle, name, managed, staticHoder, factory)
        {
        }
    }
}
