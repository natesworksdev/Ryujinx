namespace Ryujinx.Graphics.GAL
{
    public interface IWindow
    {
        void Present(ITexture texture, in ImageCrop crop);

        void SetSize(int width, int height);
    }
}
