using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Nim.ShopServiceAccessServerInterface.ShopServiceAccessServer.ShopServiceAccessor;

namespace Ryujinx.HLE.HOS.Services.Nim.ShopServiceAccessServerInterface.ShopServiceAccessServer
{
    class IShopServiceAccessor : IpcService
    {
        public IShopServiceAccessor() { }

        [Command(0)]
        // CreateAsyncInterface(u64) -> (handle<copy>, object<nn::ec::IShopServiceAsync>)
        public ResultCode CreateAsyncInterface(ServiceCtx context)
        {
            MakeObject(context, new IShopServiceAsync());

            Logger.PrintStub(LogClass.ServiceNim);

            return ResultCode.Success;
        }
    }
}