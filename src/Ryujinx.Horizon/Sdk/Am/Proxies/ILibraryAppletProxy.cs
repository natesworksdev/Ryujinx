using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;

namespace Ryujinx.Horizon.Sdk.Am.Proxies
{
    public interface ILibraryAppletProxy
    {
        Result GetCommonStateGetter(out ICommonStateGetter commonStateGetter);
        Result GetSelfController(out ISelfController selfController);
        Result GetWindowController(out IWindowController windowController);
        Result GetAudioController(out IAudioController audioController);
        Result GetDisplayController(out IDisplayController displayController);
        Result GetProcessWindingController(out IProcessWindingController processWindingController);
        Result GetLibraryAppletCreator(out ILibraryAppletCreator libraryAppletCreator);
        Result OpenLibraryAppletSelfAccessor(out ILibraryAppletSelfAccessor libraryAppletSelfAccessor);
        Result GetAppletCommonFunctions(out IAppletCommonFunctions appletCommonFunctions);
        Result GetHomeMenuFunctions();
        Result GetGlobalStateController();
        Result GetDebugFunctions(out IDebugFunctions debugFunctions);
    }
}
