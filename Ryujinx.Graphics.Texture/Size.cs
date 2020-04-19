namespace Ryujinx.Graphics.Texture
{
    public readonly struct Size
    {
        public readonly int Width  { get; }
        public readonly int Height { get; }
        public readonly int Depth  { get; }

        public Size(int width, int height, int depth)
        {
            Width  = width;
            Height = height;
            Depth  = depth;
        }
    }
}