using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLStreamBuffer : IDisposable
    {
        public int Handle { get; protected set; }

        public long Size { get; protected set; }

        protected BufferTarget Target { get; private set; }

        public OGLStreamBuffer(BufferTarget target, int size)
        {
            Target = target;
            Size   = size;

            Handle = GL.GenBuffer();

            GL.BindBuffer(target, Handle);

            GL.BufferData(target, new IntPtr(size), IntPtr.Zero, BufferUsageHint.StreamDraw);
        }

        public void SetData(IntPtr hostAddress, int size)
        {
            GL.BindBuffer(Target, Handle);

            GL.BufferSubData(Target, IntPtr.Zero, new IntPtr(size), hostAddress);
        }

        public void SetData(byte[] buffer)
        {
            GL.BindBuffer(Target, Handle);

            GL.BufferSubData(Target, IntPtr.Zero, new IntPtr(buffer.Length), buffer);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && Handle != 0)
            {
                GL.DeleteBuffer(Handle);

                Handle = 0;
            }
        }
    }
}
