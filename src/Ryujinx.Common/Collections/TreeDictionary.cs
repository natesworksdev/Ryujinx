using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Common.Collections
{
    /// <summary>
    /// Dictionary that provides the ability for O(logN) Lookups for keys that exist in the Dictionary, and O(logN) lookups for keys immediately greater than or less than a specified key.
    /// </summary>
    /// <typeparam name="TK">Key</typeparam>
    /// <typeparam name="TV">Value</typeparam>
    public class TreeDictionary<TK, TV> : IntrusiveRedBlackTreeImpl<Node<TK, TV>>, IDictionary<TK, TV> where TK : IComparable<TK>
    {
        #region Public Methods

        /// <summary>
        /// Returns the value of the node whose key is <paramref name="key"/>, or the default value if no such node exists.
        /// </summary>
        /// <param name="key">Key of the node value to get</param>
        /// <returns>Value associated w/ <paramref name="key"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        public TV Get(TK key)
        {
            ArgumentNullException.ThrowIfNull(key);

            Node<TK, TV> node = GetNode(key);

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
        public void Add(TK key, TV value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);

            Insert(key, value);
        }

        /// <summary>
        /// Removes the node whose key is <paramref name="key"/> from the tree.
        /// </summary>
        /// <param name="key">Key of the node to remove</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        public void Remove(TK key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (Delete(key) != null)
            {
                Count--;
            }
        }

        /// <summary>
        /// Returns the value whose key is equal to or immediately less than <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key for which to find the floor value of</param>
        /// <returns>Key of node immediately less than <paramref name="key"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        public TK Floor(TK key)
        {
            Node<TK, TV> node = FloorNode(key);
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
        public TK Ceiling(TK key)
        {
            Node<TK, TV> node = CeilingNode(key);
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
        public TK SuccessorOf(TK key)
        {
            Node<TK, TV> node = GetNode(key);
            if (node != null)
            {
                Node<TK, TV> successor = SuccessorOf(node);

                return successor != null ? successor.Key : default;
            }
            return default;
        }

        /// <summary>
        /// Finds the value whose key is immediately less than <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key to find the predecessor of</param>
        /// <returns>Value</returns>
        public TK PredecessorOf(TK key)
        {
            Node<TK, TV> node = GetNode(key);
            if (node != null)
            {
                Node<TK, TV> predecessor = PredecessorOf(node);

                return predecessor != null ? predecessor.Key : default;
            }
            return default;
        }

        /// <summary>
        /// Adds all the nodes in the dictionary as key/value pairs into <paramref name="list"/>.
        /// <br></br>
        /// The key/value pairs will be added in Level Order.
        /// </summary>
        /// <param name="list">List to add the tree pairs into</param>
        public List<KeyValuePair<TK, TV>> AsLevelOrderList()
        {
            List<KeyValuePair<TK, TV>> list = new();

            Queue<Node<TK, TV>> nodes = new();

            if (this.Root != null)
            {
                nodes.Enqueue(this.Root);
            }
            while (nodes.TryDequeue(out Node<TK, TV> node))
            {
                list.Add(new KeyValuePair<TK, TV>(node.Key, node.Value));
                if (node.Left != null)
                {
                    nodes.Enqueue(node.Left);
                }
                if (node.Right != null)
                {
                    nodes.Enqueue(node.Right);
                }
            }
            return list;
        }

        /// <summary>
        /// Adds all the nodes in the dictionary into <paramref name="list"/>.
        /// </summary>
        /// <returns>A list of all KeyValuePairs sorted by Key Order</returns>
        public List<KeyValuePair<TK, TV>> AsList()
        {
            List<KeyValuePair<TK, TV>> list = new();

            AddToList(Root, list);

            return list;
        }

        #endregion

        #region Private Methods (BST)

        /// <summary>
        /// Adds all nodes that are children of or contained within <paramref name="node"/> into <paramref name="list"/>, in Key Order.
        /// </summary>
        /// <param name="node">The node to search for nodes within</param>
        /// <param name="list">The list to add node to</param>
        private void AddToList(Node<TK, TV> node, List<KeyValuePair<TK, TV>> list)
        {
            if (node == null)
            {
                return;
            }

            AddToList(node.Left, list);

            list.Add(new KeyValuePair<TK, TV>(node.Key, node.Value));

            AddToList(node.Right, list);
        }

        /// <summary>
        /// Retrieve the node reference whose key is <paramref name="key"/>, or null if no such node exists.
        /// </summary>
        /// <param name="key">Key of the node to get</param>
        /// <returns>Node reference in the tree</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        private Node<TK, TV> GetNode(TK key)
        {
            ArgumentNullException.ThrowIfNull(key);

            Node<TK, TV> node = Root;
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
        private void Insert(TK key, TV value)
        {
            Node<TK, TV> newNode = BSTInsert(key, value);
            RestoreBalanceAfterInsertion(newNode);
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
        /// <returns>The inserted Node</returns>
        private Node<TK, TV> BSTInsert(TK key, TV value)
        {
            Node<TK, TV> parent = null;
            Node<TK, TV> node = Root;

            while (node != null)
            {
                parent = node;
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
            Node<TK, TV> newNode = new(key, value, parent);
            if (newNode.Parent == null)
            {
                Root = newNode;
            }
            else if (key.CompareTo(parent.Key) < 0)
            {
                parent.Left = newNode;
            }
            else
            {
                parent.Right = newNode;
            }
            Count++;
            return newNode;
        }

        /// <summary>
        /// Removes <paramref name="key"/> from the dictionary, if it exists.
        /// </summary>
        /// <param name="key">Key of the node to delete</param>
        /// <returns>The deleted Node</returns>
        private Node<TK, TV> Delete(TK key)
        {
            // O(1) Retrieval
            Node<TK, TV> nodeToDelete = GetNode(key);

            if (nodeToDelete == null)
            {
                return null;
            }

            Node<TK, TV> replacementNode;

            if (LeftOf(nodeToDelete) == null || RightOf(nodeToDelete) == null)
            {
                replacementNode = nodeToDelete;
            }
            else
            {
                replacementNode = PredecessorOf(nodeToDelete);
            }

            Node<TK, TV> tmp = LeftOf(replacementNode) ?? RightOf(replacementNode);

            if (tmp != null)
            {
                tmp.Parent = ParentOf(replacementNode);
            }

            if (ParentOf(replacementNode) == null)
            {
                Root = tmp;
            }
            else if (replacementNode == LeftOf(ParentOf(replacementNode)))
            {
                ParentOf(replacementNode).Left = tmp;
            }
            else
            {
                ParentOf(replacementNode).Right = tmp;
            }

            if (replacementNode != nodeToDelete)
            {
                nodeToDelete.Key = replacementNode.Key;
                nodeToDelete.Value = replacementNode.Value;
            }

            if (tmp != null && ColorOf(replacementNode) == Black)
            {
                RestoreBalanceAfterRemoval(tmp);
            }

            return replacementNode;
        }

        /// <summary>
        /// Returns the node whose key immediately less than or equal to <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key for which to find the floor node of</param>
        /// <returns>Node whose key is immediately less than or equal to <paramref name="key"/>, or null if no such node is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        private Node<TK, TV> FloorNode(TK key)
        {
            ArgumentNullException.ThrowIfNull(key);

            Node<TK, TV> tmp = Root;

            while (tmp != null)
            {
                int cmp = key.CompareTo(tmp.Key);
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
                        Node<TK, TV> parent = tmp.Parent;
                        Node<TK, TV> ptr = tmp;
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
        /// <returns>Node whose key is immediately greater than or equal to <paramref name="key"/>, or null if no such node is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        private Node<TK, TV> CeilingNode(TK key)
        {
            ArgumentNullException.ThrowIfNull(key);

            Node<TK, TV> tmp = Root;

            while (tmp != null)
            {
                int cmp = key.CompareTo(tmp.Key);
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
                        Node<TK, TV> parent = tmp.Parent;
                        Node<TK, TV> ptr = tmp;
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

        #endregion

        #region Interface Implementations

        // Method descriptions are not provided as they are already included as part of the interface.
        public bool ContainsKey(TK key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return GetNode(key) != null;
        }

        bool IDictionary<TK, TV>.Remove(TK key)
        {
            int count = Count;
            Remove(key);
            return count > Count;
        }

        public bool TryGetValue(TK key, [MaybeNullWhen(false)] out TV value)
        {
            ArgumentNullException.ThrowIfNull(key);

            Node<TK, TV> node = GetNode(key);
            value = node != null ? node.Value : default;
            return node != null;
        }

        public void Add(KeyValuePair<TK, TV> item)
        {
            ArgumentNullException.ThrowIfNull(item.Key);

            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TK, TV> item)
        {
            if (item.Key == null)
            {
                return false;
            }

            Node<TK, TV> node = GetNode(item.Key);
            if (node != null)
            {
                return node.Key.Equals(item.Key) && node.Value.Equals(item.Value);
            }
            return false;
        }

        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || array.Length - arrayIndex < this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            SortedList<TK, TV> list = GetKeyValues();

            int offset = 0;

            for (int i = arrayIndex; i < array.Length && offset < list.Count; i++)
            {
                array[i] = new KeyValuePair<TK, TV>(list.Keys[i], list.Values[i]);
                offset++;
            }
        }

        public bool Remove(KeyValuePair<TK, TV> item)
        {
            Node<TK, TV> node = GetNode(item.Key);

            if (node == null)
            {
                return false;
            }

            if (node.Value.Equals(item.Value))
            {
                int count = Count;
                Remove(item.Key);
                return count > Count;
            }

            return false;
        }

        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator()
        {
            return GetKeyValues().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetKeyValues().GetEnumerator();
        }

        public ICollection<TK> Keys => GetKeyValues().Keys;

        public ICollection<TV> Values => GetKeyValues().Values;

        public bool IsReadOnly => false;

        public TV this[TK key]
        {
            get => Get(key);
            set => Add(key, value);
        }

        #endregion

        #region Private Interface Helper Methods

        /// <summary>
        /// Returns a sorted list of all the node keys / values in the tree.
        /// </summary>
        /// <returns>List of node keys</returns>
        private SortedList<TK, TV> GetKeyValues()
        {
            SortedList<TK, TV> set = new();
            Queue<Node<TK, TV>> queue = new();
            if (Root != null)
            {
                queue.Enqueue(Root);
            }

            while (queue.TryDequeue(out Node<TK, TV> node))
            {
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

        #endregion
    }

    /// <summary>
    /// Represents a node in the TreeDictionary which contains a key and value of generic type K and V, respectively.
    /// </summary>
    /// <typeparam name="TK">Key of the node</typeparam>
    /// <typeparam name="TV">Value of the node</typeparam>
    public class Node<TK, TV> : IntrusiveRedBlackTreeNode<Node<TK, TV>> where TK : IComparable<TK>
    {
        internal TK Key;
        internal TV Value;

        internal Node(TK key, TV value, Node<TK, TV> parent)
        {
            Key = key;
            Value = value;
            Parent = parent;
        }
    }
}
