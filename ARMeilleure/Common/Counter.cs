namespace ARMeilleure.Common
{
    /// <summary>
    /// Represents an 8-bit counter.
    /// </summary>
    class Counter
    {
        private readonly int _index;
        private readonly EntryTable<byte> _countTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter"/> class from the specified
        /// <see cref="EntryTable{byte}"/> instance and index.
        /// </summary>
        /// <param name="countTable"><see cref="EntryTable{byte}"/> instance</param>
        /// <param name="index">Index in the <see cref="EntryTable{TEntry}"/></param>
        private Counter(EntryTable<byte> countTable, int index)
        {
            _countTable = countTable;
            _index = index;
        }

        /// <summary>
        /// Gets a reference to the value of the counter.
        /// </summary>
        public ref byte Value => ref _countTable.GetValue(_index);

        /// <summary>
        /// Tries to create a <see cref="Counter"/> instance from the specified <see cref="EntryTable{byte}"/> instance.
        /// </summary>
        /// <param name="countTable"><see cref="EntryTable{TEntry}"/> from which to create the <see cref="Counter"/></param>
        /// <param name="counter"><see cref="Counter"/> instance if success; otherwise <see langword="null"/></param>
        /// <returns><see langword="true"/> if success; otherwise <see langword="false"/></returns>
        public static bool TryCreate(EntryTable<byte> countTable, out Counter counter)
        {
            if (countTable.TryAllocate(out int index))
            {
                counter = new Counter(countTable, index);

                return true;
            }

            counter = null;

            return false;
        }
    }
}