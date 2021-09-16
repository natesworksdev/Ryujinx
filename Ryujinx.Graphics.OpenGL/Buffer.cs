using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.OpenGL
{
    static class Buffer
    {
        public unsafe static void Clear(BufferHandle destination, int offset, int size, uint value)
        {
            uint* valuePtr = stackalloc uint[3];

            valuePtr[0] = value;
            valuePtr[1] = value;
            valuePtr[2] = value;

            GL.ClearNamedBufferSubData(
                destination.ToInt32(),
                PixelInternalFormat.Rgba8ui,
                (IntPtr)offset,
                (IntPtr)size,
                PixelFormat.RgbaInteger,
                PixelType.UnsignedInt,
                (IntPtr)valuePtr);
        }

        public unsafe static BufferHandle Create()
        {
            int handle;

            GL.CreateBuffers(1, &handle);

            return Handle.FromInt32<BufferHandle>(handle);
        }

        public unsafe static BufferHandle Create(int size)
        {
            int handle;

            GL.CreateBuffers(1, &handle);

            GL.NamedBufferData(handle, size, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            return Handle.FromInt32<BufferHandle>(handle);
        }

        public static void Copy(BufferHandle source, BufferHandle destination, int srcOffset, int dstOffset, int size)
        {
            GL.CopyNamedBufferSubData(
                source.ToInt32(),
                destination.ToInt32(),
                (IntPtr)srcOffset,
                (IntPtr)dstOffset,
                (IntPtr)size);
        }

        public static unsafe ReadOnlySpan<byte> GetData(Renderer renderer, BufferHandle buffer, int offset, int size)
        {
            if (HwCapabilities.UsePersistentBufferForFlush)
            {
                return renderer.PersistentBuffers.Default.GetBufferData(buffer, offset, size);
            }
            else
            {
                IntPtr target = renderer.PersistentBuffers.Default.GetHostArray(size);

                GL.GetNamedBufferSubData(buffer.ToInt32(), (IntPtr)offset, size, target);

                return new ReadOnlySpan<byte>(target.ToPointer(), size);
            }
        }

        public static void Resize(BufferHandle handle, int size)
        {
            GL.NamedBufferData(handle.ToInt32(), size, IntPtr.Zero, BufferUsageHint.StreamCopy);
        }

        public static void SetData(BufferHandle buffer, int offset, ReadOnlySpan<byte> data)
        {
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    GL.NamedBufferSubData(buffer.ToInt32(), (IntPtr)offset, data.Length, (IntPtr)ptr);
                }
            }
        }

        public static void Delete(BufferHandle buffer)
        {
            GL.DeleteBuffer(buffer.ToInt32());
        }
    }
}
