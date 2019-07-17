using ARMeilleure.CodeGen.Optimizations;
using ARMeilleure.CodeGen.RegisterAllocators;
using ARMeilleure.CodeGen.Unwinding;
using ARMeilleure.Common;
using ARMeilleure.Diagnostics;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ARMeilleure.CodeGen.X86
{
    static class CodeGenerator
    {
        private static Action<CodeGenContext, Operation>[] _instTable;

        private enum X86IntrinsicType
        {
            Comis_,
            PopCount,
            Unary,
            UnaryToGpr,
            Binary,
            BinaryImm,
            Ternary,
            TernaryImm
        }

        private struct X86Intrinsic
        {
            public X86Instruction   Inst { get; }
            public X86IntrinsicType Type { get; }

            public X86Intrinsic(X86Instruction inst, X86IntrinsicType type)
            {
                Inst = inst;
                Type = type;
            }
        }

        private static X86Intrinsic[] _x86InstTable;

        static CodeGenerator()
        {
            _instTable = new Action<CodeGenContext, Operation>[(int)Instruction.Count];

            _x86InstTable = new X86Intrinsic[(int)Instruction.Count];

            Add(Instruction.Add,                     GenerateAdd);
            Add(Instruction.BitwiseAnd,              GenerateBitwiseAnd);
            Add(Instruction.BitwiseExclusiveOr,      GenerateBitwiseExclusiveOr);
            Add(Instruction.BitwiseNot,              GenerateBitwiseNot);
            Add(Instruction.BitwiseOr,               GenerateBitwiseOr);
            Add(Instruction.Branch,                  GenerateBranch);
            Add(Instruction.BranchIfFalse,           GenerateBranchIfFalse);
            Add(Instruction.BranchIfTrue,            GenerateBranchIfTrue);
            Add(Instruction.ByteSwap,                GenerateByteSwap);
            Add(Instruction.Call,                    GenerateCall);
            Add(Instruction.CompareAndSwap128,       GenerateCompareAndSwap128);
            Add(Instruction.CompareEqual,            GenerateCompareEqual);
            Add(Instruction.CompareGreater,          GenerateCompareGreater);
            Add(Instruction.CompareGreaterOrEqual,   GenerateCompareGreaterOrEqual);
            Add(Instruction.CompareGreaterOrEqualUI, GenerateCompareGreaterOrEqualUI);
            Add(Instruction.CompareGreaterUI,        GenerateCompareGreaterUI);
            Add(Instruction.CompareLess,             GenerateCompareLess);
            Add(Instruction.CompareLessOrEqual,      GenerateCompareLessOrEqual);
            Add(Instruction.CompareLessOrEqualUI,    GenerateCompareLessOrEqualUI);
            Add(Instruction.CompareLessUI,           GenerateCompareLessUI);
            Add(Instruction.CompareNotEqual,         GenerateCompareNotEqual);
            Add(Instruction.ConditionalSelect,       GenerateConditionalSelect);
            Add(Instruction.ConvertI64ToI32,         GenerateConvertI64ToI32);
            Add(Instruction.ConvertToFP,             GenerateConvertToFP);
            Add(Instruction.Copy,                    GenerateCopy);
            Add(Instruction.CountLeadingZeros,       GenerateCountLeadingZeros);
            Add(Instruction.Divide,                  GenerateDivide);
            Add(Instruction.DivideUI,                GenerateDivideUI);
            Add(Instruction.Fill,                    GenerateFill);
            Add(Instruction.Load,                    GenerateLoad);
            Add(Instruction.Load16,                  GenerateLoad16);
            Add(Instruction.Load8,                   GenerateLoad8);
            Add(Instruction.Multiply,                GenerateMultiply);
            Add(Instruction.Multiply64HighSI,        GenerateMultiply64HighSI);
            Add(Instruction.Multiply64HighUI,        GenerateMultiply64HighUI);
            Add(Instruction.Negate,                  GenerateNegate);
            Add(Instruction.Return,                  GenerateReturn);
            Add(Instruction.RotateRight,             GenerateRotateRight);
            Add(Instruction.ShiftLeft,               GenerateShiftLeft);
            Add(Instruction.ShiftRightSI,            GenerateShiftRightSI);
            Add(Instruction.ShiftRightUI,            GenerateShiftRightUI);
            Add(Instruction.SignExtend16,            GenerateSignExtend16);
            Add(Instruction.SignExtend32,            GenerateSignExtend32);
            Add(Instruction.SignExtend8,             GenerateSignExtend8);
            Add(Instruction.Spill,                   GenerateSpill);
            Add(Instruction.SpillArg,                GenerateSpillArg);
            Add(Instruction.StackAlloc,              GenerateStackAlloc);
            Add(Instruction.Store,                   GenerateStore);
            Add(Instruction.Store16,                 GenerateStore16);
            Add(Instruction.Store8,                  GenerateStore8);
            Add(Instruction.Subtract,                GenerateSubtract);
            Add(Instruction.VectorCreateScalar,      GenerateVectorCreateScalar);
            Add(Instruction.VectorExtract,           GenerateVectorExtract);
            Add(Instruction.VectorExtract16,         GenerateVectorExtract16);
            Add(Instruction.VectorExtract8,          GenerateVectorExtract8);
            Add(Instruction.VectorInsert,            GenerateVectorInsert);
            Add(Instruction.VectorInsert16,          GenerateVectorInsert16);
            Add(Instruction.VectorInsert8,           GenerateVectorInsert8);
            Add(Instruction.VectorOne,               GenerateVectorOne);
            Add(Instruction.VectorZero,              GenerateVectorZero);
            Add(Instruction.VectorZeroUpper64,       GenerateVectorZeroUpper64);
            Add(Instruction.VectorZeroUpper96,       GenerateVectorZeroUpper96);
            Add(Instruction.ZeroExtend16,            GenerateZeroExtend16);
            Add(Instruction.ZeroExtend32,            GenerateZeroExtend32);
            Add(Instruction.ZeroExtend8,             GenerateZeroExtend8);

            Add(Instruction.X86Addpd,      new X86Intrinsic(X86Instruction.Addpd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Addps,      new X86Intrinsic(X86Instruction.Addps,      X86IntrinsicType.Binary));
            Add(Instruction.X86Addsd,      new X86Intrinsic(X86Instruction.Addsd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Addss,      new X86Intrinsic(X86Instruction.Addss,      X86IntrinsicType.Binary));
            Add(Instruction.X86Andnpd,     new X86Intrinsic(X86Instruction.Andnpd,     X86IntrinsicType.Binary));
            Add(Instruction.X86Andnps,     new X86Intrinsic(X86Instruction.Andnps,     X86IntrinsicType.Binary));
            Add(Instruction.X86Cmppd,      new X86Intrinsic(X86Instruction.Cmppd,      X86IntrinsicType.TernaryImm));
            Add(Instruction.X86Cmpps,      new X86Intrinsic(X86Instruction.Cmpps,      X86IntrinsicType.TernaryImm));
            Add(Instruction.X86Cmpsd,      new X86Intrinsic(X86Instruction.Cmpsd,      X86IntrinsicType.TernaryImm));
            Add(Instruction.X86Cmpss,      new X86Intrinsic(X86Instruction.Cmpss,      X86IntrinsicType.TernaryImm));
            Add(Instruction.X86Comisdeq,   new X86Intrinsic(X86Instruction.Comisd,     X86IntrinsicType.Comis_));
            Add(Instruction.X86Comisdge,   new X86Intrinsic(X86Instruction.Comisd,     X86IntrinsicType.Comis_));
            Add(Instruction.X86Comisdlt,   new X86Intrinsic(X86Instruction.Comisd,     X86IntrinsicType.Comis_));
            Add(Instruction.X86Comisseq,   new X86Intrinsic(X86Instruction.Comiss,     X86IntrinsicType.Comis_));
            Add(Instruction.X86Comissge,   new X86Intrinsic(X86Instruction.Comiss,     X86IntrinsicType.Comis_));
            Add(Instruction.X86Comisslt,   new X86Intrinsic(X86Instruction.Comiss,     X86IntrinsicType.Comis_));
            Add(Instruction.X86Cvtdq2pd,   new X86Intrinsic(X86Instruction.Cvtdq2pd,   X86IntrinsicType.Unary));
            Add(Instruction.X86Cvtdq2ps,   new X86Intrinsic(X86Instruction.Cvtdq2ps,   X86IntrinsicType.Unary));
            Add(Instruction.X86Cvtpd2dq,   new X86Intrinsic(X86Instruction.Cvtpd2dq,   X86IntrinsicType.Unary));
            Add(Instruction.X86Cvtpd2ps,   new X86Intrinsic(X86Instruction.Cvtpd2ps,   X86IntrinsicType.Unary));
            Add(Instruction.X86Cvtps2dq,   new X86Intrinsic(X86Instruction.Cvtps2dq,   X86IntrinsicType.Unary));
            Add(Instruction.X86Cvtps2pd,   new X86Intrinsic(X86Instruction.Cvtps2pd,   X86IntrinsicType.Unary));
            Add(Instruction.X86Cvtsd2si,   new X86Intrinsic(X86Instruction.Cvtsd2si,   X86IntrinsicType.UnaryToGpr));
            Add(Instruction.X86Cvtsd2ss,   new X86Intrinsic(X86Instruction.Cvtsd2ss,   X86IntrinsicType.Binary));
            Add(Instruction.X86Cvtss2sd,   new X86Intrinsic(X86Instruction.Cvtss2sd,   X86IntrinsicType.Binary));
            Add(Instruction.X86Divpd,      new X86Intrinsic(X86Instruction.Divpd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Divps,      new X86Intrinsic(X86Instruction.Divps,      X86IntrinsicType.Binary));
            Add(Instruction.X86Divsd,      new X86Intrinsic(X86Instruction.Divsd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Divss,      new X86Intrinsic(X86Instruction.Divss,      X86IntrinsicType.Binary));
            Add(Instruction.X86Haddpd,     new X86Intrinsic(X86Instruction.Haddpd,     X86IntrinsicType.Binary));
            Add(Instruction.X86Haddps,     new X86Intrinsic(X86Instruction.Haddps,     X86IntrinsicType.Binary));
            Add(Instruction.X86Maxpd,      new X86Intrinsic(X86Instruction.Maxpd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Maxps,      new X86Intrinsic(X86Instruction.Maxps,      X86IntrinsicType.Binary));
            Add(Instruction.X86Maxsd,      new X86Intrinsic(X86Instruction.Maxsd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Maxss,      new X86Intrinsic(X86Instruction.Maxss,      X86IntrinsicType.Binary));
            Add(Instruction.X86Minpd,      new X86Intrinsic(X86Instruction.Minpd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Minps,      new X86Intrinsic(X86Instruction.Minps,      X86IntrinsicType.Binary));
            Add(Instruction.X86Minsd,      new X86Intrinsic(X86Instruction.Minsd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Minss,      new X86Intrinsic(X86Instruction.Minss,      X86IntrinsicType.Binary));
            Add(Instruction.X86Movhlps,    new X86Intrinsic(X86Instruction.Movhlps,    X86IntrinsicType.Binary));
            Add(Instruction.X86Movlhps,    new X86Intrinsic(X86Instruction.Movlhps,    X86IntrinsicType.Binary));
            Add(Instruction.X86Mulpd,      new X86Intrinsic(X86Instruction.Mulpd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Mulps,      new X86Intrinsic(X86Instruction.Mulps,      X86IntrinsicType.Binary));
            Add(Instruction.X86Mulsd,      new X86Intrinsic(X86Instruction.Mulsd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Mulss,      new X86Intrinsic(X86Instruction.Mulss,      X86IntrinsicType.Binary));
            Add(Instruction.X86Paddb,      new X86Intrinsic(X86Instruction.Paddb,      X86IntrinsicType.Binary));
            Add(Instruction.X86Paddd,      new X86Intrinsic(X86Instruction.Paddd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Paddq,      new X86Intrinsic(X86Instruction.Paddq,      X86IntrinsicType.Binary));
            Add(Instruction.X86Paddw,      new X86Intrinsic(X86Instruction.Paddw,      X86IntrinsicType.Binary));
            Add(Instruction.X86Pand,       new X86Intrinsic(X86Instruction.Pand,       X86IntrinsicType.Binary));
            Add(Instruction.X86Pandn,      new X86Intrinsic(X86Instruction.Pandn,      X86IntrinsicType.Binary));
            Add(Instruction.X86Pavgb,      new X86Intrinsic(X86Instruction.Pavgb,      X86IntrinsicType.Binary));
            Add(Instruction.X86Pavgw,      new X86Intrinsic(X86Instruction.Pavgw,      X86IntrinsicType.Binary));
            Add(Instruction.X86Pblendvb,   new X86Intrinsic(X86Instruction.Pblendvb,   X86IntrinsicType.Ternary));
            Add(Instruction.X86Pcmpeqb,    new X86Intrinsic(X86Instruction.Pcmpeqb,    X86IntrinsicType.Binary));
            Add(Instruction.X86Pcmpeqd,    new X86Intrinsic(X86Instruction.Pcmpeqd,    X86IntrinsicType.Binary));
            Add(Instruction.X86Pcmpeqq,    new X86Intrinsic(X86Instruction.Pcmpeqq,    X86IntrinsicType.Binary));
            Add(Instruction.X86Pcmpeqw,    new X86Intrinsic(X86Instruction.Pcmpeqw,    X86IntrinsicType.Binary));
            Add(Instruction.X86Pcmpgtb,    new X86Intrinsic(X86Instruction.Pcmpgtb,    X86IntrinsicType.Binary));
            Add(Instruction.X86Pcmpgtd,    new X86Intrinsic(X86Instruction.Pcmpgtd,    X86IntrinsicType.Binary));
            Add(Instruction.X86Pcmpgtq,    new X86Intrinsic(X86Instruction.Pcmpgtq,    X86IntrinsicType.Binary));
            Add(Instruction.X86Pcmpgtw,    new X86Intrinsic(X86Instruction.Pcmpgtw,    X86IntrinsicType.Binary));
            Add(Instruction.X86Pmaxsb,     new X86Intrinsic(X86Instruction.Pmaxsb,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pmaxsd,     new X86Intrinsic(X86Instruction.Pmaxsd,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pmaxsw,     new X86Intrinsic(X86Instruction.Pmaxsw,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pmaxub,     new X86Intrinsic(X86Instruction.Pmaxub,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pmaxud,     new X86Intrinsic(X86Instruction.Pmaxud,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pmaxuw,     new X86Intrinsic(X86Instruction.Pmaxuw,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pminsb,     new X86Intrinsic(X86Instruction.Pminsb,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pminsd,     new X86Intrinsic(X86Instruction.Pminsd,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pminsw,     new X86Intrinsic(X86Instruction.Pminsw,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pminub,     new X86Intrinsic(X86Instruction.Pminub,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pminud,     new X86Intrinsic(X86Instruction.Pminud,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pminuw,     new X86Intrinsic(X86Instruction.Pminuw,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pmovsxbw,   new X86Intrinsic(X86Instruction.Pmovsxbw,   X86IntrinsicType.Unary));
            Add(Instruction.X86Pmovsxdq,   new X86Intrinsic(X86Instruction.Pmovsxdq,   X86IntrinsicType.Unary));
            Add(Instruction.X86Pmovsxwd,   new X86Intrinsic(X86Instruction.Pmovsxwd,   X86IntrinsicType.Unary));
            Add(Instruction.X86Pmovzxbw,   new X86Intrinsic(X86Instruction.Pmovzxbw,   X86IntrinsicType.Unary));
            Add(Instruction.X86Pmovzxdq,   new X86Intrinsic(X86Instruction.Pmovzxdq,   X86IntrinsicType.Unary));
            Add(Instruction.X86Pmovzxwd,   new X86Intrinsic(X86Instruction.Pmovzxwd,   X86IntrinsicType.Unary));
            Add(Instruction.X86Pmulld,     new X86Intrinsic(X86Instruction.Pmulld,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pmullw,     new X86Intrinsic(X86Instruction.Pmullw,     X86IntrinsicType.Binary));
            Add(Instruction.X86Popcnt,     new X86Intrinsic(X86Instruction.Popcnt,     X86IntrinsicType.PopCount));
            Add(Instruction.X86Por,        new X86Intrinsic(X86Instruction.Por,        X86IntrinsicType.Binary));
            Add(Instruction.X86Pshufb,     new X86Intrinsic(X86Instruction.Pshufb,     X86IntrinsicType.Binary));
            Add(Instruction.X86Pslld,      new X86Intrinsic(X86Instruction.Pslld,      X86IntrinsicType.Binary));
            Add(Instruction.X86Pslldq,     new X86Intrinsic(X86Instruction.Pslldq,     X86IntrinsicType.Binary));
            Add(Instruction.X86Psllq,      new X86Intrinsic(X86Instruction.Psllq,      X86IntrinsicType.Binary));
            Add(Instruction.X86Psllw,      new X86Intrinsic(X86Instruction.Psllw,      X86IntrinsicType.Binary));
            Add(Instruction.X86Psrad,      new X86Intrinsic(X86Instruction.Psrad,      X86IntrinsicType.Binary));
            Add(Instruction.X86Psraw,      new X86Intrinsic(X86Instruction.Psraw,      X86IntrinsicType.Binary));
            Add(Instruction.X86Psrld,      new X86Intrinsic(X86Instruction.Psrld,      X86IntrinsicType.Binary));
            Add(Instruction.X86Psrlq,      new X86Intrinsic(X86Instruction.Psrlq,      X86IntrinsicType.Binary));
            Add(Instruction.X86Psrldq,     new X86Intrinsic(X86Instruction.Psrldq,     X86IntrinsicType.Binary));
            Add(Instruction.X86Psrlw,      new X86Intrinsic(X86Instruction.Psrlw,      X86IntrinsicType.Binary));
            Add(Instruction.X86Psubb,      new X86Intrinsic(X86Instruction.Psubb,      X86IntrinsicType.Binary));
            Add(Instruction.X86Psubd,      new X86Intrinsic(X86Instruction.Psubd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Psubq,      new X86Intrinsic(X86Instruction.Psubq,      X86IntrinsicType.Binary));
            Add(Instruction.X86Psubw,      new X86Intrinsic(X86Instruction.Psubw,      X86IntrinsicType.Binary));
            Add(Instruction.X86Punpckhbw,  new X86Intrinsic(X86Instruction.Punpckhbw,  X86IntrinsicType.Binary));
            Add(Instruction.X86Punpckhdq,  new X86Intrinsic(X86Instruction.Punpckhdq,  X86IntrinsicType.Binary));
            Add(Instruction.X86Punpckhqdq, new X86Intrinsic(X86Instruction.Punpckhqdq, X86IntrinsicType.Binary));
            Add(Instruction.X86Punpckhwd,  new X86Intrinsic(X86Instruction.Punpckhwd,  X86IntrinsicType.Binary));
            Add(Instruction.X86Punpcklbw,  new X86Intrinsic(X86Instruction.Punpcklbw,  X86IntrinsicType.Binary));
            Add(Instruction.X86Punpckldq,  new X86Intrinsic(X86Instruction.Punpckldq,  X86IntrinsicType.Binary));
            Add(Instruction.X86Punpcklqdq, new X86Intrinsic(X86Instruction.Punpcklqdq, X86IntrinsicType.Binary));
            Add(Instruction.X86Punpcklwd,  new X86Intrinsic(X86Instruction.Punpcklwd,  X86IntrinsicType.Binary));
            Add(Instruction.X86Pxor,       new X86Intrinsic(X86Instruction.Pxor,       X86IntrinsicType.Binary));
            Add(Instruction.X86Rcpps,      new X86Intrinsic(X86Instruction.Rcpps,      X86IntrinsicType.Unary));
            Add(Instruction.X86Rcpss,      new X86Intrinsic(X86Instruction.Rcpss,      X86IntrinsicType.Unary));
            Add(Instruction.X86Roundpd,    new X86Intrinsic(X86Instruction.Roundpd,    X86IntrinsicType.BinaryImm));
            Add(Instruction.X86Roundps,    new X86Intrinsic(X86Instruction.Roundps,    X86IntrinsicType.BinaryImm));
            Add(Instruction.X86Roundsd,    new X86Intrinsic(X86Instruction.Roundsd,    X86IntrinsicType.BinaryImm));
            Add(Instruction.X86Roundss,    new X86Intrinsic(X86Instruction.Roundss,    X86IntrinsicType.BinaryImm));
            Add(Instruction.X86Rsqrtps,    new X86Intrinsic(X86Instruction.Rsqrtps,    X86IntrinsicType.Unary));
            Add(Instruction.X86Rsqrtss,    new X86Intrinsic(X86Instruction.Rsqrtss,    X86IntrinsicType.Unary));
            Add(Instruction.X86Shufpd,     new X86Intrinsic(X86Instruction.Shufpd,     X86IntrinsicType.TernaryImm));
            Add(Instruction.X86Shufps,     new X86Intrinsic(X86Instruction.Shufps,     X86IntrinsicType.TernaryImm));
            Add(Instruction.X86Sqrtpd,     new X86Intrinsic(X86Instruction.Sqrtpd,     X86IntrinsicType.Unary));
            Add(Instruction.X86Sqrtps,     new X86Intrinsic(X86Instruction.Sqrtps,     X86IntrinsicType.Unary));
            Add(Instruction.X86Sqrtsd,     new X86Intrinsic(X86Instruction.Sqrtsd,     X86IntrinsicType.Unary));
            Add(Instruction.X86Sqrtss,     new X86Intrinsic(X86Instruction.Sqrtss,     X86IntrinsicType.Unary));
            Add(Instruction.X86Subpd,      new X86Intrinsic(X86Instruction.Subpd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Subps,      new X86Intrinsic(X86Instruction.Subps,      X86IntrinsicType.Binary));
            Add(Instruction.X86Subsd,      new X86Intrinsic(X86Instruction.Subsd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Subss,      new X86Intrinsic(X86Instruction.Subss,      X86IntrinsicType.Binary));
            Add(Instruction.X86Unpckhpd,   new X86Intrinsic(X86Instruction.Unpckhpd,   X86IntrinsicType.Binary));
            Add(Instruction.X86Unpckhps,   new X86Intrinsic(X86Instruction.Unpckhps,   X86IntrinsicType.Binary));
            Add(Instruction.X86Unpcklpd,   new X86Intrinsic(X86Instruction.Unpcklpd,   X86IntrinsicType.Binary));
            Add(Instruction.X86Unpcklps,   new X86Intrinsic(X86Instruction.Unpcklps,   X86IntrinsicType.Binary));
            Add(Instruction.X86Xorpd,      new X86Intrinsic(X86Instruction.Xorpd,      X86IntrinsicType.Binary));
            Add(Instruction.X86Xorps,      new X86Intrinsic(X86Instruction.Xorps,      X86IntrinsicType.Binary));
        }

        private static void Add(Instruction inst, Action<CodeGenContext, Operation> func)
        {
            _instTable[(int)inst] = func;
        }

        private static void Add(Instruction inst, X86Intrinsic intrin)
        {
            _x86InstTable[(int)inst] = intrin;
        }

        public static CompiledFunction Generate(CompilerContext cctx)
        {
            ControlFlowGraph cfg = cctx.Cfg;

            Logger.StartPass(PassName.Optimization);

            Optimizer.RunPass(cfg);

            Logger.EndPass(PassName.Optimization, cfg);

            Logger.StartPass(PassName.PreAllocation);

            StackAllocator stackAlloc = new StackAllocator();

            PreAllocator.RunPass(cctx, stackAlloc, out int maxCallArgs);

            Logger.EndPass(PassName.PreAllocation, cfg);

            Logger.StartPass(PassName.RegisterAllocation);

            FastLinearScan regAlloc = new FastLinearScan();

            RegisterMasks regMasks = new RegisterMasks(
                CallingConvention.GetIntAvailableRegisters(),
                CallingConvention.GetVecAvailableRegisters(),
                CallingConvention.GetIntCallerSavedRegisters(),
                CallingConvention.GetVecCallerSavedRegisters(),
                CallingConvention.GetIntCalleeSavedRegisters(),
                CallingConvention.GetVecCalleeSavedRegisters());

            AllocationResult allocResult = regAlloc.RunPass(cfg, stackAlloc, regMasks);

            Logger.EndPass(PassName.RegisterAllocation, cfg);

            Logger.StartPass(PassName.CodeGeneration);

            using (MemoryStream stream = new MemoryStream())
            {
                CodeGenContext context = new CodeGenContext(stream, allocResult, maxCallArgs, cfg.Blocks.Count);

                UnwindInfo unwindInfo = WritePrologue(context);

                foreach (BasicBlock block in cfg.Blocks)
                {
                    context.EnterBlock(block);

                    foreach (Node node in block.Operations)
                    {
                        if (node is Operation operation)
                        {
                            GenerateOperation(context, operation);
                        }
                    }
                }

                Logger.EndPass(PassName.CodeGeneration);

                return new CompiledFunction(context.GetCode(), unwindInfo);
            }
        }

        private static void GenerateOperation(CodeGenContext context, Operation operation)
        {
            if (operation.Inst > Instruction.X86Intrinsic_Start &&
                operation.Inst < Instruction.X86Intrinsic_End)
            {
                X86Intrinsic intrin = _x86InstTable[(int)operation.Inst];

                switch (intrin.Type)
                {
                    case X86IntrinsicType.Comis_:
                    {
                        Operand dest = operation.Dest;
                        Operand src1 = operation.GetSource(0);
                        Operand src2 = operation.GetSource(1);

                        switch (operation.Inst)
                        {
                            case Instruction.X86Comisdeq:
                                context.Assembler.Comisd(src1, src2);
                                context.Assembler.Setcc(dest, X86Condition.Equal);
                                break;

                            case Instruction.X86Comisdge:
                                context.Assembler.Comisd(src1, src2);
                                context.Assembler.Setcc(dest, X86Condition.AboveOrEqual);
                                break;

                            case Instruction.X86Comisdlt:
                                context.Assembler.Comisd(src1, src2);
                                context.Assembler.Setcc(dest, X86Condition.Below);
                                break;

                            case Instruction.X86Comisseq:
                                context.Assembler.Comiss(src1, src2);
                                context.Assembler.Setcc(dest, X86Condition.Equal);
                                break;

                            case Instruction.X86Comissge:
                                context.Assembler.Comiss(src1, src2);
                                context.Assembler.Setcc(dest, X86Condition.AboveOrEqual);
                                break;

                            case Instruction.X86Comisslt:
                                context.Assembler.Comiss(src1, src2);
                                context.Assembler.Setcc(dest, X86Condition.Below);
                                break;
                        }

                        context.Assembler.Movzx8(dest, dest);

                        break;
                    }

                    case X86IntrinsicType.PopCount:
                    {
                        Operand dest   = operation.Dest;
                        Operand source = operation.GetSource(0);

                        context.Assembler.Popcnt(dest, source);

                        break;
                    }

                    case X86IntrinsicType.Unary:
                    {
                        Operand dest   = operation.Dest;
                        Operand source = operation.GetSource(0);

                        context.Assembler.WriteInstruction(intrin.Inst, dest, source);

                        break;
                    }

                    case X86IntrinsicType.UnaryToGpr:
                    {
                        Operand dest   = operation.Dest;
                        Operand source = operation.GetSource(0);

                        context.Assembler.WriteInstruction(intrin.Inst, dest, source);

                        break;
                    }

                    case X86IntrinsicType.Binary:
                    {
                        Operand dest = operation.Dest;
                        Operand src1 = operation.GetSource(0);
                        Operand src2 = operation.GetSource(1);

                        context.Assembler.WriteInstruction(intrin.Inst, dest, src1, src2);

                        break;
                    }

                    case X86IntrinsicType.BinaryImm:
                    {
                        Operand dest   = operation.Dest;
                        Operand source = operation.GetSource(0);
                        byte    imm    = operation.GetSource(1).AsByte();

                        context.Assembler.WriteInstruction(intrin.Inst, dest, source, imm);

                        break;
                    }

                    case X86IntrinsicType.Ternary:
                    {
                        Operand dest = operation.Dest;
                        Operand src1 = operation.GetSource(0);
                        Operand src2 = operation.GetSource(1);
                        Operand src3 = operation.GetSource(2);

                        context.Assembler.WriteInstruction(intrin.Inst, dest, src1, src2, src3);

                        break;
                    }

                    case X86IntrinsicType.TernaryImm:
                    {
                        Operand dest = operation.Dest;
                        Operand src1 = operation.GetSource(0);
                        Operand src2 = operation.GetSource(1);
                        byte    imm  = operation.GetSource(2).AsByte();

                        context.Assembler.WriteInstruction(intrin.Inst, dest, src1, src2, imm);

                        break;
                    }
                }
            }
            else
            {
                Action<CodeGenContext, Operation> func = _instTable[(int)operation.Inst];

                if (func != null)
                {
                    func(context, operation);
                }
                else
                {
                    throw new ArgumentException($"Invalid instruction \"{operation.Inst}\".");
                }
            }
        }

        private static void GenerateAdd(CodeGenContext context, Operation operation)
        {
            ValidateDestSrc1(operation);

            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            if (dest.Type.IsInteger())
            {
                context.Assembler.Add(dest, src2);
            }
            else if (dest.Type == OperandType.FP32)
            {
                context.Assembler.Addss(dest, src2, src1);
            }
            else /* if (dest.Type == OperandType.FP64) */
            {
                context.Assembler.Addsd(dest, src2, src1);
            }
        }

        private static void GenerateBitwiseAnd(CodeGenContext context, Operation operation)
        {
            ValidateDestSrc1(operation);

            context.Assembler.And(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateBitwiseExclusiveOr(CodeGenContext context, Operation operation)
        {
            ValidateDestSrc1(operation);

            if (operation.Dest.Type.IsInteger())
            {
                context.Assembler.Xor(operation.Dest, operation.GetSource(1));
            }
            else
            {
                context.Assembler.Xorps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
            }
        }

        private static void GenerateBitwiseNot(CodeGenContext context, Operation operation)
        {
            context.Assembler.Not(operation.Dest);
        }

        private static void GenerateBitwiseOr(CodeGenContext context, Operation operation)
        {
            ValidateDestSrc1(operation);

            context.Assembler.Or(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateBranch(CodeGenContext context, Operation operation)
        {
            context.JumpTo(context.CurrBlock.Branch);
        }

        private static void GenerateBranchIfFalse(CodeGenContext context, Operation operation)
        {
            context.Assembler.Test(operation.GetSource(0), operation.GetSource(0));

            context.JumpTo(X86Condition.Equal, context.CurrBlock.Branch);
        }

        private static void GenerateBranchIfTrue(CodeGenContext context, Operation operation)
        {
            context.Assembler.Test(operation.GetSource(0), operation.GetSource(0));

            context.JumpTo(X86Condition.NotEqual, context.CurrBlock.Branch);
        }

        private static void GenerateByteSwap(CodeGenContext context, Operation operation)
        {
            context.Assembler.Bswap(operation.Dest);
        }

        private static void GenerateCall(CodeGenContext context, Operation operation)
        {
            context.Assembler.Call(operation.GetSource(0));
        }

        private static void GenerateCompareAndSwap128(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0);

            X86MemoryOperand memOp = new X86MemoryOperand(OperandType.I64, src1, null, Scale.x1, 0);

            context.Assembler.Cmpxchg16b(memOp);
        }

        private static void GenerateCompareEqual(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.Equal);
        }

        private static void GenerateCompareGreater(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.Greater);
        }

        private static void GenerateCompareGreaterOrEqual(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.GreaterOrEqual);
        }

        private static void GenerateCompareGreaterOrEqualUI(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.AboveOrEqual);
        }

        private static void GenerateCompareGreaterUI(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.Above);
        }

        private static void GenerateCompareLess(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.Less);
        }

        private static void GenerateCompareLessOrEqual(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.LessOrEqual);
        }

        private static void GenerateCompareLessOrEqualUI(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.BelowOrEqual);
        }

        private static void GenerateCompareLessUI(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.Below);
        }


        private static void GenerateCompareNotEqual(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.NotEqual);
        }

        private static void GenerateConditionalSelect(CodeGenContext context, Operation operation)
        {
            context.Assembler.Test(operation.GetSource(0), operation.GetSource(0));
            context.Assembler.Cmovcc(operation.Dest, operation.GetSource(1), X86Condition.NotEqual);
        }

        private static void GenerateConvertI64ToI32(CodeGenContext context, Operation operation)
        {
            Operand dest   = operation.Dest;
            Operand source = operation.GetSource(0);

            context.Assembler.Mov(dest, Get32BitsRegister(source.GetRegister()));
        }

        private static void GenerateConvertToFP(CodeGenContext context, Operation operation)
        {
            Operand dest   = operation.Dest;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type == OperandType.FP32 || dest.Type == OperandType.FP64);

            if (dest.Type == OperandType.FP32)
            {
                Debug.Assert(source.Type.IsInteger() || source.Type == OperandType.FP64);

                if (source.Type.IsInteger())
                {
                    context.Assembler.Xorps(dest, dest, dest);
                    context.Assembler.Cvtsi2ss(dest, source, dest);
                }
                else /* if (source.Type == OperandType.FP64) */
                {
                    context.Assembler.Cvtsd2ss(dest, source, dest);

                    ZeroUpper96(context, dest, dest);
                }
            }
            else /* if (dest.Type == OperandType.FP64) */
            {
                Debug.Assert(source.Type.IsInteger() || source.Type == OperandType.FP32);

                if (source.Type.IsInteger())
                {
                    context.Assembler.Xorps(dest, dest, dest);
                    context.Assembler.Cvtsi2sd(dest, source, dest);
                }
                else /* if (source.Type == OperandType.FP32) */
                {
                    context.Assembler.Cvtss2sd(dest, source, dest);

                    ZeroUpper64(context, dest, dest);
                }
            }
        }

        private static void GenerateCopy(CodeGenContext context, Operation operation)
        {
            Operand dest   = operation.Dest;
            Operand source = operation.GetSource(0);

            //Moves to the same register/memory location with the same type
            //are useless, we don't need to emit any code in this case.
            if (dest.Kind  == source.Kind &&
                dest.Type  == source.Type &&
                dest.Value == source.Value)
            {
                return;
            }

            if (dest.Kind   == OperandKind.Register &&
                source.Kind == OperandKind.Constant && source.Value == 0)
            {
                //Assemble "mov reg, 0" as "xor reg, reg" as the later is more efficient.
                dest = Get32BitsRegister(dest.GetRegister());

                context.Assembler.Xor(dest, dest);
            }
            else if (dest.GetRegister().Type == RegisterType.Vector)
            {
                context.Assembler.Movdqu(dest, source);
            }
            else
            {
                context.Assembler.Mov(dest, source);
            }
        }

        private static void GenerateCountLeadingZeros(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest;

            Operand dest32 = Get32BitsRegister(dest.GetRegister());

            context.Assembler.Bsr(dest, operation.GetSource(0));

            int operandSize = dest.Type == OperandType.I32 ? 32 : 64;
            int operandMask = operandSize - 1;

            //When the input operand is 0, the result is undefined, however the
            //ZF flag is set. We are supposed to return the operand size on that
            //case. So, add an additional jump to handle that case, by moving the
            //operand size constant to the destination register.
            context.JumpToNear(X86Condition.NotEqual);

            context.Assembler.Mov(dest32, new Operand(operandSize | operandMask));

            context.JumpHere();

            //BSR returns the zero based index of the last bit set on the operand,
            //starting from the least significant bit. However we are supposed to
            //return the number of 0 bits on the high end. So, we invert the result
            //of the BSR using XOR to get the correct value.
            context.Assembler.Xor(dest32, new Operand(operandMask));
        }

        private static void GenerateDivide(CodeGenContext context, Operation operation)
        {
            Operand dest     = operation.Dest;
            Operand dividend = operation.GetSource(0);
            Operand divisor  = operation.GetSource(1);

            if (dest.Type.IsInteger())
            {
                if (divisor.Type == OperandType.I32)
                {
                    context.Assembler.Cdq();
                }
                else
                {
                    context.Assembler.Cqo();
                }

                context.Assembler.Idiv(divisor);
            }
            else if (dest.Type == OperandType.FP32)
            {
                context.Assembler.Divss(dest, divisor, dividend);
            }
            else /* if (dest.Type == OperandType.FP64) */
            {
                context.Assembler.Divsd(dest, divisor, dividend);
            }
        }

        private static void GenerateDivideUI(CodeGenContext context, Operation operation)
        {
            Operand divisor = operation.GetSource(1);

            Operand rdx = Register(X86Register.Rdx, OperandType.I32);

            context.Assembler.Xor(rdx, rdx);
            context.Assembler.Div(divisor);
        }

        private static void GenerateFill(CodeGenContext context, Operation operation)
        {
            Operand dest   = operation.Dest;
            Operand offset = operation.GetSource(0);

            if (offset.Kind != OperandKind.Constant)
            {
                throw new InvalidOperationException("Fill has non-constant stack offset.");
            }

            int offs = offset.AsInt32() + context.CallArgsRegionSize;

            X86MemoryOperand memOp = new X86MemoryOperand(dest.Type, Register(X86Register.Rsp), null, Scale.x1, offs);

            if (dest.GetRegister().Type == RegisterType.Integer)
            {
                context.Assembler.Mov(dest, memOp);
            }
            else
            {
                context.Assembler.Movdqu(dest, memOp);
            }
        }

        private static void GenerateLoad(CodeGenContext context, Operation operation)
        {
            Operand value   = operation.Dest;
            Operand address = GetMemoryOperand(operation.GetSource(0), value.Type);

            switch (value.Type)
            {
                case OperandType.I32:
                case OperandType.I64:
                    context.Assembler.Mov(value, address);
                    break;

                case OperandType.FP32:
                case OperandType.FP64:
                    context.Assembler.Movd(value, address);
                    break;

                case OperandType.V128:
                    context.Assembler.Movdqu(value, address);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private static void GenerateLoad16(CodeGenContext context, Operation operation)
        {
            Operand value   = operation.Dest;
            Operand address = GetMemoryOperand(operation.GetSource(0), value.Type);

            context.Assembler.Movzx16(operation.Dest, address);
        }

        private static void GenerateLoad8(CodeGenContext context, Operation operation)
        {
            Operand value   = operation.Dest;
            Operand address = GetMemoryOperand(operation.GetSource(0), value.Type);

            context.Assembler.Movzx8(operation.Dest, address);
        }

        private static void GenerateMultiply(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            if (dest.Type.IsInteger())
            {
                if (src2.Kind == OperandKind.Constant)
                {
                    context.Assembler.Imul(dest, src1, src2);
                }
                else
                {
                    context.Assembler.Imul(dest, src2);
                }
            }
            else if (dest.Type == OperandType.FP32)
            {
                context.Assembler.Mulss(dest, src2, src1);
            }
            else /* if (dest.Type == OperandType.FP64) */
            {
                context.Assembler.Mulsd(dest, src2, src1);
            }
        }

        private static void GenerateMultiply64HighSI(CodeGenContext context, Operation operation)
        {
            context.Assembler.Imul(operation.GetSource(1));
        }

        private static void GenerateMultiply64HighUI(CodeGenContext context, Operation operation)
        {
            context.Assembler.Mul(operation.GetSource(1));
        }

        private static void GenerateNegate(CodeGenContext context, Operation operation)
        {
            context.Assembler.Neg(operation.Dest);
        }

        private static void GenerateReturn(CodeGenContext context, Operation operation)
        {
            WriteEpilogue(context);

            context.Assembler.Return();
        }

        private static void GenerateRotateRight(CodeGenContext context, Operation operation)
        {
            context.Assembler.Ror(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateShiftLeft(CodeGenContext context, Operation operation)
        {
            context.Assembler.Shl(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateShiftRightSI(CodeGenContext context, Operation operation)
        {
            context.Assembler.Sar(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateShiftRightUI(CodeGenContext context, Operation operation)
        {
            context.Assembler.Shr(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateSignExtend16(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movsx16(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateSignExtend32(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movsx32(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateSignExtend8(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movsx8(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateSpill(CodeGenContext context, Operation operation)
        {
            GenerateSpill(context, operation, context.CallArgsRegionSize);
        }

        private static void GenerateSpillArg(CodeGenContext context, Operation operation)
        {
            GenerateSpill(context, operation, 0);
        }

        private static void GenerateStackAlloc(CodeGenContext context, Operation operation)
        {
            Operand dest   = operation.Dest;
            Operand offset = operation.GetSource(0);

            Debug.Assert(offset.Kind == OperandKind.Constant, "StackAlloc has non-constant stack offset.");

            int offs = offset.AsInt32() + context.CallArgsRegionSize;

            X86MemoryOperand memOp = new X86MemoryOperand(OperandType.I64, Register(X86Register.Rsp), null, Scale.x1, offs);

            context.Assembler.Lea(dest, memOp);
        }

        private static void GenerateStore(CodeGenContext context, Operation operation)
        {
            Operand value   = operation.GetSource(1);
            Operand address = GetMemoryOperand(operation.GetSource(0), value.Type);

            switch (value.Type)
            {
                case OperandType.I32:
                case OperandType.I64:
                    context.Assembler.Mov(address, value);
                    break;

                case OperandType.FP32:
                case OperandType.FP64:
                    context.Assembler.Movd(address, value);
                    break;

                case OperandType.V128:
                    context.Assembler.Movdqu(address, value);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private static void GenerateStore16(CodeGenContext context, Operation operation)
        {
            Operand value   = operation.GetSource(1);
            Operand address = GetMemoryOperand(operation.GetSource(0), value.Type);

            context.Assembler.Mov16(address, value);
        }

        private static void GenerateStore8(CodeGenContext context, Operation operation)
        {
            Operand value   = operation.GetSource(1);
            Operand address = GetMemoryOperand(operation.GetSource(0), value.Type);

            context.Assembler.Mov8(address, value);
        }

        private static void GenerateSubtract(CodeGenContext context, Operation operation)
        {
            ValidateDestSrc1(operation);

            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            if (dest.Type.IsInteger())
            {
                context.Assembler.Sub(dest, src2);
            }
            else if (dest.Type == OperandType.FP32)
            {
                context.Assembler.Subss(dest, src2, src1);
            }
            else /* if (dest.Type == OperandType.FP64) */
            {
                context.Assembler.Subsd(dest, src2, src1);
            }
        }

        private static void GenerateVectorCreateScalar(CodeGenContext context, Operation operation)
        {
            Operand dest   = operation.Dest;
            Operand source = operation.GetSource(0);

            context.Assembler.Movd(dest, source);
        }

        private static void GenerateVectorExtract(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest; //Value
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Index

            Debug.Assert(src2.Kind == OperandKind.Constant, "Index is not constant.");

            byte index = src2.AsByte();

            if (dest.Type.IsInteger())
            {
                context.Assembler.Pextrd(dest, src1, index);
            }
            else
            {
                //Floating-point type.
                if ((index >= 2 && dest.Type == OperandType.FP32) ||
                    (index == 1 && dest.Type == OperandType.FP64))
                {
                    context.Assembler.Movhlps(dest, src1, dest);
                    context.Assembler.Movq(dest, dest);
                }
                else
                {
                    context.Assembler.Movq(dest, src1);
                }

                if (dest.Type == OperandType.FP32)
                {
                    context.Assembler.Pshufd(dest, dest, (byte)(0xfc | (index & 1)));
                }
            }
        }

        private static void GenerateVectorExtract16(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest; //Value
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Index

            Debug.Assert(src2.Kind == OperandKind.Constant, "Index is not constant.");

            byte index = src2.AsByte();

            context.Assembler.Pextrw(dest, src1, index);
        }

        private static void GenerateVectorExtract8(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest; //Value
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Index

            Debug.Assert(src2.Kind == OperandKind.Constant, "Index is not constant.");

            byte index = src2.AsByte();

            //TODO: SSE/SSE2 version.
            context.Assembler.Pextrb(dest, src1, index);
        }

        private static void GenerateVectorInsert(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Value
            Operand src3 = operation.GetSource(2); //Index

            Debug.Assert(src3.Kind == OperandKind.Constant, "Index is not constant.");

            byte index = src3.AsByte();

            if (src2.Type.IsInteger())
            {
                //TODO: SSE/SSE2 version.
                context.Assembler.Pinsrd(dest, src2, src1, index);
            }
            else if (src2.Type == OperandType.FP32)
            {
                if (index != 0)
                {
                    //TODO: SSE/SSE2 version.
                    context.Assembler.Insertps(dest, src2, src1, (byte)(index << 4));
                }
                else
                {
                    context.Assembler.Movss(dest, src2, src1);
                }
            }
            else /* if (src2.Type == OperandType.FP64) */
            {
                if (index != 0)
                {
                    context.Assembler.Movlhps(dest, src2, src1);
                }
                else
                {
                    context.Assembler.Movsd(dest, src2, src1);
                }
            }
        }

        private static void GenerateVectorInsert16(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Value
            Operand src3 = operation.GetSource(2); //Index

            Debug.Assert(src3.Kind == OperandKind.Constant, "Index is not constant.");

            byte index = src3.AsByte();

            context.Assembler.Pinsrw(dest, src2, src1, index);
        }

        private static void GenerateVectorInsert8(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Value
            Operand src3 = operation.GetSource(2); //Index

            Debug.Assert(src3.Kind == OperandKind.Constant, "Index is not constant.");

            byte index = src3.AsByte();

            //TODO: SSE/SSE2 version.
            context.Assembler.Pinsrb(dest, src2, src1, index);
        }

        private static void GenerateVectorOne(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pcmpeqw(operation.Dest, operation.Dest, operation.Dest);
        }

        private static void GenerateVectorZero(CodeGenContext context, Operation operation)
        {
            context.Assembler.Xorps(operation.Dest, operation.Dest, operation.Dest);
        }

        private static void GenerateVectorZeroUpper64(CodeGenContext context, Operation operation)
        {
            ZeroUpper64(context, operation.Dest, operation.GetSource(0));
        }

        private static void GenerateVectorZeroUpper96(CodeGenContext context, Operation operation)
        {
            ZeroUpper96(context, operation.Dest, operation.GetSource(0));
        }

        private static void GenerateZeroExtend16(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movzx16(operation.Dest, Get32BitsRegister(operation.GetSource(0).GetRegister()));
        }

        private static void GenerateZeroExtend32(CodeGenContext context, Operation operation)
        {
            context.Assembler.Mov(
                Get32BitsRegister(operation.Dest.GetRegister()),
                Get32BitsRegister(operation.GetSource(0).GetRegister()));
        }

        private static void GenerateZeroExtend8(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movzx8(operation.Dest, Get32BitsRegister(operation.GetSource(0).GetRegister()));
        }

        private static void GenerateCompare(CodeGenContext context, Operation operation, X86Condition condition)
        {
            context.Assembler.Cmp(operation.GetSource(0), operation.GetSource(1));
            context.Assembler.Setcc(operation.Dest, condition);
            context.Assembler.Movzx8(operation.Dest, operation.Dest);
        }

        private static void GenerateSpill(CodeGenContext context, Operation operation, int baseOffset)
        {
            Operand offset = operation.GetSource(0);
            Operand source = operation.GetSource(1);

            Debug.Assert(offset.Kind == OperandKind.Constant, "Spill has non-constant stack offset.");

            int offs = offset.AsInt32() + baseOffset;

            X86MemoryOperand memOp = new X86MemoryOperand(source.Type, Register(X86Register.Rsp), null, Scale.x1, offs);

            if (source.GetRegister().Type == RegisterType.Integer)
            {
                context.Assembler.Mov(memOp, source);
            }
            else
            {
                context.Assembler.Movdqu(memOp, source);
            }
        }

        private static void ValidateDestSrc1(Operation operation)
        {
            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            if (dest.Kind != OperandKind.Register)
            {
                throw new InvalidOperationException($"Invalid destination type \"{dest.Kind}\".");
            }

            if (src1.Kind != OperandKind.Register)
            {
                throw new InvalidOperationException($"Invalid source 1 type \"{src1.Kind}\".");
            }

            if (src2.Kind != OperandKind.Register && src2.Kind != OperandKind.Constant)
            {
                throw new InvalidOperationException($"Invalid source 2 type \"{src2.Kind}\".");
            }

            if (dest.GetRegister() != src1.GetRegister())
            {
                throw new InvalidOperationException("Destination and source 1 register mismatch.");
            }

            if (dest.Type != src1.Type || dest.Type != src2.Type)
            {
                throw new InvalidOperationException("Operand types mismatch.");
            }
        }

        private static UnwindInfo WritePrologue(CodeGenContext context)
        {
            List<UnwindPushEntry> pushEntries = new List<UnwindPushEntry>();

            int mask = CallingConvention.GetIntCalleeSavedRegisters() & context.AllocResult.IntUsedRegisters;

            while (mask != 0)
            {
                int bit = BitUtils.LowestBitSet(mask);

                context.Assembler.Push(Register((X86Register)bit));

                pushEntries.Add(new UnwindPushEntry(bit, RegisterType.Integer, context.StreamOffset));

                mask &= ~(1 << bit);
            }

            mask = CallingConvention.GetVecCalleeSavedRegisters() & context.AllocResult.VecUsedRegisters;

            int offset = 0;

            while (mask != 0)
            {
                int bit = BitUtils.LowestBitSet(mask);

                offset -= 16;

                X86MemoryOperand memOp = new X86MemoryOperand(OperandType.V128, Register(X86Register.Rsp), null, Scale.x1, offset);

                context.Assembler.Movdqu(memOp, Xmm((X86Register)bit));

                pushEntries.Add(new UnwindPushEntry(bit, RegisterType.Vector, context.StreamOffset));

                mask &= ~(1 << bit);
            }

            int reservedStackSize = context.CallArgsRegionSize + context.AllocResult.SpillRegionSize;

            reservedStackSize += context.VecCalleeSaveSize;

            if (reservedStackSize != 0)
            {
                context.Assembler.Sub(Register(X86Register.Rsp), new Operand(reservedStackSize));
            }

            return new UnwindInfo(pushEntries.ToArray(), context.StreamOffset, reservedStackSize);
        }

        private static void WriteEpilogue(CodeGenContext context)
        {
            int reservedStackSize = context.CallArgsRegionSize + context.AllocResult.SpillRegionSize;

            reservedStackSize += context.VecCalleeSaveSize;

            if (reservedStackSize != 0)
            {
                context.Assembler.Add(Register(X86Register.Rsp), new Operand(reservedStackSize));
            }

            int mask = CallingConvention.GetVecCalleeSavedRegisters() & context.AllocResult.VecUsedRegisters;

            int offset = 0;

            while (mask != 0)
            {
                int bit = BitUtils.LowestBitSet(mask);

                offset -= 16;

                X86MemoryOperand memOp = new X86MemoryOperand(OperandType.V128, Register(X86Register.Rsp), null, Scale.x1, offset);

                context.Assembler.Movdqu(Xmm((X86Register)bit), memOp);

                mask &= ~(1 << bit);
            }

            mask = CallingConvention.GetIntCalleeSavedRegisters() & context.AllocResult.IntUsedRegisters;

            while (mask != 0)
            {
                int bit = BitUtils.HighestBitSet(mask);

                context.Assembler.Pop(Register((X86Register)bit));

                mask &= ~(1 << bit);
            }
        }

        private static void ZeroUpper64(CodeGenContext context, Operand dest, Operand source)
        {
            context.Assembler.Movq(dest, source);
        }

        private static void ZeroUpper96(CodeGenContext context, Operand dest, Operand source)
        {
            context.Assembler.Movq(dest, source);
            context.Assembler.Pshufd(dest, dest, 0xfc);
        }

        private static X86MemoryOperand GetMemoryOperand(Operand operand, OperandType type)
        {
            return new X86MemoryOperand(type, operand, null, Scale.x1, 0);
        }

        private static Operand Get32BitsRegister(Register reg)
        {
            return new Operand(reg.Index, reg.Type, OperandType.I32);
        }

        private static Operand Register(X86Register register, OperandType type = OperandType.I64)
        {
            return new Operand((int)register, RegisterType.Integer, type);
        }

        private static Operand Xmm(X86Register register)
        {
            return new Operand((int)register, RegisterType.Vector, OperandType.V128);
        }
    }
}