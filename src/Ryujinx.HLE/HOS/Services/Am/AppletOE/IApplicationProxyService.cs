using Ryujinx.HLE.HOS.Services.Am.AppletOE.ApplicationProxyService;

namespace Ryujinx.HLE.HOS.Services.Am
{
    [Service("appletOE")]
    class IApplicationProxyService : IpcService
    {
#pragma warning disable IDE0060
        public IApplicationProxyService(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(0)]
        // OpenApplicationProxy(u64, pid, handle<copy>) -> object<nn::am::service::IApplicationProxy>
        public ResultCode OpenApplicationProxy(ServiceCtx context)
        {
            MakeObject(context, new IApplicationProxy(context.Request.HandleDesc.PId));

            return ResultCode.Success;
        }
    }
}