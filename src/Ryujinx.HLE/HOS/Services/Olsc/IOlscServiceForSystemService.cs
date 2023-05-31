namespace Ryujinx.HLE.HOS.Services.Olsc
{
    [Service("olsc:s")] // 4.0.0+
    sealed class IOlscServiceForSystemService : IpcService
    {
        public IOlscServiceForSystemService(ServiceCtx context) { }
    }
}