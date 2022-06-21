namespace Ryujinx.Common.Collections
{
    /// <summary>
    /// Represents a node in the Red-Black Tree.
    /// </summary>
    public class IntrusiveRedBlackTreeNode<T>
    {
        public bool Color = true;
        public T Left;
        public T Right;
        public T Parent;
    }
}