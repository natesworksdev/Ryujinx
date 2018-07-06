using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    abstract class OGLStreamBuffer : IDisposable
    {
        public int Handle { get; protected set; }

        public int Size { get; protected set; }

        protected BufferTarget Target { get; private set; }

        private bool Mapped;

        public OGLStreamBuffer(BufferTarget Target, int MaxSize)
        {
            Handle = 0;
            Mapped = false;

            this.Target = Target;
            this.Size = MaxSize;
        }

        public static OGLStreamBuffer Create(BufferTarget Target, int MaxSize)
        {
            return new MapAndOrphan(Target, MaxSize);
        }

        public IntPtr Map(int Size)
        {
            if (Handle == 0 || Mapped || Size > this.Size)
            {
                throw new InvalidOperationException();
            }

            IntPtr Memory = InternMap(Size);

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

        public abstract void Bind(int Index);

        protected abstract IntPtr InternMap(int Size);

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

        class MapAndOrphan : OGLStreamBuffer
        {
            private const BufferAccessMask Access =
                BufferAccessMask.MapInvalidateBufferBit |
                BufferAccessMask.MapUnsynchronizedBit |
                BufferAccessMask.MapWriteBit;

            public MapAndOrphan(BufferTarget Target, int MaxSize)
                : base(Target, MaxSize)
            {
                Handle = GL.GenBuffer();

                GL.BindBuffer(Target, Handle);

                GL.BufferData(Target, Size, IntPtr.Zero, BufferUsageHint.StreamDraw);
            }

            public override void Bind(int Index)
            {
                GL.BindBufferBase((BufferRangeTarget)Target, Index, Handle);
            }

            protected override IntPtr InternMap(int Size)
            {
                GL.BindBuffer(Target, Handle);

                return GL.MapBufferRange(Target, IntPtr.Zero, Size, Access);
            }

            protected override void InternUnmap(int UsedSize)
            {
                //It's not needed to bind handle to Target here, since Map was called previously

                GL.UnmapBuffer(Target);
            }
        }
    }
}
