using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Vp8
{
    unsafe class Surface : ISurface
    {
        public Surface(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public Plane YPlane { get; set; }
        public Plane UPlane { get; set; }
        public Plane VPlane { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }
        public int UvWidth { get; set; }
        public int UvHeight { get; set; }
        public int UvStride { get; set; }

        public void Dispose() { }
    }
}