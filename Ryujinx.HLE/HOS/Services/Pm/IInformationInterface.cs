using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Kernel.Process;

namespace Ryujinx.HLE.HOS.Services.Pm
{
    [Service("pm:info")]
    class IInformationInterface : IpcService
    {
        public IInformationInterface(ServiceCtx context) { }

        [CommandHipc(0)]
        // GetProgramId(os::ProcessId process_id) -> sf::Out<ncm::ProgramId> out
        public ResultCode GetProgramId(ServiceCtx context)
        {
            ulong pid = context.RequestData.ReadUInt64();

            KProcess process = KernelStatic.GetProcessByPid(pid);

            if (process != null)
            {
                context.ResponseData.Write(process.TitleId);

                return ResultCode.Success;
            }

            return ResultCode.ProcessNotFound;
        }
    }
}