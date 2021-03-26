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
        public string _name { get; private set; }
        public string Name { get { return _name; } set { _name = value; } }
        public string _category { get; private set; }
        public string Category { get { return _category; } set { _category = value; } }

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
                _value = value;

                _readerWriterLock.ReleaseWriterLock();

                if (!oldIsInitialized || !oldValue.Equals(_value))
                {
                    if (!string.IsNullOrEmpty(Name))
                        //When values are changed, logged to console. If value does not have a Name, no log is printed
                        //Some reactive object names are left commented out here, since they don't need to be exposed to the end user but the option is there for devs
                        Logger.Info?.Print(LogClass.Application, $"({Category}) {Name} set to: {_value}");

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
