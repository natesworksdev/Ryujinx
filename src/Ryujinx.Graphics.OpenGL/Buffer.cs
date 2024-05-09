using Silk.NET.OpenGL;
using Ryujinx.Graphics.GAL;
using System;

namespace Ryujinx.Graphics.OpenGL
{
    static class Buffer
    {
        public static void Clear(BufferHandle destination, int offset, int size, uint value)
        {
            GL.BindBuffer(BufferTargetARB.CopyWriteBuffer, destination.ToInt32());

            unsafe
            {
                uint* valueArr = stackalloc uint[1];

                valueArr[0] = value;

                GL.ClearBufferSubData(
                    BufferTargetARB.CopyWriteBuffer,
                    InternalFormat.Rgba8ui,
                    (IntPtr)offset,
                    (IntPtr)size,
                    PixelFormat.RgbaInteger,
                    PixelType.UnsignedByte,
                    (IntPtr)valueArr);
            }
        }

        public static BufferHandle Create()
        {
            return Handle.FromInt32<BufferHandle>(GL.GenBuffer());
        }

        public static BufferHandle Create(int size)
        {
            int handle = GL.GenBuffer();

            GL.BindBuffer(BufferTargetARB.CopyWriteBuffer, handle);
            GL.BufferData(BufferTargetARB.CopyWriteBuffer, size, IntPtr.Zero, BufferUsageARB.DynamicDraw);

            return Handle.FromInt32<BufferHandle>(handle);
        }

        public static BufferHandle CreatePersistent(int size)
        {
            int handle = GL.GenBuffer();

            GL.BindBuffer(BufferTargetARB.CopyWriteBuffer, handle);
            GL.BufferStorage(BufferTargetARB.CopyWriteBuffer, size, IntPtr.Zero,
                BufferStorageMask.MapPersistentBit |
                BufferStorageMask.MapCoherentBit |
                BufferStorageMask.ClientStorageBit |
                BufferStorageMask.MapReadBit);

            return Handle.FromInt32<BufferHandle>(handle);
        }

        public static void Copy(BufferHandle source, BufferHandle destination, int srcOffset, int dstOffset, int size)
        {
            GL.BindBuffer(BufferTargetARB.CopyReadBuffer, source.ToInt32());
            GL.BindBuffer(BufferTargetARB.CopyWriteBuffer, destination.ToInt32());

            GL.CopyBufferSubData(
                BufferTargetARB.CopyReadBuffer,
                BufferTargetARB.CopyWriteBuffer,
                (IntPtr)srcOffset,
                (IntPtr)dstOffset,
                (IntPtr)size);
        }

        public static unsafe PinnedSpan<byte> GetData(OpenGLRenderer renderer, BufferHandle buffer, int offset, int size)
        {
            // Data in the persistent buffer and host array is guaranteed to be available
            // until the next time the host thread requests data.

            if (renderer.PersistentBuffers.TryGet(buffer, out IntPtr ptr))
            {
                return new PinnedSpan<byte>(IntPtr.Add(ptr, offset).ToPointer(), size);
            }
            else if (HwCapabilities.UsePersistentBufferForFlush)
            {
                return PinnedSpan<byte>.UnsafeFromSpan(renderer.PersistentBuffers.Default.GetBufferData(buffer, offset, size));
            }
            else
            {
                IntPtr target = renderer.PersistentBuffers.Default.GetHostArray(size);

                GL.BindBuffer(BufferTargetARB.CopyReadBuffer, buffer.ToInt32());

                GL.GetBufferSubData(BufferTargetARB.CopyReadBuffer, (IntPtr)offset, size, target);

                return new PinnedSpan<byte>(target.ToPointer(), size);
            }
        }

        public static void Resize(BufferHandle handle, int size)
        {
            GL.BindBuffer(BufferTargetARB.CopyWriteBuffer, handle.ToInt32());
            GL.BufferData(BufferTargetARB.CopyWriteBuffer, size, IntPtr.Zero, BufferUsageARB.StreamCopy);
        }

        public static void SetData(BufferHandle buffer, int offset, ReadOnlySpan<byte> data)
        {
            GL.BindBuffer(BufferTargetARB.CopyWriteBuffer, buffer.ToInt32());

            unsafe
            {
                fixed (byte* ptr = data)
                {
                    GL.BufferSubData(BufferTargetARB.CopyWriteBuffer, (IntPtr)offset, data.Length, (IntPtr)ptr);
                }
            }
        }

        public static void Delete(BufferHandle buffer)
        {
            GL.DeleteBuffer(buffer.ToInt32());
        }
    }
}
