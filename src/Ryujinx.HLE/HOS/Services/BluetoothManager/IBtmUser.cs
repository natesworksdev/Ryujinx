using Ryujinx.HLE.HOS.Services.BluetoothManager.BtmUser;

namespace Ryujinx.HLE.HOS.Services.BluetoothManager
{
    [Service("btm:u")] // 5.0.0+
    class IBtmUser : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IBtmUser(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(0)] // 5.0.0+
        // GetCore() -> object<nn::btm::IBtmUserCore>
        public ResultCode GetCore(ServiceCtx context)
        {
            MakeObject(context, new IBtmUserCore());

            return ResultCode.Success;
        }
    }
}