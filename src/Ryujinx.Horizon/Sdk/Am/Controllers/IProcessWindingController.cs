using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    public interface IProcessWindingController
    {
        Result GetLaunchReason();
        Result OpenCallingLibraryApplet();
        Result PushContext();
        Result PopContext();
        Result CancelWindingReservation();
        Result WindAndDoReserved();
        Result ReserveToStartAndWaitAndUnwindThis();
        Result ReserveToStartAndWait();
    }
}
