using Ryujinx.Graphics.GAL;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.OpenGL
{
    /// <summary>
    /// Represents a buffer that stores data of a given type.
    /// </summary>
    /// <typeparam name="T">Type of the buffer data</typeparam>
    class TypedBuffer<T> where T : unmanaged
    {
        private readonly OpenGLRenderer _renderer;
        private BufferHandle _buffer;

        /// <summary>
        /// Size of the buffer in bytes.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Creates a new instance of the typed buffer.
        /// </summary>
        /// <param name="renderer">Renderer</param>
        /// <param name="count">Number of data elements on the buffer</param>
        public TypedBuffer(OpenGLRenderer renderer, int count)
        {
            _renderer = renderer;
            _buffer = renderer.CreateBuffer(Size = count * Unsafe.SizeOf<T>(), BufferHandle.Null);
            renderer.SetBufferData(_buffer, 0, new byte[Size]);
        }

        /// <summary>
        /// Writes data into a given buffer index.
        /// </summary>
        /// <param name="index">Index to write the data</param>
        /// <param name="value">Data to be written</param>
        public void Write(int index, T value)
        {
            _renderer.SetBufferData(_buffer, index * Unsafe.SizeOf<T>(), MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref value, 1)));
        }

        /// <summary>
        /// Ensures that the buffer can hold a given number of elements.
        /// </summary>
        /// <param name="count">Number of elements</param>
        /// <returns>True if the buffer was resized and needs to be rebound, false otherwise</returns>
        public bool EnsureCapacity(int count)
        {
            int size = count * Unsafe.SizeOf<T>();

            if (Size < size)
            {
                BufferHandle oldBuffer = _buffer;
                BufferHandle newBuffer = _renderer.CreateBuffer(size, BufferHandle.Null);
                _renderer.SetBufferData(newBuffer, 0, new byte[size]);

                _renderer.Pipeline.CopyBuffer(oldBuffer, newBuffer, 0, 0, Size);
                _renderer.DeleteBuffer(oldBuffer);

                _buffer = newBuffer;
                Size = size;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a buffer range covering the whole buffer.
        /// </summary>
        /// <returns>The buffer range</returns>
        public BufferRange GetBufferRange()
        {
            return new BufferRange(_buffer, 0, Size);
        }
    }
}
