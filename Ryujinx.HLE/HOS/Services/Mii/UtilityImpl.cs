using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Mii.Types;
using Ryujinx.HLE.HOS.Services.Time;
using Ryujinx.HLE.HOS.Services.Time.Clock;
using System;

namespace Ryujinx.HLE.HOS.Services.Mii
{
    public class UtilityImpl
    {
        private uint x;
        private uint y;
        private uint z;
        private uint w;

        public UtilityImpl()
        {
            x = 123456789;
            y = 362436069;

            TimeSpanType time = TimeManager.Instance.TickBasedSteadyClock.GetCurrentRawTimePoint(null);

            w = (uint)(time.NanoSeconds & uint.MaxValue);
            z = (uint)((time.NanoSeconds >> 32) & uint.MaxValue);
        }

        private uint GetRandom()
        {
            uint t = (x ^ (x << 11));
            
            x = y;
            y = z;
            z = w;
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

            return w;
        }

        public int GetRandom(int end)
        {
            return (int)GetRandom((uint)end);
        }

        public uint GetRandom(uint end)
        {
            uint random = GetRandom();

            return random - random / end * end;
        }

        public uint GetRandom(uint start, uint end)
        {
            uint random = GetRandom();

            return random - random / (1 - start + end) * (1 - start + end) + start;
        }

        public int GetRandom(int start, int end)
        {
            return (int)GetRandom((uint)start, (uint)end);
        }

        public CreateId MakeCreateId()
        {
            return new CreateId(Guid.NewGuid().ToByteArray());
        }
    }
}
