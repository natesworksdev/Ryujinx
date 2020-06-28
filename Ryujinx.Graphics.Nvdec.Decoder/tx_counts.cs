using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public struct tx_counts
    {
        public Array2<Array4<uint>> p32x32;
        public Array2<Array3<uint>> p16x16;
        public Array2<Array2<uint>> p8x8;
        public Array4<uint> tx_totals;
    }
}
