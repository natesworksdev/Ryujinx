using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Storage;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface ILibraryAppletAccessor : IAppletAccessor
    {
        Result SetOutOfFocusApplicationSuspendingEnabled(bool arg0);
        Result PresetLibraryAppletGpuTimeSliceZero();
        Result PushInData(IStorage arg0);
        Result PopOutData(out IStorage data);
        Result PushExtraStorage(IStorage data);
        Result PushInteractiveInData(IStorage data);
        Result PopInteractiveOutData(out IStorage arg0);
        Result GetPopOutDataEvent(out int handle);
        Result GetPopInteractiveOutDataEvent(out int handle);
        Result NeedsToExitProcess(out bool arg0);
        Result GetLibraryAppletInfo(out LibraryAppletInfo arg0);
        Result RequestForAppletToGetForeground();
        Result GetIndirectLayerConsumerHandle(out ulong arg0, ulong arg1, ulong pid);
    }
}
