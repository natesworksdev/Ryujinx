using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Common;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Npad
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SixAxisSensorState : ISampledDataStruct
    {
        // MUST BE THE 1st MEMBER
        public ulong SamplingNumber;

        public ulong DeltaTime;
        public HidVector Acceleration;
        public HidVector AngularVelocity;
        public HidVector Angle;
        public Array9<float> Direction;
        public SixAxisSensorAttribute Attributes;
        private uint _reserved;
    }
}