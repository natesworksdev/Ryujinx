using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLFrameBuffer
    {
        private struct FrameBuffer
        {
            public int Handle;
            public int RbHandle;
            public int TexHandle;
        }

        private struct ShaderProgram
        {
            public int Handle;
            public int VpHandle;
            public int FpHandle;
        }

        private Dictionary<long, FrameBuffer> Fbs;

        private ShaderProgram Shader;

        private bool IsInitialized;

        private int CurrFbHandle;
        private int CurrTexHandle;
        private int RawFbTexHandle;
        private int VaoHandle;
        private int VboHandle;

        public OGLFrameBuffer()
        {
            Fbs = new Dictionary<long, FrameBuffer>();

            Shader = new ShaderProgram();
        }

        public void Create(long Tag, int Width, int Height)
        {
            if (Fbs.ContainsKey(Tag))
            {
                return;
            }

            Width  = 1280;
            Height = 720;

            FrameBuffer Fb = new FrameBuffer();

            Fb.Handle    = GL.GenFramebuffer();
            Fb.RbHandle  = GL.GenRenderbuffer();
            Fb.TexHandle = GL.GenTexture();

            SetupTexture(Fb.TexHandle, Width, Height);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Fb.Handle);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Fb.RbHandle);

            GL.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer,
                RenderbufferStorage.Depth24Stencil8,
                Width,
                Height);

            GL.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthStencilAttachment,
                RenderbufferTarget.Renderbuffer,
                Fb.RbHandle);

            GL.FramebufferTexture(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                Fb.TexHandle,
                0);

            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

            Fbs.Add(Tag, Fb);
        }

        public void Bind(long Tag)
        {
            if (Fbs.TryGetValue(Tag, out FrameBuffer Fb))
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, Fb.Handle);

                CurrFbHandle = Fb.Handle;
            }
        }

        public void BindTexture(long Tag, int Index)
        {
            if (Fbs.TryGetValue(Tag, out FrameBuffer Fb))
            {
                GL.ActiveTexture(TextureUnit.Texture0 + Index);

                GL.BindTexture(TextureTarget.Texture2D, Fb.TexHandle);
            }
        }

        public void Set(long Tag)
        {
            if (Fbs.TryGetValue(Tag, out FrameBuffer Fb))
            {
                CurrTexHandle = Fb.TexHandle;
            }
        }

        public void Set(byte[] Data, int Width, int Height)
        {
            EnsureInitialized();

            GL.ActiveTexture(TextureUnit.Texture0);

            GL.BindTexture(TextureTarget.Texture2D, RawFbTexHandle);

            (PixelFormat Format, PixelType Type) = OGLEnumConverter.GetTextureFormat(GalTextureFormat.A8B8G8R8);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, Format, Type, Data);

            CurrTexHandle = RawFbTexHandle;
        }

        public void Render()
        {
            if (CurrTexHandle != 0)
            {
                EnsureInitialized();

                GL.ActiveTexture(TextureUnit.Texture0);

                GL.BindTexture(TextureTarget.Texture2D, CurrTexHandle);

                int CurrentProgram = GL.GetInteger(GetPName.CurrentProgram);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                GL.BindVertexArray(VaoHandle);

                GL.UseProgram(Shader.Handle);

                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

                //Restore the original state.
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, CurrFbHandle);

                GL.UseProgram(CurrentProgram);
            }
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                IsInitialized = true;

                SetupShader();
                SetupVertex();

                RawFbTexHandle = GL.GenTexture();

                SetupTexture(RawFbTexHandle, 1280, 720);
            }
        }

        private void SetupShader()
        {
            Shader.VpHandle = GL.CreateShader(ShaderType.VertexShader);
            Shader.FpHandle = GL.CreateShader(ShaderType.FragmentShader);

            string VpSource = EmbeddedResource.GetString("GlFbVtxShader");
            string FpSource = EmbeddedResource.GetString("GlFbFragShader");

            GL.ShaderSource(Shader.VpHandle, VpSource);
            GL.ShaderSource(Shader.FpHandle, FpSource);
            GL.CompileShader(Shader.VpHandle);
            GL.CompileShader(Shader.FpHandle);

            Shader.Handle = GL.CreateProgram();

            GL.AttachShader(Shader.Handle, Shader.VpHandle);
            GL.AttachShader(Shader.Handle, Shader.FpHandle);
            GL.LinkProgram(Shader.Handle);
            GL.UseProgram(Shader.Handle);

            Matrix2 Transform = Matrix2.CreateScale(1, -1);

            int TexUniformLocation = GL.GetUniformLocation(Shader.Handle, "tex");

            GL.Uniform1(TexUniformLocation, 0);

            int WindowSizeUniformLocation = GL.GetUniformLocation(Shader.Handle, "window_size");

            GL.Uniform2(WindowSizeUniformLocation, new Vector2(1280.0f, 720.0f));

            int TransformUniformLocation = GL.GetUniformLocation(Shader.Handle, "transform");

            GL.UniformMatrix2(TransformUniformLocation, false, ref Transform);
        }

        private void SetupVertex()
        {
            VaoHandle = GL.GenVertexArray();
            VboHandle = GL.GenBuffer();

            float[] Buffer = new float[]
            {
                -1,  1,  0,  0,
                 1,  1,  1,  0,
                -1, -1,  0,  1,
                 1, -1,  1,  1
            };

            IntPtr Length = new IntPtr(Buffer.Length * 4);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, Length, Buffer, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.BindVertexArray(VaoHandle);

            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 16, 0);

            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 16, 8);
        }

        private void SetupTexture(int Handle, int Width, int Height)
        {
            GL.BindTexture(TextureTarget.Texture2D, Handle);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            (PixelFormat Format, PixelType Type) = OGLEnumConverter.GetTextureFormat(GalTextureFormat.A8B8G8R8);

            const PixelInternalFormat InternalFmt = PixelInternalFormat.Rgba;

            const int Level  = 0;
            const int Border = 0;

            GL.TexImage2D(
                TextureTarget.Texture2D,
                Level,
                InternalFmt,
                Width,
                Height,
                Border,
                Format,
                Type,
                IntPtr.Zero);
        }
    }
}