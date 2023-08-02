using System;
using System.Threading;

namespace Ryujinx.Common
{
    public class ReactiveObject<T>
    {
        private readonly ReaderWriterLockSlim _readerWriterLock = new();
        private bool _isInitialized;
        private T _value;

        public event EventHandler<ReactiveEventArgs<T>> Event;

        public T Value
        {
            get
            {
                try
                {
                    _readerWriterLock.TryEnterReadLock(Timeout.Infinite);

                    return _value;
                }
                finally
                {
                    if (_readerWriterLock.IsReadLockHeld)
                    {
                        _readerWriterLock.ExitReadLock();
                    }
                }
            }
            set
            {
                T oldValue;
                bool oldIsInitialized;

                try
                {
                    _readerWriterLock.TryEnterWriteLock(Timeout.Infinite);

                    oldValue = _value;
                    oldIsInitialized = _isInitialized;

                    _isInitialized = true;
                    _value = value;
                }
                finally
                {
                    if (_readerWriterLock.IsWriteLockHeld)
                    {
                        _readerWriterLock.ExitWriteLock();
                    }
                }
                if (!oldIsInitialized || oldValue == null || !oldValue.Equals(_value))
                {
                    Event?.Invoke(this, new ReactiveEventArgs<T>(oldValue, value));
                }
            }
        }


        public static implicit operator T(ReactiveObject<T> obj)
        {
            return obj.Value;
        }
    }

    public class ReactiveEventArgs<T>
    {
        public T OldValue { get; }
        public T NewValue { get; }

        public ReactiveEventArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
