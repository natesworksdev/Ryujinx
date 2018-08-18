using System;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalRenderer
    {
        void Initialize();

        void QueueAction(Action ActionMthd);

        void RunActions();

        IGalConstBuffer Buffer { get; }

        IGalFrameBuffer FrameBuffer { get; }

        IGalRasterizer Rasterizer { get; }

        IGalShader Shader { get; }

        IGalPipeline Pipeline { get; }

        IGalTexture Texture { get; }
    }
}