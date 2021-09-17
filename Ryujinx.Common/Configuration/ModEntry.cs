using System.Collections.Generic;

namespace Ryujinx.Common.Configuration
{
    public struct ModEntry
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public ModEntry(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }
}