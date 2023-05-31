namespace Ryujinx.HLE.HOS.Services.Mig
{
    [Service("mig:usr")] // 4.0.0+
    sealed class IService : IpcService
    {
        public IService(ServiceCtx context) { }
    }
}