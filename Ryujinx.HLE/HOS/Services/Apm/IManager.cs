namespace Ryujinx.HLE.HOS.Services.Apm
{
    abstract class IManager : IpcService
    {
        public IManager(ServiceCtx context) { }

        [Command(0)]
        // OpenSession() -> object<nn::apm::ISession>
        public ResultCode OpenSession(ServiceCtx context)
        {
            ResultCode resultCode = OpenSession(out ISession sessionObj);

            if (resultCode == ResultCode.Success)
            {
                MakeObject(context, sessionObj);
            }

            return resultCode;
        }

        [Command(1)]
        // GetPerformanceMode() -> nn::apm::PerformanceMode
        public ResultCode GetPerformanceMode(ServiceCtx context)
        {
            context.ResponseData.Write((uint)GetPerformanceMode());

            return ResultCode.Success;
        }

        [Command(6)] // 7.0.0+
        // IsCpuOverclockEnabled() -> bool
        public ResultCode IsCpuOverclockEnabled(ServiceCtx context)
        {
            context.ResponseData.Write(IsCpuOverclockEnabled());

            return ResultCode.Success;
        }

        protected abstract ResultCode OpenSession(out ISession sessionObj);

        protected abstract PerformanceMode GetPerformanceMode();

        protected abstract bool IsCpuOverclockEnabled();
    }
}