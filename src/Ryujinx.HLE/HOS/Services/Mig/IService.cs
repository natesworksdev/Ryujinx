namespace Ryujinx.HLE.HOS.Services.Mig
{
    [Service("mig:usr")] // 4.0.0+
    class IService : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IService(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}