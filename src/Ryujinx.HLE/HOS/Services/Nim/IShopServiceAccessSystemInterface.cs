namespace Ryujinx.HLE.HOS.Services.Nim
{
    [Service("nim:ecas")] // 7.0.0+
    sealed class IShopServiceAccessSystemInterface : IpcService
    {
        public IShopServiceAccessSystemInterface(ServiceCtx context) { }
    }
}