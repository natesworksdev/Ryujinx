using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    interface ITextureInfo
    {
        ITextureInfo Storage { get; }
        uint Handle { get; }
        uint FirstLayer => 0;
        uint FirstLevel => 0;

        TextureCreateInfo Info { get; }
    }
}
