namespace Ryujinx.Graphics.Gal
{
    public interface IGalTexture
    {
        void LockCache();
        void UnlockCache();

        void CreateEmpty(long Key, int Size, GalImage Image);

        void CreateData(long Key, byte[] Data, GalImage Image);

        bool TryGetImage(long Key, out GalImage Image);

        void Bind(long Key, int Index, GalImage Image);

        void SetSampler(GalTextureSampler Sampler);
    }
}