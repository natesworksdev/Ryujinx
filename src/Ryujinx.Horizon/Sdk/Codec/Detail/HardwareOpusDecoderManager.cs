using Ryujinx.Common;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;

namespace Ryujinx.Horizon.Sdk.Codec.Detail
{
    partial class HardwareOpusDecoderManager : IHardwareOpusDecoderManager
    {
        [CmifCommand(0)]
        public Result OpenHardwareOpusDecoder(
            out IHardwareOpusDecoder decoder,
            HardwareOpusDecoderParameterInternal parameter,
            [CopyHandle] int workBufferHandle,
            int workBufferSize)
        {
            decoder = new HardwareOpusDecoder(parameter.SampleRate, parameter.ChannelsCount, workBufferHandle);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result GetWorkBufferSize(out int size, HardwareOpusDecoderParameterInternal parameter)
        {
            int opusDecoderSize = GetOpusDecoderSize(parameter.ChannelsCount);

            int frameSize = BitUtils.AlignUp(parameter.ChannelsCount * 1920 / (48000 / parameter.SampleRate), 64);
            size = opusDecoderSize + 1536 + frameSize;

            return Result.Success;
        }

        [CmifCommand(2)] // 3.0.0+
        public Result OpenHardwareOpusDecoderForMultiStream(
            out IHardwareOpusDecoder decoder,
            [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer, 0x110)] in HardwareOpusMultiStreamDecoderParameterInternal parameter,
            [CopyHandle] int workBufferHandle,
            int workBufferSize)
        {
            decoder = new HardwareOpusDecoder(
                parameter.SampleRate,
                parameter.ChannelsCount,
                parameter.NumberOfStreams,
                parameter.NumberOfStereoStreams,
                parameter.ChannelMappings.AsSpan().ToArray(),
                workBufferHandle);

            return Result.Success;
        }

        [CmifCommand(3)] // 3.0.0+
        public Result GetWorkBufferSizeForMultiStream(out int size, HardwareOpusMultiStreamDecoderParameterInternal parameter)
        {
            int opusDecoderSize = GetOpusMultistreamDecoderSize(parameter.NumberOfStreams, parameter.NumberOfStereoStreams);

            int streamSize = BitUtils.AlignUp(parameter.NumberOfStreams * 1500, 64);
            int frameSize = BitUtils.AlignUp(parameter.ChannelsCount * 1920 / (48000 / parameter.SampleRate), 64);
            size = opusDecoderSize + streamSize + frameSize;

            return Result.Success;
        }

        [CmifCommand(4)] // 12.0.0+
        public Result OpenHardwareOpusDecoderEx(
            out IHardwareOpusDecoder decoder,
            HardwareOpusDecoderParameterInternalEx parameter,
            [CopyHandle] int workBufferHandle,
            int workBufferSize)
        {
            decoder = new HardwareOpusDecoder(parameter.SampleRate, parameter.ChannelsCount, workBufferHandle);

            return Result.Success;
        }

        [CmifCommand(5)] // 12.0.0+
        public Result GetWorkBufferSizeEx(out int size, HardwareOpusDecoderParameterInternalEx parameter)
        {
            int opusDecoderSize = GetOpusDecoderSize(parameter.ChannelsCount);

            int frameSizeMono48KHz = parameter.Flags.HasFlag(OpusDecoderFlags.LargeFrameSize) ? 5760 : 1920;
            int frameSize = BitUtils.AlignUp(parameter.ChannelsCount * frameSizeMono48KHz / (48000 / parameter.SampleRate), 64);
            size = opusDecoderSize + 1536 + frameSize;

            return Result.Success;
        }

