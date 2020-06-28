using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public struct Vp9BackwardUpdates
    {
        public Array4<Array10<uint>> y_mode;
        public Array10<Array10<uint>> uv_mode;
        public Array16<Array4<uint>> partition;
        public Array4<Array2<Array2<Array6<Array6<Array4<uint>>>>>> coef; // vp9_coeff_count_model
        public Array4<Array2<Array2<Array6<Array6<uint>>>>> eob_branch;
        public Array4<Array3<uint>> switchable_interp;
        public Array7<Array4<uint>> inter_mode;
        public Array4<Array2<uint>> intra_inter;
        public Array5<Array2<uint>> comp_inter;
        public Array5<Array2<Array2<uint>>> single_ref;
        public Array5<Array2<uint>> comp_ref;
        public tx_counts tx;
        public Array3<Array2<uint>> skip;
        public nmv_context_counts mv;
    }
}
