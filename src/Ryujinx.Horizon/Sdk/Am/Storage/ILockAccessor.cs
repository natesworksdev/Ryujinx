using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Storage
{
    interface ILockAccessor : IServiceObject
    {
        Result TryLock(out bool arg0, out int handle, bool arg2);
        Result Unlock();
        Result GetEvent(out int handle);
        Result IsLocked(out bool arg0);
    }
}
