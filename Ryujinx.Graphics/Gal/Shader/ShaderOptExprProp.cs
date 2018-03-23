using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.Shader
{
    static class ShaderOptExprProp
    {
        private struct UseSite
        {
            public object Parent;

            public int OperIndex;

            public UseSite(object Parent, int OperIndex)
            {
                this.Parent    = Parent;
                this.OperIndex = OperIndex;
            }
        }

        private class RegUse
        {
            public ShaderIrNode Node { get; private set; }

            private List<UseSite> Sites;

            public RegUse()
            {
                Sites = new List<UseSite>();
            }

            public void AddUseSite(UseSite Site)
            {
                Sites.Add(Site);
            }

            public bool TryPropagate()
            {
                //If the use count of the register is more than 1,
                //then propagating the expression is not worth it,
                //because the code will be larger, harder to read,
                //and less efficient due to the common sub-expression being
                //propagated.
                if (Sites.Count == 1 || !(Node.Src is ShaderIrOperOp))
                {
                    foreach (UseSite Site in Sites)
                    {
                        if (Site.Parent is ShaderIrOperOp Op)
                        {
                            switch (Site.OperIndex)
                            {
                                case 0: Op.OperandA = Node.Src; break;
                                case 1: Op.OperandB = Node.Src; break;
                                case 2: Op.OperandC = Node.Src; break;

                                default: throw new InvalidOperationException();
                            }
                        }
                        else if (Site.Parent is ShaderIrNode SiteNode)
                        {
                            SiteNode.Src = Node.Src;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    return true;
                }

                return Sites.Count == 0;
            }

            public void SetNewAsg(ShaderIrNode Node)
            {
                this.Node = Node;

                Sites.Clear();
            }
        }

        public static void Optimize(List<ShaderIrNode> Nodes)
        {
            Dictionary<int, RegUse> Uses = new Dictionary<int, RegUse>();

            RegUse GetRegUse(int GprIndex)
            {
                RegUse Use;

                if (!Uses.TryGetValue(GprIndex, out Use))
                {
                    Use = new RegUse();

                    Uses.Add(GprIndex, Use);
                }

                return Use;
            }

            void TryAddRegUse(object Parent, ShaderIrOper Oper, int OperIndex = 0)
            {
                if (Oper is ShaderIrOperOp Op)
                {
                    TryAddRegUse(Op, Op.OperandA, 0);
                    TryAddRegUse(Op, Op.OperandB, 1);
                    TryAddRegUse(Op, Op.OperandC, 2);
                }
                else if (Oper is ShaderIrOperReg Reg && Reg.GprIndex != 0xff)
                {
                    GetRegUse(Reg.GprIndex).AddUseSite(new UseSite(Parent, OperIndex));
                }
            }

            for (int Index = 0; Index < Nodes.Count; Index++)
            {
                ShaderIrNode Node = Nodes[Index];

                if (Node.Src is ShaderIrOperOp Op)
                {
                    TryAddRegUse(Node, Op);
                }
                else if (Node.Src is ShaderIrOperReg)
                {
                    TryAddRegUse(Node, Node.Src);
                }

                if (Node.Dst is ShaderIrOperReg Reg && Reg.GprIndex != 0xff)
                {
                    RegUse Use = GetRegUse(Reg.GprIndex);

                    if (Use.Node != null && Use.TryPropagate())
                    {
                        Nodes.Remove(Use.Node);

                        Index--;
                    }

                    Use.SetNewAsg(Node);
                }
            }

            foreach (RegUse Use in Uses.Values)
            {
                if (Use.TryPropagate())
                {
                    Nodes.Remove(Use.Node);
                }
            }
        }
    }
}