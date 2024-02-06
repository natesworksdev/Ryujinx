using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Applet;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Audio.Detail
{
    interface IAudioRendererManager : IServiceObject
    {
        Result OpenAudioRenderer(
            out IAudioRenderer renderer,
            AudioRendererParameterInternal parameter,
            int processHandle,
            int workBufferHandle,
            ulong workBufferSize,
            AppletResourceUserId appletUserId,
            ulong pid);
        Result GetWorkBufferSize(out long workBufferSize, AudioRendererParameterInternal parameter);
        Result GetAudioDeviceService(out IAudioDevice audioDevice, AppletResourceUserId appletUserId);
        Result OpenAudioRendererForManualExecution(
            out IAudioRenderer renderer,
            AudioRendererParameterInternal parameter,
            ulong arg2,
            int arg3,
            ulong arg4,
            AppletResourceUserId appletUserId,
            ulong pid);
        Result GetAudioDeviceServiceWithRevisionInfo(out IAudioDevice audioDevice, AppletResourceUserId appletUserId, uint revision);
    }
}
