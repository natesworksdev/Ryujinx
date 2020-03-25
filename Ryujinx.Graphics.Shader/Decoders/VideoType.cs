namespace Ryujinx.Graphics.Shader.Decoders
{
    enum VideoType
    {
        U8  = 0,
        U16 = 2,
        U32 = 3,

        Signed = 1 << 2,

        S8  = Signed | U8,
        S16 = Signed | U16,
        S32 = Signed | U32
    }
}
