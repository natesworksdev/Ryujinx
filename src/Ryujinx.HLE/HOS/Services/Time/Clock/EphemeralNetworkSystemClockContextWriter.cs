namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    sealed class EphemeralNetworkSystemClockContextWriter : SystemClockContextUpdateCallback
    {
        protected override ResultCode Update()
        {
            return ResultCode.Success;
        }
    }
}
