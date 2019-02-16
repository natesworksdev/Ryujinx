using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Profiler
{
    public class ProfilerSettings
    {
        // Default settings for profiler
        public bool   Enabled         { get; set; } = false;
        public bool   FileDumpEnabled { get; set; } = false;
        public string DumpLocation    { get; set; } = "";
        public float  UpdateRate      { get; set; } = 0.1f;
        public int    MaxLevel        { get; set; } = 0;
        public int    MaxFlags        { get; set; } = 1000;

        // 19531225 = 5 seconds in ticks
        public long   History { get; set; } = 19531225;

        // Controls
        public NpadDebug Controls;
    }
}
