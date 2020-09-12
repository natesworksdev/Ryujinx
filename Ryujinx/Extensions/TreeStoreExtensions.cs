using Gtk;
using System;

namespace Ryujinx.Extensions
{
    public static class TreeStoreExtensions
    {
        public static void ForEachChildren(this TreeStore treeStore, TreeIter parentIter, Action<TreeIter> childAction)
        {
            if (treeStore.IterChildren(out TreeIter childIter, parentIter))
            {
                do
                {
                    childAction.Invoke(childIter);
                }
                while (treeStore.IterNext(ref childIter));
            }
        }

        public static void ForEach(this TreeStore treeStore, Action<TreeIter> iterAction)
        {
            if (treeStore.GetIterFirst(out TreeIter iter))
            {
                do
                {
                    iterAction.Invoke(iter);
                } 
                while (treeStore.IterNext(ref iter));
            }            
        }
    }
}
