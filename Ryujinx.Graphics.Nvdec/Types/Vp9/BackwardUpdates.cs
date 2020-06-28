using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Types.Vp9
{
    struct BackwardUpdates
    {
        public Array7<Array3<Array2<uint>>> InterModeCounts;
        public Array4<Array10<uint>> SbYModeCounts;
        public Array10<Array10<uint>> UvModeCounts;
        public Array16<Array4<uint>> PartitionCounts;
        public Array4<Array3<uint>> SwitchableInterpsCount;
        public Array4<Array2<uint>> IntraInterCount;
        public Array5<Array2<uint>> CompInterCount;
        public Array5<Array2<Array2<uint>>> SingleRefCount;
        public Array5<Array2<uint>> CompRefCount;
        public TxCounts TxCounts;
        public Array3<Array2<uint>> MbSkipCount;
        public NmvContextCounts Mv;
        public Array4<Array2<Array2<Array6<Array6<Array4<uint>>>>>> CoefCounts;
        public Array4<Array2<Array2<Array6<Array6<uint>>>>> EobCounts;

        public BackwardUpdates(ref Vp9BackwardUpdates counts)
        {
            InterModeCounts = new Array7<Array3<Array2<uint>>>();

            for (int i = 0; i < 7; i++)
            {
                InterModeCounts[i][0][0] = counts.inter_mode[i][2];
                InterModeCounts[i][0][1] = counts.inter_mode[i][0] + counts.inter_mode[i][1] + counts.inter_mode[i][3];
                InterModeCounts[i][1][0] = counts.inter_mode[i][0];
                InterModeCounts[i][1][1] = counts.inter_mode[i][1] + counts.inter_mode[i][3];
                InterModeCounts[i][2][0] = counts.inter_mode[i][1];
                InterModeCounts[i][2][1] = counts.inter_mode[i][3];
            }

            SbYModeCounts = counts.y_mode;
            UvModeCounts = counts.uv_mode;
            PartitionCounts = counts.partition;
            SwitchableInterpsCount = counts.switchable_interp;
            IntraInterCount = counts.intra_inter;
            CompInterCount = counts.comp_inter;
            SingleRefCount = counts.single_ref;
            CompRefCount = counts.comp_ref;
            TxCounts = new TxCounts(ref counts.tx);
            MbSkipCount = counts.skip;
            Mv = new NmvContextCounts(ref counts.mv);
            CoefCounts = counts.coef;
            EobCounts = counts.eob_branch;
        }
    }
}
