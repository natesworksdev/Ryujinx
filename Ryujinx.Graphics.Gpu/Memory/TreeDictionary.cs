using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Graphics.Gpu.Memory
{

    class TreeDictionary<K, V> where K : IComparable<K>
    {
        private TreeNode<K, V> root;

        public TreeDictionary()
        {
            this.root = null;
        }

        public TreeDictionary(K key, V value)
        {
            this.root = new TreeNode<K, V>(key, value);
        }

        public TreeNode<K, V> Floor(K key)
        {
            return Floor(root, key);
        }

        private TreeNode<K, V> Floor(TreeNode<K, V> node, K key)
        {
            if (node == null) return default;
            if (node.Key.Equals(key)) return node;
            int compare = key.CompareTo(node.Key);
            if(compare > 0)
            {
                if(node.Right != null)
                {
                    return Floor(node.Right, key);
                }
                else
                {
                    return node;
                }
            }
            else
            {
                if(node.Left != null)
                {
                    return Floor(node.Left, key);
                }
                return node;
            }
        }

        public TreeNode<K, V> Ceiling(K key)
        {
            return Ceiling(root, key);
        }

        private TreeNode<K, V> Ceiling(TreeNode<K, V> node, K key)
        {
            if (node == null) return default;
            if (node.Key.Equals(key)) return node;
            int compare = key.CompareTo(node.Key);
            if (compare < 0)
            {
                if (node.Left != null)
                {
                    return Ceiling(node.Left, key);
                }
                else
                {
                    return node;
                }
            }
            else
            {
                if (node.Right != null)
                {
                    return Ceiling(node.Right, key);
                }
                return node;
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

        public V Get(K key)
        {
            return Get(root, key);
        }

        private TreeNode<K, V> Add(TreeNode<K, V> node, K key, V value)
        {
            {
                if (node == null)
                {
                    TreeNode<K, V> n = new TreeNode<K, V>(key, value);
                    return n;
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

        private TreeNode<K, V> Remove(TreeNode<K, V> node, K key)
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
                    TreeNode<K, V> smallestNode = findSmallestKey(node.Right);
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

        private TreeNode<K, V> findSmallestKey(TreeNode<K, V> node)
        {
            return node.Left == null ? node : findSmallestKey(node.Left);
        }

        private V Get(TreeNode<K,V> node, K key)
        {
            if (node == null) return default;
            if (key.Equals(node.Key)) return node.Value;
            return key.CompareTo(node.Key) < 0 ? Get(node.Left, key) : Get(node.Right, key);
        }

        
    }
    public class TreeNode<E, C>
    {
        public TreeNode<E, C> Left;
        public TreeNode<E, C> Right;
        public E Key;
        public C Value;

        public TreeNode(E key, C value)
        {
            this.Key = key;
            this.Value = value;
            this.Left = null;
            this.Right = null;
        }
    }
}
