namespace Ryujinx.HLE.HOS.Services.Srepo
{
    [Service("srepo:a")] // 5.0.0+
    [Service("srepo:u")] // 5.0.0+
    class ISrepoService : IpcService
    {
#pragma warning disable IDE0060
        public ISrepoService(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
