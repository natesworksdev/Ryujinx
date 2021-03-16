using System;

namespace Ryujinx.Common
{
    /// <summary>
    /// A base class to implement IDisposable pattern for classes with unmanaged resources.
    /// </summary>
    /// <remarks>
    /// Use <see cref="DisposableBase"/> when there are only managed resources to dispose.
    /// </remarks>
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
