namespace Ryujinx.Graphics.Gal
{
    public interface IGalTexture
    {
        void Create(long Tag, byte[] Data, GalTexture Texture);

        bool TryGetCachedTexture(long Tag, long DataSize, out GalTexture Texture);

        void Bind(long Tag, int Index);

        void SetSampler(GalTextureSampler Sampler);
    }
}