using Ryujinx.HLE.HOS.Tamper;
using System;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidVibrationValue
    {
        public float AmplitudeLow;
        public float FrequencyLow;
        public float AmplitudeHigh;
        public float FrequencyHigh;

        public override bool Equals(object obj)
        {
            return obj is HidVibrationValue vibrationValue && Equals(vibrationValue);
        }

        public bool Equals(HidVibrationValue other)
        {
            // freq are ignored for now for non-hd rumble
            return AmplitudeLow == other.AmplitudeLow && AmplitudeHigh == other.AmplitudeHigh;
        }
    }
}