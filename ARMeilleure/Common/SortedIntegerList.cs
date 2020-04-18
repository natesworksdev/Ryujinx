using System.Collections.Generic;

namespace ARMeilleure.Common
{
    internal class SortedIntegerList
    {
        private readonly List<int> _items;

        internal int Count => _items.Count;

        internal int this[int index]
        {
            get
            {
                return _items[index];
            }
            set
            {
                _items[index] = value;
            }
        }

        internal SortedIntegerList()
        {
            _items = new List<int>();
        }

        internal bool Add(int value)
        {
            if (_items.Count == 0 || value > Last())
            {
                _items.Add(value);
                return true;
            }
            else
            {
                int index = _items.BinarySearch(value);
                if (index >= 0)
                {
                    return false;
                }

                _items.Insert(-1 - index, value);
                return true;
            }
        }

        internal int FindLessEqualIndex(int value)
        {
            int index = _items.BinarySearch(value);
            return (index < 0) ? (-2 - index) : index;
        }

        internal void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                _items.RemoveRange(index, count);
            }
        }

        internal int Last()
        {
            return _items[Count - 1];
        }

        internal List<int> GetList()
        {
            return _items;
        }
    }
}
