using System;

namespace Ryujinx.Graphics.Gal
{
    public unsafe interface IGalRenderer
    {
        void QueueAction(Action ActionMthd);
        void RunActions();

        void InitializeFrameBuffer();
        void ResetFrameBuffer();
        void Render();
        void SetWindowSize(int Width, int Height);
        void SetFrameBuffer(
            byte* Fb,
            int   Width,
            int   Height,
            float ScaleX,
            float ScaleY,
            float OffsX,
            float OffsY,
            float Rotate);

        //Shader
        void CreateShader(long Tag, byte[] Data, GalShaderType Type);
        void SetShaderCb(long Tag, int Cbuf, byte[] Data);
        void BindShader(long Tag);

        void SendVertexBuffer(int Index, byte[] Buffer, int Stride, GalVertexAttrib[] Attribs);

        void SendR8G8B8A8Texture(int Index, byte[] Buffer, int Width, int Height);

        void BindTexture(int Index);
    }
}