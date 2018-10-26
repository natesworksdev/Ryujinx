namespace Ryujinx.Graphics.Gal
{
    public class ShaderDeclInfo
    {
        public string Name { get; private set; }

        public int  Index { get; private set; }
        public bool IsCb  { get; private set; }
        public int  Cbuf  { get; private set; }
        public int  Size  { get; private set; }

        public ShaderDeclInfo(
            string name,
            int    index,
            bool   isCb = false,
            int    cbuf = 0,
            int    size = 1)
        {
            Name  = name;
            Index = index;
            IsCb  = isCb;
            Cbuf  = cbuf;
            Size  = size;
        }

        internal void Enlarge(int newSize)
        {
            if (Size < newSize)
            {
                Size = newSize;
            }
        }
    }
}