        [CmifCommand(6)] // 12.0.0+
        public Result OpenHardwareOpusDecoderForMultiStreamEx(
            out IHardwareOpusDecoder decoder,
            [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer, 0x118)] in HardwareOpusMultiStreamDecoderParameterInternalEx parameter,
            [CopyHandle] int workBufferHandle,
            int workBufferSize)
        {
            decoder = new HardwareOpusDecoder(
                parameter.SampleRate,
                parameter.ChannelsCount,
                parameter.NumberOfStreams,
                parameter.NumberOfStereoStreams,
                parameter.ChannelMappings.AsSpan().ToArray(),
                workBufferHandle);

            return Result.Success;
        }

        [CmifCommand(7)] // 12.0.0+
        public Result GetWorkBufferSizeForMultiStreamEx(out int size, HardwareOpusMultiStreamDecoderParameterInternalEx parameter)
        {
            int opusDecoderSize = GetOpusMultistreamDecoderSize(parameter.NumberOfStreams, parameter.NumberOfStereoStreams);

            int frameSizeMono48KHz = parameter.Flags.HasFlag(OpusDecoderFlags.LargeFrameSize) ? 5760 : 1920;
            int streamSize = BitUtils.AlignUp(parameter.NumberOfStreams * 1500, 64);
            int frameSize = BitUtils.AlignUp(parameter.ChannelsCount * frameSizeMono48KHz / (48000 / parameter.SampleRate), 64);
            size = opusDecoderSize + streamSize + frameSize;

            return Result.Success;
        }

        [CmifCommand(8)] // 16.0.0+
        public Result GetWorkBufferSizeExEx(out int size, HardwareOpusDecoderParameterInternalEx parameter)
        {
            // NOTE: GetWorkBufferSizeEx uses hardcoded values to compute the returned size.
            //       GetWorkBufferSizeExEx fixes that by using dynamic values.
            //       Since we're already doing that, it's fine to call it directly.

            return GetWorkBufferSizeEx(out size, parameter);
        }

        [CmifCommand(9)] // 16.0.0+
        public Result GetWorkBufferSizeForMultiStreamExEx(out int size, HardwareOpusMultiStreamDecoderParameterInternalEx parameter)
        {
            // NOTE: GetWorkBufferSizeForMultiStreamEx uses hardcoded values to compute the returned size.
            //       GetWorkBufferSizeForMultiStreamExEx fixes that by using dynamic values.
            //       Since we're already doing that, it's fine to call it directly.

            return GetWorkBufferSizeForMultiStreamEx(out size, parameter);
        }

        private static int Align4(int value)
        {
            return BitUtils.AlignUp(value, 4);
        }

        private static int GetOpusDecoderSize(int channelsCount)
        {
            const int SilkDecoderSize = 0x2160;

            if (channelsCount < 1 || channelsCount > 2)
            {
                return 0;
            }

            int celtDecoderSize = GetCeltDecoderSize(channelsCount);
            int opusDecoderSize = GetOpusDecoderAllocSize(channelsCount) | 0x4c;

            return opusDecoderSize + SilkDecoderSize + celtDecoderSize;
        }

        private static int GetOpusMultistreamDecoderSize(int streams, int coupledStreams)
        {
            if (streams < 1 || coupledStreams > streams || coupledStreams < 0)
            {
                return 0;
            }

            int coupledSize = GetOpusDecoderSize(2);
            int monoSize = GetOpusDecoderSize(1);

            return Align4(monoSize - GetOpusDecoderAllocSize(1)) * (streams - coupledStreams) +
                Align4(coupledSize - GetOpusDecoderAllocSize(2)) * coupledStreams + 0xb90c;
        }

        private static int GetOpusDecoderAllocSize(int channelsCount)
        {
            return (channelsCount * 0x800 + 0x4803) & -0x800;
        }

        private static int GetCeltDecoderSize(int channelsCount)
        {
            const int DecodeBufferSize = 0x2030;
            const int Overlap = 120;
            const int EBandsCount = 21;

            return (DecodeBufferSize + Overlap * 4) * channelsCount + EBandsCount * 16 + 0x50;
        }
    }
}
