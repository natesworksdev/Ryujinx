namespace Ryujinx.HLE.HOS.Services.Nv
{
    [Service("nvgem:cd")]
    sealed class INvGemCoreDump : IpcService
    {
        public INvGemCoreDump(ServiceCtx context) { }
    }
}