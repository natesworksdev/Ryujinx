using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface ILibraryAppletSelfAccessor : IServiceObject
    {
        Result PopInData();
        Result PushOutData();
        Result PopInteractiveInData();
        Result PushInteractiveOutData();
        Result GetPopInDataEvent();
        Result GetPopInteractiveInDataEvent();
        Result ExitProcessAndReturn();
        Result GetLibraryAppletInfo();
        Result GetMainAppletIdentityInfo();
        Result CanUseApplicationCore();
        Result GetCallerAppletIdentityInfo();
        Result GetMainAppletApplicationControlProperty();
        Result GetMainAppletStorageId();
        Result GetCallerAppletIdentityInfoStack();
        Result GetNextReturnDestinationAppletIdentityInfo();
        Result GetDesirableKeyboardLayout();
        Result PopExtraStorage();
        Result GetPopExtraStorageEvent();
        Result UnpopInData();
        Result UnpopExtraStorage();
        Result GetIndirectLayerProducerHandle();
        Result ReportVisibleError();
        Result ReportVisibleErrorWithErrorContext();
        Result GetMainAppletApplicationDesiredLanguage();
        Result GetCurrentApplicationId();
        Result RequestExitToSelf();
        Result CreateApplicationAndPushAndRequestToLaunch();
        Result CreateGameMovieTrimmer();
        Result ReserveResourceForMovieOperation();
        Result UnreserveResourceForMovieOperation();
        Result GetMainAppletAvailableUsers();
        Result GetLaunchStorageInfoForDebug();
        Result GetGpuErrorDetectedSystemEvent();
        Result SetApplicationMemoryReservation();
        Result ShouldSetGpuTimeSliceManually();
        // 160 (17.0.0+) Unknown Function
    }
}
