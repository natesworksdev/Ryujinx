namespace Ryujinx.HLE.HOS.Services.Vi.RootService.ApplicationDisplayService
{
    static class AndroidSurfaceComposerClient
    {
        // NOTE: This is android::SurfaceComposerClient::getDisplayInfo.
#pragma warning disable IDE0060
        public static (ulong, ulong) GetDisplayInfo(ServiceCtx context, ulong displayId = 0)
        {
            // TODO: This need to be REd, it should returns the driver resolution and more.
            if (context.Device.System.State.DockedMode)
            {
                return (1920, 1080);
            }
            else
            {
                return (1280, 720);
            }
        }
#pragma warning restore IDE0060
    }
}
