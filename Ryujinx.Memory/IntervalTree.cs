using Ryujinx.Memory.Range;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Memory
{
    /// <summary>
    /// Dictionary that provides the ability for O(logN) Lookups for keys that exist in the Dictionary, and O(logN) lookups for keys immediately greater than or less than a specified key.
    /// </summary>
    /// <typeparam name="K">Key</typeparam>
    /// <typeparam name="V">Value</typeparam>
    public class IntervalTree<T> where T : IRange
    {
        private const bool Black = true;
        private const bool Red = false;
        private IntervalNode<T> _root = null;
        private int _count = 0;

        public IntervalTree() { }

        #region Public Methods

        /// <summary>
        /// Returns the value of the node whose key is <paramref name="key"/>, or the default value if no such node exists.
        /// </summary>
        /// <param name="key">Key of the node value to get</param>
        /// <returns>Value associated w/ <paramref name="key"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        public ulong Get(T key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            IntervalNode<T> node = GetNode(key);

            if (node == null)
            {
                return default;
            }

            return node.Value;
        }

        /// <summary>
        /// Adds a new node into the tree whose key is <paramref name="key"/> key and value is <paramref name="value"/>.
        /// <br></br>
        /// <b>Note:</b> Adding the same key multiple times will cause the value for that key to be overwritten.
        /// </summary>
        /// <param name="key">Key of the node to add</param>
        /// <param name="value">Value of the node to add</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> are null</exception>
        public void Add(T key, ulong value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Insert(key, value);
        }

        /// <summary>
        /// Removes the node whose key is <paramref name="key"/> from the tree.
        /// </summary>
        /// <param name="key">Key of the node to remove</param>
        /// <returns>Boolean true if an item was removed, false otherwise</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        public bool Remove(T key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (Delete(key) != null)
            {
                _count--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the value whose key is equal to or immediately less than <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key for which to find the floor value of</param>
        /// <returns>Key of node immediately less than <paramref name="key"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        public T FloorOf(T key)
        {
            IntervalNode<T> node = Floor(key);
            if (node != null)
            {
                return node.Key;
            }
            return default;
        }

        /// <summary>
        /// Returns the node whose key is equal to or immediately greater than <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key for which to find the ceiling node of</param>
        /// <returns>Key of node immediately greater than <paramref name="key"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        public T CeilingOf(T key)
        {
            IntervalNode<T> node = Ceiling(key);
            if (node != null)
            {
                return node.Key;
            }
            return default;
        }

        /// <summary>
        /// Finds the value whose key is immediately greater than <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key to find the successor of</param>
        /// <returns>Value</returns>
        public T SuccessorOf(T key)
        {
            IntervalNode<T> node = GetNode(key);
            if (node != null)
            {
                IntervalNode<T> successor = SuccessorOf(node);

                return successor != null ? successor.Key : default;
            }
            return default;
        }

        /// <summary>
        /// Finds the value whose key is immediately less than <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key to find the predecessor of</param>
        /// <returns>Value</returns>
        public T PredecessorOf(T key)
        {
            IntervalNode<T> node = GetNode(key);
            if (node != null)
            {
                IntervalNode<T> predecessor = PredecessorOf(node);

                return predecessor != null ? predecessor.Key : default;
            }
            return default;
        }

        public int OverlapsOf(ulong Address, ulong EndAddress, ref T[] arr)
        {
            int insertionPoint = 0;

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == null)
                {
                    insertionPoint = i;
                    break;
                }
            }

            return Intersect(_root, new Interval(Address, EndAddress), ref insertionPoint, ref arr);
        }

        private int Intersect(IntervalNode<T> node, Interval point, ref int insertionPoint, ref T[] arr)
        {
            if (node == null)
            {
                return insertionPoint;
            }

            T range = node.Key;

            if (!((range.Address > point.EndAddress) || (range.EndAddress < point.Address)))
            {

                if (insertionPoint == arr.Length)
                {
                    Array.Resize<T>(ref arr, arr.Length + 32);
                }
                arr[insertionPoint++] = range;
            }

            IntervalNode<T> left = LeftOf(node);

            if ((left != null) && (left.MaxInterval >= point.Address))
            {
                Intersect(left, point, ref insertionPoint, ref arr);
            }

            Intersect(RightOf(node), point, ref insertionPoint, ref arr);

            return insertionPoint;
        }

        #endregion
        #region Private Methods (BST)

        /// <summary>
        /// Retrieve the node reference whose key is <paramref name="key"/>, or null if no such node exists.
        /// </summary>
        /// <param name="key">Key of the node to get</param>
        /// <returns>IntervalNode<T> reference in the tree</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        private IntervalNode<T> GetNode(IRange key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            IntervalNode<T> node = _root;
            while (node != null)
            {
                int cmp = key.CompareTo(node.Key);
                if (cmp < 0)
                {
                    node = node.Left;
                }
                else if (cmp > 0)
                {
                    node = node.Right;
                }
                else
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// Inserts a new node into the tree whose key is <paramref name="key"/> and value is <paramref name="value"/>.
        /// <br></br>
        /// Adding the same key multiple times will overwrite the previous value.
        /// </summary>
        /// <param name="key">Key of the node to insert</param>
        /// <param name="value">Value of the node to insert</param>
        private void Insert(T key, ulong value)
        {
            IntervalNode<T> node = BSTInsert(key, value);
            RestoreBalanceAfterInsertion(node);
        }

        /// <summary>
        /// Insertion Mechanism for a Binary Search Tree (BST).
        /// <br></br>
        /// Iterates the tree starting from the root and inserts a new node where all children in the left subtree are less than <paramref name="key"/>, and all children in the right subtree are greater than <paramref name="key"/>.
        /// <br></br>
        /// <b>Note: </b> If a node whose key is <paramref name="key"/> already exists, it's value will be overwritten.
        /// </summary>
        /// <param name="key">Key of the node to insert</param>
        /// <param name="value">Value of the node to insert</param>
        /// <returns>The inserted IntervalNode<T></returns>
        private IntervalNode<T> BSTInsert(T key, ulong value)
        {
            IntervalNode<T> parent = null;
            IntervalNode<T> node = _root;

            while (node != null)
            {
                parent = node;

                if (value > node.MaxInterval)
                {
                    node.MaxInterval = value;
                }
                int cmp = key.CompareTo(node.Key);
                if (cmp < 0)
                {
                    node = node.Left;
                }
                else if (cmp > 0)
                {
                    node = node.Right;
                }
                else
                {
                    node.Value = value;
                    return node;
                }
            }
            IntervalNode<T> newNode = new IntervalNode<T>(key, value, parent);
            if (newNode.Parent == null)
            {
                _root = newNode;
            }
            else if (key.CompareTo(parent.Key) < 0)
            {
                parent.Left = newNode;
            }
            else
            {
                parent.Right = newNode;
            }
            _count++;
            return newNode;
        }

        /// <summary>
        /// Removes <paramref name="key"/> from the dictionary, if it exists.
        /// </summary>
        /// <param name="key">Key of the node to delete</param>
        /// <returns>The deleted IntervalNode<T></returns>
        private IntervalNode<T> Delete(IRange key)
        {
            // O(1) Retrieval
            IntervalNode<T> nodeToDelete = GetNode(key);

            if (nodeToDelete == null) return null;

            IntervalNode<T> replacement;

            if (LeftOf(nodeToDelete) == null || RightOf(nodeToDelete) == null)
            {
                replacement = nodeToDelete;
            }
            else
            {
                replacement = PredecessorOf(nodeToDelete);
            }

            IntervalNode<T> tmp = LeftOf(replacement) ?? RightOf(replacement);

            if (tmp != null)
            {
                tmp.Parent = ParentOf(replacement);
            }

            if (ParentOf(replacement) == null)
            {
                _root = tmp;
            }

            else if (replacement == LeftOf(ParentOf(replacement)))
            {
                ParentOf(replacement).Left = tmp;
            }
            else
            {
                ParentOf(replacement).Right = tmp;
            }

            if (replacement != nodeToDelete)
            {
                nodeToDelete.Key = replacement.Key;
                nodeToDelete.Value = replacement.Value;
            }

            if (tmp != null && ColorOf(replacement) == Black)
            {
                RestoreBalanceAfterRemoval(tmp);
            }

            return replacement;
        }

        /// <summary>
        /// Returns the node with the largest key where <paramref name="node"/> is considered the root node.
        /// </summary>
        /// <param name="node">Root IntervalNode<T></param>
        /// <returns>IntervalNode<T> with the maximum key in the tree of <paramref name="node"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is null</exception>
        private static IntervalNode<T> Maximum(IntervalNode<T> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            IntervalNode<T> tmp = node;
            while (tmp.Right != null)
            {
                tmp = tmp.Right;
            }

            return tmp;
        }

        /// <summary>
        /// Returns the node with the smallest key where <paramref name="node"/> is considered the root node.
        /// </summary>
        /// <param name="node">Root IntervalNode<T></param>
        /// <returns>IntervalNode<T> with the minimum key in the tree of <paramref name="node"/></returns>
        ///<exception cref="ArgumentNullException"><paramref name="node"/> is null</exception>
        private static IntervalNode<T> Minimum(IntervalNode<T> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            IntervalNode<T> tmp = node;
            while (tmp.Left != null)
            {
                tmp = tmp.Left;
            }

            return tmp;
        }

        /// <summary>
        /// Returns the node whose key immediately less than or equal to <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key for which to find the floor node of</param>
        /// <returns>IntervalNode<T> whose key is immediately less than or equal to <paramref name="key"/>, or null if no such node is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        private IntervalNode<T> Floor(T key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            IntervalNode<T> tmp = _root;

            while (tmp != null)
            {
                int cmp = key.CompareTo(tmp.Key); ;
                if (cmp > 0)
                {
                    if (tmp.Right != null)
                    {
                        tmp = tmp.Right;
                    }
                    else
                    {
                        return tmp;
                    }
                }
                else if (cmp < 0)
                {
                    if (tmp.Left != null)
                    {
                        tmp = tmp.Left;
                    }
                    else
                    {
                        IntervalNode<T> parent = tmp.Parent;
                        IntervalNode<T> ptr = tmp;
                        while (parent != null && ptr == parent.Left)
                        {
                            ptr = parent;
                            parent = parent.Parent;
                        }
                        return parent;
                    }
                }
                else
                {
                    return tmp;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the node whose key is immediately greater than or equal to than <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key for which to find the ceiling node of</param>
        /// <returns>IntervalNode<T> whose key is immediately greater than or equal to <paramref name="key"/>, or null if no such node is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        private IntervalNode<T> Ceiling(T key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            IntervalNode<T> tmp = _root;

            while (tmp != null)
            {
                int cmp = key.CompareTo(tmp.Key); ;
                if (cmp < 0)
                {
                    if (tmp.Left != null)
                    {
                        tmp = tmp.Left;
                    }
                    else
                    {
                        return tmp;
                    }
                }
                else if (cmp > 0)
                {
                    if (tmp.Right != null)
                    {
                        tmp = tmp.Right;
                    }
                    else
                    {
                        IntervalNode<T> parent = tmp.Parent;
                        IntervalNode<T> ptr = tmp;
                        while (parent != null && ptr == parent.Right)
                        {
                            ptr = parent;
                            parent = parent.Parent;
                        }
                        return parent;
                    }
                }
                else
                {
                    return tmp;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the node with the key immediately greater than <paramref name="node"/>.Key.
        /// </summary>
        /// <param name="node">IntervalNode<T> to find the successor of</param>
        /// <returns>Successor of <paramref name="node"/></returns>
        private static IntervalNode<T> SuccessorOf(IntervalNode<T> node)
        {
            if (node.Right != null)
            {
                return Minimum(node.Right);
            }
            IntervalNode<T> parent = node.Parent;
            while (parent != null && node == parent.Right)
            {
                node = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        /// <summary>
        /// Finds the node whose key immediately less than <paramref name="node"/>.Key.
        /// </summary>
        /// <param name="node">IntervalNode<T> to find the predecessor of</param>
        /// <returns>Predecessor of <paramref name="node"/></returns>
        private static IntervalNode<T> PredecessorOf(IntervalNode<T> node)
        {
            if (node.Left != null)
            {
                return Maximum(node.Left);
            }
            IntervalNode<T> parent = node.Parent;
            while (parent != null && node == parent.Left)
            {
                node = parent;
                parent = parent.Parent;
            }
            return parent;
        }
        #endregion
        #region Private Methods (RBL)

        private void RestoreBalanceAfterRemoval(IntervalNode<T> balanceNode)
        {
            IntervalNode<T> ptr = balanceNode;

            while (ptr != _root && ColorOf(ptr) == Black)
            {
                if (ptr == LeftOf(ParentOf(ptr)))
                {
                    IntervalNode<T> sibling = RightOf(ParentOf(ptr));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(sibling, Black);
                        SetColor(ParentOf(ptr), Red);
                        RotateLeft(ParentOf(ptr));
                        sibling = RightOf(ParentOf(ptr));
                    }
                    if (ColorOf(LeftOf(sibling)) == Black && ColorOf(RightOf(sibling)) == Black)
                    {
                        SetColor(sibling, Red);
                        ptr = ParentOf(ptr);
                    }
                    else
                    {
                        if (ColorOf(RightOf(sibling)) == Black)
                        {
                            SetColor(LeftOf(sibling), Black);
                            SetColor(sibling, Red);
                            RotateRight(sibling);
                            sibling = RightOf(ParentOf(ptr));
                        }
                        SetColor(sibling, ColorOf(ParentOf(ptr)));
                        SetColor(ParentOf(ptr), Black);
                        SetColor(RightOf(sibling), Black);
                        RotateLeft(ParentOf(ptr));
                        ptr = _root;
                    }
                }
                else
                {
                    IntervalNode<T> sibling = LeftOf(ParentOf(ptr));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(sibling, Black);
                        SetColor(ParentOf(ptr), Red);
                        RotateRight(ParentOf(ptr));
                        sibling = LeftOf(ParentOf(ptr));
                    }
                    if (ColorOf(RightOf(sibling)) == Black && ColorOf(LeftOf(sibling)) == Black)
                    {
                        SetColor(sibling, Red);
                        ptr = ParentOf(ptr);
                    }
                    else
                    {
                        if (ColorOf(LeftOf(sibling)) == Black)
                        {
                            SetColor(RightOf(sibling), Black);
                            SetColor(sibling, Red);
                            RotateLeft(sibling);
                            sibling = LeftOf(ParentOf(ptr));
                        }
                        SetColor(sibling, ColorOf(ParentOf(ptr)));
                        SetColor(ParentOf(ptr), Black);
                        SetColor(LeftOf(sibling), Black);
                        RotateRight(ParentOf(ptr));
                        ptr = _root;
                    }
                }
            }
            SetColor(ptr, Black);
        }

        private void RestoreBalanceAfterInsertion(IntervalNode<T> balanceNode)
        {
            SetColor(balanceNode, Red);
            while (balanceNode != null && balanceNode != _root && ColorOf(ParentOf(balanceNode)) == Red)
            {
                if (ParentOf(balanceNode) == LeftOf(ParentOf(ParentOf(balanceNode))))
                {
                    IntervalNode<T> sibling = RightOf(ParentOf(ParentOf(balanceNode)));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(ParentOf(balanceNode), Black);
                        SetColor(sibling, Black);
                        SetColor(ParentOf(ParentOf(balanceNode)), Red);
                        balanceNode = ParentOf(ParentOf(balanceNode));
                    }
                    else
                    {
                        if (balanceNode == RightOf(ParentOf(balanceNode)))
                        {
                            balanceNode = ParentOf(balanceNode);
                            RotateLeft(balanceNode);
                        }
                        SetColor(ParentOf(balanceNode), Black);
                        SetColor(ParentOf(ParentOf(balanceNode)), Red);
                        RotateRight(ParentOf(ParentOf(balanceNode)));
                    }
                }
                else
                {
                    IntervalNode<T> sibling = LeftOf(ParentOf(ParentOf(balanceNode)));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(ParentOf(balanceNode), Black);
                        SetColor(sibling, Black);
                        SetColor(ParentOf(ParentOf(balanceNode)), Red);
                        balanceNode = ParentOf(ParentOf(balanceNode));
                    }
                    else
                    {
                        if (balanceNode == LeftOf(ParentOf(balanceNode)))
                        {
                            balanceNode = ParentOf(balanceNode);
                            RotateRight(balanceNode);
                        }
                        SetColor(ParentOf(balanceNode), Black);
                        SetColor(ParentOf(ParentOf(balanceNode)), Red);
                        RotateLeft(ParentOf(ParentOf(balanceNode)));
                    }
                }
            }
            SetColor(_root, Black);
        }

        private void RotateLeft(IntervalNode<T> node)
        {
            if (node != null)
            {
                IntervalNode<T> right = RightOf(node);
                node.Right = LeftOf(right);
                if (LeftOf(right) != null)
                {
                    LeftOf(right).Parent = node;
                }
                right.Parent = ParentOf(node);
                if (ParentOf(node) == null)
                {
                    _root = right;
                }
                else if (node == LeftOf(ParentOf(node)))
                {
                    ParentOf(node).Left = right;
                }
                else
                {
                    ParentOf(node).Right = right;
                }
                right.Left = node;
                node.Parent = right;
            }
        }

        private void RotateRight(IntervalNode<T> node)
        {
            if (node != null)
            {
                IntervalNode<T> left = LeftOf(node);
                node.Left = RightOf(left);
                if (RightOf(left) != null)
                {
                    RightOf(left).Parent = node;
                }
                left.Parent = node.Parent;
                if (ParentOf(node) == null)
                {
                    _root = left;
                }
                else if (node == RightOf(ParentOf(node)))
                {
                    ParentOf(node).Right = left;
                }
                else
                {
                    ParentOf(node).Left = left;
                }
                left.Right = node;
                node.Parent = left;
            }
        }
        #endregion

        #region Safety-Methods

        // These methods save memory by allowing us to forego sentinel nil nodes, as well as serve as protection against nullpointerexceptions.

        /// <summary>
        /// Returns the color of <paramref name="node"/>, or Black if it is null.
        /// </summary>
        /// <param name="node">IntervalNode<T></param>
        /// <returns>The boolean color of <paramref name="node"/>, or black if null</returns>
        private static bool ColorOf(IntervalNode<T> node)
        {
            return node == null || node.Color;
        }

        /// <summary>
        /// Sets the color of <paramref name="node"/> node to <paramref name="color"/>.
        /// <br></br>
        /// This method does nothing if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">IntervalNode<T> to set the color of</param>
        /// <param name="color">Color (Boolean)</param>
        private static void SetColor(IntervalNode<T> node, bool color)
        {
            if (node != null)
            {
                node.Color = color;
            }
        }

        /// <summary>
        /// This method returns the left node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">IntervalNode<T> to retrieve the left child from</param>
        /// <returns>Left child of <paramref name="node"/></returns>
        private static IntervalNode<T> LeftOf(IntervalNode<T> node)
        {
            return node?.Left;
        }

        /// <summary>
        /// This method returns the right node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">IntervalNode<T> to retrieve the right child from</param>
        /// <returns>Right child of <paramref name="node"/></returns>
        private static IntervalNode<T> RightOf(IntervalNode<T> node)
        {
            return node?.Right;
        }

        /// <summary>
        /// Returns the parent node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">IntervalNode<T> to retrieve the parent from</param>
        /// <returns>Parent of <paramref name="node"/></returns>
        private static IntervalNode<T> ParentOf(IntervalNode<T> node)
        {
            return node?.Parent;
        }
        #endregion

        #region Interface Implementations


        public int Count => _count;

        public void Clear()
        {
            _root = null;
            _count = 0;
        }
        #endregion

        /// <summary>
        /// Returns a sorted list of all the node keys / values in the tree.
        /// </summary>
        /// <returns>List of node keys</returns>
        private SortedList<T, ulong> GetKeyValues()
        {
            SortedList<T, ulong> set = new SortedList<T, ulong>();
            Queue<IntervalNode<T>> queue = new Queue<IntervalNode<T>>();
            if (_root != null)
            {
                queue.Enqueue(_root);
            }

            while (queue.Count > 0)
            {
                IntervalNode<T> node = queue.Dequeue();
                set.Add(node.Key, node.Value);
                if (null != node.Left)
                {
                    queue.Enqueue(node.Left);
                }
                if (null != node.Right)
                {
                    queue.Enqueue(node.Right);
                }
            }

            return set;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetKeyValues().Keys.GetEnumerator();
        }

    }

    internal struct Interval
    {
        internal ulong Address;
        internal ulong EndAddress;

        public Interval(ulong Address, ulong EndAddress)
        {
            this.Address = Address;
            this.EndAddress = EndAddress;
        }
    }
    /// <summary>
    /// Represents a node in the TreeDictionary which contains a key and value of generic type K and V, respectively.
    /// </summary>
    /// <typeparam name="K">Key of the node</typeparam>
    /// <typeparam name="V">Value of the node</typeparam>
    internal class IntervalNode<T> where T: IRange
    {
        internal bool Color = true;
        internal IntervalNode<T> Left = null;
        internal IntervalNode<T> Right = null;
        internal IntervalNode<T> Parent = null;
        internal T Key;
        internal ulong Value;
        internal ulong MaxInterval;

        public IntervalNode(T key, ulong value, IntervalNode<T> parent)
        {
            this.Key = key;
            this.Value = value;
            this.MaxInterval = value;
            this.Parent = parent;
        }
    }
}
