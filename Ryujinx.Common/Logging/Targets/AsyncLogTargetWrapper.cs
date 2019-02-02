using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.Common.Logging
{
    public enum AsyncLogTargetOverflowAction
    {
        /// <summary>
        /// Block until there's more room in the queue
        /// </summary>
        Block = 0,

        /// <summary>
        /// Discard the overflowing item
        /// </summary>
        Discard = 1
    }

    public class AsyncLogTargetWrapper : ILogTarget
    {
        private ILogTarget _target;

        private Thread _messageThread;

        private BlockingCollection<LogEventArgs> _messageQueue;

        private AsyncLogTargetOverflowAction _overflowAction;

        public AsyncLogTargetWrapper(ILogTarget target)
            : this(target, -1, AsyncLogTargetOverflowAction.Block)
        { }

        public AsyncLogTargetWrapper(ILogTarget target, int queueLimit, AsyncLogTargetOverflowAction overflowAction)
        {
            _target = target;

            _overflowAction = overflowAction;

            _messageQueue = new BlockingCollection<LogEventArgs>(queueLimit);

            _messageThread = new Thread(() => {
                while (!_messageQueue.IsCompleted)
                {
                    try
                    {
                        _target.Log(this, _messageQueue.Take());
                    }
                    catch (InvalidOperationException)
                    {
                        // IOE means that Take() was called on a completed collection.
                        // Some other thread can call CompleteAdding after we pass the
                        // IsCompleted check but before we call Take.
                        // We can simply catch the exception since the loop will break
                        // on the next iteration.
                    }
                }
            });

            _messageThread.IsBackground = true;
            _messageThread.Start();
        }

        public void Log(object sender, LogEventArgs e)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                if(!_messageQueue.TryAdd(e) && _overflowAction == AsyncLogTargetOverflowAction.Block)
                {
                    _messageQueue.Add(e);
                }
            }
        }

        public void Dispose()
        {
            _messageQueue.CompleteAdding();
            _messageThread.Join();
        }
    }
}