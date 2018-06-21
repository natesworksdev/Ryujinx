using System;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalFrameBuffer
    {
        void Create(long Tag, int Width, int Height);

        void Bind(long Tag);

        void BindTexture(long Tag, int Index);

        void Set(long Tag);

        void Set(byte[] Data, int Width, int Height);

        void SetTransform(float SX, float SY, float Rotate, float TX, float TY);

        void SetWindowSize(int Width, int Height);

        void SetViewport(int X, int Y, int Width, int Height);

        void Render();

        void GetBufferData(long Tag, Action<byte[]> Callback);
    }
}