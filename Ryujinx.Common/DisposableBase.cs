using System;

namespace Ryujinx.Common
{
    /// <summary>
    /// A base class to implement IDisposable pattern for classes with only managed resources.
    /// </summary>
    /// <remarks>
    /// Use <see cref="DisposableUnmanagedBase"/> when there are unmanaged resources to dispose.
    /// </remarks>
    public abstract class DisposableBase : IDisposable
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

            Disposed = true;
        }

        /// <summary>
        /// Dispose managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
        }

        protected abstract void DisposeManaged();
    }
}
