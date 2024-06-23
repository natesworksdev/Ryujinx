using ARMeilleure.Diagnostics;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ARMeilleure.Common
{
    /// <summary>
    /// Represents a table of guest address to a value.
    /// </summary>
    /// <typeparam name="TEntry">Type of the value</typeparam>
    public unsafe class AddressTable<TEntry> : IDisposable where TEntry : unmanaged
    {
        /// <summary>
        /// If true, the sparse 2-level table should be used to improve performance.
        /// If false, the platform doesn't properly support it, or will be negatively impacted.
        /// </summary>
        public static bool UseSparseTable => true;

        /// <summary>
        /// Represents a level in an <see cref="AddressTable{TEntry}"/>.
        /// </summary>
        public readonly struct Level
        {
            /// <summary>
            /// Gets the index of the <see cref="Level"/> in the guest address.
            /// </summary>
            public int Index { get; }

            /// <summary>
            /// Gets the length of the <see cref="Level"/> in the guest address.
            /// </summary>
            public int Length { get; }

            /// <summary>
            /// Gets the mask which masks the bits used by the <see cref="Level"/>.
            /// </summary>
            public ulong Mask => ((1ul << Length) - 1) << Index;

            /// <summary>
            /// Initializes a new instance of the <see cref="Level"/> structure with the specified
            /// <paramref name="index"/> and <paramref name="length"/>.
            /// </summary>
            /// <param name="index">Index of the <see cref="Level"/></param>
            /// <param name="length">Length of the <see cref="Level"/></param>
            public Level(int index, int length)
            {
                (Index, Length) = (index, length);
            }

            /// <summary>
            /// Gets the value of the <see cref="Level"/> from the specified guest <paramref name="address"/>.
            /// </summary>
            /// <param name="address">Guest address</param>
            /// <returns>Value of the <see cref="Level"/> from the specified guest <paramref name="address"/></returns>
            public int GetValue(ulong address)
            {
                return (int)((address & Mask) >> Index);
            }
        }

        private readonly struct AddressTablePage
        {
            public readonly bool IsSparse;
            public readonly IntPtr Address;

            public AddressTablePage(bool isSparse, IntPtr address)
            {
                IsSparse = isSparse;
                Address = address;
            }
        }

        private bool _disposed;
        private TEntry** _table;
        private readonly List<AddressTablePage> _pages;
        private TEntry _fill;

        private readonly bool _sparse;
        private readonly MemoryBlock _sparseFill;
        private readonly SparseMemoryBlock _fillBottomLevel;
        private readonly TEntry* _fillBottomLevelPtr;

        private readonly List<SparseMemoryBlock> _sparseReserved;
        private readonly ulong _sparseBlockSize;
        private readonly ReaderWriterLockSlim _sparseLock;
        private ulong _sparseReservedOffset;

        /// <summary>
        /// Gets the bits used by the <see cref="Levels"/> of the <see cref="AddressTable{TEntry}"/> instance.
        /// </summary>
        public ulong Mask { get; }

        /// <summary>
        /// Gets the <see cref="Level"/>s used by the <see cref="AddressTable{TEntry}"/> instance.
        /// </summary>
        public Level[] Levels { get; }

        /// <summary>
        /// Gets or sets the default fill value of newly created leaf pages.
        /// </summary>
        public TEntry Fill
        {
            get
            {
                return _fill;
            }
            set
            {
                UpdateFill(value);
            }
        }

        /// <summary>
        /// Gets the base address of the <see cref="EntryTable{TEntry}"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="EntryTable{TEntry}"/> instance was disposed</exception>
        public IntPtr Base
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                lock (_pages)
                {
                    return (IntPtr)GetRootPage();
                }
            }
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="AddressTable{TEntry}"/> class with the specified list of
        /// <see cref="Level"/>.
        /// </summary>
        /// <param name="levels">Levels for the address table</param>
        /// <param name="sparse">True if the bottom page should be sparsely mapped</param>
        /// <exception cref="ArgumentNullException"><paramref name="levels"/> is null</exception>
        /// <exception cref="ArgumentException">Length of <paramref name="levels"/> is less than 2</exception>
        public AddressTable(Level[] levels, bool sparse)
        {
            ArgumentNullException.ThrowIfNull(levels);

            if (levels.Length < 2)
            {
                throw new ArgumentException("Table must be at least 2 levels deep.", nameof(levels));
            }

            _pages = new List<AddressTablePage>(capacity: 16);

            Levels = levels;
            Mask = 0;

            foreach (var level in Levels)
            {
                Mask |= level.Mask;
            }

            _sparse = sparse;

            if (sparse)
            {
                // If the address table is sparse, allocate a fill block

                _sparseFill = new MemoryBlock(65536, MemoryAllocationFlags.Mirrorable);

                ulong bottomLevelSize = (1ul << levels.Last().Length) * (ulong)sizeof(TEntry);

                _fillBottomLevel = new SparseMemoryBlock(bottomLevelSize, null, _sparseFill);
                _fillBottomLevelPtr = (TEntry*)_fillBottomLevel.Block.Pointer;

                _sparseReserved = new List<SparseMemoryBlock>();
                _sparseLock = new ReaderWriterLockSlim();

                _sparseBlockSize = bottomLevelSize << 3;
            }
        }

        private void UpdateFill(TEntry fillValue)
        {
            if (_sparseFill != null)
            {
                Span<byte> span = _sparseFill.GetSpan(0, (int)_sparseFill.Size);
                MemoryMarshal.Cast<byte, TEntry>(span).Fill(fillValue);
            }

            _fill = fillValue;
        }

        /// <summary>
        /// Determines if the specified <paramref name="address"/> is in the range of the
        /// <see cref="AddressTable{TEntry}"/>.
        /// </summary>
        /// <param name="address">Guest address</param>
        /// <returns><see langword="true"/> if is valid; otherwise <see langword="false"/></returns>
        public bool IsValid(ulong address)
        {
            return (address & ~Mask) == 0;
        }

        /// <summary>
        /// Gets a reference to the value at the specified guest <paramref name="address"/>.
        /// </summary>
        /// <param name="address">Guest address</param>
        /// <returns>Reference to the value at the specified guest <paramref name="address"/></returns>
        /// <exception cref="ObjectDisposedException"><see cref="EntryTable{TEntry}"/> instance was disposed</exception>
        /// <exception cref="ArgumentException"><paramref name="address"/> is not mapped</exception>
        public ref TEntry GetValue(ulong address)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!IsValid(address))
            {
                throw new ArgumentException($"Address 0x{address:X} is not mapped onto the table.", nameof(address));
            }

            lock (_pages)
            {
                TEntry* page = GetPage(address);

                int index = Levels[^1].GetValue(address);

                EnsureMapped((IntPtr)(page + index));

                return ref page[index];
            }
        }

        /// <summary>
        /// Gets the leaf page for the specified guest <paramref name="address"/>.
        /// </summary>
        /// <param name="address">Guest address</param>
        /// <returns>Leaf page for the specified guest <paramref name="address"/></returns>
        private TEntry* GetPage(ulong address)
        {
            TEntry** page = GetRootPage();

            for (int i = 0; i < Levels.Length - 1; i++)
            {
                ref Level level = ref Levels[i];
                ref TEntry* nextPage = ref page[level.GetValue(address)];

                if (nextPage == null || nextPage == _fillBottomLevelPtr)
                {
                    ref Level nextLevel = ref Levels[i + 1];

                    if (i == Levels.Length - 2)
                    {
                        nextPage = (TEntry*)Allocate(1 << nextLevel.Length, Fill, leaf: true);
                    }
                    else
                    {
                        nextPage = (TEntry*)Allocate(1 << nextLevel.Length, GetFillValue(i), leaf: false);
                    }
                }

                page = (TEntry**)nextPage;
            }

            return (TEntry*)page;
        }

        private void EnsureMapped(IntPtr ptr)
        {
            if (_sparse)
            {
                // Check sparse allocations to see if the pointer is in any of them.
                // Ensure the page is committed if there's a match.

                _sparseLock.EnterReadLock();

                try
                {
                    foreach (SparseMemoryBlock sparse in _sparseReserved)
                    {
                        if (ptr >= sparse.Block.Pointer && ptr < sparse.Block.Pointer + (IntPtr)sparse.Block.Size)
                        {
                            sparse.EnsureMapped((ulong)(ptr - sparse.Block.Pointer));

                            break;
                        }
                    }
                }
                finally
                {
                    _sparseLock.ExitReadLock();
                }
            }
        }

        private IntPtr GetFillValue(int level)
        {
            if (_fillBottomLevel != null && level == Levels.Length - 2)
            {
                return (IntPtr)_fillBottomLevelPtr;
            }
            else
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Lazily initialize and get the root page of the <see cref="AddressTable{TEntry}"/>.
        /// </summary>
        /// <returns>Root page of the <see cref="AddressTable{TEntry}"/></returns>
        private TEntry** GetRootPage()
        {
            if (_table == null)
            {
                _table = (TEntry**)Allocate(1 << Levels[0].Length, GetFillValue(0), leaf: false);
            }

            return _table;
        }

        private void InitLeafPage(Span<byte> page)
        {
            MemoryMarshal.Cast<byte, TEntry>(page).Fill(_fill);
        }

        /// <summary>
        /// Allocates a block of memory of the specified type and length.
        /// </summary>
        /// <typeparam name="T">Type of elements</typeparam>
        /// <param name="length">Number of elements</param>
        /// <param name="fill">Fill value</param>
        /// <param name="leaf"><see langword="true"/> if leaf; otherwise <see langword="false"/></param>
        /// <returns>Allocated block</returns>
        private IntPtr Allocate<T>(int length, T fill, bool leaf) where T : unmanaged
        {
            var size = sizeof(T) * length;

            AddressTablePage page;

            if (_sparse && leaf)
            {
                _sparseLock.EnterWriteLock();

                if (_sparseReserved.Count == 0 || _sparseReservedOffset == _sparseBlockSize)
                {
                    _sparseReserved.Add(new SparseMemoryBlock(_sparseBlockSize, InitLeafPage, _sparseFill));

                    _sparseReservedOffset = 0;
                }

                SparseMemoryBlock block = _sparseReserved.Last();

                page = new AddressTablePage(true, block.Block.Pointer + (IntPtr)_sparseReservedOffset);

                _sparseReservedOffset += (ulong)size;

                _sparseLock.ExitWriteLock();
            }
            else
            {
                var address = (IntPtr)NativeAllocator.Instance.Allocate((uint)size);
                page = new AddressTablePage(false, address);

                var span = new Span<T>((void*)page.Address, length);
                span.Fill(fill);
            }

            _pages.Add(page);

            TranslatorEventSource.Log.AddressTableAllocated(size, leaf);

            return page.Address;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="AddressTable{TEntry}"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all unmanaged and optionally managed resources used by the <see cref="AddressTable{TEntry}"/>
        /// instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to dispose managed resources also; otherwise just unmanaged resouces</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                foreach (var page in _pages)
                {
                    if (!page.IsSparse)
                    {
                        Marshal.FreeHGlobal(page.Address);
                    }
                }

                if (_sparse)
                {
                    foreach (SparseMemoryBlock block in _sparseReserved)
                    {
                        block.Dispose();
                    }

                    _fillBottomLevel.Dispose();
                    _sparseFill.Dispose();
                    _sparseLock.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Frees resources used by the <see cref="AddressTable{TEntry}"/> instance.
        /// </summary>
        ~AddressTable()
        {
            Dispose(false);
        }
    }
}
