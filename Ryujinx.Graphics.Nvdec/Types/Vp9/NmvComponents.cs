using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Nvdec.Types.Vp9
{
    struct NmvComponents
    {
        public Array2<byte> Sign;
        public Array2<Array1<byte>> Class0;
        public Array2<Array3<byte>> Fp;
        public Array2<byte> Class0Hp;
        public Array2<byte> Hp;
        public Array2<Array10<byte>> Classes;
        public Array2<Array2<Array3<byte>>> Class0Fp;
        public Array2<Array10<byte>> Bits;
    }
}
