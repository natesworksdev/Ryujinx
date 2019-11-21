using System;
using System.Threading;

namespace Ryujinx.Common
{
    public class ReactiveObject<T>
    {
        private ReaderWriterLock _readerWriterLock = new ReaderWriterLock();
        private T                _value;

        public event EventHandler<ReactiveEventArgs<T>> Event;

        public T Value
        {
            get
            {
                _readerWriterLock.AcquireReaderLock(int.MaxValue);
                T value = _value;
                _readerWriterLock.ReleaseReaderLock();

                return value;
            }
            set
            {
                _readerWriterLock.AcquireWriterLock(int.MaxValue);

                T oldValue = _value;
                
                _value = value;

                _readerWriterLock.ReleaseWriterLock();

                if (oldValue == null || !oldValue.Equals(_value))
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
        public T OldValue { get; private set; }
        public T Value    { get; private set; }

        public ReactiveEventArgs(T oldValue, T value)
        {
            OldValue = oldValue;
            Value    = value;
        }
    }
}
