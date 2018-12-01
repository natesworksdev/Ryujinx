using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services
{
    internal interface IIpcService
    {
        IReadOnlyDictionary<int, ServiceProcessRequest> Commands { get; }
    }
}