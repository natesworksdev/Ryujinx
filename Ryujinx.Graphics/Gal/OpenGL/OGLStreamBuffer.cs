using System;
using OpenTK.Graphics.OpenGL;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    abstract class OGLStreamBuffer : IDisposable
    {
        public int Handle { get; protected set; }

        protected BufferTarget Target;

        protected int Size;

        private bool Mapped = false;

        public OGLStreamBuffer(BufferTarget Target, int MaxSize)
        {
            Handle = 0;
            Mapped = false;

            this.Target = Target;
            this.Size = MaxSize;
        }

        public static OGLStreamBuffer Create(BufferTarget Target, int MaxSize)
        {
            return new SubDataBuffer(Target, MaxSize);
        }

        public void Allocate()
        {
            if (this.Handle == 0)
            {
                GL.CreateBuffers(1, out int Handle);

                this.Handle = Handle;

                InternAllocate();
            }
        }

        public byte[] Map(int Size)
        {
            if (Handle == 0 || Mapped || Size > this.Size)
            {
                throw new InvalidOperationException();
            }

            byte[] Memory = InternMap(Size);

            Mapped = true;

            return Memory;
        }

        public void Unmap(int UsedSize)
        {
            if (Handle == 0 || !Mapped)
            {
                throw new InvalidOperationException();
            }

            InternUnmap(UsedSize);

            Mapped = false;
        }

        protected abstract void InternAllocate();

        protected abstract byte[] InternMap(int Size);

        protected abstract void InternUnmap(int UsedSize);

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool Disposing)
        {
            if (Disposing && Handle != 0)
            {
                GL.DeleteBuffer(Handle);

                Handle = 0;
            }
        }
    }

    class SubDataBuffer : OGLStreamBuffer
    {
        private byte[] Memory;

        public SubDataBuffer(BufferTarget Target, int MaxSize)
            : base(Target, MaxSize)
        {
            Memory = new byte[MaxSize];
        }

        protected override void InternAllocate()
        {
            GL.BindBuffer(Target, Handle);

            GL.BufferData(Target, Size, IntPtr.Zero, BufferUsageHint.StreamDraw);
        }

        protected override byte[] InternMap(int Size)
        {
            return Memory;
        }

        protected override void InternUnmap(int UsedSize)
        {
            GL.BindBuffer(Target, Handle);
            
            unsafe
            {
                fixed (byte* MemoryPtr = Memory)
                {
                    GL.BufferSubData(Target, IntPtr.Zero, UsedSize, (IntPtr)MemoryPtr);
                }
            }
        }
    }
}