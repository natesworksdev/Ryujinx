using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Proxies
{
    partial class SystemAppletProxy : ISystemAppletProxy
    {
        [CmifCommand(0)]
        public Result GetCommonStateGetter(out ICommonStateGetter commonStateGetter, ulong pid)
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(1)]
        public Result GetSelfController()
        {
            throw new System.NotImplementedException();
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
        public Result GetHomeMenuFunctions()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(21)]
        public Result GetGlobalStateController()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(22)]
        public Result GetApplicationCreator()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(23)]
        public Result GetAppletCommonFunctions()
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
