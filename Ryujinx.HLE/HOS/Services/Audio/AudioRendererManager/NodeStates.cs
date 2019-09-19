using Ryujinx.HLE.Utilities;

namespace Ryujinx.HLE.HOS.Services.Audio.AudioRendererManager
{
    static class NodeStates
    {
        public static long GetWorkBufferSize(int totalMixCount)
        {
            int size = IntUtils.AlignUp(totalMixCount, AudioRendererConsts.BufferAlignment);

            if (size < 0)
            {
                size |= 7;
            }

            return 4 * (totalMixCount * totalMixCount) + 0x12 * totalMixCount + 2 * (size / 8);
        }
    }
}