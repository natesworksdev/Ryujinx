namespace Ryujinx.Graphics.Texture.FileFormats
{
    public readonly struct ImageParameters
    {
        public int Width { get; }
        public int Height { get; }
        public int DepthOrLayers { get; }
        public int Levels { get; }
        public ImageFormat Format { get; }
        public ImageDimensions Dimensions { get; }

        public ImageParameters(int width, int height, int depthOrLayers, int levels, ImageFormat format, ImageDimensions dimensions)
        {
            Width = width;
            Height = height;
            DepthOrLayers = depthOrLayers;
            Levels = levels;
            Format = format;
            Dimensions = dimensions;
        }
    }
}
