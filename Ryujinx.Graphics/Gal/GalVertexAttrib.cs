namespace Ryujinx.Graphics.Gal
{
    public struct GalVertexAttrib
    {
        public bool   IsConst    { get; private set; }
        public int    ArrayIndex { get; private set; }
        public int    Offset     { get; private set; }
        public byte[] Data       { get; private set; }

        public GalVertexAttribSize Size { get; private set; }
        public GalVertexAttribType Type { get; private set; }

        public bool IsBgra { get; private set; }

        public GalVertexAttrib(
            bool                IsConst,
            int                 ArrayIndex,
            int                 Offset,
            byte[]              Data,
            GalVertexAttribSize Size,
            GalVertexAttribType Type,
            bool                IsBgra)
        {
            this.IsConst    = IsConst;
            this.Data       = Data;
            this.ArrayIndex = ArrayIndex;
            this.Offset     = Offset;
            this.Size       = Size;
            this.Type       = Type;
            this.IsBgra     = IsBgra;
        }
    }
}