using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am
{
    public interface IOverlayAppletProxy
    {
        Result GetCommonStateGetter();
        Result GetSelfController();
        Result GetWindowController();
        Result GetAudioController();
        Result GetDisplayController();
        Result GetProcessWindingController();
        Result GetLibraryAppletCreator();
        Result GetOverlayFunctions();
        Result GetAppletCommonFunctions();
        Result GetGlobalStateController();
        Result GetDebugFunctions();
    }
}
