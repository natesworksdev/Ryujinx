using Ryujinx.Horizon.Common;
using System;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    public interface IDisplayController
    {
        Result GetLastForegroundCaptureImage(Span<byte> capture);
        Result UpdateLastForegroundCaptureImage(Span<byte> capture);
        Result GetLastApplicationCaptureImage(Span<byte> capture);
        Result GetCallerAppletCaptureImage();
        Result UpdateCallerAppletCaptureImage();
        Result GetLastForegroundCaptureImageEx();
        Result GetLastApplicationCaptureImageEx();
        Result GetCallerAppletCaptureImageEx();
        Result TakeScreenShotOfOwnLayer();
        Result CopyBetweenCaptureBuffers();
        Result AcquireLastApplicationCaptureBuffer();
        Result ReleaseLastApplicationCaptureBuffer();
        Result AcquireLastForegroundCaptureBuffer();
        Result ReleaseLastForegroundCaptureBuffer();
        Result AcquireCallerAppletCaptureBuffer();
        Result ReleaseCallerAppletCaptureBuffer();
        Result AcquireLastApplicationCaptureBufferEx();
        Result AcquireLastForegroundCaptureBufferEx();
        Result AcquireCallerAppletCaptureBufferEx();
        Result ClearCaptureBuffer();
        Result ClearAppletTransitionBuffer();
        Result AcquireLastApplicationCaptureSharedBuffer();
        Result ReleaseLastApplicationCaptureSharedBuffer();
        Result AcquireLastForegroundCaptureSharedBuffer();
        Result ReleaseLastForegroundCaptureSharedBuffer();
        Result AcquireCallerAppletCaptureSharedBuffer();
        Result ReleaseCallerAppletCaptureSharedBuffer();
        Result TakeScreenShotOfOwnLayerEx();
    }
}
