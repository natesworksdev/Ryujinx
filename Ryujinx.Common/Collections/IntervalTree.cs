using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Common.Collections
{
    public class IntervalTree<TKey, TValue> : IIntervalTree<TKey, TValue>
    {
        private IntervalTreeNode<TKey, TValue> root;
        private List<RangeValuePair<TKey, TValue>> items;
        private readonly IComparer<TKey> comparer;
        private bool isInSync;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TKey Max
        {
            get
            {
                if (!isInSync)
                    Rebuild();

                return root.Max;
            }
        }

        public TKey Min
        {
            get
            {
                if (!isInSync)
                    Rebuild();

                return root.Min;
            }
        }

        public IEnumerable<TValue> Values => items.Select(i => i.Value);

        public int Count => items.Count;

        /// <summary>
        /// Initializes an empty tree.
        /// </summary>
        public IntervalTree() : this(Comparer<TKey>.Default) { }

        /// <summary>
        /// Initializes an empty tree.
        /// </summary>
        public IntervalTree(IComparer<TKey> comparer)
        {
            this.comparer = comparer ?? Comparer<TKey>.Default;
            isInSync = true;
            root = new IntervalTreeNode<TKey, TValue>(this.comparer);
            items = new List<RangeValuePair<TKey, TValue>>();
        }

        public TValue FindOverlap(TKey value)
        {
            if (!isInSync)
                Rebuild();

            return root.FindOverlap(value);
        }

        public IEnumerable<TValue> FindOverlaps(TKey value)
        {
            if (!isInSync)
                Rebuild();

            return root.FindOverlaps(value);
        }

        public TValue FindOverlap(TKey from, TKey to)
        {
            if (!isInSync)
                Rebuild();

            return root.FindOverlap(from, to);
        }

        public IEnumerable<TValue> FindOverlaps(TKey from, TKey to)
        {
            if (!isInSync)
                Rebuild();

            return root.FindOverlaps(from, to);
        }

        public void Add(TKey from, TKey to, TValue value)
        {
            if (comparer.Compare(from, to) > 0)
                throw new ArgumentOutOfRangeException($"{nameof(from)} cannot be larger than {nameof(to)}");

            isInSync = false;
            items.Add(new RangeValuePair<TKey, TValue>(from, to, value));
        }

        public void Remove(TValue value)
        {
            isInSync = false;
            items = items.Where(l => !l.Value.Equals(value)).ToList();
        }

        public void Remove(IEnumerable<TValue> items)
        {
            isInSync = false;
            this.items = this.items.Where(l => !items.Contains(l.Value)).ToList();
        }

        public void Clear()
        {
            root = new IntervalTreeNode<TKey, TValue>(comparer);
            items = new List<RangeValuePair<TKey, TValue>>();
            isInSync = true;
        }

        public IEnumerator<RangeValuePair<TKey, TValue>> GetEnumerator()
        {
            if (!isInSync)
                Rebuild();

            return items.GetEnumerator();
        }

        private void Rebuild()
        {
            if (isInSync)
                return;

            if (items.Count > 0)
                root = new IntervalTreeNode<TKey, TValue>(items, comparer);
            else
                root = new IntervalTreeNode<TKey, TValue>(comparer);
            isInSync = true;
        }
    }
}