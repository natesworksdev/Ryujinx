using System.Collections.Generic;

namespace ARMeilleure.Common
{
    public class SortedIntegerList
    {
        private List<int> _items;

        public int Count => _items.Count;

        public int this[int index]
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

        public SortedIntegerList()
        {
            _items = new List<int>();
        }

        public bool Add(int value)
        {
            if (_items.Count > 0 && value > Last())
            {
                _items.Add(value);
                return true;
            }
            else
            {
                // Binary search for the location to insert.
                int min = 0;
                int max = Count - 1;

                while (min <= max)
                {
                    int mid = min + (max - min) / 2;
                    int existing = _items[mid];
                    if (value > existing)
                    {
                        min = mid + 1;
                    }
                    else if (value < existing)
                    {
                        max = mid - 1;
                    }
                    else
                    {
                        // This value already exists in the list. Return false.
                        return false;
                    }
                }

                _items.Insert(min, value);
                return true;
            }
        }

        public int FindLessEqualIndex(int value)
        {
            int min = 0;
            int max = Count - 1;

            while (min <= max)
            {
                int mid = min + (max - min) / 2;
                int existing = _items[mid];
                if (value > existing)
                {
                    min = mid + 1;
                }
                else if (value < existing)
                {
                    max = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            return max;
        }

        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                _items.RemoveRange(index, count);
            }
        }

        public int Last()
        {
            return _items[Count - 1];
        }

        public List<int> GetList()
        {
            return _items;
        }
    }
}
