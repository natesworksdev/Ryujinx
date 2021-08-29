namespace Ryujinx.Graphics.Texture
{
    public struct Region
    {
        public int Offset;
        public int Size;

        public Region(int offset, int size)
        {
            Offset = offset;
            Size = size;
        }
    }
}
