namespace Ryujinx.Memory.Range
{
    public interface IMultiRangeItem
    {
        MultiRange Range { get; }

        ulong BaseAddress => Range.GetRange(0).Address;
    }
}
