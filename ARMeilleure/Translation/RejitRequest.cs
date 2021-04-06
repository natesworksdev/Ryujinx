using ARMeilleure.State;

namespace ARMeilleure.Translation
{
    readonly struct RejitRequest
    {
        public readonly ulong Address;
        public readonly ExecutionMode Mode;

        public RejitRequest(ulong address, ExecutionMode mode)
        {
            Address = address;
            Mode = mode;
        }
    }
}
