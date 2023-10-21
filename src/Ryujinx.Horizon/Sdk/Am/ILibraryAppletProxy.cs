using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am
{
    public interface ILibraryAppletProxy
    {
        Result GetCommonStateGetter();
        Result GetSelfController();
        Result GetWindowController();
        Result GetAudioController();
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
