using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class DisplayController : IDisplayController
    {
        [CmifCommand(0)]
        public Result GetLastForegroundCaptureImage()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(1)]
        public Result UpdateLastForegroundCaptureImage()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(2)]
        public Result GetLastApplicationCaptureImage()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(3)]
        public Result GetCallerAppletCaptureImage()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(4)]
        public Result UpdateCallerAppletCaptureImage()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(5)]
        public Result GetLastForegroundCaptureImageEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(6)]
        public Result GetLastApplicationCaptureImageEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(7)]
        public Result GetCallerAppletCaptureImageEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(8)]
        public Result TakeScreenShotOfOwnLayer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(9)]
        public Result CopyBetweenCaptureBuffers()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(10)]
        public Result AcquireLastApplicationCaptureBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(11)]
        public Result ReleaseLastApplicationCaptureBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(12)]
        public Result AcquireLastForegroundCaptureBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(13)]
        public Result ReleaseLastForegroundCaptureBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(14)]
        public Result AcquireCallerAppletCaptureBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(15)]
        public Result ReleaseCallerAppletCaptureBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(16)]
        public Result AcquireLastApplicationCaptureBufferEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(17)]
        public Result AcquireLastForegroundCaptureBufferEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(18)]
        public Result AcquireCallerAppletCaptureBufferEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(20)]
        public Result ClearCaptureBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(21)]
        public Result ClearAppletTransitionBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(22)]
        public Result AcquireLastApplicationCaptureSharedBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(23)]
        public Result ReleaseLastApplicationCaptureSharedBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(24)]
        public Result AcquireLastForegroundCaptureSharedBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(25)]
        public Result ReleaseLastForegroundCaptureSharedBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(26)]
        public Result AcquireCallerAppletCaptureSharedBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(27)]
        public Result ReleaseCallerAppletCaptureSharedBuffer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(28)]
        public Result TakeScreenShotOfOwnLayerEx()
        {
            throw new System.NotImplementedException();
        }
    }
}
