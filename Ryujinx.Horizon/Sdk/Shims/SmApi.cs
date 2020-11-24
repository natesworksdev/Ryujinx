using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sm;

namespace Ryujinx.Horizon.Sdk.Shims
{
    class SmApi
    {
        public static Result RegisterService(out int handle, ServiceName name, int maxSessions, bool isLight)
        {
            handle = 0;

            return Result.Success;
        }
    }
}
