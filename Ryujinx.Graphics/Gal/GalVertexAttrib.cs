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
            Index   = index;
            IsConst = isConst;
            Pointer = pointer;
            Offset  = offset;
            Size    = size;
            Type    = type;
            IsBgra  = isBgra;
        }
    }
}