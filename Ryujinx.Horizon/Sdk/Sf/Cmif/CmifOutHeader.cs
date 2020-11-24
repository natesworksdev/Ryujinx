using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Sf.Cmif
{
    struct CmifOutHeader
    {
        public uint Magic;
        public uint Version;
        public Result Result;
        public uint Token;
    }
}
