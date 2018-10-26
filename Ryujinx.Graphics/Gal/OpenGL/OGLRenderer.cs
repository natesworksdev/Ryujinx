using System;
using System.Collections.Concurrent;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    public class OGLRenderer : IGalRenderer
    {
        public IGalConstBuffer Buffer { get; private set; }

        public IGalRenderTarget RenderTarget { get; private set; }

        public IGalRasterizer Rasterizer { get; private set; }

        public IGalShader Shader { get; private set; }

        public IGalPipeline Pipeline { get; private set; }

        public IGalTexture Texture { get; private set; }

        private ConcurrentQueue<Action> _actionsQueue;

        public OGLRenderer()
        {
            Buffer = new OGLConstBuffer();

            Texture = new OGLTexture();

            RenderTarget = new OGLRenderTarget(Texture as OGLTexture);

            Rasterizer = new OGLRasterizer();

            Shader = new OGLShader(Buffer as OGLConstBuffer);

            Pipeline = new OGLPipeline(Buffer as OGLConstBuffer, Rasterizer as OGLRasterizer, Shader as OGLShader);

            _actionsQueue = new ConcurrentQueue<Action>();
        }

        public void QueueAction(Action actionMthd)
        {
            _actionsQueue.Enqueue(actionMthd);
        }

        public void RunActions()
        {
            int count = _actionsQueue.Count;

            while (count-- > 0 && _actionsQueue.TryDequeue(out Action renderAction))
            {
                renderAction();
            }
        }
    }
}