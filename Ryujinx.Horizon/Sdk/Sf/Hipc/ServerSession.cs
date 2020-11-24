using Ryujinx.Horizon.Sdk.OsTypes;
using Ryujinx.Horizon.Sdk.Sf.Cmif;

namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    class ServerSession : WaitableHolderOfHandle
    {
        public ServiceObjectHolder ServiceObjectHolder { get; set; }
        public PointerAndSize PointerBuffer { get; }
        public PointerAndSize SavedMessage { get; }
        public int SessionHandle { get; }
        public bool IsClosed { get; set; }
        public bool HasReceived { get; set; }

        public ServerSession(int handle, ServiceObjectHolder obj) : base(handle)
        {
            ServiceObjectHolder = obj;
            SessionHandle = handle;
        }
    }
}
