using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Graphics.Gpu.Memory
{

    class TreeDictionary<K, V> where K : IComparable<K>
    {
        private Node root;

        public TreeDictionary()
        {
            this.root = null;
        }

        public TreeDictionary(K key, V value)
        {
            this.root = new Node(key, value);
        }


        public K Floor(K key)
        {
            return Floor(root, key);
        }

        private K Floor(Node node, K key)
        {
            if (node == null) return default;
            if (node.Key.Equals(key)) return key;
            int compare = key.CompareTo(node.Key);
            if(compare > 0)
            {
                if(node.Right != null)
                {
                    return Floor(node.Right, key);
                }
                else
                {
                    return node.Key;
                }
            }
            else
            {
                if(node.Left != null)
                {
                    return Floor(node.Left, key);
                }
                return node.Key;
            }
        }

        public K Ceiling(K key)
        {
            return Ceiling(root, key);
        }

        private K Ceiling(Node node, K key)
        {
            if (node == null) return default;
            if (node.Key.Equals(key)) return key;
            int compare = key.CompareTo(node.Key);
            if (compare < 0)
            {
                if (node.Left != null)
                {
                    return Ceiling(node.Left, key);
                }
                else
                {
                    return node.Key;
                }
            }
            else
            {
                if (node.Right != null)
                {
                    return Ceiling(node.Right, key);
                }
                return node.Key;
            }
        }

        public void Add(K key, V value)
        {
            root = Add(root, key, value);
        }

        public void Remove(K key)
        {
            Remove(root, key);
        }

        public bool Contains(K key)
        {
            return Contains(root, key);
        }

        private Node Add(Node node, K key, V value)
        {
            {
                if (node == null)
                {
                    return new Node(key, value);
                }
                int cmp = key.CompareTo(node.Key);
                if (cmp < 0)
                {
                    node.Left = Add(node.Left, key, value);
                }
                else if (cmp > 0)
                {
                    node.Right = Add(node.Right, key, value);
                }
                else
                {
                    return node;
                }

                return node;
            }
        }

        private Node Remove(Node node, K key)
        {
            if (node == null) return null;
            if(key.Equals(node.Key))
            {
                if (node.Left == null && node.Right == null)
                {
                    return null;
                }
                else if(node.Left == null)
                {
                    return node.Right;
                }
                else if(node.Right == null)
                {
                    return node.Left;
                }
                else
                {
                    Node smallestNode = findSmallestKey(node.Right);
                    node.Key = smallestNode.Key;
                    node.Value = smallestNode.Value;
                    node.Right = Remove(node.Right, smallestNode.Key);
                    return node;
                }
            }
            if(key.CompareTo(node.Key) < 0)
            {
                node.Left = Remove(node.Left, key);
                return node;
            }
            node.Right = Remove(node.Right, key);
            return node;
        }

        private Node findSmallestKey(Node node)
        {
            return node.Left == null ? node : findSmallestKey(node.Left);
        }

        private bool Contains(Node node, K key)
        {
            if (node == null) return false;
            if (key.Equals(node.Key)) return true;
            return key.CompareTo(node.Key) < 0 ? Contains(node.Left, key) : Contains(node.Right, key);
        }

        protected class Node
        {
            public Node Left;
            public Node Right;
            public K Key;
            public V Value;

            public Node(K key, V value)
            {
                this.Key = key;
                this.Value = value;
                this.Left = null;
                this.Right = null;
            }
        }
    }
}
