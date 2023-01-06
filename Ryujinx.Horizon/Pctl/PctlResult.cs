using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Pctl
{
    static class PctlResult
    {
        private const int ModuleId = 142;

        public static Result FreeCommunicationDisabled => new Result(ModuleId, 101);
        public static Result StereoVisionDenied => new Result(ModuleId, 104);
        public static Result InvalidPid => new Result(ModuleId, 131);
        public static Result PermissionDenied => new Result(ModuleId, 133);
        public static Result StereoVisionRestrictionConfigurableDisabled => new Result(ModuleId, 181);
    }
}