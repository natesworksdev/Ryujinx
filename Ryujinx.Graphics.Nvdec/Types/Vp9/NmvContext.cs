using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Types.Vp9
{
    struct NmvContext
    {
        public Array3<byte> Joints;
        public NmvComponents Comps;

        public void Convert(ref nmv_context nmvc)
        {
            nmvc.joints = Joints;

            for (int i = 0; i < 2; i++)
            {
                nmvc.comps[i].sign = Comps.Sign[i];
                nmvc.comps[i].class0 = Comps.Class0[i];
                nmvc.comps[i].fp = Comps.Fp[i];
                nmvc.comps[i].class0_hp = Comps.Class0Hp[i];
                nmvc.comps[i].hp = Comps.Hp[i];
                nmvc.comps[i].classes = Comps.Classes[i];
                nmvc.comps[i].class0_fp = Comps.Class0Fp[i];
                nmvc.comps[i].bits = Comps.Bits[i];
            }
        }
    }
}
