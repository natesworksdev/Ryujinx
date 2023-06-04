namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audctl")]
    class IAudioController : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IAudioController(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}