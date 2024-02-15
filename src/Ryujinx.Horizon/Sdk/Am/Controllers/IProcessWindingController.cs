using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface IProcessWindingController : IServiceObject
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
