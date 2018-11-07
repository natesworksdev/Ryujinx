using System.Collections;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KPageList : IEnumerable<KPageNode>
    {
        private LinkedList<KPageNode> Nodes;

        public KPageList()
        {
            Nodes = new LinkedList<KPageNode>();
        }

        public KernelResult AddRange(long Address, long PagesCount)
        {
            if (PagesCount != 0)
            {
                if (Nodes.Last != null)
                {
                    KPageNode LastNode = Nodes.Last.Value;

                    if (LastNode.Address + LastNode.PagesCount * 4096 == Address)
                    {
                        Address     = LastNode.Address;
                        PagesCount += LastNode.PagesCount;

                        Nodes.RemoveLast();
                    }
                }

                Nodes.AddLast(new KPageNode(Address, PagesCount));
            }

            return KernelResult.Success;
        }

        public long GetPagesCount()
        {
            long Sum = 0;

            foreach (KPageNode Node in Nodes)
            {
                Sum += Node.PagesCount;
            }

            return Sum;
        }

        public IEnumerator<KPageNode> GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}