namespace Ryujinx.HLE.HOS.Services.Ptm.Fgm
{
    [Service("fgm")]   // 9.0.0+
    [Service("fgm:0")] // 9.0.0+
    [Service("fgm:9")] // 9.0.0+
    class ISession : IpcService
    {
#pragma warning disable IDE0060
        public ISession(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
