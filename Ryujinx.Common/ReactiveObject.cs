using System;
using System.Threading;
using Ryujinx.Common.Logging;

namespace Ryujinx.Common
{
    public class ReactiveObject<T>
    {
        private ReaderWriterLock _readerWriterLock = new ReaderWriterLock();
        private bool _isInitialized = false;
        private T _value;

        private string _loggedName;

        public ReactiveObject(string loggedName = "")
        {
            _loggedName = loggedName;
        }

        public event EventHandler<ReactiveEventArgs<T>> Event;

        public T Value
        {
            get
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                T value = _value;
                _readerWriterLock.ReleaseReaderLock();

                return value;
            }
            set
            {
                _readerWriterLock.AcquireWriterLock(Timeout.Infinite);

                T oldValue = _value;

                bool oldIsInitialized = _isInitialized;

                _isInitialized = true;
                _value         = value;

                _readerWriterLock.ReleaseWriterLock();

                if (!oldIsInitialized || !oldValue.Equals(_value))
                {
                    if (!string.IsNullOrEmpty(_loggedName))
                    {
                        Logger.Info?.Print(LogClass.Configuration, $"{_loggedName} set to: {_value}");
                    }

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
