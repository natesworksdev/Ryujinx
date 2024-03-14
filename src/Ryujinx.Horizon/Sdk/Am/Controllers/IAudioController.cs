using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface IAudioController : IServiceObject
    {
        Result SetExpectedMasterVolume(float mainAppletVolume, float libraryAppletVolume);
        Result GetMainAppletExpectedMasterVolume(out float mainAppletVolume);
        Result GetLibraryAppletExpectedMasterVolume(out float libraryAppletVolume);
        Result ChangeMainAppletMasterVolume(float volume, ulong value);
        Result SetTransparentVolumeRate(float volume);
    }
}
