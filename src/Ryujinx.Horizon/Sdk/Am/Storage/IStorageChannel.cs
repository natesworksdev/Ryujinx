using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Storage
{
    interface IStorageChannel : IServiceObject
    {
        Result Push(IStorage arg0);
        Result Unpop(IStorage arg0);
        Result Pop(out IStorage arg0);
        Result GetPopEventHandle(out int arg0);
        Result Clear();
    }
}
