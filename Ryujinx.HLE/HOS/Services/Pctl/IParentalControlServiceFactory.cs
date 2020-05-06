using Ryujinx.HLE.HOS.Services.Pctl.ParentalControlServiceFactory;

namespace Ryujinx.HLE.HOS.Services.Pctl
{
    [Service("pctl",   0x303)]
    [Service("pctl:a", 0x83BE)]
    [Service("pctl:r", 0x8040)]
    [Service("pctl:s", 0x838E)]
    class IParentalControlServiceFactory : IpcService
    {
        private int _unknownFlag;

        public IParentalControlServiceFactory(ServiceCtx context, int unknownFlag)
        {
            _unknownFlag = unknownFlag;
        }

        [Command(0)]
        // CreateService(u64, pid) -> object<nn::pctl::detail::ipc::IParentalControlService>
        public ResultCode CreateService(ServiceCtx context)
        {
            // TODO: Should pass the pid.
            MakeObject(context, new IParentalControlService(context, true, _unknownFlag));

            return ResultCode.Success;
        }

        [Command(1)] // 4.0.0+
        // CreateServiceWithoutInitialize(u64, pid) -> object<nn::pctl::detail::ipc::IParentalControlService>
        public ResultCode CreateServiceWithoutInitialize(ServiceCtx context)
        {
            // TODO: Should pass the pid.
            MakeObject(context, new IParentalControlService(context, false, _unknownFlag));

            return ResultCode.Success;
        }
    }
}