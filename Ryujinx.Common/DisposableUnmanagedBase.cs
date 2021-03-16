using System;

namespace Ryujinx
{
    /// <summary>
    /// A base class to implement IDisposable pattern for classes with unmanaged resources.
    /// Use DisposableBase when there are only manage resources to dispose.
    /// </summary>
    public abstract class DisposableUnmanagedBase : IDisposable
    {
        /// <summary>
        /// A flag that indicates whether object has been disposed.
        /// </summary>
        public bool Disposed { get; private set; }

        protected void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
                DisposeManaged();
            }

            DisposeUnmanaged();
            Disposed = true;
        }

        ~DisposableUnmanagedBase()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Dispose managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeManaged() { }

        protected abstract void DisposeUnmanaged();
    }
}
