using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public struct tx_probs
    {
        public Array2<Array3<byte>> p32x32;
        public Array2<Array2<byte>> p16x16;
        public Array2<Array1<byte>> p8x8;
    }
}
