namespace Ryujinx.HLE.HOS.Kernel.Common
{
    interface IKFutureSchedulerObject
    {
        long TimePoint { get; set; }

        void TimeUp();
    }
}
