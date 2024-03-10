using Ryujinx.Common.Memory;
using Ryujinx.Horizon.Sdk.Hid.SharedMemory.Common;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Hid.SharedMemory.Npad
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SixAxisSensorState : ISampledDataStruct
    {
        public ulong DeltaTime;
        public ulong SamplingNumber;
        public HidVector Acceleration;
        public HidVector AngularVelocity;
        public HidVector Angle;
        public Array9<float> Direction;
        public SixAxisSensorAttribute Attributes;
        private readonly uint _reserved;
    }
}
