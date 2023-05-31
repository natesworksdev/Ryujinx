namespace Ryujinx.HLE.HOS.Services.Cec
{
    [Service("cec-mgr")]
    sealed class ICecManager : IpcService
    {
        public ICecManager(ServiceCtx context) { }
    }
}