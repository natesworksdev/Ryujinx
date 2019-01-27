using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Profiler
{
    public class ProfilerSettings
    {
        // Default settings for profiler
        public bool   Enabled         = false;
        public bool   FileDumpEnabled = false;
        public string DumpLocation    = "";
        public float  UpdateRate      = 0.1f;
    }
}
