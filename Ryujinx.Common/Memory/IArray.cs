namespace Ryujinx.Common.Memory
{
    public interface IArray<T> where T : unmanaged
    {
        ref T this[int index] { get; }
        int Length { get; }
    }
}
