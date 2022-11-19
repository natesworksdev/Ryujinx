using Ryujinx.Graphics.OpenGL.Image;
using System;

namespace Ryujinx.Graphics.OpenGL.Effects
{
    internal interface IScaler : IDisposable
    {
        float Level { get; set; }
        void Run(
            TextureView view,
            TextureView destinationTexture,
            int width,
            int height,
            int srcX0,
            int srcX1,
            int srcY0,
            int srcY1,
            int dstX0,
            int dstX1,
            int dstY0,
            int dstY1);
    }
}