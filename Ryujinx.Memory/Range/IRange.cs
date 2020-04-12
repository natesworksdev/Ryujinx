namespace Ryujinx.Memory.Range
{
    /// <summary>
    /// Range of memory.
    /// </summary>
    interface IRange
    {
        ulong Address { get; }
        ulong Size { get; }
        ulong EndAddress { get; }

        /// <summary>
        /// Check if this range overlaps with another.
        /// </summary>
        /// <param name="address">Base address</param>
        /// <param name="size">Size of the range</param>
        /// <returns>True if overlapping, false otherwise</returns>
        bool OverlapsWith(ulong address, ulong size);
    }
}