namespace Ryujinx.HLE.HOS.Services.Nim
{
    [Service("nim:shp")]
    sealed class IShopServiceManager : IpcService
    {
        public IShopServiceManager(ServiceCtx context) { }
    }
}