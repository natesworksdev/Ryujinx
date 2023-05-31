namespace Ryujinx.HLE.HOS.Services.Nv
{
    [Service("nvgem:c")]
    sealed class INvGemControl : IpcService
    {
        public INvGemControl(ServiceCtx context) { }
    }
}