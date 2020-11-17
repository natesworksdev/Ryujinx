using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Graphics.Gpu.Memory
{
    class TreeMap<TKey, TValue> where TKey : IComparable<TKey>
    {
        private Entry<TKey, TValue> root = null;
        private int count = 0;

        private static readonly bool BLACK = true;
        private static readonly bool RED = false;

        public Entry<TKey, TValue> GetEntry(TKey key)
        {
            if (key == null) throw new ArgumentNullException();
            Entry<TKey, TValue> p = root;
            while (p != null)
            {
                int cmp = key.CompareTo(p.Key);

                if (cmp < 0)
                {
                    p = p.Left;
                }
                else if (cmp > 0)
                {
                    p = p.Right;
                }
                else
                {
                    return p;
                }
            }
            return null;
        }

        public Entry<TKey, TValue> GetCeilingEntry(TKey key)
        {
            Entry<TKey, TValue> p = root;
            while (p != null)
            {
                int cmp = key.CompareTo(p.Key);

                if (cmp < 0)
                {
                    if (p.Left != null)
                    {
                        p = p.Left;
                    }
                    else
                    {
                        return p;
                    }
                }
                else if (cmp > 0)
                {
                    if (p.Right != null)
                    {
                        p = p.Right;
                    }
                    else
                    {
                        Entry<TKey, TValue> parent = p.Parent;
                        Entry<TKey, TValue> ch = p;
                        while (parent != null && ch == parent.Right)
                        {
                            ch = parent;
                            parent = parent.Parent;
                        }
                        return parent;
                    }
                }
                else
                {
                    return p;
                }
            }
            return null;
        }

        public Entry<TKey, TValue> GetFloorEntry(TKey key)
        {
            Entry<TKey, TValue> p = root;
            while (p != null)
            {
                int cmp = key.CompareTo(p.Key);

                if (cmp > 0)
                {
                    if (p.Right != null)
                    {
                        p = p.Right;
                    }
                    else
                    {
                        return p;
                    }
                }
                else if (cmp < 0)
                {
                    if (p.Left != null)
                    {
                        p = p.Left;
                    }
                    else
                    {
                        Entry<TKey, TValue> parent = p.Left;
                        Entry<TKey, TValue> ch = p;
                        while (parent != null && ch == parent.Left)
                        {
                            ch = parent;
                            parent = parent.Parent;
                        }
                        return parent;
                    }
                }
                else
                {
                    return p;
                }
            }
            return null;
        }

        public TValue Put(TKey key, TValue value)
        {
            Entry<TKey, TValue> t = root;
            if (t == null)
            {
                root = new Entry<TKey, TValue>(key, value);
                count = 1;
                return value;
            }
            if (key == null || value == null)
            {
                throw new ArgumentNullException();
            }

            int cmp;
            Entry<TKey, TValue> parent;

            do
            {
                parent = t;
                cmp = key.CompareTo(t.Key);
                if (cmp < 0)
                {
                    t = t.Left;
                }
                else if (cmp > 0)
                {
                    t = t.Right;
                }
                else
                {
                    t.Value = value;
                    return t.Value;
                }
            } while (t != null);

            Entry<TKey, TValue> e = new Entry<TKey, TValue>(key, value, parent);
            if (cmp < 0)
            {
                parent.Left = e;
            }
            else
            {
                parent.Right = e;
            }

            FixAfterInsertion(e);

            this.count++;
            return default;
        }

        public TValue Remove(TKey key)
        {
            Entry<TKey, TValue> p = GetEntry(key);

            if (p == null)
            {
                return default;
            }

            TValue deletedValue = p.Value;

            DeleteEntry(p);
            return deletedValue;
        }

        public void Clear()
        {
            this.count = 0;
            this.root = null;
        }

        public int Count()
        {
            return this.count;
        }


        // =================== [ PRIVATE METHODS ] ==================== //

        private static bool ColorOf(Entry<TKey, TValue> e)
        {
            return (e == null ? BLACK : e.Color);
        }

        private static Entry<TKey, TValue> LeftOf(Entry<TKey, TValue> p)
        {
            return (p == null) ? null : p.Left;
        }

        private static Entry<TKey, TValue> RightOf(Entry<TKey, TValue> p)
        {
            return (p == null) ? null : p.Right;
        }

        private static Entry<TKey, TValue> ParentOf(Entry<TKey, TValue> e)
        {
            return (e == null ? null : e.Parent);
        }

        private static void SetColor(Entry<TKey, TValue> e, bool color)
        {
            if (e != null)
            {
                e.Color = color;
            }
        }

        /**
         * Returns the successor of the specified Entry, or null if no such.
         */
        private Entry<TKey, TValue> SuccessorOf(Entry<TKey, TValue> t)
        {
            if (t == null)
                return null;
            else if (t.Right != null)
            {
                Entry<TKey, TValue> p = t.Right;
                while (p.Left != null)
                    p = p.Left;
                return p;
            }
            else
            {
                Entry<TKey, TValue> p = t.Parent;
                Entry<TKey, TValue> ch = t;
                while (p != null && ch == p.Right)
                {
                    ch = p;
                    p = p.Parent;
                }
                return p;
            }
        }

        /**
         * Returns the predecessor of the specified Entry, or null if no such.
         */
        private static Entry<TKey, TValue> PredecessorOf(Entry<TKey, TValue> t)
        {
            if (t == null)
                return null;
            else if (t.Left != null)
            {
                Entry<TKey, TValue> p = t.Left;
                while (p.Right != null)
                    p = p.Right;
                return p;
            }
            else
            {
                Entry<TKey, TValue> p = t.Parent;
                Entry<TKey, TValue> ch = t;
                while (p != null && ch == p.Left)
                {
                    ch = p;
                    p = p.Parent;
                }
                return p;
            }
        }

        private void RotateLeft(Entry<TKey, TValue> p)
        {
            if (p != null)
            {
                Entry<TKey, TValue> r = p.Right;
                p.Right = r.Left;
                if (r.Left != null)
                    r.Left.Parent = p;
                r.Parent = p.Parent;
                if (p.Parent == null)
                    root = r;
                else if (p.Parent.Left == p)
                    p.Parent.Left = r;
                else
                    p.Parent.Right = r;
                r.Left = p;
                p.Parent = r;
            }
        }

        /** From CLR */
        private void RotateRight(Entry<TKey, TValue> p)
        {
            if (p != null)
            {
                Entry<TKey, TValue> l = p.Left;
                p.Left = l.Right;
                if (l.Right != null) l.Right.Parent = p;
                l.Parent = p.Parent;
                if (p.Parent == null)
                    root = l;
                else if (p.Parent.Right == p)
                    p.Parent.Right = l;
                else p.Parent.Left = l;
                l.Right = p;
                p.Parent = l;
            }
        }

        /** From CLR */
        private void FixAfterInsertion(Entry<TKey, TValue> x)
        {
            x.Color = RED;

            while (x != null && x != root && x.Parent.Color == RED)
            {
                if (ParentOf(x) == LeftOf(ParentOf(ParentOf(x))))
                {
                    Entry<TKey, TValue> y = RightOf(ParentOf(ParentOf(x)));
                    if (ColorOf(y) == RED)
                    {
                        SetColor(ParentOf(x), BLACK);
                        SetColor(y, BLACK);
                        SetColor(ParentOf(ParentOf(x)), RED);
                        x = ParentOf(ParentOf(x));
                    }
                    else
                    {
                        if (x == RightOf(ParentOf(x)))
                        {
                            x = ParentOf(x);
                            RotateLeft(x);
                        }
                        SetColor(ParentOf(x), BLACK);
                        SetColor(ParentOf(ParentOf(x)), RED);
                        RotateRight(ParentOf(ParentOf(x)));
                    }
                }
                else
                {
                    Entry<TKey, TValue> y = LeftOf(ParentOf(ParentOf(x)));
                    if (ColorOf(y) == RED)
                    {
                        SetColor(ParentOf(x), BLACK);
                        SetColor(y, BLACK);
                        SetColor(ParentOf(ParentOf(x)), RED);
                        x = ParentOf(ParentOf(x));
                    }
                    else
                    {
                        if (x == LeftOf(ParentOf(x)))
                        {
                            x = ParentOf(x);
                            RotateRight(x);
                        }
                        SetColor(ParentOf(x), BLACK);
                        SetColor(ParentOf(ParentOf(x)), RED);
                        RotateLeft(ParentOf(ParentOf(x)));
                    }
                }
            }
            root.Color = BLACK;
        }

        /**
         * Delete node p, and then rebalance the tree.
         */
        private void DeleteEntry(Entry<TKey, TValue> p)
        {
            this.count--;
            // If strictly internal, copy successor's element to p and then make p
            // point to successor.
            if (p.Left != null && p.Right != null)
            {
                Entry<TKey, TValue> s = SuccessorOf(p);
                p.Key = s.Key;
                p.Value = s.Value;
                p = s;
            } // p has 2 children

            // Start fixup at replacement node, if it exists.
            Entry<TKey, TValue> replacement = (p.Left != null ? p.Left : p.Right);

            if (replacement != null)
            {
                // Link replacement to Parent
                replacement.Parent = p.Parent;
                if (p.Parent == null)
                    root = replacement;
                else if (p == p.Parent.Left)
                    p.Parent.Left = replacement;
                else
                    p.Parent.Right = replacement;

                // Null out links so they are OK to use by fixAfterDeletion.
                p.Left = p.Right = p.Parent = null;

                // Fix replacement
                if (p.Color == BLACK)
                    FixAfterDeletion(replacement);
            }
            else if (p.Parent == null)
            { // return if we are the only node.
                root = null;
            }
            else
            { //  No children. Use self as phantom replacement and unlink.
                if (p.Color == BLACK)
                    FixAfterDeletion(p);

                if (p.Parent != null)
                {
                    if (p == p.Parent.Left)
                        p.Parent.Left = null;
                    else if (p == p.Parent.Right)
                        p.Parent.Right = null;
                    p.Parent = null;
                }
            }
        }

        /** From CLR */
        private void FixAfterDeletion(Entry<TKey, TValue> x)
        {
            while (x != root && ColorOf(x) == BLACK)
            {
                if (x == LeftOf(ParentOf(x)))
                {
                    Entry<TKey, TValue> sib = RightOf(ParentOf(x));

                    if (ColorOf(sib) == RED)
                    {
                        SetColor(sib, BLACK);
                        SetColor(ParentOf(x), RED);
                        RotateLeft(ParentOf(x));
                        sib = RightOf(ParentOf(x));
                    }

                    if (ColorOf(LeftOf(sib)) == BLACK &&
                        ColorOf(RightOf(sib)) == BLACK)
                    {
                        SetColor(sib, RED);
                        x = ParentOf(x);
                    }
                    else
                    {
                        if (ColorOf(RightOf(sib)) == BLACK)
                        {
                            SetColor(LeftOf(sib), BLACK);
                            SetColor(sib, RED);
                            RotateRight(sib);
                            sib = RightOf(ParentOf(x));
                        }
                        SetColor(sib, ColorOf(ParentOf(x)));
                        SetColor(ParentOf(x), BLACK);
                        SetColor(RightOf(sib), BLACK);
                        RotateLeft(ParentOf(x));
                        x = root;
                    }
                }
                else
                { // symmetric
                    Entry<TKey, TValue> sib = LeftOf(ParentOf(x));

                    if (ColorOf(sib) == RED)
                    {
                        SetColor(sib, BLACK);
                        SetColor(ParentOf(x), RED);
                        RotateRight(ParentOf(x));
                        sib = LeftOf(ParentOf(x));
                    }

                    if (ColorOf(RightOf(sib)) == BLACK &&
                        ColorOf(LeftOf(sib)) == BLACK)
                    {
                        SetColor(sib, RED);
                        x = ParentOf(x);
                    }
                    else
                    {
                        if (ColorOf(LeftOf(sib)) == BLACK)
                        {
                            SetColor(RightOf(sib), BLACK);
                            SetColor(sib, RED);
                            RotateLeft(sib);
                            sib = LeftOf(ParentOf(x));
                        }
                        SetColor(sib, ColorOf(ParentOf(x)));
                        SetColor(ParentOf(x), BLACK);
                        SetColor(LeftOf(sib), BLACK);
                        RotateRight(ParentOf(x));
                        x = root;
                    }
                }
            }

            SetColor(x, BLACK);
        }

    }

    class Entry<NKey, NValue>
    {

        public bool Color { get; set; } = true;
        public NKey Key { get; set; }
        public NValue Value { get; set; }
        public Entry<NKey, NValue> Left { get; set; } = null;
        public Entry<NKey, NValue> Right { get; set; } = null;
        public Entry<NKey, NValue> Parent { get; set; } = null;

        public Entry(NKey key, NValue value)
        {
            this.Key = key;
            this.Value = value;
        }

        public Entry(NKey key, NValue value, Entry<NKey, NValue> parent)
        {
            this.Key = key;
            this.Value = value;
            this.Parent = parent;
        }

        public Entry<NKey, NValue> CeilingEntry()
        {
            if (this.Right != null) return this.Right;
            return this.Parent;
        }

        public Entry<NKey, NValue> FloorEntry()
        {
            if (this.Left != null) return this.Left;
            return this.Parent;
        }
    }
}


