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

        public int CompareTo(IRange obj)
        {
            if (this.Address < obj.Address) return -1;
            else if (this.Address == obj.Address)
            { 
                return this.EndAddress <= obj.EndAddress ? -1 : 1;
            }
            return 1;
        }
    }
}