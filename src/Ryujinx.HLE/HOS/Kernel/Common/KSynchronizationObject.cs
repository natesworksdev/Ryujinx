using Ryujinx.Horizon.Common;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Kernel.Common
{
    // TODO: clean this up and disambiguate with KReadableEvent
    class KSynchronizationObject : KAutoObject
    {
        private readonly object _lock = new();
        // TODO: might be inefficient to have a TCS per sync obj
        private TaskCompletionSource<Result> _tcs = new ();

        public KSynchronizationObject(KernelContext context) : base(context)
        {
            lock (_lock)
            {
                // _tcs.TrySetResult(Result.Success);
            }
        }
        
        public void Signal()
        {
            Signal(Result.Success);
        }
        
        public virtual void Signal(Result result)
        {
            lock (_lock)
            {
                _tcs.TrySetResult(result);
            }
        }
        
        public virtual bool IsSignaled()
        {
            return _tcs.Task.IsCompleted;
        }

        public virtual Task<Result> WaitSignaled()
        {
            return _tcs.Task;
        }

        public Result Clear()
        {
            lock (_lock)
            {
                // TODO: review, might not be expected behaviour
                _tcs.TrySetResult(Result.Success); // Flush existing waiters to avoid deadlock  
                _tcs = new ();
                return Result.Success;
            }
        }

        public Result ClearIfSignaled()
        {
            if (IsSignaled())
            {
                return Clear();
            } else {
                return KernelResult.InvalidState;
            }
            
            // lock (_lock)
            // {
            //     if (!_tcs.Task.IsCompleted)
            //     {
            //         return KernelResult.InvalidState;
            //     }
            //     _tcs = new TaskCompletionSource<Result>();
            //     return Result.Success;
            // }
        }
    }
}
