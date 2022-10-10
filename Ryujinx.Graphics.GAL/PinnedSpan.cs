using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.GAL
{
    public unsafe struct PinnedSpan<T> : IDisposable where T : unmanaged
    {
        private void* _ptr;
        private int _size;
        private Action _disposeAction;

        /// <summary>
        /// Creates a new PinnedSpan from an existing ReadOnlySpan. The data must be guaranteed to live until disposeAction is called.
        /// </summary>
        /// <param name="span">Existing span</param>
        /// <param name="disposeAction">Action to call on dispose</param>
        /// <remarks>
        /// If a dispose action is not provided, it is safe to assume the resource will be available until the next call.
        /// </remarks>
        public PinnedSpan(ReadOnlySpan<T> span, Action disposeAction = null)
        {
            _ptr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
            _size = span.Length;
            _disposeAction = disposeAction;
        }

        public ReadOnlySpan<T> Get()
        {
            return new ReadOnlySpan<T>(_ptr, _size * Unsafe.SizeOf<T>());
        }

        public void Dispose()
        {
            _disposeAction?.Invoke();
        }
    }
}
