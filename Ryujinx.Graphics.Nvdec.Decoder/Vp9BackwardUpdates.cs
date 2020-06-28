using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public struct Vp9BackwardUpdates
    {
        public Array4<Array10<uint>> y_mode;
        public Array10<Array10<uint>> uv_mode;
        public Array16<Array4<uint>> partition;
        public Array4<Array2<Array2<Array6<Array6<Array4<uint>>>>>> coef;
        public Array4<Array2<Array2<Array6<Array6<uint>>>>> eob_branch;
        public Array4<Array3<uint>> switchable_interp;
        public Array7<Array4<uint>> inter_mode;
        public Array4<Array2<uint>> intra_inter;
        public Array5<Array2<uint>> comp_inter;
        public Array5<Array2<Array2<uint>>> single_ref;
        public Array5<Array2<uint>> comp_ref;
        public Array2<Array4<uint>> p32x32;
        public Array2<Array3<uint>> p16x16;
        public Array2<Array2<uint>> p8x8;
        public Array4<uint> tx_totals;
        public Array3<Array2<uint>> skip;
        public Array4<uint> joints;
        public Array2<Array2<uint>> sign;
        public Array2<Array11<uint>> classes;
        public Array2<Array2<uint>> class0;
        public Array2<Array10<Array2<uint>>> bits;
        public Array2<Array2<Array4<uint>>> class0_fp;
        public Array2<Array4<uint>> fp;
        public Array2<Array2<uint>> class0_hp;
        public Array2<Array2<uint>> hp;
    }
}
