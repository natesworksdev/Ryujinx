using Ryujinx.Graphics.Shader.Decoders;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Shader.Translation
{
    static class FunctionMatch
    {
        private struct BlockVisitor
        {
            private readonly Block[] _blocks;
            private int _blockIndex;
            private int _opIndex;
            private int _instIndex;

            private byte[] _predRegAlloc;
            private byte[] _gprRegAlloc;

            private byte[] _predLastUses;
            private byte[] _gprLastUses;

            private int _predsInUse;
            private ulong[] _gprsInUse;

            public BlockVisitor(Block[] blocks, IInstPattern[] pattern)
            {
                _blocks = blocks;
                _blockIndex = 0;
                _opIndex = 0;
                _instIndex = -1;

                int expectedCodeSize = pattern.Length;

                _predRegAlloc = new byte[expectedCodeSize];
                _gprRegAlloc = new byte[expectedCodeSize];

                _predLastUses = new byte[expectedCodeSize];
                _gprLastUses = new byte[expectedCodeSize];

                _predsInUse = 0;
                _gprsInUse = new ulong[(RegisterConsts.GprsCount + (sizeof(ulong) * 8) - 1) / (sizeof(ulong) * 8)];

                for (int index = 0; index < expectedCodeSize; index++)
                {
                    RegUses regUses = pattern[index].RegUses;

                    UpdatePredLastUse(regUses.PredDefPos, index);
                    UpdatePredLastUse(regUses.SP1DefPos, index);
                    UpdatePredLastUse(regUses.SP2DefPos, index);
                    UpdateGprLastUse(regUses.SrcADefPos, index);
                    UpdateGprLastUse(regUses.SrcBDefPos, index);
                    UpdateGprLastUse(regUses.SrcCDefPos, index);
                }
            }

            private void UpdatePredLastUse(int defPos, int usePos)
            {
                if (defPos >= 0)
                {
                    _predLastUses[defPos] = (byte)Math.Max(_predLastUses[defPos], usePos);
                }
            }

            private void UpdateGprLastUse(int defPos, int usePos)
            {
                if (defPos >= 0)
                {
                    _gprLastUses[defPos] = (byte)Math.Max(_gprLastUses[defPos], usePos);
                }
            }

            public void Rewind()
            {
                _blockIndex = 0;
                _opIndex = 0;
                _instIndex = -1;

                _predsInUse = 0;
                new Span<ulong>(_gprsInUse).Fill(0);
            }

            public bool GetOp(InstName name, out InstOp opInfo)
            {
                while (_opIndex == _blocks[_blockIndex].OpCodes.Count)
                {
                    _opIndex = 0;
                    _blockIndex++;

                    if (_blockIndex >= _blocks.Length)
                    {
                        opInfo = default;
                        return false;
                    }
                }

                opInfo = _blocks[_blockIndex].OpCodes[_opIndex++];
                if (opInfo.Name != name)
                {
                    return false;
                }

                int currentInstIndex = ++_instIndex;
                _gprRegAlloc[currentInstIndex] = byte.MaxValue;
                _predRegAlloc[currentInstIndex] = byte.MaxValue;

                return true;
            }

            public bool DefPred(byte predIndex)
            {
                if (predIndex != RegisterConsts.PredicateTrueIndex)
                {
                    _predRegAlloc[_instIndex] = predIndex;

                     // Set the bit for this predicate, and return true if it was free, false otherwise.
                    int oldMask = _predsInUse;
                    int setMask = 1 << predIndex;
                    _predsInUse |= setMask;
                    return (oldMask & setMask) == 0;
                }

                return true;
            }

            public bool DefGpr(byte regIndex)
            {
                if (regIndex != RegisterConsts.RegisterZeroIndex)
                {
                    _gprRegAlloc[_instIndex] = regIndex;

                    // Set the bit for this register, and return true if it was free, false otherwise.
                    ref ulong mask = ref _gprsInUse[regIndex / (sizeof(ulong) * 8)];
                    ulong oldMask = mask;
                    ulong setMask = 1UL << (regIndex & (sizeof(ulong) * 8 - 1));
                    mask |= setMask;
                    return (oldMask & setMask) == 0;
                }

                return true;
            }

            public byte UsePred(int instIndex)
            {
                byte predIndex = _predRegAlloc[instIndex];

                if (_predLastUses[instIndex] == _instIndex)
                {
                    _predsInUse &= ~(1 << predIndex);
                }

                return predIndex;
            }

            public byte UseGpr(int instIndex)
            {
                byte regIndex = _gprRegAlloc[instIndex];

                if (_gprLastUses[instIndex] == _instIndex)
                {
                    _gprsInUse[regIndex / (sizeof(ulong) * 8)] &= ~(1UL << (regIndex & (sizeof(ulong) * 8 - 1)));
                }

                return regIndex;
            }
        }

        private enum RegUseFlags : byte
        {
            None = 0,
            PredInv = 1 << 0,
            Cb = 1 << 1,
            Ib = 1 << 2
        }

        private struct RegUses
        {
            public static RegUses Default => new RegUses(RegUseFlags.None, -1);

            public RegUseFlags Flags { get; }
            public int PredDefPos { get; }
            public int SP1DefPos { get; }
            public int SP2DefPos { get; }
            public int SrcADefPos { get; }
            public int SrcBDefPos { get; }
            public int SrcCDefPos { get; }

            private RegUses(
                RegUseFlags flags,
                int predDefPos,
                int sp1DefPos = -2,
                int sp2DefPos = -2,
                int srcADefPos = -2,
                int srcBDefPos = -2,
                int srcCDefPos = -2)
            {
                Flags = flags;
                PredDefPos = predDefPos;
                SP1DefPos = sp1DefPos;
                SP2DefPos = sp2DefPos;
                SrcADefPos = srcADefPos;
                SrcBDefPos = srcBDefPos;
                SrcCDefPos = srcCDefPos;
            }

            public static RegUses Pn(int predDefPos)
            {
                return new RegUses(RegUseFlags.None, predDefPos);
            }

            public static RegUses NPn(int predDefPos)
            {
                return new RegUses(RegUseFlags.PredInv, predDefPos);
            }

            public static RegUses PnRabc(int predDefPos, int srcADefPos, int srcBDefPos = -2, int srcCDefPos = -2)
            {
                return new RegUses(RegUseFlags.None, predDefPos, -2, -2, srcADefPos, srcBDefPos, srcCDefPos);
            }

            public static RegUses PtPn(int sp1DefPos)
            {
                return new RegUses(RegUseFlags.None, -1, sp1DefPos);
            }

            public static RegUses PtRaCb(int srcADefPos)
            {
                return new RegUses(RegUseFlags.Cb, -1, -2, -2, srcADefPos);
            }

            public static RegUses PtRaIb(int srcADefPos)
            {
                return new RegUses(RegUseFlags.Ib, -1, -2, -2, srcADefPos);
            }

            public static RegUses PtRabc(int srcADefPos, int srcBDefPos = -2, int srcCDefPos = -2)
            {
                return new RegUses(RegUseFlags.None, -1, -2, -2, srcADefPos, srcBDefPos, srcCDefPos);
            }

            public static RegUses PtPt()
            {
                return new RegUses(RegUseFlags.None, -1, -1);
            }

            public static RegUses PtPtRabc(int srcADefPos, int srcBDefPos = -2, int srcCDefPos = -2)
            {
                return new RegUses(RegUseFlags.None, -1, -1, -2, srcADefPos, srcBDefPos, srcCDefPos);
            }
        }

        private interface IInstPattern
        {
            RegUses RegUses { get; }
            bool Matches(ref BlockVisitor bv);
        }

        private struct InstPattern<T> : IInstPattern where T : unmanaged
        {
            private readonly InstName _name;
            private readonly Func<T, bool> _match;
            private readonly RegUses _regUses;
            public RegUses RegUses => _regUses;

            public InstPattern(InstName name, Func<T, bool> match, RegUses regUses)
            {
                _name = name;
                _match = match;
                _regUses = regUses;
            }

            public InstPattern(InstName name, Func<T, bool> match) : this(name, match, RegUses.Default)
            {
            }

            public InstPattern(InstName name, RegUses regUses) : this(name, AlwaysMatch, regUses)
            {
            }

            public InstPattern(InstName name) : this(name, RegUses.Default)
            {
            }

            private static bool AlwaysMatch(T data)
            {
                return true;
            }

            public bool Matches(ref BlockVisitor bv)
            {
                if (!bv.GetOp(_name, out InstOp opInfo))
                {
                    return false;
                }

                ulong rawOp = opInfo.RawOpCode;
                InstProps props = opInfo.Props;

                T op = Unsafe.As<ulong, T>(ref rawOp);

                if (!_match(op))
                {
                    return false;
                }

                if (props.HasFlag(InstProps.Rd2) && (byte)(rawOp >> 28) != RegisterConsts.RegisterZeroIndex)
                {
                    return false;
                }

                if (props.HasFlag(InstProps.Pdn) && (byte)(rawOp & 7) != RegisterConsts.PredicateTrueIndex)
                {
                    return false;
                }

                if ((_regUses.SrcADefPos != -2 && (props & InstProps.Ra) == 0) ||
                    (_regUses.SrcBDefPos != -2 && (props & (InstProps.Rb | InstProps.Rb2)) == 0) ||
                    (_regUses.SrcCDefPos != -2 && (props & InstProps.Rc) == 0))
                {
                    return false;
                }

                if (_regUses.Flags.HasFlag(RegUseFlags.Ib) != props.HasFlag(InstProps.Ib))
                {
                    return false;
                }

                if (_regUses.Flags.HasFlag(RegUseFlags.Cb) && (props & (InstProps.Rb | InstProps.Rb2 | InstProps.Ib)) != 0)
                {
                    return false;
                }

                int predDefPos = props.HasFlag(InstProps.NoPred) ? -2 : _regUses.PredDefPos;
                bool predInv = (rawOp & 0x80000) != 0;

                if (_regUses.Flags.HasFlag(RegUseFlags.PredInv))
                {
                    predInv = !predInv;
                }

                if (!MatchesPred(ref bv, (byte)((rawOp >> 16) & 7), predInv, predDefPos))
                {
                    return false;
                }

                if (!MatchesPred(ref bv, (byte)((rawOp >> 39) & 7), (rawOp & 0x40000000000) != 0, _regUses.SP1DefPos))
                {
                    return false;
                }

                byte ra = (byte)(rawOp >> 8);
                byte rb = props.HasFlag(InstProps.Rb2) ? (byte)rawOp : (byte)(rawOp >> 20);
                byte rc = (byte)(rawOp >> 39);

                if (!MatchesGpr(ref bv, ra, _regUses.SrcADefPos) ||
                    !MatchesGpr(ref bv, rb, _regUses.SrcBDefPos) ||
                    !MatchesGpr(ref bv, rc, _regUses.SrcCDefPos))
                {
                    return false;
                }

                if ((props & InstProps.PdMask) != 0)
                {
                    InstProps pdType = props & InstProps.PdMask;
                    int bit = pdType switch
                    {
                        InstProps.Pd => 3,
                        InstProps.LPd => 48,
                        InstProps.SPd => 30,
                        InstProps.TPd => 51,
                        InstProps.VPd => 45,
                        _ => throw new InvalidOperationException($"Table has unknown predicate destination {pdType}.")
                    };

                    if (!bv.DefPred((byte)((rawOp >> bit) & 7)))
                    {
                        return false;
                    }
                }

                if (props.HasFlag(InstProps.Rd) && !bv.DefGpr((byte)rawOp))
                {
                    return false;
                }

                return true;
            }

            private static bool MatchesPred(ref BlockVisitor bv, byte predIndex, bool predInv, int defPos)
            {
                if (defPos == -2)
                {
                    return true;
                }

                int expectedPred = defPos != -1 ? bv.UsePred(defPos) : RegisterConsts.PredicateTrueIndex;
                return expectedPred == predIndex && !predInv;
            }

            private static bool MatchesGpr(ref BlockVisitor bv, byte regIndex, int defPos)
            {
                if (defPos == -2)
                {
                    return true;
                }

                int expectedGpr = defPos != -1 ? bv.UseGpr(defPos) : RegisterConsts.RegisterZeroIndex;
                return expectedGpr == regIndex;
            }
        }

        private static readonly IInstPattern[] _fsiGetAddressPattern = new IInstPattern[]
        {
            new InstPattern<InstS2r>(InstName.S2r, (x) => x.SReg == SReg.Affinity),
            new InstPattern<InstS2r>(InstName.S2r, (x) => x.SReg == SReg.OrderingTicket),
            new InstPattern<InstLop32i>(InstName.Lop32i, (x) => !x.NegA && !x.NegB && !x.X && !x.WriteCC && x.LogicOp == LogicOp.And && x.Imm32 == 0xff, RegUses.PtRabc(0)),
            new InstPattern<InstLop32i>(InstName.Lop32i, (x) => !x.NegA && !x.NegB && !x.X && !x.WriteCC && x.LogicOp == LogicOp.And && x.Imm32 == 0xff, RegUses.PtRabc(1)),
            new InstPattern<InstIscaddR>(InstName.Iscadd, (x) => !x.WriteCC && x.AvgMode == AvgMode.NoNeg && x.Imm5 == 8, RegUses.PtRabc(2, 3)),
            new InstPattern<InstIscaddC>(InstName.Iscadd, (x) => x.WriteCC && x.AvgMode == AvgMode.NoNeg && x.Imm5 == 2 && x.CbufSlot == 0 && x.CbufOffset == 404, RegUses.PtRaCb(4)),
            new InstPattern<InstLop32i>(InstName.Lop32i, (x) => !x.NegA && !x.NegB && !x.X && !x.WriteCC && x.LogicOp == LogicOp.And && x.Imm32 == 0xfe00, RegUses.PtRabc(1)),
            new InstPattern<InstShrI>(InstName.Shr, (x) => !x.Signed && !x.Brev && x.M && x.XMode == 0 && x.Imm20 == 16, RegUses.PtRaIb(1)),
            new InstPattern<InstIadd32i>(InstName.Iadd32i, (x) => !x.Sat && !x.WriteCC && !x.X && x.AvgMode == AvgMode.NoNeg && x.Imm32 == 0x200, RegUses.PtRabc(6)),
            new InstPattern<InstIaddC>(InstName.Iadd, (x) => !x.WriteCC && x.X && x.AvgMode == AvgMode.NoNeg && x.CbufSlot == 0 && x.CbufOffset == 405, RegUses.PtRaCb(-1)),
            new InstPattern<InstRet>(InstName.Ret, (x) => x.Ccc == Ccc.T)
        };

        private static readonly IInstPattern[] _fsiIsLastWarpThreadPattern = new IInstPattern[]
        {
            new InstPattern<InstS2r>(InstName.S2r, (x) => x.SReg == SReg.ThreadKill),
            new InstPattern<InstS2r>(InstName.S2r, (x) => x.SReg == SReg.LaneId),
            new InstPattern<InstLopR>(InstName.Lop, (x) => !x.NegA && x.NegB && !x.WriteCC && !x.X && x.Lop == LogicOp.PassB && x.PredicateOp == PredicateOp.F, RegUses.PtRabc(-1, 0)),
            new InstPattern<InstIsetpR>(InstName.Isetp, (x) => !x.Signed && x.IComp == IComp.Ne && x.Bop == BoolOp.And, RegUses.PtPtRabc(2, -1)),
            new InstPattern<InstVote>(InstName.Vote, (x) => x.VoteMode == VoteMode.Any, RegUses.PtPn(3)),
            new InstPattern<InstFloR>(InstName.Flo, (x) => !x.Signed && !x.Sh && !x.NegB && !x.WriteCC, RegUses.PtRabc(-2, 4)),
            new InstPattern<InstIsetpR>(InstName.Isetp, (x) => !x.Signed && x.IComp == IComp.Eq && x.Bop == BoolOp.And, RegUses.PtPtRabc(5, 1)),
            new InstPattern<InstRet>(InstName.Ret, (x) => x.Ccc == Ccc.T)
        };

        private static readonly IInstPattern[] _fsiBeginPattern = new IInstPattern[]
        {
            new InstPattern<InstCal>(InstName.Cal, (x) => !x.Ca && x.Inc),
            new InstPattern<InstRet>(InstName.Ret, (x) => x.Ccc == Ccc.T, RegUses.Pn(-2)),
            new InstPattern<InstLdg>(InstName.Ldg, (x) => x.E && x.CacheOp == CacheOpLd.Cg),
            new InstPattern<InstShrI>(InstName.Shr, (x) => !x.Signed && !x.Brev && x.M && x.XMode == 0 && x.Imm20 == 16, RegUses.PtRaIb(2)),
            new InstPattern<InstIsetpR>(InstName.Isetp, (x) => !x.Signed && x.IComp == IComp.Eq && x.Bop == BoolOp.And, RegUses.PtPtRabc(3)),
            new InstPattern<InstRet>(InstName.Ret, (x) => x.Ccc == Ccc.T, RegUses.Pn(4)),
            new InstPattern<InstLdg>(InstName.Ldg, (x) => x.E && x.CacheOp == CacheOpLd.Cg),
            new InstPattern<InstShrI>(InstName.Shr, (x) => !x.Signed && !x.Brev && x.M && x.XMode == 0 && x.Imm20 == 16, RegUses.PtRaIb(6)),
            new InstPattern<InstIsetpR>(InstName.Isetp, (x) => !x.Signed && x.IComp == IComp.Eq && x.Bop == BoolOp.And, RegUses.PtPtRabc(7)),
            new InstPattern<InstBra>(InstName.Bra, (x) => x.Ccc == Ccc.T && !x.Ca && x.Imm24 == (-48 & 0xffffff), RegUses.NPn(8)),
            new InstPattern<InstRet>(InstName.Ret, (x) => x.Ccc == Ccc.T)
        };

        private static readonly IInstPattern[] _fsiEndPattern = new IInstPattern[]
        {
            new InstPattern<InstCal>(InstName.Cal, (x) => !x.Ca && x.Inc),
            new InstPattern<InstRet>(InstName.Ret, (x) => x.Ccc == Ccc.T, RegUses.Pn(-2)),
            new InstPattern<InstMembar>(InstName.Membar, (x) => x.Membar == Membar.Vc),
            new InstPattern<InstS2r>(InstName.S2r, (x) => x.SReg == SReg.ThreadKill),
            new InstPattern<InstIsetpR>(InstName.Isetp, (x) => !x.Signed && x.IComp == IComp.Ne && x.Bop == BoolOp.And, RegUses.PtPtRabc(3, -1)),
            new InstPattern<InstRet>(InstName.Ret, (x) => x.Ccc == Ccc.T, RegUses.Pn(4)),
            new InstPattern<InstVote>(InstName.Vote, (x) => x.VoteMode == VoteMode.All, RegUses.PtPt()),
            new InstPattern<InstS2r>(InstName.S2r, (x) => x.SReg == SReg.LaneId),
            new InstPattern<InstFloR>(InstName.Flo, (x) => !x.Signed && !x.Sh && !x.NegB && !x.WriteCC, RegUses.PtRabc(-2, 6)),
            new InstPattern<InstPopcR>(InstName.Popc, (x) => !x.NegB, RegUses.PtRabc(-2, 6)),
            new InstPattern<InstIsetpR>(InstName.Isetp, (x) => !x.Signed && x.IComp == IComp.Eq && x.Bop == BoolOp.And, RegUses.PtPtRabc(8, 7)),
            new InstPattern<InstXmadR>(InstName.Xmad, (x) => x.XmadCop == XmadCop.Cfull && !x.Psl && !x.Mrg && !x.HiloA && !x.HiloB, RegUses.PtRabc(-2, 9, -1)),
            new InstPattern<InstXmadR>(InstName.Xmad, (x) => x.XmadCop == XmadCop.Cfull && !x.Psl && x.Mrg && !x.HiloA && x.HiloB, RegUses.PtRabc(-2, 9, -1)),
            new InstPattern<InstXmadR>(InstName.Xmad, (x) => x.XmadCop == XmadCop.Cbcc && x.Psl && !x.Mrg && x.HiloA && x.HiloB, RegUses.PtRabc(-2, 12, 11)),
            new InstPattern<InstRed>(InstName.Red, (x) => x.E && x.RedOp == RedOp.Add && x.RedSize == AtomSize.U32, RegUses.PnRabc(10, -2, 13)),
            new InstPattern<InstRet>(InstName.Ret, (x) => x.Ccc == Ccc.T)
        };

        public static FunctionMatchResult FindMatch(Block[][] blocks, int funcIndex)
        {
            Block[] blocksForFunc = blocks[funcIndex];

            if (Matches(blocksForFunc, _fsiGetAddressPattern) || Matches(blocksForFunc, _fsiIsLastWarpThreadPattern))
            {
                return FunctionMatchResult.Unused;
            }
            else if (MatchesWithInnerCall(blocks, funcIndex, _fsiBeginPattern, _fsiIsLastWarpThreadPattern))
            {
                return FunctionMatchResult.FSIBegin;
            }
            else if (MatchesWithInnerCall(blocks, funcIndex, _fsiEndPattern, _fsiIsLastWarpThreadPattern))
            {
                return FunctionMatchResult.FSIEnd;
            }

            return FunctionMatchResult.NoMatch;
        }

        private static bool Matches(Block[] blocks, IInstPattern[] pattern)
        {
            BlockVisitor bv = new BlockVisitor(blocks, pattern);

            for (int i = 0; i < pattern.Length; i++)
            {
                if (!pattern[i].Matches(ref bv))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesWithInnerCall(Block[][] blocks, int funcIndex, IInstPattern[] pattern, IInstPattern[] innerPattern)
        {
            Block[] blocksForFunc = blocks[funcIndex];

            BlockVisitor bv = new BlockVisitor(blocksForFunc, pattern);

            if (!bv.GetOp(InstName.Cal, out InstOp opInfo))
            {
                return false;
            }

            Block[] callTarget = FindFunc(blocks, opInfo.GetAbsoluteAddress());
            if (callTarget == null || !Matches(callTarget, innerPattern))
            {
                return false;
            }

            bv.Rewind();

            for (int i = 0; i < pattern.Length; i++)
            {
                if (!pattern[i].Matches(ref bv))
                {
                    return false;
                }
            }

            return true;
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
    }
}
