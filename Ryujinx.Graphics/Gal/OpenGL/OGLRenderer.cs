using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Concurrent;
using System.Text;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    public class OGLRenderer : IGalRenderer
    {
        public IGalConstBuffer Buffer { get; private set; }

        public IGalFrameBuffer FrameBuffer { get; private set; }

        public IGalRasterizer Rasterizer { get; private set; }

        public IGalShader Shader { get; private set; }

        public IGalPipeline Pipeline { get; private set; }

        public IGalTexture Texture { get; private set; }

        private ConcurrentQueue<Action> ActionsQueue;

        private DebugProc DebugProcDelegate;

        public OGLRenderer()
        {
            Buffer = new OGLConstBuffer();

            FrameBuffer = new OGLFrameBuffer();

            Rasterizer = new OGLRasterizer();

            Shader = new OGLShader(Buffer as OGLConstBuffer);

            Pipeline = new OGLPipeline(Buffer as OGLConstBuffer, Rasterizer as OGLRasterizer, Shader as OGLShader);

            Texture = new OGLTexture();

            ActionsQueue = new ConcurrentQueue<Action>();
        }

        public void Initialize()
        {
            if (GraphicsConfig.DebugMode && OGLExtension.HasDebug())
            {
                DebugProcDelegate = DebugCallback;

                GL.DebugMessageCallback(DebugProcDelegate, IntPtr.Zero);
            }
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

        private static unsafe void DebugCallback(
            DebugSource Source,
            DebugType Type,
            int Id,
            DebugSeverity Severity,
            int Length,
            IntPtr Message,
            IntPtr UserParam)
        {
            bool IsError = Type == DebugType.DebugTypeError || Type == DebugType.DebugTypeDeprecatedBehavior;

            if (!IsError && !GraphicsConfig.DebugEnableInfo)
            {
                return;
            }

            string MessageLog = Encoding.UTF8.GetString((byte*)Message, Length);

            Console.ForegroundColor = IsError ? ConsoleColor.Red : ConsoleColor.Cyan;

            Console.WriteLine(MessageLog);

            Console.ResetColor();

            if (IsError && GraphicsConfig.DebugFatalErrors)
            {
                throw new OpenGLException(MessageLog);
            }
        }
    }
}