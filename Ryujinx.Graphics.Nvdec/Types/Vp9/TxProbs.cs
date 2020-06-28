using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Types.Vp9
{
    struct TxProbs
    {
        public Array2<Array1<byte>> P8x8;
        public Array2<Array2<byte>> P16x16;
        public Array2<Array3<byte>> P32x32;

        public void Convert(ref tx_probs probs)
        {
            probs.p8x8 = P8x8;
            probs.p16x16 = P16x16;
            probs.p32x32 = P32x32;
        }
    }
}
