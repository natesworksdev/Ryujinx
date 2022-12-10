namespace Ryujinx.Audio.Backends.SoundIo.Native
{
    public enum SoundIoBackend : int
    {
        None,
        Jack,
        PulseAudio,
        Alsa,
        CoreAudio,
        Wasapi,
        Dummy
    }
}
