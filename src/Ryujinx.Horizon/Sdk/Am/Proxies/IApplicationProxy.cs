using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;

namespace Ryujinx.Horizon.Sdk.Am.Proxies
{
    public interface IApplicationProxy
    {
        Result GetCommonStateGetter(out ICommonStateGetter commonStateGetter);
        Result GetSelfController(out ISelfController selfController);
        Result GetWindowController(out IWindowController windowController);
        Result GetAudioController(out IAudioController audioController);
        Result GetDisplayController(out IDisplayController displayController);
        Result GetProcessWindingController();
        Result GetLibraryAppletCreator();
        Result GetApplicationFunctions();
        Result GetDebugFunctions();
    }
}
