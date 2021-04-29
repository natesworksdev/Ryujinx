using Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Common;

namespace Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Npad
{
    struct NpadGcTriggerState : ISampledData
    {
        public ulong SamplingNumber;
        public uint TriggerL;
        public uint TriggerR;

        ulong ISampledData.SamplingNumber => SamplingNumber;
    }
}