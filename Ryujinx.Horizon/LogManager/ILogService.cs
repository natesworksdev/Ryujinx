using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Lm;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.LogManager
{
    partial class ILogService : IServiceObject
    {
        public LogDestination LogDestination { get; set; } = LogDestination.TargetManager;

        [CmifCommand(0)]
        // OpenLogger(u64, pid) -> object<nn::lm::ILogger>
        public Result OpenLogger(out ILogger logger, [ClientProcessId] ulong pid)
        {
            logger = new ILogger(this, pid);

            return Result.Success;
        }
    }
}