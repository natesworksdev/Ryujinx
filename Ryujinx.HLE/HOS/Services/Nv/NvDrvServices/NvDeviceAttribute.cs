using System;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices
{
    class NvDeviceAttribute : Attribute
    {
        public readonly string Path;

        public NvDeviceAttribute(string path) => Path = path;
    }
}
