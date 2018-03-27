namespace Ryujinx.Graphics.Gal.Shader
{
    public class GlslDeclInfo
    {
        public string Name { get; private set; }

        public int Index { get; private set; }
        public int Size  { get; private set; }

        public GlslDeclInfo(string Name, int Index, int Size)
        {
            this.Name  = Name;
            this.Index = Index;
            this.Size  = Size;
        }

        public void Enlarge(int NewSize)
        {
            if (Size < NewSize)
            {
                Size = NewSize;
            }
        }
    }
}