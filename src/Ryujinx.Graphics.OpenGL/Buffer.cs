using Ryujinx.Graphics.GAL;
using Silk.NET.OpenGL.Legacy;
using System;

namespace Ryujinx.Graphics.OpenGL
{
    static class Buffer
    {
        public static void Clear(GL api, BufferHandle destination, int offset, uint size, uint value)
        {
            api.BindBuffer(BufferTargetARB.CopyWriteBuffer, destination.ToUInt32());

            unsafe
            {
                uint* valueArr = stackalloc uint[1];

                valueArr[0] = value;

                api.ClearBufferSubData(
                    BufferTargetARB.CopyWriteBuffer,
                    SizedInternalFormat.Rgba8ui,
                    offset,
                    size,
                    PixelFormat.RgbaInteger,
                    PixelType.UnsignedByte,
                    valueArr);
            }
        }

        public static BufferHandle Create(GL api)
        {
            return Handle.FromUInt32<BufferHandle>(api.GenBuffer());
        }

        public static BufferHandle Create(GL api, int size)
        {
            uint handle = api.GenBuffer();

            api.BindBuffer(BufferTargetARB.CopyWriteBuffer, handle);
            api.BufferData(BufferTargetARB.CopyWriteBuffer, (uint)size, in UIntPtr.Zero, BufferUsageARB.DynamicDraw);

            return Handle.FromUInt32<BufferHandle>(handle);
        }

        public static BufferHandle CreatePersistent(GL api, int size)
        {
            uint handle = api.GenBuffer();

            api.BindBuffer(BufferTargetARB.CopyWriteBuffer, handle);
            api.BufferStorage(BufferStorageTarget.CopyWriteBuffer, (uint)size, in UIntPtr.Zero,
                BufferStorageMask.MapPersistentBit |
                BufferStorageMask.MapCoherentBit |
                BufferStorageMask.ClientStorageBit |
                BufferStorageMask.MapReadBit);

            return Handle.FromUInt32<BufferHandle>(handle);
        }

        public static void Copy(GL api, BufferHandle source, BufferHandle destination, int srcOffset, int dstOffset, int size)
        {
            api.BindBuffer(BufferTargetARB.CopyReadBuffer, source.ToUInt32());
            api.BindBuffer(BufferTargetARB.CopyWriteBuffer, destination.ToUInt32());

            api.CopyBufferSubData(
                CopyBufferSubDataTarget.CopyReadBuffer,
                CopyBufferSubDataTarget.CopyWriteBuffer,
                srcOffset,
                dstOffset,
                (uint)size);
        }

        public static unsafe PinnedSpan<byte> GetData(OpenGLRenderer gd, BufferHandle buffer, int offset, int size)
        {
            // Data in the persistent buffer and host array is guaranteed to be available
            // until the next time the host thread requests data.

            if (gd.PersistentBuffers.TryGet(buffer, out IntPtr ptr))
            {
                return new PinnedSpan<byte>(IntPtr.Add(ptr, offset).ToPointer(), size);
            }
            else if (gd.Capabilities.UsePersistentBufferForFlush)
            {
                return PinnedSpan<byte>.UnsafeFromSpan(gd.PersistentBuffers.Default.GetBufferData(buffer, offset, size));
            }
            else
            {
                IntPtr target = gd.PersistentBuffers.Default.GetHostArray(size);

                gd.Api.BindBuffer(BufferTargetARB.CopyReadBuffer, buffer.ToUInt32());

                gd.Api.GetBufferSubData(BufferTargetARB.CopyReadBuffer, offset, (uint)size, (void*)target);

                return new PinnedSpan<byte>(target.ToPointer(), size);
            }
        }

        public static void Resize(GL api, BufferHandle handle, int size)
        {
            api.BindBuffer(BufferTargetARB.CopyWriteBuffer, handle.ToUInt32());
            api.BufferData(BufferTargetARB.CopyWriteBuffer, (uint)size, in UIntPtr.Zero, BufferUsageARB.StreamCopy);
        }

        public static void SetData(GL api, BufferHandle buffer, int offset, ReadOnlySpan<byte> data)
        {
            api.BindBuffer(BufferTargetARB.CopyWriteBuffer, buffer.ToUInt32());
            api.BufferSubData(BufferTargetARB.CopyWriteBuffer, offset, (uint)data.Length, data);
        }

        public static void Delete(GL api, BufferHandle buffer)
        {
            api.DeleteBuffer(buffer.ToUInt32());
        }
    }
}
