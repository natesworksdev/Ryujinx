using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Types.Vp9
{
    struct EntropyProbs
    {
        public Array10<Array10<Array8<byte>>> KfYModeProbsE0ToE7;
        public Array10<Array10<byte>> KfYModeProbsE8;
        public Array3<byte> Padding384;
        public Array7<byte> SegTreeProbs;
        public Array3<byte> SegPredProbs;
        public Array15<byte> Padding391;
        public Array10<Array8<byte>> KfUvModeProbsE0ToE7;
        public Array10<byte> KfUvModeProbsE8;
        public Array6<byte> Padding3FA;
        public Array7<Array4<byte>> InterModeProbs;
        public Array4<byte> IntraInterProbs;
        public Array10<Array8<byte>> UvModeProbsE0ToE7;
        public TxProbs TxProbs;
        public Array4<byte> YModeProbsE8;
        public Array4<Array8<byte>> YModeProbsE0ToE7;
        public Array16<Array4<byte>> KfPartitionProbs;
        public Array16<Array4<byte>> PartitionProbs;
        public Array10<byte> UvModeProbsE8;
        public Array4<Array2<byte>> SwitchableInterpProbs;
        public Array5<byte> CompInterProbs;
        public Array4<byte> SkipProbs;
        public NmvContext Nmvc;
        public Array5<Array2<byte>> SingleRefProbs;
        public Array5<byte> CompRefProbs;
        public Array17<byte> Padding58F;
        public Array4<Array2<Array2<Array6<Array6<Array4<byte>>>>>> CoefProbs;

        public void Convert(ref Vp9EntropyProbs fc)
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    for (int k = 0; k < 9; k++)
                    {
                        fc.KfYModeProbs[i][j][k] = k < 8 ? KfYModeProbsE0ToE7[i][j][k] : KfYModeProbsE8[i][j];
                    }
                }
            }

            fc.seg_tree_probs = SegTreeProbs;
            fc.seg_pred_probs = SegPredProbs;

            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    fc.inter_mode_probs[i][j] = InterModeProbs[i][j];
                }
            }

            fc.intra_inter_prob = IntraInterProbs;
            
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    fc.KfUvModeProbs[i][j] = j < 8 ? KfUvModeProbsE0ToE7[i][j] : KfUvModeProbsE8[i];
                    fc.uv_mode_prob[i][j] = j < 8 ? UvModeProbsE0ToE7[i][j] : UvModeProbsE8[i];
                }
            }

            TxProbs.Convert(ref fc.tx_probs);

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    fc.y_mode_prob[i][j] = j < 8 ? YModeProbsE0ToE7[i][j] : YModeProbsE8[i];
                }
            }

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    fc.KfPartitionProbs[i][j] = KfPartitionProbs[i][j];
                    fc.partition_prob[i][j] = PartitionProbs[i][j];
                }
            }

            fc.switchable_interp_prob = SwitchableInterpProbs;
            fc.comp_inter_prob = CompInterProbs;
            fc.skip_probs[0] = SkipProbs[0];
            fc.skip_probs[1] = SkipProbs[1];
            fc.skip_probs[2] = SkipProbs[2];

            Nmvc.Convert(ref fc.nmvc);

            fc.single_ref_prob = SingleRefProbs;
            fc.comp_ref_prob = CompRefProbs;

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        for (int l = 0; l < 6; l++)
                        {
                            for (int m = 0; m < 6; m++)
                            {
                                for (int n = 0; n < 3; n++)
                                {
                                    fc.coef_probs[i][j][k][l][m][n] = CoefProbs[i][j][k][l][m][n];
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
