namespace Ryujinx.HLE.HOS.Services.Ldn
{
    [Service("ldn:s")]
    sealed class ISystemServiceCreator : IpcService
    {
        public ISystemServiceCreator(ServiceCtx context) { }
    }
}