namespace Ryujinx.Graphics.Shader
{
    public enum BindlessTextureFlags : ushort
    {
        None = 0,

        BindlessConverted = 1 << 0,
        BindlessNvn = 1 << 1,
        BindlessFull = 1 << 2,
    }
}