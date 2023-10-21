using Ryujinx.Horizon.Am.Ipc.Controllers;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Proxies
{
    partial class ApplicationProxy : IApplicationProxy
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
        public Result GetWindowController()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(3)]
        public Result GetAudioController()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(4)]
        public Result GetDisplayController()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(10)]
        public Result GetProcessWindingController()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(11)]
        public Result GetLibraryAppletCreator()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(20)]
        public Result GetApplicationFunctions()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(1000)]
        public Result GetDebugFunctions()
        {
            throw new System.NotImplementedException();
        }
    }
}
