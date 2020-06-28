using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Types.Vp9
{
    struct TxCounts
    {
        public Array2<Array4<uint>> Tx32x32;
        public Array2<Array3<uint>> Tx16x16;
        public Array2<Array2<uint>> Tx8x8;

        public TxCounts(ref tx_counts counts)
        {
            Tx32x32 = counts.p32x32;
            Tx16x16 = counts.p16x16;
            Tx8x8 = counts.p8x8;
        }
    }
}
