namespace Ryujinx.Graphics.Video
{
    public interface IDecoder
    {
        bool IsHardwareAccelerated { get; }

        ISurface CreateSurface(int width, int height);

        bool ReceiveFrame(ISurface surface);
    }
}
