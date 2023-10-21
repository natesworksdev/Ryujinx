using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am
{
    public interface ISystemAppletProxy
    {
        Result GetCommonStateGetter();
        Result GetSelfController();
        Result GetWindowController();
        Result GetAudioController();
        Result GetDisplayController();
        Result GetProcessWindingController();
        Result GetLibraryAppletCreator();
        Result GetHomeMenuFunctions();
        Result GetGlobalStateController();
        Result GetApplicationCreator();
        Result GetAppletCommonFunctions();
        Result GetDebugFunctions();
    }
}
