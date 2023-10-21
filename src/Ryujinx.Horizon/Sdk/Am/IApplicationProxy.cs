using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am
{
    public interface IApplicationProxy
    {
        Result GetCommonStateGetter(out ICommonStateGetter commonStateGetter, ulong pid);
        Result GetSelfController();
        Result GetWindowController();
        Result GetAudioController();
        Result GetDisplayController();
        Result GetProcessWindingController();
        Result GetLibraryAppletCreator();
        Result GetApplicationFunctions();
        Result GetDebugFunctions();
    }
}
