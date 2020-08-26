using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.OsTypes
{
    public static class OsResult
    {
        private const int ModuleId = 3;

        public static Result OutOfResource => new Result(ModuleId, 9);
    }
}
