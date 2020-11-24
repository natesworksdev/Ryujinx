using Ryujinx.Horizon.Sdk.OsTypes;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sm;
using System;

namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    class ServerBase : WaitableHolderOfHandle
    {
        public int PortHandle { get; }

        private readonly ServiceName _name;
        private readonly bool _managed;
        private readonly ServiceObjectHolder _staticObject;
        private readonly Func<IServiceObject> _factory;

        public ServerBase(int portHandle, ServiceName name, bool managed, ServiceObjectHolder staticHoder, Func<IServiceObject> factory) : base(portHandle)
        {
            PortHandle    = portHandle;
            _name         = name;
            _managed      = managed;
            _staticObject = staticHoder;
            _factory      = factory;
        }

        public void CreateSessionObjectHolder(out ServiceObjectHolder outObj)
        {
            outObj = _staticObject != null ? _staticObject.Clone() : new ServiceObjectHolder(_factory());
        }
    }
}
