using System;

namespace Ryujinx.Memory.Range
{
    /// <summary>
    /// Range of memory.
    /// </summary>
    public interface IRange
    {
        /// <summary>
        /// Base address.
        /// </summary>
        ulong Address { get; }

        /// <summary>
        /// Size of the range.
        /// </summary>
        ulong Size { get; }

        /// <summary>
        /// End address.
        /// </summary>
        ulong EndAddress { get; }

        /// <summary>
        /// Check if this range overlaps with another.
        /// </summary>
        /// <param name="address">Base address</param>
        /// <param name="size">Size of the range</param>
        /// <returns>True if overlapping, false otherwise</returns>
        bool OverlapsWith(ulong address, ulong size);

        int CompareTo(IRange range)
        {
            int cmp = Address.CompareTo(range.Address);
            if(cmp == 0)
            {
                return EndAddress.CompareTo(range.EndAddress);
            }
            return cmp;
        }
    }
}