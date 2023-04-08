namespace Ryujinx.HLE.HOS.Services.Ngct
{
    [Service("ngct:u")] // 9.0.0+
    class IService : IpcService
    {
#pragma warning disable IDE0060
        public IService(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(0)]
        // Match(buffer<string, 9>) -> b8
        public static ResultCode Match(ServiceCtx context)
        {
            return NgctServer.Match(context);
        }

        [CommandCmif(1)]
        // Filter(buffer<string, 9>) -> buffer<filtered_string, 10>
        public static ResultCode Filter(ServiceCtx context)
        {
            return NgctServer.Filter(context);
        }
    }
}
