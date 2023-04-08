namespace Ryujinx.HLE.HOS.Services.News
{
    [Service("news:a")]
    [Service("news:c")]
    [Service("news:m")]
    [Service("news:p")]
    [Service("news:v")]
    class IServiceCreator : IpcService
    {
#pragma warning disable IDE0060
        public IServiceCreator(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
