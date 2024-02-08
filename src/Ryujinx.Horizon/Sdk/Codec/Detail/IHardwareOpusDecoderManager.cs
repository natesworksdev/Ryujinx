using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Codec.Detail
{
    interface IHardwareOpusDecoderManager : IServiceObject
    {
        Result OpenHardwareOpusDecoder(out IHardwareOpusDecoder arg0, HardwareOpusDecoderParameterInternal arg1, int arg2, int arg3);
        Result GetWorkBufferSize(out int arg0, HardwareOpusDecoderParameterInternal arg1);
        Result OpenHardwareOpusDecoderForMultiStream(out IHardwareOpusDecoder arg0, in HardwareOpusMultiStreamDecoderParameterInternal arg1, int arg2, int arg3);
        Result GetWorkBufferSizeForMultiStream(out int arg0, HardwareOpusMultiStreamDecoderParameterInternal arg1);
        Result OpenHardwareOpusDecoderEx(out IHardwareOpusDecoder arg0, HardwareOpusDecoderParameterInternalEx arg1, int arg2, int arg3);
        Result GetWorkBufferSizeEx(out int arg0, HardwareOpusDecoderParameterInternalEx arg1);
        Result OpenHardwareOpusDecoderForMultiStreamEx(out IHardwareOpusDecoder arg0, in HardwareOpusMultiStreamDecoderParameterInternalEx arg1, int arg2, int arg3);
        Result GetWorkBufferSizeForMultiStreamEx(out int arg0, HardwareOpusMultiStreamDecoderParameterInternalEx arg1);
        Result GetWorkBufferSizeExEx(out int arg0, HardwareOpusDecoderParameterInternalEx arg1);
        Result GetWorkBufferSizeForMultiStreamExEx(out int arg0, HardwareOpusMultiStreamDecoderParameterInternalEx arg1);
    }
}
