using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am
{
    public interface ILibraryAppletProxy
    {
        Result GetCommonStateGetter(out ICommonStateGetter commonStateGetter);
        Result GetSelfController(out ISelfController selfController);
        Result GetWindowController(out IWindowController windowController);
        Result GetAudioController(out IAudioController audioController);
        Result GetDisplayController();
        Result GetProcessWindingController();
        Result GetLibraryAppletCreator();
        Result OpenLibraryAppletSelfAccessor();
        Result GetAppletCommonFunctions();
        Result GetHomeMenuFunctions();
        Result GetGlobalStateController();
        Result GetDebugFunctions();
    }
}
