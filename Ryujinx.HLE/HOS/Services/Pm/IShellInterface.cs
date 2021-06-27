namespace Ryujinx.HLE.HOS.Services.Pm
{
    [Service("pm:shell")]
    class IShellInterface : IpcService
    {
        public IShellInterface(ServiceCtx context) { }

        [CommandHipc(6)]
        // GetApplicationPid() -> u64
        public ResultCode GetApplicationPid(ServiceCtx context)
        {
            // FIXME: This is wrong but needed to make hb loader works
            // TODO: Change this when we will have a way to process via a PM like interface.
            long pid = context.Request.HandleDesc.PId;

            context.ResponseData.Write((ulong)pid);

            return ResultCode.Success;
        }
    }
}
