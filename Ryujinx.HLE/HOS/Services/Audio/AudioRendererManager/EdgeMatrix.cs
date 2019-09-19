using Ryujinx.HLE.Utilities;

namespace Ryujinx.HLE.HOS.Services.Audio.AudioRendererManager
{
    static class EdgeMatrix
    {
        public static int GetWorkBufferSize(int totalMixCount)
        {
            int size = IntUtils.AlignUp(totalMixCount * totalMixCount, AudioRendererConsts.BufferAlignment);

            if (size < 0)
            {
                size |= 7;
            }

            return size / 8;
        }
    }
}