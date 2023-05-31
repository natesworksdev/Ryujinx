namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audctl")]
    sealed class IAudioController : IpcService
    {
        public IAudioController(ServiceCtx context) { }
    }
}