using Ryujinx.HLE.HOS.Kernel.Threading;
using System;

namespace Ryujinx.HLE.HOS.Services.Audio.AudioRenderer
{
    interface IAudioDevice
    {
        Span<string> ListAudioDeviceName();
        ResultCode SetAudioDeviceOutputVolume(string name, float volume);
        ResultCode GetAudioDeviceOutputVolume(string name,  out float volume);
        string GetActiveAudioDeviceName();
        KEvent QueryAudioDeviceSystemEvent();
        uint GetActiveChannelCount();
        KEvent QueryAudioDeviceInputEvent();
        KEvent QueryAudioDeviceOutputEvent();
        string GetActiveAudioOutputDeviceName();
        Span<string> ListAudioOutputDeviceName();
    }
}
