namespace Ryujinx.HLE.HOS.Services.Ptm.Fgm
{
    [Service("fgm:dbg")] // 9.0.0+
    class IDebugger : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IDebugger(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}