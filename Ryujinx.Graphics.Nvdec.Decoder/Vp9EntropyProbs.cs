using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public struct Vp9EntropyProbs
    {
        public Array10<Array10<Array9<byte>>> KfYModeProbs;
        public Array7<byte> seg_tree_probs;
        public Array3<byte> seg_pred_probs;
        public Array10<Array9<byte>> KfUvModeProbs;
        public Array4<Array9<byte>> y_mode_prob;
        public Array10<Array9<byte>> uv_mode_prob;
        public Array16<Array3<byte>> KfPartitionProbs;
        public Array16<Array3<byte>> partition_prob;
        public Array4<Array2<Array2<Array6<Array6<Array3<byte>>>>>> coef_probs;
        public Array4<Array2<byte>> switchable_interp_prob;
        public Array7<Array3<byte>> inter_mode_probs;
        public Array4<byte> intra_inter_prob;
        public Array5<byte> comp_inter_prob;
        public Array5<Array2<byte>> single_ref_prob;
        public Array5<byte> comp_ref_prob;
        public tx_probs tx_probs;
        public Array3<byte> skip_probs;
        public nmv_context nmvc;
    }
}
