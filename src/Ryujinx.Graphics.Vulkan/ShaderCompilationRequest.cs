using System.Threading.Tasks;

namespace Ryujinx.Graphics.Vulkan
{
    struct ShaderCompilationRequest
    {
        private readonly Task _task;
        private readonly ShaderCompilationQueue _queue;
        private readonly int _queueIndex;
        private readonly ulong _requestId;

        public bool IsCompleted
        {
            get
            {
                if (_task != null)
                {
                    return _task.IsCompleted;
                }
                else
                {
                    return _queue.IsCompleted(_queueIndex, _requestId);
                }
            }
        }

        public ShaderCompilationRequest(Task task)
        {
            _task = task;
            _queue = null;
            _queueIndex = 0;
            _requestId = 0;
        }

        public ShaderCompilationRequest(ShaderCompilationQueue queue, int queueIndex, ulong requestId)
        {
            _task = null;
            _queue = queue;
            _queueIndex = queueIndex;
            _requestId = requestId;
        }

        public void Wait()
        {
            if (_task != null)
            {
                _task.Wait();
            }
            else
            {
                _queue.Wait(_queueIndex, _requestId);
            }
        }
    }
}