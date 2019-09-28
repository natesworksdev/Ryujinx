using System;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.HOS.Services.Time.Clock;
using Ryujinx.HLE.Utilities;

namespace Ryujinx.HLE.HOS.Services.Time
{
    class TimeSharedMemory
    {
        private Switch        _device;
        private KSharedMemory _sharedMemory;
        private long          _timeSharedMemoryAddress;
        private int           _timeSharedMemorySize;

        public void Initialize(Switch device, KSharedMemory sharedMemory, long timeSharedMemoryAddress, int timeSharedMemorySize)
        {
            _device                  = device;
            _sharedMemory            = sharedMemory;
            _timeSharedMemoryAddress = timeSharedMemoryAddress;
            _timeSharedMemorySize    = timeSharedMemorySize;

            // Clean the shared memory
            _device.Memory.FillWithZeros(_timeSharedMemoryAddress, _timeSharedMemorySize);
        }

        public KSharedMemory GetSharedMemory()
        {
            return _sharedMemory;
        }

        public void SetupStandardSteadyClock(UInt128 clockSourceId, TimeSpanType currentTimePoint)
        {
            // TODO
        }

        public void SetAutomaticCorrectionEnabled(bool isAutomaticCorrectionEnabled)
        {
            // TODO
        }

        public void SetSteadyClockRawTimePoint(TimeSpanType currentTimePoint)
        {
            // TODO
        }
    }
}
