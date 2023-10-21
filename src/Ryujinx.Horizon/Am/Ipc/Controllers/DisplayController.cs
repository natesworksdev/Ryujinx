using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class DisplayController : IDisplayController
    {
        [CmifCommand(0)]
        public Result GetLastForegroundCaptureImage([Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<byte> capture)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result UpdateLastForegroundCaptureImage([Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<byte> capture)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetLastApplicationCaptureImage([Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<byte> capture)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result GetCallerAppletCaptureImage()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result UpdateCallerAppletCaptureImage()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(5)]
        public Result GetLastForegroundCaptureImageEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(6)]
        public Result GetLastApplicationCaptureImageEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(7)]
        public Result GetCallerAppletCaptureImageEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(8)]
        public Result TakeScreenShotOfOwnLayer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(9)]
        public Result CopyBetweenCaptureBuffers()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result AcquireLastApplicationCaptureBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result ReleaseLastApplicationCaptureBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(12)]
        public Result AcquireLastForegroundCaptureBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(13)]
        public Result ReleaseLastForegroundCaptureBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(14)]
        public Result AcquireCallerAppletCaptureBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(15)]
        public Result ReleaseCallerAppletCaptureBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(16)]
        public Result AcquireLastApplicationCaptureBufferEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(17)]
        public Result AcquireLastForegroundCaptureBufferEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(18)]
        public Result AcquireCallerAppletCaptureBufferEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result ClearCaptureBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(21)]
        public Result ClearAppletTransitionBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(22)]
        public Result AcquireLastApplicationCaptureSharedBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(23)]
        public Result ReleaseLastApplicationCaptureSharedBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(24)]
        public Result AcquireLastForegroundCaptureSharedBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(25)]
        public Result ReleaseLastForegroundCaptureSharedBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(26)]
        public Result AcquireCallerAppletCaptureSharedBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(27)]
        public Result ReleaseCallerAppletCaptureSharedBuffer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(28)]
        public Result TakeScreenShotOfOwnLayerEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
