using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public struct nmv_component_counts
    {
        public Array2<uint> sign;
        public Array11<uint> classes;
        public Array2<uint> class0;
        public Array10<Array2<uint>> bits;
        public Array2<Array4<uint>> class0_fp;
        public Array4<uint> fp;
        public Array2<uint> class0_hp;
        public Array2<uint> hp;
    }
}
