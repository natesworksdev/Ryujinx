using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Types.Vp9
{
    struct NmvComponentCounts
    {
        public Array2<Array2<uint>> Sign;
        public Array2<Array11<uint>> Classes;
        public Array2<Array2<uint>> Class0;
        public Array2<Array10<Array2<uint>>> Bits;
        public Array2<Array2<Array4<uint>>> Class0Fp;
        public Array2<Array4<uint>> Fp;
        public Array2<Array2<uint>> Class0Hp;
        public Array2<Array2<uint>> Hp;

        public NmvComponentCounts(ref Array2<nmv_component_counts> counts)
        {
            Sign = new Array2<Array2<uint>>();
            Classes = new Array2<Array11<uint>>();
            Class0 = new Array2<Array2<uint>>();
            Bits = new Array2<Array10<Array2<uint>>>();
            Class0Fp = new Array2<Array2<Array4<uint>>>();
            Fp = new Array2<Array4<uint>>();
            Class0Hp = new Array2<Array2<uint>>();
            Hp = new Array2<Array2<uint>>();

            for (int i = 0; i < 2; i++)
            {
                Sign[i] = counts[i].sign;
                Classes[i] = counts[i].classes;
                Class0[i] = counts[i].class0;
                Bits[i] = counts[i].bits;
                Class0Fp[i] = counts[i].class0_fp;
                Fp[i] = counts[i].fp;
                Class0Hp[i] = counts[i].class0_hp;
                Hp[i] = counts[i].hp;
            }
        }
    }
}
