using Ryujinx.Graphics.Shader.Decoders;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Shader.Translation
{
    static class FunctionMatch
    {
        private static IPatternTreeNode[] _fsiGetAddressTree = PatternTrees.GetFsiGetAddress();
        private static IPatternTreeNode[] _fsiGetAddressV2Tree = PatternTrees.GetFsiGetAddressV2();
        private static IPatternTreeNode[] _fsiIsLastWarpThreadPatternTree = PatternTrees.GetFsiIsLastWarpThread();
        private static IPatternTreeNode[] _fsiBeginPatternTree = PatternTrees.GetFsiBeginPattern();
        private static IPatternTreeNode[] _fsiEndPatternTree = PatternTrees.GetFsiEndPattern();

        public static FunctionMatchResult FindMatch(Block[][] blocks, int funcIndex)
        {
            Block[] blocksForFunc = blocks[funcIndex];

            if (Matches(_fsiGetAddressTree, blocksForFunc) ||
                Matches(_fsiGetAddressV2Tree, blocksForFunc) ||
                Matches(_fsiIsLastWarpThreadPatternTree, blocksForFunc))
            {
                return FunctionMatchResult.Unused;
            }
            else if (MatchesWithInnerCall(_fsiBeginPatternTree, _fsiIsLastWarpThreadPatternTree, blocks, funcIndex))
            {
                return FunctionMatchResult.FSIBegin;
            }
            else if (MatchesWithInnerCall(_fsiEndPatternTree, _fsiIsLastWarpThreadPatternTree, blocks, funcIndex))
            {
                return FunctionMatchResult.FSIEnd;
            }

            return FunctionMatchResult.NoMatch;
        }

        private struct TreeNodeUse
        {
            public TreeNode Node { get; }
            public int Index { get; }
            public bool Inverted { get; }

            private TreeNodeUse(int index, bool inverted, TreeNode node)
            {
                Index = index;
                Inverted = inverted;
                Node = node;
            }

            public TreeNodeUse(int index, TreeNode node) : this(index, false, node)
            {
            }

            public TreeNodeUse Flip()
            {
                return new TreeNodeUse(Index, !Inverted, Node);
            }
        }

        private class TreeNode
        {
            public readonly InstOp Op;
            public readonly List<TreeNodeUse> Uses;

            public TreeNode(InstOp op)
            {
                Op = op;
                Uses = new List<TreeNodeUse>();
            }
        }

        private static TreeNode[] BuildTree(Block[] blocks)
        {
            List<TreeNode> nodes = new List<TreeNode>();

            TreeNodeUse[] predDefs = new TreeNodeUse[RegisterConsts.PredsCount];
            TreeNodeUse[] gprDefs = new TreeNodeUse[RegisterConsts.GprsCount];

            void DefPred(byte predIndex, int index, TreeNode node)
            {
                if (predIndex != RegisterConsts.PredicateTrueIndex)
                {
                    predDefs[predIndex] = new TreeNodeUse(index, node);
                }
            }

            void DefGpr(byte regIndex, int index, TreeNode node)
            {
                if (regIndex != RegisterConsts.RegisterZeroIndex)
                {
                    gprDefs[regIndex] = new TreeNodeUse(index, node);
                }
            }

            TreeNodeUse UsePred(byte predIndex, bool predInv)
            {
                if (predIndex != RegisterConsts.PredicateTrueIndex)
                {
                    TreeNodeUse use = predDefs[predIndex];

                    if (use.Node != null)
                    {
                        nodes.Remove(use.Node);
                    }

                    return predInv ? use.Flip() : use;
                }

                return new TreeNodeUse(-1, null);
            }

            TreeNodeUse UseGpr(byte regIndex)
            {
                if (regIndex != RegisterConsts.RegisterZeroIndex)
                {
                    TreeNodeUse use = gprDefs[regIndex];

                    if (use.Node != null)
                    {
                        nodes.Remove(use.Node);
                    }

                    return use;
                }

                return new TreeNodeUse(-1, null);
            }

            for (int index = 0; index < blocks.Length; index++)
            {
                Block block = blocks[index];

                for (int opIndex = 0; opIndex < block.OpCodes.Count; opIndex++)
                {
                    InstOp op = block.OpCodes[opIndex];

                    TreeNode node = new TreeNode(op);

                    // Add uses.

                    if (!op.Props.HasFlag(InstProps.NoPred))
                    {
                        byte predIndex = (byte)((op.RawOpCode >> 16) & 7);
                        bool predInv = (op.RawOpCode & 0x80000) != 0;
                        node.Uses.Add(UsePred(predIndex, predInv));
                    }

                    if (op.Props.HasFlag(InstProps.Ps))
                    {
                        byte predIndex = (byte)((op.RawOpCode >> 39) & 7);
                        bool predInv = (op.RawOpCode & 0x40000000000) != 0;
                        node.Uses.Add(UsePred(predIndex, predInv));
                    }

                    if (op.Props.HasFlag(InstProps.Ra))
                    {
                        byte ra = (byte)(op.RawOpCode >> 8);
                        node.Uses.Add(UseGpr(ra));
                    }

                    if ((op.Props & (InstProps.Rb | InstProps.Rb2)) != 0)
                    {
                        byte rb = op.Props.HasFlag(InstProps.Rb2) ? (byte)op.RawOpCode : (byte)(op.RawOpCode >> 20);
                        node.Uses.Add(UseGpr(rb));
                    }

                    if (op.Props.HasFlag(InstProps.Rc))
                    {
                        byte rc = (byte)(op.RawOpCode >> 39);
                        node.Uses.Add(UseGpr(rc));
                    }

                    // Make definitions.

                    int defIndex = 0;

                    InstProps pdType = op.Props & InstProps.PdMask;

                    if (pdType != 0)
                    {
                        int bit = pdType switch
                        {
                            InstProps.Pd => 3,
                            InstProps.LPd => 48,
                            InstProps.SPd => 30,
                            InstProps.TPd => 51,
                            InstProps.VPd => 45,
                            _ => throw new InvalidOperationException($"Table has unknown predicate destination {pdType}.")
                        };

                        byte predIndex = (byte)((op.RawOpCode >> bit) & 7);
                        DefPred(predIndex, defIndex++, node);
                    }

                    if (op.Props.HasFlag(InstProps.Rd))
                    {
                        byte rd = (byte)op.RawOpCode;
                        DefGpr(rd, defIndex++, node);
                    }

                    nodes.Add(node);
                }
            }

            return nodes.ToArray();
        }

        private static bool InstHasSideEffects(InstName name)
        {
            switch (name)
            {
                case InstName.Atom:
                case InstName.AtomCas:
                case InstName.Atoms:
                case InstName.AtomsCas:
                case InstName.Bar:
                case InstName.Membar:
                case InstName.Red:
                case InstName.St:
                case InstName.Stg:
                case InstName.Stl:
                case InstName.Sts:
                case InstName.Sust:
                case InstName.SustB:
                case InstName.SustD:
                case InstName.SustDB:
                case InstName.Suatom:
                case InstName.SuatomB:
                case InstName.SuatomB2:
                case InstName.SuatomCas:
                case InstName.SuatomCasB:
                case InstName.Sured:
                case InstName.SuredB:
                    return true;
            }

            return false;
        }

        private interface IPatternTreeNode
        {
            List<PatternTreeNodeUse> Uses { get; }
            InstName Name { get; }
            bool IsImm { get; }
            bool Matches(in InstOp opInfo);
        }

        private struct PatternTreeNodeUse
        {
            public IPatternTreeNode Node { get; }
            public int Index { get; }
            public bool Inverted { get; }
            public PatternTreeNodeUse Inv => new PatternTreeNodeUse(Index, !Inverted, Node);

            private PatternTreeNodeUse(int index, bool inverted, IPatternTreeNode node)
            {
                Index = index;
                Inverted = inverted;
                Node = node;
            }

            public PatternTreeNodeUse(int index, IPatternTreeNode node) : this(index, false, node)
            {
            }
        }

        private class PatternTreeNode<T> : IPatternTreeNode
        {
            public List<PatternTreeNodeUse> Uses { get; }
            private readonly Func<T, bool> _match;
            private readonly InstName _name;
            private readonly bool _isImm;

            public InstName Name => _name;
            public bool IsImm => _isImm;
            public PatternTreeNodeUse Out => new PatternTreeNodeUse(0, this);

            public PatternTreeNode(InstName name, Func<T, bool> match, bool isImm = false)
            {
                _name = name;
                _match = match;
                _isImm = isImm;
                Uses = new List<PatternTreeNodeUse>();
            }

            public PatternTreeNode<T> Use(PatternTreeNodeUse use)
            {
                Uses.Add(use);
                return this;
            }

            public PatternTreeNodeUse OutAt(int index)
            {
                return new PatternTreeNodeUse(index, this);
            }

            public bool Matches(in InstOp opInfo)
            {
                if (opInfo.Name != _name)
                {
                    return false;
                }

                ulong rawOp = opInfo.RawOpCode;
                T op = Unsafe.As<ulong, T>(ref rawOp);

                if (!_match(op))
                {
                    return false;
                }

                return true;
            }
        }

        private static bool MatchesWithInnerCall(IPatternTreeNode[] pattern, IPatternTreeNode[] innerPattern, Block[][] blocks, int funcIndex)
        {
            Block[] blocksForFunc = blocks[funcIndex];

            if (blocksForFunc.Length == 0)
            {
                return false;
            }

            InstOp callOp = blocksForFunc[0].GetLastOp();

            if (callOp.Name != InstName.Cal)
            {
                return false;
            }

            Block[] callTarget = FindFunc(blocks, callOp.GetAbsoluteAddress());

            if (callTarget == null || !Matches(innerPattern, callTarget))
            {
                return false;
            }

            return Matches(pattern, blocksForFunc);
        }

        private static Block[] FindFunc(Block[][] blocks, ulong funcAddress)
        {
            for (int index = 0; index < blocks.Length; index++)
            {
                if (blocks[index].Length > 0 && blocks[index][0].Address == funcAddress)
                {
                    return blocks[index];
                }
            }

            return null;
        }

        private static bool Matches(IPatternTreeNode[] pTree, Block[] code)
        {
            return Matches(pTree, BuildTree(code));
        }

        private static bool Matches(IPatternTreeNode[] pTree, TreeNode[] cTree)
        {
            if (pTree.Length != cTree.Length)
            {
                return false;
            }

            for (int index = 0; index < pTree.Length; index++)
            {
                if (!Matches(pTree[index], cTree[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Matches(IPatternTreeNode pTreeNode, TreeNode cTreeNode)
        {
            if (!pTreeNode.Matches(in cTreeNode.Op) || pTreeNode.IsImm != cTreeNode.Op.Props.HasFlag(InstProps.Ib))
            {
                return false;
            }

            if (pTreeNode.Uses.Count != cTreeNode.Uses.Count)
            {
                return false;
            }

            for (int index = 0; index < pTreeNode.Uses.Count; index++)
            {
                var pUse = pTreeNode.Uses[index];
                var cUse = cTreeNode.Uses[index];

                if (pUse.Index != cUse.Index || pUse.Inverted != cUse.Inverted)
                {
                    return false;
                }

                if ((pUse.Node == null) != (cUse.Node == null))
                {
                    return false;
                }

                if (pUse.Node != null && !Matches(pUse.Node, cUse.Node))
                {
                    return false;
                }
            }

            return true;
        }

        private static class PatternTrees
        {
            public static IPatternTreeNode[] GetFsiGetAddress()
            {
                var affinityValue = S2r(SReg.Affinity).Use(PT).Out;
                var orderingTicketValue = S2r(SReg.OrderingTicket).Use(PT).Out;

                return new IPatternTreeNode[]
                {
                    Iscadd(cc: true, 2, 0, 404)
                        .Use(PT)
                        .Use(Iscadd(cc: false, 8)
                            .Use(PT)
                            .Use(Lop32i(LogicOp.And, 0xff).Use(PT).Use(affinityValue).Out)
                            .Use(Lop32i(LogicOp.And, 0xff).Use(PT).Use(orderingTicketValue).Out).Out),
                    ShrU32W(16).Use(PT).Use(orderingTicketValue),
                    Iadd32i(0x200)
                        .Use(PT)
                        .Use(Lop32i(LogicOp.And, 0xfe00).Use(PT).Use(orderingTicketValue).Out),
                    Iadd(x: true, 0, 405).Use(PT).Use(RZ),
                    Ret().Use(PT)
                };
            }

            public static IPatternTreeNode[] GetFsiGetAddressV2()
            {
                var affinityValue = S2r(SReg.Affinity).Use(PT).Out;
                var orderingTicketValue = S2r(SReg.OrderingTicket).Use(PT).Out;

                return new IPatternTreeNode[]
                {
                    ShrU32W(16).Use(PT).Use(orderingTicketValue),
                    Iadd32i(0x200)
                        .Use(PT)
                        .Use(Lop32i(LogicOp.And, 0xfe00).Use(PT).Use(orderingTicketValue).Out),
                    Iscadd(cc: true, 2, 0, 404)
                        .Use(PT)
                        .Use(Bfi(0x808)
                            .Use(PT)
                            .Use(affinityValue)
                            .Use(Lop32i(LogicOp.And, 0xff).Use(PT).Use(orderingTicketValue).Out).Out),
                    Iadd(x: true, 0, 405).Use(PT).Use(RZ),
                    Ret().Use(PT)
                };
            }

            public static IPatternTreeNode[] GetFsiIsLastWarpThread()
            {
                var threadKillValue = S2r(SReg.ThreadKill).Use(PT).Out;
                var laneIdValue = S2r(SReg.LaneId).Use(PT).Out;

                return new IPatternTreeNode[]
                {
                    IsetpU32(IComp.Eq)
                        .Use(PT)
                        .Use(PT)
                        .Use(FloU32()
                            .Use(PT)
                            .Use(Vote(VoteMode.Any)
                                .Use(PT)
                                .Use(IsetpU32(IComp.Ne)
                                    .Use(PT)
                                    .Use(PT)
                                    .Use(Lop(negB: true, LogicOp.PassB).Use(PT).Use(RZ).Use(threadKillValue).OutAt(1))
                                    .Use(RZ).Out).OutAt(1)).Out)
                        .Use(laneIdValue),
                    Ret().Use(PT)
                };
            }

            public static IPatternTreeNode[] GetFsiBeginPattern()
            {
                static PatternTreeNodeUse HighU16Equals(PatternTreeNodeUse x)
                {
                    return IsetpU32(IComp.Eq).Use(PT).Use(PT)
                        .Use(ShrU32W(16).Use(PT).Use(x).Out)
                        .Use(Undef).Out;
                }

                return new IPatternTreeNode[]
                {
                    Cal(),
                    Ret().Use(Undef.Inv),
                    Ret().Use(HighU16Equals(LdgE(CacheOpLd.Cg, LsSize.B32).Use(PT).Use(Undef).Out)),
                    Bra().Use(HighU16Equals(LdgE(CacheOpLd.Cg, LsSize.B32).Use(PT).Use(Undef).Out).Inv),
                    Ret().Use(PT)
                };
            }

            public static IPatternTreeNode[] GetFsiEndPattern()
            {
                var voteResult = Vote(VoteMode.All).Use(PT).Use(PT).OutAt(1);
                var popcResult = Popc().Use(PT).Use(voteResult).Out;
                var threadKillValue = S2r(SReg.ThreadKill).Use(PT).Out;
                var laneIdValue = S2r(SReg.LaneId).Use(PT).Out;

                return new IPatternTreeNode[]
                {
                    Cal(),
                    Ret().Use(Undef.Inv),
                    Membar(Decoders.Membar.Vc).Use(PT),
                    Ret().Use(IsetpU32(IComp.Ne).Use(PT).Use(PT).Use(threadKillValue).Use(RZ).Out),
                    RedE(RedOp.Add, AtomSize.U32)
                        .Use(IsetpU32(IComp.Eq).Use(PT).Use(PT).Use(FloU32().Use(PT).Use(voteResult).Out).Use(laneIdValue).Out)
                        .Use(Undef)
                        .Use(XmadH1H1(XmadCop.Cbcc, psl: true, mrg: false)
                            .Use(PT)
                            .Use(Undef)
                            .Use(XmadH0H1(XmadCop.Cfull, psl: false, mrg: true).Use(PT).Use(Undef).Use(popcResult).Use(RZ).Out)
                            .Use(XmadH0H0(XmadCop.Cfull, psl: false, mrg: false).Use(PT).Use(Undef).Use(popcResult).Use(RZ).Out).Out),
                    Ret().Use(PT)
                };
            }

            private static PatternTreeNode<InstBfiI> Bfi(int imm)
            {
                return new(InstName.Bfi, (op) => !op.WriteCC && op.Imm20 == imm, isImm: true);
            }

            private static PatternTreeNode<InstBra> Bra()
            {
                return new(InstName.Bra, (op) => op.Ccc == Ccc.T && !op.Ca);
            }

            private static PatternTreeNode<InstCal> Cal()
            {
                return new(InstName.Cal, (op) => !op.Ca && op.Inc);
            }

            private static PatternTreeNode<InstFloR> FloU32()
            {
                return new(InstName.Flo, (op) => !op.Signed && !op.Sh && !op.NegB && !op.WriteCC);
            }

            private static PatternTreeNode<InstIaddC> Iadd(bool x, int cbufSlot, int cbufOffset)
            {
                return new(InstName.Iadd, (op) =>
                    !op.Sat &&
                    !op.WriteCC &&
                    op.X == x &&
                    op.AvgMode == AvgMode.NoNeg &&
                    op.CbufSlot == cbufSlot &&
                    op.CbufOffset == cbufOffset);
            }

            private static PatternTreeNode<InstIadd32i> Iadd32i(int imm)
            {
                return new(InstName.Iadd32i, (op) => !op.Sat && !op.WriteCC && !op.X && op.AvgMode == AvgMode.NoNeg && op.Imm32 == imm);
            }

            private static PatternTreeNode<InstIscaddR> Iscadd(bool cc, int imm)
            {
                return new(InstName.Iscadd, (op) => op.WriteCC == cc && op.AvgMode == AvgMode.NoNeg && op.Imm5 == imm);
            }

            private static PatternTreeNode<InstIscaddC> Iscadd(bool cc, int imm, int cbufSlot, int cbufOffset)
            {
                return new(InstName.Iscadd, (op) =>
                    op.WriteCC == cc &&
                    op.AvgMode == AvgMode.NoNeg &&
                    op.Imm5 == imm &&
                    op.CbufSlot == cbufSlot &&
                    op.CbufOffset == cbufOffset);
            }

            private static PatternTreeNode<InstIsetpR> IsetpU32(IComp comp)
            {
                return new(InstName.Isetp, (op) => !op.Signed && op.IComp == comp && op.Bop == BoolOp.And);
            }

            private static PatternTreeNode<InstLopR> Lop(bool negB, LogicOp logicOp)
            {
                return new(InstName.Lop, (op) => !op.NegA && op.NegB == negB && !op.WriteCC && !op.X && op.Lop == logicOp && op.PredicateOp == PredicateOp.F);
            }

            private static PatternTreeNode<InstLop32i> Lop32i(LogicOp logicOp, int imm)
            {
                return new(InstName.Lop32i, (op) => !op.NegA && !op.NegB && !op.X && !op.WriteCC && op.LogicOp == logicOp && op.Imm32 == imm);
            }

            private static PatternTreeNode<InstMembar> Membar(Membar membar)
            {
                return new(InstName.Membar, (op) => op.Membar == membar);
            }

            private static PatternTreeNode<InstPopcR> Popc()
            {
                return new(InstName.Popc, (op) => !op.NegB);
            }

            private static PatternTreeNode<InstRet> Ret()
            {
                return new(InstName.Ret, (op) => op.Ccc == Ccc.T);
            }

            private static PatternTreeNode<InstS2r> S2r(SReg reg)
            {
                return new(InstName.S2r, (op) => op.SReg == reg);
            }

            private static PatternTreeNode<InstShrI> ShrU32W(int imm)
            {
                return new(InstName.Shr, (op) => !op.Signed && !op.Brev && op.M && op.XMode == 0 && op.Imm20 == imm, isImm: true);
            }

            private static PatternTreeNode<InstLdg> LdgE(CacheOpLd cacheOp, LsSize size)
            {
                return new(InstName.Ldg, (op) => op.E && op.CacheOp == cacheOp && op.LsSize == size);
            }

            private static PatternTreeNode<InstRed> RedE(RedOp redOp, AtomSize size)
            {
                return new(InstName.Red, (op) => op.E && op.RedOp == redOp && op.RedSize == size);
            }

            private static PatternTreeNode<InstVote> Vote(VoteMode mode)
            {
                return new(InstName.Vote, (op) => op.VoteMode == mode);
            }

            private static PatternTreeNode<InstXmadR> XmadH0H0(XmadCop cop, bool psl, bool mrg) => Xmad(cop, psl, mrg, false, false);
            private static PatternTreeNode<InstXmadR> XmadH0H1(XmadCop cop, bool psl, bool mrg) => Xmad(cop, psl, mrg, false, true);
            private static PatternTreeNode<InstXmadR> XmadH1H0(XmadCop cop, bool psl, bool mrg) => Xmad(cop, psl, mrg, true, false);
            private static PatternTreeNode<InstXmadR> XmadH1H1(XmadCop cop, bool psl, bool mrg) => Xmad(cop, psl, mrg, true, true);

            private static PatternTreeNode<InstXmadR> Xmad(XmadCop cop, bool psl, bool mrg, bool hiloA, bool hiloB)
            {
                return new(InstName.Xmad, (op) => op.XmadCop == cop && op.Psl == psl && op.Mrg == mrg && op.HiloA == hiloA && op.HiloB == hiloB);
            }

            private static PatternTreeNodeUse PT => PTOrRZ();
            private static PatternTreeNodeUse RZ => PTOrRZ();
            private static PatternTreeNodeUse Undef => new PatternTreeNodeUse(0, null);

            private static PatternTreeNodeUse PTOrRZ()
            {
                return new PatternTreeNodeUse(-1, null);
            }
        }

        private static void PrintTreeNode(TreeNode node, string indentation)
        {
            Console.WriteLine($" {node.Op.Name}");

            for (int i = 0; i < node.Uses.Count; i++)
            {
                TreeNodeUse use = node.Uses[i];
                bool last = i == node.Uses.Count - 1;
                char separator = last ? '`' : '|';

                if (use.Node != null)
                {
                    Console.Write($"{indentation} {separator}- ({(use.Inverted ? "INV " : "")}{use.Index})");
                    PrintTreeNode(use.Node, indentation + (last ? "       " : " |     "));
                }
                else
                {
                    Console.WriteLine($"{indentation} {separator}- ({(use.Inverted ? "INV " : "")}{use.Index}) NULL");
                }
            }
        }

        private static void PrintTreeNode(IPatternTreeNode node, string indentation)
        {
            Console.WriteLine($" {node.Name}");

            for (int i = 0; i < node.Uses.Count; i++)
            {
                PatternTreeNodeUse use = node.Uses[i];
                bool last = i == node.Uses.Count - 1;
                char separator = last ? '`' : '|';

                if (use.Node != null)
                {
                    Console.Write($"{indentation} {separator}- ({(use.Inverted ? "INV " : "")}{use.Index})");
                    PrintTreeNode(use.Node, indentation + (last ? "       " : " |     "));
                }
                else
                {
                    Console.WriteLine($"{indentation} {separator}- ({(use.Inverted ? "INV " : "")}{use.Index}) NULL");
                }
            }
        }
    }
}
