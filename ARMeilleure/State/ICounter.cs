namespace ARMeilleure.State
{
    public interface ICounter
    {
        ulong Frequency { get; }
        ulong Counter { get; }
    }
}