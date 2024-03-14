using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am
{
    static class AmResult
    {
        private const int Moduleld = 128;

        public static Result NotAvailable => new(Moduleld, 2);
        public static Result NoMessages => new(Moduleld, 3);
        public static Result AppletLaunchFailed => new(Moduleld, 35);
        public static Result TitleIdNotFound => new(Moduleld, 37);
        public static Result ObjectInvalid => new(Moduleld, 500);
        public static Result IStorageInUse => new(Moduleld, 502);
        public static Result OutOfBounds => new(Moduleld, 502);
        public static Result BufferNotAcquired => new(Moduleld, 504);
        public static Result BufferAlreadyAcquired => new(Moduleld, 505);
        public static Result InvalidParameters => new(Moduleld, 506);
        public static Result OpenedAsWrongType => new(Moduleld, 511);
        public static Result UnbalancedFatalSection => new(Moduleld, 512);
        public static Result NullObject => new(Moduleld, 518);
        public static Result MemoryAllocationFailed => new(Moduleld, 600);
        public static Result StackPoolExhausted => new(Moduleld, 712);
        public static Result DebugModeNotEnabled => new(Moduleld, 974);
        public static Result DevFunctionNotEnabled => new(Moduleld, 980);
        public static Result Stubbed => new(Moduleld, 999);
    }
}
