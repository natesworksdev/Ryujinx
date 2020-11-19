using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Common.Collections
{
    public class TreeDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : IComparable<TKey>
    {
        private TreeNode<TKey, TValue> _root = null;
        private bool _isModified = true;
        private int _count = 0;

        private const bool Black = true;
        private const bool Red = false;

        private LinkedList<TreeNode<TKey, TValue>> _nodes = new LinkedList<TreeNode<TKey, TValue>>();
        private List<TValue> _values;
        private List<TKey> _keys;

        #region Public Methods
        /// <summary>
        /// Retrieves the value whose key is equal to key, or the default value of TValue if no value is found.
        /// </summary>
        /// <param name="key">Key to search</param>
        /// <returns></returns>
        public TValue Get(TKey key)
        {
            TreeNode<TKey, TValue> node = GetNode(key);
            if (node != null) return node.Value;
            return default;
        }

        public TreeNode<TKey, TValue> GetNode(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            TreeNode<TKey, TValue> entry = _root;
            while (entry != null)
            {
                int cmp = key.CompareTo(entry.Key);

                if (cmp < 0)
                {
                    entry = entry.Left;
                }
                else if (cmp > 0)
                {
                    entry = entry.Right;
                }
                else
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the value whose key is equal to or immediately greater than key.
        /// </summary>
        /// <param name="key">Key to search</param>
        /// <returns></returns>
        public TValue Ceiling(TKey key)
        {
            TreeNode<TKey, TValue> node = CeilingNode(key);
            if (node != null) return node.Value;
            return default;
        }

        public TreeNode<TKey, TValue> CeilingNode(TKey key)
        {
            TreeNode<TKey, TValue> entry = _root;
            while (entry != null)
            {
                int cmp = key.CompareTo(entry.Key);

                if (cmp < 0)
                {
                    if (entry.Left != null)
                    {
                        entry = entry.Left;
                    }
                    else
                    {
                        return entry;
                    }
                }
                else if (cmp > 0)
                {
                    if (entry.Right != null)
                    {
                        entry = entry.Right;
                    }
                    else
                    {
                        TreeNode<TKey, TValue> parent = entry.Parent;
                        TreeNode<TKey, TValue> tmp = entry;
                        while (parent != null && tmp == parent.Right)
                        {
                            tmp = parent;
                            parent = parent.Parent;
                        }
                        return parent != null ? parent : default;
                    }
                }
                else
                {
                    return entry;
                }
            }
            return default;
        }

        /// <summary>
        /// Retrieves the value whose key is equal to or immediately less than key.
        /// </summary>
        /// <param name="key">Key to search</param>
        /// <returns></returns>
        public TValue Floor(TKey key)
        {
            TreeNode<TKey, TValue> node = FloorNode(key);
            if (node != null) return node.Value;
            return default;
        }

        public TreeNode<TKey, TValue> FloorNode(TKey key)
        {
            TreeNode<TKey, TValue> entry = _root;
            while (entry != null)
            {
                int cmp = key.CompareTo(entry.Key);

                if (cmp > 0)
                {
                    if (entry.Right != null)
                    {
                        entry = entry.Right;
                    }
                    else
                    {
                        return entry;
                    }
                }
                else if (cmp < 0)
                {
                    if (entry.Left != null)
                    {
                        entry = entry.Left;
                    }
                    else
                    {
                        TreeNode<TKey, TValue> parent = entry.Left;
                        TreeNode<TKey, TValue> tmp = entry;
                        while (parent != null && tmp == parent.Left)
                        {
                            tmp = parent;
                            parent = parent.Parent;
                        }
                        return parent != null ? parent : default;
                    }
                }
                else
                {
                    return entry;
                }
            }
            return default;
        }


        /// <summary>
        /// Removes and returns the key/value pair with the specified key from the tree if it exists.
        /// </summary>
        /// <param name="key">Key of value to remove</param>
        /// <returns>The removed value</returns>
        public TValue Remove(TKey key)
        {
            TreeNode<TKey, TValue> entry = GetEntryNode(key);

            if (entry == null)
            {
                return default;
            }

            TValue deletedValue = entry.Value;

            DeleteEntry(entry);
            return deletedValue;
        }

        /// <summary>
        /// Removes all key/value pairs from the tree.
        /// </summary>
        public void Clear()
        {
            this._count = 0;
            this._root = null;
            this._isModified = true;
        }

        /// <summary>
        /// Insert or overwrite a key/value pair in the tree.
        /// If the key already exists, its value will be overwritten.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        public void Add(TKey key, TValue value)
        {
            Put(key, value);
        }

        /// <summary>
        /// Checks if a value exists for the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if a value exists, false otherwise</returns>
        public bool ContainsKey(TKey key)
        {
            return Get(key) != null;
        }

        /// <summary>
        /// Removes and returns the key/value pair with the specified key from the tree if it exists.
        /// </summary>
        /// <param name="key">Key of value to remove</param>
        /// <returns>The removed value</returns>
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            return Remove(key) != null;
        }

        /// <summary>
        /// Checks if a value exists for the specified key and stores it in value.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value if key was found, default of TValue otherwise</param>
        /// <returns>True if a value was found, false otherwise</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            value = Get(key);
            return value != null;
        }

        /// <summary>
        /// Insert or overwrite a key/value pair in the tree.
        /// If the key already exists, its value will be overwritten.
        /// </summary>
        /// <param name="item">Key/Value pair</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Put(item.Key, item.Value);
        }

        /// <summary>
        /// Checks if an item exists in the tree whose key is item.Key, and whose value is item.Value
        /// </summary>
        /// <param name="item">Key/Value pair to match against to match against</param>
        /// <returns>True if both key and value match, false otherwise</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue t = Get(item.Key);
            return t.Equals(item.Value);
        }

        /// <summary>
        /// Copies all items in the tree to array starting at index arrayIndex
        /// 
        /// array must contain enough space to carry all elements, otherwise an exception will be thrown.
        /// </summary>
        /// <param name="array">Array to copy the key/value pairs into</param>
        /// <param name="arrayIndex">Index at which to start copying into array</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (arrayIndex >= array.Length) throw new ArgumentException($"arrayIndex is greater than the length of array");
            if (array.Length - arrayIndex < this._count) throw new ArgumentException($"There is not enough space in array");
            if (this._count == 0) return;

            LinkedListNode<TreeNode<TKey, TValue>> node = _nodes.First;
            for (int i = 0; i < this._count && node != null; i++)
            {
                array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(node.Value.Key, node.Value.Value);
                node = node.Next;
            }
        }

        /// <summary>
        /// Removes the item whose key is item.Key
        /// </summary>
        /// <param name="item">Key/Value Pair to remove</param>
        /// <returns>True if an item was removed, false otherwise</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key) != null;
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        int ICollection<KeyValuePair<TKey, TValue>>.Count => this._count;

        public int Count => this._count;

        public bool IsReadOnly => false;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys();

        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values();

        public TValue this[TKey key] { get => Get(key); set => Put(key, value); }

        #endregion

        #region Helper Methods (Avoid Null-Pointers)

        private static bool ColorOf(TreeNode<TKey, TValue> entry)
        {
            return (entry == null ? Black : entry.Color);
        }

        private static TreeNode<TKey, TValue> LeftOf(TreeNode<TKey, TValue> entry)
        {
            return (entry == null) ? null : entry.Left;
        }

        private static TreeNode<TKey, TValue> RightOf(TreeNode<TKey, TValue> entry)
        {
            return (entry == null) ? null : entry.Right;
        }

        private static TreeNode<TKey, TValue> ParentOf(TreeNode<TKey, TValue> entry)
        {
            return (entry == null ? null : entry.Parent);
        }

        private static void SetColor(TreeNode<TKey, TValue> e, bool color)
        {
            if (e != null)
            {
                e.Color = color;
            }
        }

        #endregion

        #region IDictionary Implementation Helpers

        private ICollection<TKey> Keys()
        {
            if (this._isModified)
            {
                this._keys = new List<TKey>(_nodes.Count);

                foreach (TreeNode<TKey, TValue> node in _nodes)
                {
                    _keys.Add(node.Key);
                }
                this._isModified = false;
            }

            return _keys;
        }

        private ICollection<TValue> Values()
        {
            if (this._isModified)
            {
                this._values = new List<TValue>(_nodes.Count);

                foreach (TreeNode<TKey, TValue> node in _nodes)
                {
                    _values.Add(node.Value);
                }
                this._isModified = false;
            }
            return this._values;
        }


        private TValue Put(TKey key, TValue value)
        {
            TreeNode<TKey, TValue> entry = _root;
            if (entry == null)
            {
                _root = new TreeNode<TKey, TValue>(key, value);
                _count = 1;
                this._isModified = true;
                _nodes.AddLast(_root);
                return value;
            }
            if (key == null || value == null)
            {
                throw new ArgumentNullException();
            }

            int cmp;
            TreeNode<TKey, TValue> parent;

            do
            {
                parent = entry;
                cmp = key.CompareTo(entry.Key);
                if (cmp < 0)
                {
                    entry = entry.Left;
                }
                else if (cmp > 0)
                {
                    entry = entry.Right;
                }
                else
                {
                    entry.Value = value;
                    return entry.Value;
                }
            } while (entry != null);

            TreeNode<TKey, TValue> newEntry = new TreeNode<TKey, TValue>(key, value, parent);
            if (cmp < 0)
            {
                parent.Left = newEntry;
            }
            else
            {
                parent.Right = newEntry;
            }

            FixAfterInsertion(newEntry);

            _nodes.AddLast(newEntry);
            this._isModified = true;
            this._count++;
            return default;
        }

        private TreeNode<TKey, TValue> GetEntryNode(TKey key)
        {
            if (key == null) throw new ArgumentNullException();
            TreeNode<TKey, TValue> entry = _root;
            while (entry != null)
            {
                int cmp = key.CompareTo(entry.Key);

                if (cmp < 0)
                {
                    entry = entry.Left;
                }
                else if (cmp > 0)
                {
                    entry = entry.Right;
                }
                else
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the node with the key greater than treeNode.
        /// </summary>
        /// <param name="treeNode"></param>
        /// <returns></returns>
        public TreeNode<TKey, TValue> SuccessorOf(TreeNode<TKey, TValue> treeNode)
        {
            if (treeNode == null)
                return null;
            else if (treeNode.Right != null)
            {
                TreeNode<TKey, TValue> tmp = treeNode.Right;
                while (tmp.Left != null)
                    tmp = tmp.Left;
                return tmp;
            }
            else
            {
                TreeNode<TKey, TValue> parent = treeNode.Parent;
                TreeNode<TKey, TValue> tmp = treeNode;
                while (parent != null && tmp == parent.Right)
                {
                    tmp = parent;
                    parent = parent.Parent;
                }
                return parent;
            }
        }

        /// <summary>
        /// Returns the node the occurs prior to treeNode
        /// </summary>
        /// <param name="treeNode"></param>
        /// <returns></returns>
        public TreeNode<TKey, TValue> PredecessorOf(TreeNode<TKey, TValue> treeNode)
        {
            if (treeNode == null)
                return null;
            else if (treeNode.Left != null)
            {
                TreeNode<TKey, TValue> tmp = treeNode.Left;
                while (tmp.Right != null)
                    tmp = tmp.Right;
                return tmp;
            }
            else
            {
                TreeNode<TKey, TValue> parent = treeNode.Parent;
                TreeNode<TKey, TValue> tmp = treeNode;
                while (parent != null && tmp == parent.Left)
                {
                    tmp = parent;
                    parent = parent.Parent;
                }
                return parent;
            }
        }

        /// <summary>
        /// Rotate to reduce the height of the tree, to maximize operation efficieny.
        /// </summary>
        /// <param name="treeNode"></param>
        private void RotateLeft(TreeNode<TKey, TValue> treeNode)
        {
            if (treeNode != null)
            {
                TreeNode<TKey, TValue> tmp = treeNode.Right;
                treeNode.Right = tmp.Left;
                if (tmp.Left != null)
                    tmp.Left.Parent = treeNode;
                tmp.Parent = treeNode.Parent;
                if (treeNode.Parent == null)
                    _root = tmp;
                else if (treeNode.Parent.Left == treeNode)
                    treeNode.Parent.Left = tmp;
                else
                    treeNode.Parent.Right = tmp;
                tmp.Left = treeNode;
                treeNode.Parent = tmp;
            }
        }

        /// <summary>
        /// Rotate to reduce the height of the tree, to maximize operation efficieny.
        /// </summary>
        /// <param name="treeNode"></param>
        private void RotateRight(TreeNode<TKey, TValue> treeNode)
        {
            if (treeNode != null)
            {
                TreeNode<TKey, TValue> tmp = treeNode.Left;
                treeNode.Left = tmp.Right;
                if (tmp.Right != null) tmp.Right.Parent = treeNode;
                tmp.Parent = treeNode.Parent;
                if (treeNode.Parent == null)
                    _root = tmp;
                else if (treeNode.Parent.Right == treeNode)
                    treeNode.Parent.Right = tmp;
                else treeNode.Parent.Left = tmp;
                tmp.Right = treeNode;
                treeNode.Parent = tmp;
            }
        }

        /// <summary>
        /// Rebalance the tree after an insertion.
        /// This is important for O(logN) operations.
        /// </summary>
        /// <param name="treeNode"></param>
        private void FixAfterInsertion(TreeNode<TKey, TValue> treeNode)
        {
            treeNode.Color = Red;

            while (treeNode != null && treeNode != _root && treeNode.Parent.Color == Red)
            {
                if (ParentOf(treeNode) == LeftOf(ParentOf(ParentOf(treeNode))))
                {
                    TreeNode<TKey, TValue> tmp = RightOf(ParentOf(ParentOf(treeNode)));
                    if (ColorOf(tmp) == Red)
                    {
                        SetColor(ParentOf(treeNode), Black);
                        SetColor(tmp, Black);
                        SetColor(ParentOf(ParentOf(treeNode)), Red);
                        treeNode = ParentOf(ParentOf(treeNode));
                    }
                    else
                    {
                        if (treeNode == RightOf(ParentOf(treeNode)))
                        {
                            treeNode = ParentOf(treeNode);
                            RotateLeft(treeNode);
                        }
                        SetColor(ParentOf(treeNode), Black);
                        SetColor(ParentOf(ParentOf(treeNode)), Red);
                        RotateRight(ParentOf(ParentOf(treeNode)));
                    }
                }
                else
                {
                    TreeNode<TKey, TValue> tmp = LeftOf(ParentOf(ParentOf(treeNode)));
                    if (ColorOf(tmp) == Red)
                    {
                        SetColor(ParentOf(treeNode), Black);
                        SetColor(tmp, Black);
                        SetColor(ParentOf(ParentOf(treeNode)), Red);
                        treeNode = ParentOf(ParentOf(treeNode));
                    }
                    else
                    {
                        if (treeNode == LeftOf(ParentOf(treeNode)))
                        {
                            treeNode = ParentOf(treeNode);
                            RotateRight(treeNode);
                        }
                        SetColor(ParentOf(treeNode), Black);
                        SetColor(ParentOf(ParentOf(treeNode)), Red);
                        RotateLeft(ParentOf(ParentOf(treeNode)));
                    }
                }
            }
            _root.Color = Black;
        }

        /// <summary>
        /// Remove treeNode from the tree and rebalance it.
        /// </summary>
        /// <param name="treeNode"></param>
        private void DeleteEntry(TreeNode<TKey, TValue> treeNode)
        {
            this._count--;
            this._isModified = true;
            _nodes.Remove(treeNode);
            // If strictly internal, copy successor's element to p and then make p
            // point to successor.
            if (treeNode.Left != null && treeNode.Right != null)
            {
                TreeNode<TKey, TValue> s = SuccessorOf(treeNode);
                treeNode.Key = s.Key;
                treeNode.Value = s.Value;
                treeNode = s;
            } // p has 2 children

            // Start fixup at replacement node, if it exists.
            TreeNode<TKey, TValue> replacement = (treeNode.Left != null ? treeNode.Left : treeNode.Right);

            if (replacement != null)
            {
                // Link replacement to Parent
                replacement.Parent = treeNode.Parent;
                if (treeNode.Parent == null)
                    _root = replacement;
                else if (treeNode == treeNode.Parent.Left)
                    treeNode.Parent.Left = replacement;
                else
                    treeNode.Parent.Right = replacement;

                // Null out links so they are OK to use by fixAfterDeletion.
                treeNode.Left = treeNode.Right = treeNode.Parent = null;

                // Fix replacement
                if (treeNode.Color == Black)
                    FixAfterDeletion(replacement);
            }
            else if (treeNode.Parent == null)
            { // return if we are the only node.
                _root = null;
            }
            else
            { //  No children. Use self as phantom replacement and unlink.
                if (treeNode.Color == Black)
                    FixAfterDeletion(treeNode);

                if (treeNode.Parent != null)
                {
                    if (treeNode == treeNode.Parent.Left)
                        treeNode.Parent.Left = null;
                    else if (treeNode == treeNode.Parent.Right)
                        treeNode.Parent.Right = null;
                    treeNode.Parent = null;
                }
            }
        }

        /// <summary>
        /// Rebalances the tree after a deletion takes place.
        /// This is important to guarantee O(logN) operations.
        /// </summary>
        /// <param name="treeNode"></param>
        private void FixAfterDeletion(TreeNode<TKey, TValue> treeNode)
        {
            while (treeNode != _root && ColorOf(treeNode) == Black)
            {
                if (treeNode == LeftOf(ParentOf(treeNode)))
                {
                    TreeNode<TKey, TValue> sibling = RightOf(ParentOf(treeNode));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(sibling, Black);
                        SetColor(ParentOf(treeNode), Red);
                        RotateLeft(ParentOf(treeNode));
                        sibling = RightOf(ParentOf(treeNode));
                    }

                    if (ColorOf(LeftOf(sibling)) == Black &&
                        ColorOf(RightOf(sibling)) == Black)
                    {
                        SetColor(sibling, Red);
                        treeNode = ParentOf(treeNode);
                    }
                    else
                    {
                        if (ColorOf(RightOf(sibling)) == Black)
                        {
                            SetColor(LeftOf(sibling), Black);
                            SetColor(sibling, Red);
                            RotateRight(sibling);
                            sibling = RightOf(ParentOf(treeNode));
                        }
                        SetColor(sibling, ColorOf(ParentOf(treeNode)));
                        SetColor(ParentOf(treeNode), Black);
                        SetColor(RightOf(sibling), Black);
                        RotateLeft(ParentOf(treeNode));
                        treeNode = _root;
                    }
                }
                else
                {
                    // The exact opposite occurs over here.
                    TreeNode<TKey, TValue> sibling = LeftOf(ParentOf(treeNode));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(sibling, Black);
                        SetColor(ParentOf(treeNode), Red);
                        RotateRight(ParentOf(treeNode));
                        sibling = LeftOf(ParentOf(treeNode));
                    }

                    if (ColorOf(RightOf(sibling)) == Black &&
                        ColorOf(LeftOf(sibling)) == Black)
                    {
                        SetColor(sibling, Red);
                        treeNode = ParentOf(treeNode);
                    }
                    else
                    {
                        if (ColorOf(LeftOf(sibling)) == Black)
                        {
                            SetColor(RightOf(sibling), Black);
                            SetColor(sibling, Red);
                            RotateLeft(sibling);
                            sibling = LeftOf(ParentOf(treeNode));
                        }
                        SetColor(sibling, ColorOf(ParentOf(treeNode)));
                        SetColor(ParentOf(treeNode), Black);
                        SetColor(LeftOf(sibling), Black);
                        RotateRight(ParentOf(treeNode));
                        treeNode = _root;
                    }
                }
            }

            SetColor(treeNode, Black);
        }

        #endregion

        public void TraverseInOrder()
        {
            TreeNode<TKey, TValue> n = _root;
            PrintEntryInOrder(n);
        }

        private void PrintEntryInOrder(TreeNode<TKey, TValue> e)
        {
            if (e == null) return;
            PrintEntryInOrder(e.Left);
            Console.WriteLine(e.ToString());
            PrintEntryInOrder(e.Right);
        }


    }

    #region Node Implementation
    public class TreeNode<NKey, NValue>
    {

        public bool Color { get; set; } = true;
        public NKey Key { get; set; }
        public NValue Value { get; set; }
        public TreeNode<NKey, NValue> Left { get; set; } = null;
        public TreeNode<NKey, NValue> Right { get; set; } = null;
        public TreeNode<NKey, NValue> Parent { get; set; } = null;

        public TreeNode(NKey key, NValue value)
        {
            this.Key = key;
            this.Value = value;
        }

        public TreeNode(NKey key, NValue value, TreeNode<NKey, NValue> parent)
        {
            this.Key = key;
            this.Value = value;
            this.Parent = parent;
        }

        public override String ToString()
        {
            return Value.ToString();
        }
    }
    #endregion
}