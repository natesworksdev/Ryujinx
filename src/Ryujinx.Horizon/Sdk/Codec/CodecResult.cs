using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Codec
{
    static class CodecResult
    {
        private const int ModuleId = 153;

        public static Result OpusInvalidInput => new(ModuleId, 6);
    }
}
