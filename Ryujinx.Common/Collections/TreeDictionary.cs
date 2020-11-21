using System;
using System.Collections.Generic;

namespace Ryujinx.Common.Collections
{

    /// <summary>
    /// Hybrid-Type Dictionary that provides the ability for O(1) Lookups for keys that exist in the Dictionary, and O(logN) Lookups for keys immediately greater than or less than a specified key.
    /// </summary>
    /// <typeparam name="K">Key</typeparam>
    /// <typeparam name="V">Value</typeparam>
    public class TreeDictionary<K, V> where K : IComparable<K>
    {
        private const bool Black = true;
        private const bool Red   = false;
        private Node<K, V> _root = null;
        private readonly Dictionary<K, Node<K,V>> _dictionary  = new Dictionary<K,Node<K,V>>();
        public TreeDictionary() { }

        #region Public Methods

        /// <summary>
        /// Retrieve the node reference whose key is <paramref name="key"/>, or null if no such node exists.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Node reference in the tree</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Node<K, V> GetNode(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException($"{nameof(key)} may not be null");
            }
            // O(1) Lookup for keys
            if (!_dictionary.ContainsKey(key)) return null;

            return _dictionary[key];
        }

        /// <summary>
        /// Returns the value of the node whose key is <paramref name="key"/>, or the default value if no such node exists.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public V Get(K key)
        {
            if(key == null)
            {
                throw new ArgumentNullException($"{nameof(key)} may not be null");
            }

            Node<K, V> node = GetNode(key);

            if (node == null) { 
                return default; 
            }

            return node.Value;
        }

        /// <summary>
        /// Adds a new node into the tree whose key is <paramref name="key"/> key and value is <paramref name="value"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void Add(K key, V value)
        {
            if(null == key)
            {
                throw new ArgumentNullException($"{nameof(key)} may not be null");
            }
            if(null == value)
            {
                throw new ArgumentNullException($"{nameof(value)} may not be null");
            }

            // O(1) Overwrites
            if (_dictionary.ContainsKey(key))
            {
                _dictionary[key].Value = value;
            }
            else
            {
                Insert(key, value);
            }
        }

        /// <summary>
        /// Removes the node whose key is <paramref name="key"/> from the tree.
        /// </summary>
        /// <param name="key">Key</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Remove(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException($"{nameof(key)} may not be null");
            }
            if (!_dictionary.ContainsKey(key))
            {
                return;
            }
            Delete(key);
        }

        /// <summary>
        /// Returns the node whose key is equal to or immediately less than <paramref name="key"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Node<K, V> FloorNode(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException($"{nameof(key)} may not be null");
            }
            Node<K, V> tmp = _root;

            while(tmp != null)
            {
                int cmp = key.CompareTo(tmp.Key);
                if(cmp > 0)
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
                else if(cmp < 0)
                {
                    if(tmp.Left != null)
                    {
                        tmp = tmp.Left;
                    }
                    else
                    {
                        Node<K, V> parent = tmp.Parent;
                        Node<K, V> ptr = tmp;
                        while(parent != null && ptr == parent.Left)
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
        /// Returns the node whose key is equal to or immediately greater than <paramref name="key"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Node<K, V> CeilingNode(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException($"{nameof(key)} may not be null");
            }
            Node<K, V> tmp = _root;

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
                        Node<K, V> parent = tmp.Parent;
                        Node<K, V> ptr = tmp;
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
        /// Finds the node with the key immediately greater than <paramref name="node"/>.Key
        /// </summary>
        /// <param name="node">Node to find the successor of</param>
        /// <returns>Node</returns>
        public Node<K, V> SuccessorOf(Node<K, V> node)
        {
            if(node.Right != null)
            {
                return Minimum(node.Right);
            }
            Node<K, V> parent = node.Parent;
            while(parent != null && node == parent.Right)
            {
                node = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        /// <summary>
        /// Finds the node with the key immediately less than <paramref name="node"/>.Key
        /// </summary>
        /// <param name="node">Node to find the predecessor of</param>
        /// <returns>Node</returns>
        public Node<K, V> PredecessorOf(Node<K, V> node)
        {
            if(node.Left != null)
            {
                return Maximum(node.Left);
            }
            Node<K, V> parent = node.Parent;
            while(parent != null && node == parent.Left)
            {
                node = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        public int Count => _dictionary.Count;

        #endregion
        #region Private Methods (BST)

        /// <summary>
        /// Inserts a new node into the tree whose key is <paramref name="key"/> and value is <paramref name="value"/>
        /// <br></br>
        /// Adding the same key multiple times will overwrite the previous value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void Insert(K key, V value)
        {
            Node<K, V> newNode = BSTInsert(key, value);
            RestoreBalanceAfterInsertion(newNode);
        }

        /// <summary>
        /// Standard Insertion Mechanism for a Binary Search Tree (BST)
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Node</returns>
        private Node<K,V> BSTInsert(K key, V value)
        {
            Node<K, V> parent = null;
            Node<K, V> node = _root;

            while(node != null)
            {
                parent = node;
                node = key.CompareTo(node.Key) < 0 ? node.Left : node.Right;
            }
            Node<K, V> newNode = new Node<K, V>(key, value, parent);
            if(newNode.Parent == null)
            {
                _root = newNode;
            }
            else if(key.CompareTo(parent.Key) < 0)
            {
                parent.Left = newNode;
            }
            else
            {
                parent.Right = newNode;
            }
            _dictionary[key] = newNode;
            return newNode;
        }

        /// <summary>
        /// Removes <paramref name="key"/> from the dictionary, if it exists.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private Node<K, V> Delete(K key)
        {
            // O(1) Retrieval
            Node<K, V> node = GetNode(key);
            if (node == null) return null;

            _dictionary.Remove(key);
            
            if(!(node.Left == null || node.Right == null))
            {
                Node<K, V> tmpNode = SuccessorOf(node);
                node.Key = tmpNode.Key;
                node.Value = tmpNode.Value;
                node = tmpNode;
            }

            Node<K, V> tmp = node.Left ?? node.Right;
            if (tmp != null)
            {
                tmp.Parent = node.Parent;
                if (node.Parent == null)
                {
                    _root = tmp;
                }
                else if (node == node.Parent.Left)
                {
                    node.Parent.Left = tmp;
                }
                else
                {
                    node.Parent.Right = tmp;
                }

                node.Left = null;
                node.Right = null;
                node.Parent = null;

                if (ColorOf(node) == Black)
                {
                    RestoreBalanceAfterRemoval(tmp);
                }
            }
            else if (node.Parent == null)
            {
                _root = null;
            }
            else
            {
                if (ColorOf(node) == Black)
                {
                    RestoreBalanceAfterRemoval(node);
                }

                if (node.Parent != null)
                {
                    if (node == node.Parent.Left)
                    {
                        node.Parent.Left = null;
                    }
                    else if (node == node.Parent.Right)
                    {
                        node.Parent.Right = null;
                    }
                    node.Parent = null;
                }
            }
            return node;
        }

        /// <summary>
        /// Returns the node with the largest key where <paramref name="node"/> is considered the root node.
        /// </summary>
        /// <param name="node">Root Node</param>
        /// <returns>Node</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static Node<K,V> Maximum(Node<K,V> node)
        {
            if(node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            Node<K, V> tmp = node;
            while(tmp.Right != null)
            {
                tmp = tmp.Right;
            }

            return tmp;
        }

        /// <summary>
        /// Returns the node with the smallest key where <paramref name="node"/> is considered the root node.
        /// </summary>
        /// <param name="node">Root Node</param>
        /// <returns></returns>
        ///<exception cref="ArgumentNullException"></exception>
        private static Node<K, V> Minimum(Node<K, V> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            Node<K, V> tmp = node;
            while (tmp.Left != null)
            {
                tmp = tmp.Left;
            }

            return tmp;
        }
        #endregion
        #region Private Methods (RBL)

        private void RestoreBalanceAfterRemoval(Node<K, V> balanceNode)
        {
            Node<K, V> node = balanceNode;
            
            while(node != _root && ColorOf(node) == Black)
            {
                if(node == LeftOf(ParentOf(node)))
                {
                    Node<K, V> sibling = RightOf(ParentOf(node));

                    if(ColorOf(sibling) == Red)
                    {
                        SetColor(sibling, Black);
                        SetColor(ParentOf(node), Red);
                        RotateLeft(ParentOf(node));
                        sibling = RightOf(ParentOf(node));
                    }
                    if(ColorOf(LeftOf(sibling)) == Black && ColorOf(RightOf(sibling)) == Black)
                    {
                        SetColor(sibling, Red);
                        node = ParentOf(node);
                    }
                    else {
                        if (ColorOf(RightOf(sibling)) == Black)
                        {
                            SetColor(LeftOf(sibling),  Black);
                            SetColor(sibling, Red);
                            RotateRight(sibling);
                            sibling = RightOf(ParentOf(node));
                        }
                        SetColor(sibling, ColorOf(ParentOf(node)));
                        SetColor(ParentOf(node), Black);
                        SetColor(RightOf(sibling), Black);
                        RotateLeft(ParentOf(node));
                        node = _root;
                    }
                }
                else
                {
                    Node<K, V> sibling = LeftOf(ParentOf(node));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(sibling, Black);
                        SetColor(ParentOf(node), Red);
                        RotateRight(ParentOf(node));
                        sibling = LeftOf(ParentOf(node));
                    }
                    if (ColorOf(RightOf(sibling)) == Black && ColorOf(LeftOf(sibling)) == Black)
                    {
                        SetColor(sibling, Red);
                        node = ParentOf(node);
                    }
                    else
                    {
                        if (ColorOf(LeftOf(sibling)) == Black)
                        {
                            SetColor(RightOf(sibling), Black);
                            SetColor(sibling, Red);
                            RotateLeft(sibling);
                            sibling = LeftOf(ParentOf(node));
                        }
                        SetColor(sibling, ColorOf(ParentOf(node)));
                        SetColor(ParentOf(node), Black);
                        SetColor(LeftOf(sibling), Black);
                        RotateRight(ParentOf(node));
                        node = _root;
                    }
                }
            }
            SetColor(node, Black);
        }

        private void RestoreBalanceAfterInsertion(Node<K, V> insertedNode)
        {
            SetColor(insertedNode, Red);
            while (insertedNode != null && insertedNode != _root && ColorOf(ParentOf(insertedNode)) == Red)
            {
                if (ParentOf(insertedNode) == LeftOf(ParentOf(ParentOf(insertedNode))))
                {
                    Node<K, V> y = RightOf(ParentOf(ParentOf(insertedNode)));

                    if(ColorOf(y) == Red)
                    {
                        SetColor(ParentOf(insertedNode),  Black);
                        SetColor(ParentOf(ParentOf(insertedNode)),  Red);
                        insertedNode = ParentOf(ParentOf(insertedNode));
                    }
                    else {
                        if (insertedNode == RightOf(ParentOf(insertedNode)))
                        {
                            insertedNode = ParentOf(insertedNode);
                            RotateLeft(insertedNode);
                        }
                        SetColor(ParentOf(insertedNode),  Black);
                        SetColor(ParentOf(ParentOf(insertedNode)),  Red);
                        RotateRight(ParentOf(ParentOf(insertedNode)));
                    }
                }
                else
                {
                    Node<K, V> y = LeftOf(ParentOf(ParentOf(insertedNode)));

                    if (ColorOf(y) == Red)
                    {
                        SetColor(ParentOf(insertedNode),  Black);
                        SetColor(ParentOf(ParentOf(insertedNode)),  Red);
                        insertedNode = ParentOf(ParentOf(insertedNode));
                    }
                    else
                    {
                        if (insertedNode == LeftOf(ParentOf(insertedNode)))
                        {
                            insertedNode = ParentOf(insertedNode);
                            RotateRight(insertedNode);
                        }
                        SetColor(ParentOf(insertedNode),  Black);
                        SetColor(ParentOf(ParentOf(insertedNode)),  Red);
                        RotateLeft(ParentOf(ParentOf(insertedNode)));
                    }
                }
            }
            SetColor(_root,  Black);
        }

        private void RotateLeft(Node<K, V> node)
        {
            if (node != null)
            {
                Node<K, V> right = RightOf(node);
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

        private void RotateRight(Node<K, V> node)
        {
            if (node != null)
            {
                Node<K, V> left = LeftOf(node);
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
        /// <param name="node">Node</param>
        /// <returns>Boolean</returns>
        private static bool ColorOf(Node<K,V> node)
        {
            return node == null || node.Color;
        }

        /// <summary>
        /// Sets the color of <paramref name="node"/> node to <paramref name="color"/>
        /// <br></br>
        /// This method does nothing if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node</param>
        /// <param name="color">Color (Boolean)</param>
        private static void SetColor(Node<K, V> node, bool color)
        {
            if(node != null)
            {
                node.Color = color;
            }
        }

        /// <summary>
        /// This method returns the left node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>Left Node</returns>
        private static Node<K,V> LeftOf(Node<K, V> node)
        {
            return node?.Left;
        }

        /// <summary>
        /// This method returns the right node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>Right Node</returns>
        private static Node<K, V> RightOf(Node<K, V> node)
        {
            return node?.Right;
        }

        /// <summary>
        /// This method returns the parent node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>Parent Node</returns>
        private static Node<K, V> ParentOf(Node<K, V> node)
        {
            return node?.Parent;
        }
        #endregion
    }

    public class Node<K, V>
    {
        internal bool Color        = true;
        internal Node<K, V> Left   = null;
        internal Node<K, V> Right  = null;
        internal Node<K, V> Parent = null;
        public K Key;
        public V Value;

        public Node(K key, V value)
        {
            this.Key = key;
            this.Value = value;
        }

        public Node(K key, V value, Node<K, V> parent)
        {
            this.Key = key;
            this.Value = value;
            this.Parent = parent;
        }
    }
}
