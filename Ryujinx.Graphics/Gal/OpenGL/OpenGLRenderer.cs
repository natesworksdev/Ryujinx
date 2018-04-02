using OpenTK;
using System;
using System.Collections.Concurrent;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    public class OpenGLRenderer : IGalRenderer
    {
        private OGLRasterizer Rasterizer;

        private OGLShader Shader;

        private OGLTexture Texture;

        private ConcurrentQueue<Action> ActionsQueue;

        private FrameBuffer FbRenderer;

        public OpenGLRenderer()
        {
            Rasterizer = new OGLRasterizer();

            Shader = new OGLShader();

            Texture = new OGLTexture(Shader);

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

        public void CreateShader(long Tag, GalShaderType Type, byte[] Data)
        {
            if (Data == null)
            {
                throw new ArgumentNullException(nameof(Data));
            }

            ActionsQueue.Enqueue(() => Shader.Create(Tag, Type, Data));
        }

        public void SetShaderConstBuffer(long Tag, int Cbuf, byte[] Data)
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

        public void UpdateTextures(Func<int, GalShaderType, GalTexture> RequestTextureCallback)
        {
            ActionsQueue.Enqueue(() => Texture.UpdateTextures(RequestTextureCallback));
        }
    }
}