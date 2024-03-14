using Ryujinx.Horizon.Am.Ipc.Controllers;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Am.Proxies;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Proxies
{
    partial class SystemAppletProxy : ISystemAppletProxy
    {
        [CmifCommand(0)]
        public Result GetCommonStateGetter(out ICommonStateGetter commonStateGetter)
        {
            commonStateGetter = new CommonStateGetter();

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result GetSelfController(out ISelfController selfController)
        {
            selfController = new SelfController();

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetWindowController(out IWindowController windowController)
        {
            windowController = new WindowController();

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result GetAudioController(out IAudioController audioController)
        {
            audioController = new AudioController();

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result GetDisplayController(out IDisplayController displayController)
        {
            displayController = new DisplayController();

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result GetProcessWindingController(out IProcessWindingController processWindingController)
        {
            processWindingController = new ProcessWindingController();

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result GetLibraryAppletCreator(out ILibraryAppletCreator libraryAppletCreator)
        {
            libraryAppletCreator = new LibraryAppletCreator();

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result GetHomeMenuFunctions(out IHomeMenuFunctions homeMenuFunctions)
        {
            homeMenuFunctions = new HomeMenuFunctions();

            return Result.Success;
        }

        [CmifCommand(21)]
        public Result GetGlobalStateController(out IGlobalStateController globalStateController)
        {
            globalStateController = new GlobalStateController();

            return Result.Success;
        }

        [CmifCommand(22)]
        public Result GetApplicationCreator(out IApplicationCreator applicationCreator)
        {
            applicationCreator = new ApplicationCreator();

            return Result.Success;
        }

        [CmifCommand(23)]
        public Result GetAppletCommonFunctions(out IAppletCommonFunctions appletCommonFunctions)
        {
            appletCommonFunctions = new AppletCommonFunctions();

            return Result.Success;
        }

        [CmifCommand(1000)]
        public Result GetDebugFunctions(out IDebugFunctions debugFunctions)
        {
            debugFunctions = new DebugFunctions();

            return Result.Success;
        }
    }
}
