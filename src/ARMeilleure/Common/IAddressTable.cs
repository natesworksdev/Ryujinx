using System;

namespace ARMeilleure.Common
{
    public interface IAddressTable<TEntry> : IDisposable where TEntry : unmanaged
    {
        /// <summary>
        /// If true, the sparse 2-level table should be used to improve performance.
        /// If false, the platform doesn't properly support it, or will be negatively impacted.
        /// </summary>
        static bool UseSparseTable { get; }

        /// <summary>
        /// Gets the bits used by the <see cref="Levels"/> of the <see cref="AddressTable{TEntry}"/> instance.
        /// </summary>
        ulong Mask { get; }

        /// <summary>
        /// Gets the <see cref="Level"/>s used by the <see cref="AddressTable{TEntry}"/> instance.
        /// </summary>
        AddressTableLevel[] Levels { get; }

        /// <summary>
        /// Gets or sets the default fill value of newly created leaf pages.
        /// </summary>
        TEntry Fill { get; set; }

        /// <summary>
        /// Gets the base address of the <see cref="EntryTable{TEntry}"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="EntryTable{TEntry}"/> instance was disposed</exception>
        IntPtr Base { get; }

        /// <summary>
        /// Determines if the specified <paramref name="address"/> is in the range of the
        /// <see cref="AddressTable{TEntry}"/>.
        /// </summary>
        /// <param name="address">Guest address</param>
        /// <returns><see langword="true"/> if is valid; otherwise <see langword="false"/></returns>
        bool IsValid(ulong address);

        /// <summary>
        /// Gets a reference to the value at the specified guest <paramref name="address"/>.
        /// </summary>
        /// <param name="address">Guest address</param>
        /// <returns>Reference to the value at the specified guest <paramref name="address"/></returns>
        /// <exception cref="ObjectDisposedException"><see cref="EntryTable{TEntry}"/> instance was disposed</exception>
        /// <exception cref="ArgumentException"><paramref name="address"/> is not mapped</exception>
        ref TEntry GetValue(ulong address);
    }
}
