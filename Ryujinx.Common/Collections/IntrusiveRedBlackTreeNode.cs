namespace Ryujinx.Common.Collections
{
    /// <summary>
    /// Represents a node in the Red-Black Tree.
    /// </summary>
    public class IntrusiveRedBlackTreeNode<T>
    {
        internal bool Color = true;
        internal T Left;
        internal T Right;
        internal T Parent;
    }
}