using System;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalRenderer
    {
        void QueueAction(Action ActionMthd);

        void RunActions();

        IGalFrameBuffer FrameBuffer { get; }

        IGalRasterizer Rasterizer { get; }

        IGalShader Shader { get; }

        IGalPipeline Pipeline { get; }

        IGalTexture Texture { get; }
    }
}