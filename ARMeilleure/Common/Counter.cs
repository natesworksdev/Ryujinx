using System;

namespace ARMeilleure.Common
{
    /// <summary>
    /// Represents a numeric counter which can be used for instrumentation of compiled code.
    /// </summary>
    /// <typeparam name="T">Type of the counter</typeparam>
    class Counter<T> : IDisposable where T : unmanaged
    {
        private bool _disposed;
        private readonly int _index;
        private readonly EntryTable<T> _countTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class from the specified
        /// <see cref="EntryTable{T}"/> instance and index.
        /// </summary>
        /// <param name="countTable"><see cref="EntryTable{T}"/> instance</param>
        /// <param name="index">Index in the <see cref="EntryTable{T}"/></param>
        private Counter(EntryTable<T> countTable, int index)
        {
            _countTable = countTable;
            _index = index;
        }

        /// <summary>
        /// Gets a reference to the value of the counter.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="Counter{T}"/> instance was disposed</exception>
        public ref T Value 
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(null);
                }

                return ref _countTable.GetValue(_index); 
            }
        }

        /// <summary>
        /// Tries to create a <see cref="Counter{T}"/> instance from the specified <see cref="EntryTable{T}"/> instance.
        /// </summary>
        /// <param name="countTable"><see cref="EntryTable{T}"/> from which to create the <see cref="Counter{T}"/></param>
        /// <param name="counter"><see cref="Counter{T}"/> instance if success; otherwise <see langword="null"/></param>
        /// <returns><see langword="true"/> if success; otherwise <see langword="false"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="countTable"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentException"><typeparamref name="T"/> is unsupported</exception>
        public static bool TryCreate(EntryTable<T> countTable, out Counter<T> counter)
        {
            if (countTable == null)
            {
                throw new ArgumentNullException(nameof(countTable));
            }

            if (typeof(T) != typeof(byte) && typeof(T) != typeof(sbyte) &&
                typeof(T) != typeof(short) && typeof(T) != typeof(ushort) &&
                typeof(T) != typeof(int) && typeof(T) != typeof(uint) &&
                typeof(T) != typeof(long) && typeof(T) != typeof(ulong) &&
                typeof(T) != typeof(nint) && typeof(T) != typeof(nuint) &&
                typeof(T) != typeof(float) && typeof(T) != typeof(double))
            {
                throw new ArgumentException("Counter does not support the specified type", nameof(countTable));
            }

            if (countTable.TryAllocate(out int index))
            {
                counter = new Counter<T>(countTable, index);

                return true;
            }

            counter = null;

            return false;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Counter{T}"/> instance.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _countTable.Free(_index);

                _disposed = true;
            }
        }
    }
}
