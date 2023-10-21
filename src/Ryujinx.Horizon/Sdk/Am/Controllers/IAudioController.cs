using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    public interface IAudioController
    {
        Result SetExpectedMasterVolume(float mainAppletVolume, float libraryAppletVolume);
        Result GetMainAppletExpectedMasterVolume(out float mainAppletVolume);
        Result GetLibraryAppletExpectedMasterVolume(out float libraryAppletVolume);
        Result ChangeMainAppletMasterVolume(float volume, ulong value);
        Result SetTransparentVolumeRate(float volume);
    }
}
