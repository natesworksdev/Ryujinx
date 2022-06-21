using System;

namespace Ryujinx.Common.Collections
{
    /// <summary>
    /// Tree that provides the ability for O(logN) lookups for keys that exist in the tree, and O(logN) lookups for keys immediately greater than or less than a specified key.
    /// </summary>
    /// <typeparam name="T">Derived node type</typeparam>
    public class IntrusiveRedBlackTree<T> where T : IntrusiveRedBlackTreeNode<T>, IComparable<T>
    {
        private const bool Black = true;
        private const bool Red = false;
        private T _root = null;
        private int _count = 0;

        /// <summary>
        /// Number of nodes on the tree.
        /// </summary>
        public int Count => _count;

        #region Public Methods

        /// <summary>
        /// Adds a new node into the tree.
        /// </summary>
        /// <param name="node">Node to be added</param>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is null</exception>
        public void Add(T node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Insert(node);
        }

        /// <summary>
        /// Removes a node from the tree.
        /// </summary>
        /// <param name="node">Note to be removed</param>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is null</exception>
        public void Remove(T node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (Delete(node) != null)
            {
                _count--;
            }
        }

        /// <summary>
        /// Retrieve the node that is considered equal to the specified node by the comparator.
        /// </summary>
        /// <param name="searchNode">Node to compare with</param>
        /// <returns>Node that is equal to <paramref name="searchNode"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchNode"/> is null</exception>
        public T GetNode(T searchNode)
        {
            if (searchNode == null)
            {
                throw new ArgumentNullException(nameof(searchNode));
            }

            T node = _root;
            while (node != null)
            {
                int cmp = searchNode.CompareTo(node);
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

        #endregion

        #region Private Methods (BST)

        /// <summary>
        /// Inserts a new node into the tree.
        /// </summary>
        /// <param name="node">Node to be inserted</param>
        private void Insert(T node)
        {
            T newNode = BSTInsert(node);
            RestoreBalanceAfterInsertion(newNode);
        }

        /// <summary>
        /// Insertion Mechanism for a Binary Search Tree (BST).
        /// <br></br>
        /// Iterates the tree starting from the root and inserts a new node
        /// where all children in the left subtree are less than <paramref name="newNode"/>,
        /// and all children in the right subtree are greater than <paramref name="newNode"/>.
        /// </summary>
        /// <param name="newNode">Node to be inserted</param>
        /// <returns>The inserted Node</returns>
        private T BSTInsert(T newNode)
        {
            T parent = null;
            T node = _root;

            while (node != null)
            {
                parent = node;
                int cmp = newNode.CompareTo(node);
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
            newNode.Parent = parent;
            if (parent == null)
            {
                _root = newNode;
            }
            else if (newNode.CompareTo(parent) < 0)
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
        /// Removes <paramref name="nodeToDelete"/> from the tree, if it exists.
        /// </summary>
        /// <param name="nodeToDelete">Node to be removed</param>
        /// <returns>The deleted Node</returns>
        private T Delete(T nodeToDelete)
        {
            if (nodeToDelete == null)
            {
                return null;
            }

            T old = nodeToDelete;
            T child;
            T parent;
            bool color;

            if (LeftOf(nodeToDelete) == null)
            {
                child = RightOf(nodeToDelete);
            }
            else if (RightOf(nodeToDelete) == null)
            {
                child = LeftOf(nodeToDelete);
            }
            else
            {
                T element = Minimum(RightOf(nodeToDelete));

                child = RightOf(element);
                parent = ParentOf(element);
                color = ColorOf(element);

                if (child != null)
                {
                    child.Parent = parent;
                }

                if (parent == null)
                {
                    _root = child;
                }
                else if (element == LeftOf(parent))
                {
                    parent.Left = child;
                }
                else
                {
                    parent.Right = child;
                }

                if (ParentOf(element) == old)
                {
                    parent = element;
                }

                element.Color = old.Color;
                element.Left = old.Left;
                element.Right = old.Right;
                element.Parent = old.Parent;

                if (ParentOf(old) == null)
                {
                    _root = element;
                }
                else if (old == LeftOf(ParentOf(old)))
                {
                    ParentOf(old).Left = element;
                }
                else
                {
                    ParentOf(old).Right = element;
                }

                LeftOf(old).Parent = element;

                if (RightOf(old) != null)
                {
                    RightOf(old).Parent = element;
                }

                if (child != null && color == Black)
                {
                    RestoreBalanceAfterRemoval(child);
                }

                return old;
            }

            parent = ParentOf(nodeToDelete);
            color = ColorOf(nodeToDelete);

            if (child != null)
            {
                child.Parent = parent;
            }

            if (parent == null)
            {
                _root = child;
            }
            else if (nodeToDelete == LeftOf(parent))
            {
                parent.Left = child;
            }
            else
            {
                parent.Right = child;
            }

            if (child != null && color == Black)
            {
                RestoreBalanceAfterRemoval(child);
            }

            return old;
        }

        /// <summary>
        /// Returns the node with the largest key where <paramref name="node"/> is considered the root node.
        /// </summary>
        /// <param name="node">Root node</param>
        /// <returns>Node with the maximum key in the tree of <paramref name="node"/></returns>
        private static T Maximum(T node)
        {
            T tmp = node;
            while (tmp.Right != null)
            {
                tmp = tmp.Right;
            }

            return tmp;
        }

        /// <summary>
        /// Returns the node with the smallest key where <paramref name="node"/> is considered the root node.
        /// </summary>
        /// <param name="node">Root node</param>
        /// <returns>Node with the minimum key in the tree of <paramref name="node"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is null</exception>
        private static T Minimum(T node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            T tmp = node;
            while (tmp.Left != null)
            {
                tmp = tmp.Left;
            }

            return tmp;
        }

        /// <summary>
        /// Returns the node whose key immediately less than or equal to <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node for which to find the floor node of</param>
        /// <returns>Node whose key is immediately less than or equal to <paramref name="node"/>, or null if no such node is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is null</exception>
        private T FloorNode(T node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            T tmp = _root;

            while (tmp != null)
            {
                int cmp = node.CompareTo(tmp);
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
                        T parent = tmp.Parent;
                        T ptr = tmp;
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
        /// Returns the node whose key is immediately greater than or equal to than <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node for which to find the ceiling node of</param>
        /// <returns>Node whose key is immediately greater than or equal to <paramref name="node"/>, or null if no such node is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is null</exception>
        private T CeilingNode(T node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            T tmp = _root;

            while (tmp != null)
            {
                int cmp = node.CompareTo(tmp);
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
                        T parent = tmp.Parent;
                        T ptr = tmp;
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
        /// Finds the node whose key is immediately greater than <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node to find the successor of</param>
        /// <returns>Successor of <paramref name="node"/></returns>
        public static T SuccessorOf(T node)
        {
            if (node.Right != null)
            {
                return Minimum(node.Right);
            }
            T parent = node.Parent;
            while (parent != null && node == parent.Right)
            {
                node = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        /// <summary>
        /// Finds the node whose key is immediately less than <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node to find the predecessor of</param>
        /// <returns>Predecessor of <paramref name="node"/></returns>
        public static T PredecessorOf(T node)
        {
            if (node.Left != null)
            {
                return Maximum(node.Left);
            }
            T parent = node.Parent;
            while (parent != null && node == parent.Left)
            {
                node = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        #endregion

        #region Private Methods (RBL)

        private void RestoreBalanceAfterRemoval(T balanceNode)
        {
            T ptr = balanceNode;

            while (ptr != _root && ColorOf(ptr) == Black)
            {
                if (ptr == LeftOf(ParentOf(ptr)))
                {
                    T sibling = RightOf(ParentOf(ptr));

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
                    T sibling = LeftOf(ParentOf(ptr));

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

        private void RestoreBalanceAfterInsertion(T balanceNode)
        {
            SetColor(balanceNode, Red);
            while (balanceNode != null && balanceNode != _root && ColorOf(ParentOf(balanceNode)) == Red)
            {
                if (ParentOf(balanceNode) == LeftOf(ParentOf(ParentOf(balanceNode))))
                {
                    T sibling = RightOf(ParentOf(ParentOf(balanceNode)));

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
                    T sibling = LeftOf(ParentOf(ParentOf(balanceNode)));

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

        private void RotateLeft(T node)
        {
            if (node != null)
            {
                T right = RightOf(node);
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

        private void RotateRight(T node)
        {
            if (node != null)
            {
                T left = LeftOf(node);
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

        // These methods save memory by allowing us to forego sentinel nil nodes, as well as serve as protection against NullReferenceExceptions.

        /// <summary>
        /// Returns the color of <paramref name="node"/>, or Black if it is null.
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>The boolean color of <paramref name="node"/>, or black if null</returns>
        private static bool ColorOf(T node)
        {
            return node == null || node.Color;
        }

        /// <summary>
        /// Sets the color of <paramref name="node"/> node to <paramref name="color"/>.
        /// <br></br>
        /// This method does nothing if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node to set the color of</param>
        /// <param name="color">Color (Boolean)</param>
        private static void SetColor(T node, bool color)
        {
            if (node != null)
            {
                node.Color = color;
            }
        }

        /// <summary>
        /// This method returns the left node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node to retrieve the left child from</param>
        /// <returns>Left child of <paramref name="node"/></returns>
        private static T LeftOf(T node)
        {
            return node?.Left;
        }

        /// <summary>
        /// This method returns the right node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node to retrieve the right child from</param>
        /// <returns>Right child of <paramref name="node"/></returns>
        private static T RightOf(T node)
        {
            return node?.Right;
        }

        /// <summary>
        /// Returns the parent node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node to retrieve the parent from</param>
        /// <returns>Parent of <paramref name="node"/></returns>
        private static T ParentOf(T node)
        {
            return node?.Parent;
        }

        #endregion
    }
}
