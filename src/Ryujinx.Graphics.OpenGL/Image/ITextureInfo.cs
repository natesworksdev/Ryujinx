using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    interface ITextureInfo
    {
        ITextureInfo Storage { get; }
        uint Handle { get; }
        int FirstLayer => 0;
        int FirstLevel => 0;

        TextureCreateInfo Info { get; }
    }
}
