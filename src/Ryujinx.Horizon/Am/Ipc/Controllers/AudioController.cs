using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class AudioController : IAudioController
    {
        private float _mainAppletVolume;
        private float _libraryAppletVolume;

        [CmifCommand(0)]
        public Result SetExpectedMasterVolume(float mainAppletVolume, float libraryAppletVolume)
        {
            _mainAppletVolume = mainAppletVolume;
            _libraryAppletVolume = libraryAppletVolume;

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result GetMainAppletExpectedMasterVolume(out float mainAppletVolume)
        {
            mainAppletVolume = _mainAppletVolume;

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetLibraryAppletExpectedMasterVolume(out float libraryAppletVolume)
        {
            libraryAppletVolume = _libraryAppletVolume;

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result ChangeMainAppletMasterVolume(float volume, ulong value)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result SetTransparentVolumeRate(float volume)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
