namespace Ryujinx.HLE.HOS.Services.Caps
{
    [Service("caps:sc")]
    sealed class IScreenShotControlService : IpcService
    {
        public IScreenShotControlService(ServiceCtx context) { }
    }
}