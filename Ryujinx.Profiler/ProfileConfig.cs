using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Profiler
{
    public struct ProfileConfig
    {
        public string Name;
    }

    public static class Profiles
    {
        public static class CPU
        {
            public static ProfileConfig Test = new ProfileConfig()
            {
                Name = "CPU.Test",
            };
        }
    }
}
