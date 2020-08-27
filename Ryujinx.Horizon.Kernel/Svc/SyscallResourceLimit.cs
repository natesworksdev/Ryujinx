using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public partial class Syscall
    {
        public Result GetResourceLimitLimitValue(out long limit, int handle, LimitableResource resource)
        {
            limit = 0;

            if ((uint)resource >= (uint)LimitableResource.Count)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            KResourceLimit resourceLimit = _context.Scheduler.GetCurrentProcess().HandleTable.GetObject<KResourceLimit>(handle);

            if (resourceLimit == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            limit = resourceLimit.GetLimitValue(resource);

            return CheckResult(Result.Success);
        }

        public Result GetResourceLimitCurrentValue(out long value, int handle, LimitableResource resource)
        {
            value = 0;

            if ((uint)resource >= (uint)LimitableResource.Count)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            KResourceLimit resourceLimit = _context.Scheduler.GetCurrentProcess().HandleTable.GetObject<KResourceLimit>(handle);

            if (resourceLimit == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            value = resourceLimit.GetCurrentValue(resource);

            return CheckResult(Result.Success);
        }

        public Result CreateResourceLimit(out int handle)
        {
            KResourceLimit resourceLimit = new KResourceLimit(_context);

            return CheckResult(_context.Scheduler.GetCurrentProcess().HandleTable.GenerateHandle(resourceLimit, out handle));
        }

        public Result SetResourceLimitLimitValue(int handle, LimitableResource resource, long limit)
        {
            if ((uint)resource >= (uint)LimitableResource.Count)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            KResourceLimit resourceLimit = _context.Scheduler.GetCurrentProcess().HandleTable.GetObject<KResourceLimit>(handle);

            if (resourceLimit == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            return CheckResult(resourceLimit.SetLimitValue(resource, limit));
        }
    }
}
