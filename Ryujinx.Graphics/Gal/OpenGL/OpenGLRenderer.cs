using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Concurrent;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    public class OpenGLRenderer : IGalRenderer
    {
        private struct Texture
        {
            public int Handle;
        }

        private Texture[] Textures;

        private OGLRasterizer Rasterizer;

        private OGLShader Shader;

        private ConcurrentQueue<Action> ActionsQueue;

        private FrameBuffer FbRenderer;

        public OpenGLRenderer()
        {
            Textures = new Texture[8];

            Rasterizer = new OGLRasterizer();

            Shader = new OGLShader();

            ActionsQueue = new ConcurrentQueue<Action>();
        }

        public void InitializeFrameBuffer()
        {
            FbRenderer = new FrameBuffer(1280, 720);
        }

        public void ResetFrameBuffer()
        {
            FbRenderer.Reset();
        }

        public void QueueAction(Action ActionMthd)
        {
            ActionsQueue.Enqueue(ActionMthd);
        }

        public void RunActions()
        {
            int Count = ActionsQueue.Count;

            while (Count-- > 0 && ActionsQueue.TryDequeue(out Action RenderAction))
            {
                RenderAction();
            }
        }

        public void Render()
        {
            FbRenderer.Render();
        }

        public void SetWindowSize(int Width, int Height)
        {
            FbRenderer.WindowWidth  = Width;
            FbRenderer.WindowHeight = Height;
        }

        public unsafe void SetFrameBuffer(
            byte* Fb,
            int   Width,
            int   Height,
            float ScaleX,
            float ScaleY,
            float OffsX,
            float OffsY,
            float Rotate)
        {
            Matrix2 Transform;

            Transform  = Matrix2.CreateScale(ScaleX, ScaleY);
            Transform *= Matrix2.CreateRotation(Rotate);

            Vector2 Offs = new Vector2(OffsX, OffsY);

            FbRenderer.Set(Fb, Width, Height, Transform, Offs);
        }

        public void ClearBuffers(int RtIndex, GalClearBufferFlags Flags)
        {
            ActionsQueue.Enqueue(() => Rasterizer.ClearBuffers(RtIndex, Flags));
        }

        public void SetVertexArray(int VbIndex, int Stride, byte[] Buffer, GalVertexAttrib[] Attribs)
        {
            if ((uint)VbIndex > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(VbIndex));
            }

            ActionsQueue.Enqueue(() => Rasterizer.SetVertexArray(VbIndex, Stride,
                Buffer  ?? throw new ArgumentNullException(nameof(Buffer)),
                Attribs ?? throw new ArgumentNullException(nameof(Attribs))));
        }

        public void RenderVertexArray(int VbIndex)
        {
            if ((uint)VbIndex > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(VbIndex));
            }

            ActionsQueue.Enqueue(() => Rasterizer.RenderVertexArray(VbIndex));
        }

        public void SendR8G8B8A8Texture(int Index, byte[] Buffer, int Width, int Height)
        {
            EnsureTexInitialized(Index);

            GL.BindTexture(TextureTarget.Texture2D, Textures[Index].Handle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                Width,
                Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                Buffer);
        }

        public void BindTexture(int Index)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + Index);

            GL.BindTexture(TextureTarget.Texture2D, Textures[Index].Handle);
        }

        public void CreateShader(long Tag, GalShaderType Type, byte[] Data)
        {
            if (Data == null)
            {
                throw new ArgumentNullException(nameof(Data));
            }

            ActionsQueue.Enqueue(() => Shader.Create(Tag, Type, Data));
        }

        public void SetShaderCb(long Tag, int Cbuf, byte[] Data)
        {
            if (Data == null)
            {
                throw new ArgumentNullException(nameof(Data));
            }

            ActionsQueue.Enqueue(() => Shader.SetConstBuffer(Tag, Cbuf, Data));
        }

        public void BindShader(long Tag)
        {
            ActionsQueue.Enqueue(() => Shader.Bind(Tag));
        }

        public void BindProgram()
        {
            ActionsQueue.Enqueue(() => Shader.BindProgram());
        }

        private void EnsureTexInitialized(int TexIndex)
        {
            Texture Tex = Textures[TexIndex];

            if (Tex.Handle == 0)
            {
                Tex.Handle = GL.GenTexture();
            }

            Textures[TexIndex] = Tex;
        }
    }
}