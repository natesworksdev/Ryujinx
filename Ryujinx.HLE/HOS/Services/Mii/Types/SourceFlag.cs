using System;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [Flags]
    public enum SourceFlag : int
    {
        Database = 1 << Source.Database,
        Default  = 1 << Source.Default,
        All      = Database | Default
    }
}
