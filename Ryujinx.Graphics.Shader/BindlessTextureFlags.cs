namespace Ryujinx.Graphics.Shader
{
    public enum BindlessTextureFlags : ushort
    {
        None = 0,

        BindlessConverted = 1 << 0,
        BindlessNvn = 1 << 1,
        BindlessNvnSeparate = 1 << 2,
        BindlessNvnImage = 1 << 3,
        BindlessFull = 1 << 4
    }
}