using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Pctl
{
    static class PctlResult
    {
        private const int ModuleId = 142;

        public static Result FreeCommunicationDisabled => new(ModuleId, 101);
        public static Result StereoVisionDenied => new(ModuleId, 104);
        public static Result InvalidPid => new(ModuleId, 131);
        public static Result PermissionDenied => new(ModuleId, 133);
        public static Result StereoVisionRestrictionConfigurableDisabled => new(ModuleId, 181);
    }
}
