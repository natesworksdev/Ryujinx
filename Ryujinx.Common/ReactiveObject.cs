﻿using System;
using System.Threading;

namespace Ryujinx.Common
{
    public class ReactiveObject<T>
    {
        private readonly ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim();
        private bool _isInitialized = false;
        private T _value;

        public event EventHandler<ReactiveEventArgs<T>> Event;

        public T Value
        {
            get
            {
                _readerWriterLock.EnterReadLock();
                try
                {
                    return _value;
                }
                finally
                {
                    _readerWriterLock.ExitReadLock();
                }
            }
            set
            {
                T oldValue;
                bool oldIsInitialized;

                _readerWriterLock.EnterWriteLock();
                try
                {
                    oldValue = _value;
                    oldIsInitialized = _isInitialized;

                    _isInitialized = true;
                    _value = value;

                }
                finally
                {
                    _readerWriterLock.ExitWriteLock();
                }

                if (!oldIsInitialized || !oldValue.Equals(_value))
                {
                    Event?.Invoke(this, new ReactiveEventArgs<T>(oldValue, value));
                }
            }
        }

        public static implicit operator T(ReactiveObject<T> obj) => obj.Value;
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
