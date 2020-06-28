using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public struct nmv_component
    {
        public byte sign;
        public Array10<byte> classes;
        public Array1<byte> class0;
        public Array10<byte> bits;
        public Array2<Array3<byte>> class0_fp;
        public Array3<byte> fp;
        public byte class0_hp;
        public byte hp;
    }
}
