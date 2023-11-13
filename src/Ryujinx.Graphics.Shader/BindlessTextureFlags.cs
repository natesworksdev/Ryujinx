namespace Ryujinx.Graphics.Shader
{
    public enum BindlessTextureFlags : ushort
    {
        None = 0,

        BindlessConverted = 1 << 0,
        BindlessNvnCombined = 1 << 1,
        BindlessNvnSeparateTexture = 1 << 2,
        BindlessNvnSeparateSampler = 1 << 3,
        BindlessFull = 1 << 4,
        BindlessNvnAny = BindlessNvnCombined | BindlessNvnSeparateTexture | BindlessNvnSeparateSampler,
    }
}
