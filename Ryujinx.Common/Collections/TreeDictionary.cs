using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private Dictionary<K, Node<K,V>> _set  = new Dictionary<K,Node<K,V>>();
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
            if (!_set.ContainsKey(key)) return null;

            return _set[key];
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
            if (_set.ContainsKey(key))
            {
                _set[key].Value = value;
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
            if (!_set.ContainsKey(key))
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
        public Node<K, V> FloorNode(K key)
        {
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
        public Node<K, V> CeilingNode(K key)
        {
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

        public int Count => _set.Count;

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
            Node<K, V> node = _root;
            if (node == null)
            {
                _root = new Node<K, V>(key, value);
                _set[key] = _root;
                return;
            }

            Node<K, V> tmp = null;
            while(node != null)
            {
                tmp = node;
                node = key.CompareTo(node.Key) < 0 ? node.Left : node.Right;
            }
            Node<K, V> newNode = new Node<K, V>(key, value, tmp);
            if(tmp == null)
            {
                _root = newNode;
            }
            else
            {
                int cmp = key.CompareTo(tmp.Key);
                if(cmp < 0)
                {
                    tmp.Left = newNode;
                }
                else
                {
                    tmp.Right = newNode;
                }
            }
            _set[key] = newNode;
            RestoreBalanceAfterInsertion(newNode);
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

            _set.Remove(key);
            
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

                if (node.Color == Black)
                {
                    RestoreBalanceAfterRemoval(node);
                }
            }
            else if (node.Parent == null)
            {
                _root = null;
            }
            else
            {
                if (node.Color == Black)
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
        private Node<K,V> Maximum(Node<K,V> node)
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
        private Node<K, V> Minimum(Node<K, V> node)
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

        private void RestoreBalanceAfterRemoval(Node<K, V> node)
        {
            Node<K, V> tmp = node;
            
            while(tmp != _root && ColorOf(tmp) == Black)
            {
                if(tmp == tmp.Parent.Left)
                {
                    Node<K, V> sibling = tmp.Parent.Right;

                    if(ColorOf(sibling) == Red)
                    {
                        sibling.Color = Black;
                        tmp.Parent.Color = Red;
                        RotateLeft(tmp.Parent);
                        sibling = tmp.Parent.Right;
                    }
                    if(ColorOf(sibling.Left) == Black && ColorOf(sibling.Right) == Black)
                    {
                        sibling.Color = Red;
                        tmp = tmp.Parent;
                    }
                    else {
                        if (ColorOf(sibling.Right) == Black)
                        {
                            sibling.Left.Color = Black;
                            sibling.Color = Red;
                            RotateRight(sibling);
                            sibling = tmp.Parent.Right;
                        }
                        sibling.Color = ColorOf(tmp.Parent);
                        tmp.Parent.Color = Black;
                        sibling.Right.Color = Black;
                        RotateLeft(tmp.Parent);
                        tmp = _root;
                    }
                }
                else
                {
                    Node<K, V> sibling = tmp.Parent.Left;

                    if (ColorOf(sibling) == Red)
                    {
                        sibling.Color = Black;
                        tmp.Parent.Color = Red;
                        RotateRight(tmp.Parent);
                        sibling = tmp.Parent.Left;
                    }
                    if (ColorOf(sibling.Right) == Black && ColorOf(sibling.Left) == Black)
                    {
                        sibling.Color = Red;
                        tmp = tmp.Parent;
                    }
                    else
                    {
                        if (ColorOf(sibling.Left) == Black)
                        {
                            sibling.Right.Color = Black;
                            sibling.Color = Red;
                            RotateLeft(sibling);
                            sibling = tmp.Parent.Left;
                        }
                    }
                    sibling.Color = ColorOf(tmp.Parent);
                    tmp.Parent.Color = Black;
                    sibling.Left.Color = Black;
                    RotateRight(tmp.Parent);
                    tmp = _root;
                }
            }
            tmp.Color = Black;
        }

        private void RestoreBalanceAfterInsertion(Node<K, V> x)
        {
            x.Color = Red;
            while (x != null && x != _root && x.Parent.Color == Red)
            {
                if (x.Parent == x.Parent.Parent.Left)
                {
                    Node<K, V> y = x.Parent.Parent.Right;

                    if(ColorOf(y) == Red)
                    {
                        x.Parent.Color = Black;
                        x.Parent.Parent.Color = Red;
                        x = x.Parent.Parent;
                    }
                    else {
                        if (x == x.Parent.Right)
                        {
                            x = x.Parent;
                            RotateLeft(x);
                        }
                        x.Parent.Color = Black;
                        x.Parent.Parent.Color = Red;
                        RotateRight(x.Parent.Parent);
                    }
                }
                else
                {
                    Node<K, V> y = x.Parent.Parent.Left;

                    if (ColorOf(y) == Red)
                    {
                        x.Parent.Color = Black;
                        x.Parent.Parent.Color = Red;
                        x = x.Parent.Parent;
                    }
                    else
                    {
                        if (x == x.Parent.Left)
                        {
                            x = x.Parent;
                            RotateRight(x);
                        }
                        x.Parent.Color = Black;
                        x.Parent.Parent.Color = Red;
                        RotateLeft(x.Parent.Parent);
                    }
                }
            }
            _root.Color = Black;
        }

        private void RotateLeft(Node<K, V> node)
        {
            Node<K, V> right = node.Right;
            node.Right = right.Left;
            if(right.Left != null)
            {
                right.Left.Parent = node;
            }
            right.Parent = node.Parent;
            if(node.Parent == null)
            {
                _root = right;
            }
            else if(node == node.Parent.Left)
            {
                node.Parent.Left = right;
            }
            else
            {
                node.Parent.Right = right;
            }
            right.Left = node;
            node.Parent = right;
        }

        private void RotateRight(Node<K, V> node)
        {
            Node<K, V> left = node.Left;
            node.Left = left.Right;
            if (left.Right != null)
            {
                left.Parent.Right = node;
            }
            left.Parent = node.Parent;
            if (node.Parent == null)
            {
                _root = left;
            }
            else if (node == node.Parent.Right)
            {
                node.Parent.Right = left;
            }
            else
            {
                node.Parent.Left = left;
            }
            left.Right = node;
            node.Parent = left;
        }

        private bool ColorOf(Node<K,V> node)
        {
            return node != null ? node.Color : Black;
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
