using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Storage
{
    interface IStorageChannel : IServiceObject
    {
        Result Push(IStorage storage);
        Result Unpop(IStorage storage);
        Result Pop(out IStorage storage);
        Result GetPopEventHandle(out int handle);
        Result Clear();
    }
}
