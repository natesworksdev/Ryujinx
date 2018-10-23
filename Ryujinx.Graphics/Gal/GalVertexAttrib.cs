using System;

namespace Ryujinx.Graphics.Gal
{
    public struct GalVertexAttrib
    {
        public int    Index   { get; private set; }
        public bool   IsConst { get; private set; }
        public int    Offset  { get; private set; }
        public IntPtr Pointer { get; private set; }

        public GalVertexAttribSize Size { get; private set; }
        public GalVertexAttribType Type { get; private set; }

        public bool IsBgra { get; private set; }

        public GalVertexAttrib(
            int                 index,
            bool                isConst,
            int                 offset,
            IntPtr              pointer,
            GalVertexAttribSize size,
            GalVertexAttribType type,
            bool                isBgra)
        {
            this.Index   = index;
            this.IsConst = isConst;
            this.Pointer = pointer;
            this.Offset  = offset;
            this.Size    = size;
            this.Type    = type;
            this.IsBgra  = isBgra;
        }
    }
}