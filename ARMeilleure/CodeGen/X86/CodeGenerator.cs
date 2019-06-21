using ARMeilleure.CodeGen.Optimizations;
using ARMeilleure.CodeGen.RegisterAllocators;
using ARMeilleure.Common;
using ARMeilleure.Diagnostics;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using ARMeilleure.Translation;
using System;
using System.Diagnostics;
using System.IO;

namespace ARMeilleure.CodeGen.X86
{
    static class CodeGenerator
    {
        private static Action<CodeGenContext, Operation>[] _instTable;

        static CodeGenerator()
        {
            _instTable = new Action<CodeGenContext, Operation>[(int)Instruction.Count];

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
            Add(Instruction.ConvertToFP,             GenerateConvertToFP);
            Add(Instruction.Copy,                    GenerateCopy);
            Add(Instruction.CountLeadingZeros,       GenerateCountLeadingZeros);
            Add(Instruction.Divide,                  GenerateDivide);
            Add(Instruction.DivideUI,                GenerateDivideUI);
            Add(Instruction.Fill,                    GenerateFill);
            Add(Instruction.Load,                    GenerateLoad);
            Add(Instruction.LoadFromContext,         GenerateLoadFromContext);
            Add(Instruction.LoadSx16,                GenerateLoadSx16);
            Add(Instruction.LoadSx32,                GenerateLoadSx32);
            Add(Instruction.LoadSx8,                 GenerateLoadSx8);
            Add(Instruction.LoadZx16,                GenerateLoadZx16);
            Add(Instruction.LoadZx8,                 GenerateLoadZx8);
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
            Add(Instruction.StoreToContext,          GenerateStoreToContext);
            Add(Instruction.Subtract,                GenerateSubtract);
            Add(Instruction.VectorExtract,           GenerateVectorExtract);
            Add(Instruction.VectorExtract16,         GenerateVectorExtract16);
            Add(Instruction.VectorExtract8,          GenerateVectorExtract8);
            Add(Instruction.VectorInsert,            GenerateVectorInsert);
            Add(Instruction.VectorInsert16,          GenerateVectorInsert16);
            Add(Instruction.VectorInsert8,           GenerateVectorInsert8);
            Add(Instruction.VectorZero,              GenerateVectorZero);
            Add(Instruction.VectorZeroUpper64,       GenerateVectorZeroUpper64);
            Add(Instruction.VectorZeroUpper96,       GenerateVectorZeroUpper96);
            Add(Instruction.X86Addpd,                GenerateX86Addpd);
            Add(Instruction.X86Addps,                GenerateX86Addps);
            Add(Instruction.X86Addsd,                GenerateX86Addsd);
            Add(Instruction.X86Addss,                GenerateX86Addss);
            Add(Instruction.X86Andnpd,               GenerateX86Andnpd);
            Add(Instruction.X86Andnps,               GenerateX86Andnps);
            Add(Instruction.X86Cmppd,                GenerateX86Cmppd);
            Add(Instruction.X86Cmpps,                GenerateX86Cmpps);
            Add(Instruction.X86Cmpsd,                GenerateX86Cmpsd);
            Add(Instruction.X86Cmpss,                GenerateX86Cmpss);
            Add(Instruction.X86Comisdeq,             GenerateX86Comisdeq);
            Add(Instruction.X86Comisdge,             GenerateX86Comisdge);
            Add(Instruction.X86Comisdlt,             GenerateX86Comisdlt);
            Add(Instruction.X86Comisseq,             GenerateX86Comisseq);
            Add(Instruction.X86Comissge,             GenerateX86Comissge);
            Add(Instruction.X86Comisslt,             GenerateX86Comisslt);
            Add(Instruction.X86Cvtdq2pd,             GenerateX86Cvtdq2pd);
            Add(Instruction.X86Cvtdq2ps,             GenerateX86Cvtdq2ps);
            Add(Instruction.X86Cvtpd2dq,             GenerateX86Cvtpd2dq);
            Add(Instruction.X86Cvtpd2ps,             GenerateX86Cvtpd2ps);
            Add(Instruction.X86Cvtps2dq,             GenerateX86Cvtps2dq);
            Add(Instruction.X86Cvtps2pd,             GenerateX86Cvtps2pd);
            Add(Instruction.X86Cvtsd2si,             GenerateX86Cvtsd2si);
            Add(Instruction.X86Cvtsd2ss,             GenerateX86Cvtsd2ss);
            Add(Instruction.X86Cvtss2sd,             GenerateX86Cvtss2sd);
            Add(Instruction.X86Divpd,                GenerateX86Divpd);
            Add(Instruction.X86Divps,                GenerateX86Divps);
            Add(Instruction.X86Divsd,                GenerateX86Divsd);
            Add(Instruction.X86Divss,                GenerateX86Divss);
            Add(Instruction.X86Haddpd,               GenerateX86Haddpd);
            Add(Instruction.X86Haddps,               GenerateX86Haddps);
            Add(Instruction.X86Maxpd,                GenerateX86Maxpd);
            Add(Instruction.X86Maxps,                GenerateX86Maxps);
            Add(Instruction.X86Maxsd,                GenerateX86Maxsd);
            Add(Instruction.X86Maxss,                GenerateX86Maxss);
            Add(Instruction.X86Minpd,                GenerateX86Minpd);
            Add(Instruction.X86Minps,                GenerateX86Minps);
            Add(Instruction.X86Minsd,                GenerateX86Minsd);
            Add(Instruction.X86Minss,                GenerateX86Minss);
            Add(Instruction.X86Movhlps,              GenerateX86Movhlps);
            Add(Instruction.X86Movlhps,              GenerateX86Movlhps);
            Add(Instruction.X86Mulpd,                GenerateX86Mulpd);
            Add(Instruction.X86Mulps,                GenerateX86Mulps);
            Add(Instruction.X86Mulsd,                GenerateX86Mulsd);
            Add(Instruction.X86Mulss,                GenerateX86Mulss);
            Add(Instruction.X86Paddb,                GenerateX86Paddb);
            Add(Instruction.X86Paddd,                GenerateX86Paddd);
            Add(Instruction.X86Paddq,                GenerateX86Paddq);
            Add(Instruction.X86Paddw,                GenerateX86Paddw);
            Add(Instruction.X86Pand,                 GenerateX86Pand);
            Add(Instruction.X86Pandn,                GenerateX86Pandn);
            Add(Instruction.X86Pavgb,                GenerateX86Pavgb);
            Add(Instruction.X86Pavgw,                GenerateX86Pavgw);
            Add(Instruction.X86Pblendvb,             GenerateX86Pblendvb);
            Add(Instruction.X86Pcmpeqb,              GenerateX86Pcmpeqb);
            Add(Instruction.X86Pcmpeqd,              GenerateX86Pcmpeqd);
            Add(Instruction.X86Pcmpeqq,              GenerateX86Pcmpeqq);
            Add(Instruction.X86Pcmpeqw,              GenerateX86Pcmpeqw);
            Add(Instruction.X86Pcmpgtb,              GenerateX86Pcmpgtb);
            Add(Instruction.X86Pcmpgtd,              GenerateX86Pcmpgtd);
            Add(Instruction.X86Pcmpgtq,              GenerateX86Pcmpgtq);
            Add(Instruction.X86Pcmpgtw,              GenerateX86Pcmpgtw);
            Add(Instruction.X86Pmaxsb,               GenerateX86Pmaxsb);
            Add(Instruction.X86Pmaxsd,               GenerateX86Pmaxsd);
            Add(Instruction.X86Pmaxsw,               GenerateX86Pmaxsw);
            Add(Instruction.X86Pmaxub,               GenerateX86Pmaxub);
            Add(Instruction.X86Pmaxud,               GenerateX86Pmaxud);
            Add(Instruction.X86Pmaxuw,               GenerateX86Pmaxuw);
            Add(Instruction.X86Pminsb,               GenerateX86Pminsb);
            Add(Instruction.X86Pminsd,               GenerateX86Pminsd);
            Add(Instruction.X86Pminsw,               GenerateX86Pminsw);
            Add(Instruction.X86Pminub,               GenerateX86Pminub);
            Add(Instruction.X86Pminud,               GenerateX86Pminud);
            Add(Instruction.X86Pminuw,               GenerateX86Pminuw);
            Add(Instruction.X86Pmovsxbw,             GenerateX86Pmovsxbw);
            Add(Instruction.X86Pmovsxdq,             GenerateX86Pmovsxdq);
            Add(Instruction.X86Pmovsxwd,             GenerateX86Pmovsxwd);
            Add(Instruction.X86Pmovzxbw,             GenerateX86Pmovzxbw);
            Add(Instruction.X86Pmovzxdq,             GenerateX86Pmovzxdq);
            Add(Instruction.X86Pmovzxwd,             GenerateX86Pmovzxwd);
            Add(Instruction.X86Pmulld,               GenerateX86Pmulld);
            Add(Instruction.X86Pmullw,               GenerateX86Pmullw);
            Add(Instruction.X86Popcnt,               GenerateX86Popcnt);
            Add(Instruction.X86Por,                  GenerateX86Por);
            Add(Instruction.X86Pshufb,               GenerateX86Pshufb);
            Add(Instruction.X86Pslld,                GenerateX86Pslld);
            Add(Instruction.X86Pslldq,               GenerateX86Pslldq);
            Add(Instruction.X86Psllq,                GenerateX86Psllq);
            Add(Instruction.X86Psllw,                GenerateX86Psllw);
            Add(Instruction.X86Psrad,                GenerateX86Psrad);
            Add(Instruction.X86Psraw,                GenerateX86Psraw);
            Add(Instruction.X86Psrld,                GenerateX86Psrld);
            Add(Instruction.X86Psrlq,                GenerateX86Psrlq);
            Add(Instruction.X86Psrldq,               GenerateX86Psrldq);
            Add(Instruction.X86Psrlw,                GenerateX86Psrlw);
            Add(Instruction.X86Psubb,                GenerateX86Psubb);
            Add(Instruction.X86Psubd,                GenerateX86Psubd);
            Add(Instruction.X86Psubq,                GenerateX86Psubq);
            Add(Instruction.X86Psubw,                GenerateX86Psubw);
            Add(Instruction.X86Punpckhbw,            GenerateX86Punpckhbw);
            Add(Instruction.X86Punpckhdq,            GenerateX86Punpckhdq);
            Add(Instruction.X86Punpckhqdq,           GenerateX86Punpckhqdq);
            Add(Instruction.X86Punpckhwd,            GenerateX86Punpckhwd);
            Add(Instruction.X86Punpcklbw,            GenerateX86Punpcklbw);
            Add(Instruction.X86Punpckldq,            GenerateX86Punpckldq);
            Add(Instruction.X86Punpcklqdq,           GenerateX86Punpcklqdq);
            Add(Instruction.X86Punpcklwd,            GenerateX86Punpcklwd);
            Add(Instruction.X86Pxor,                 GenerateX86Pxor);
            Add(Instruction.X86Rcpps,                GenerateX86Rcpps);
            Add(Instruction.X86Rcpss,                GenerateX86Rcpss);
            Add(Instruction.X86Roundpd,              GenerateX86Roundpd);
            Add(Instruction.X86Roundps,              GenerateX86Roundps);
            Add(Instruction.X86Roundsd,              GenerateX86Roundsd);
            Add(Instruction.X86Roundss,              GenerateX86Roundss);
            Add(Instruction.X86Rsqrtps,              GenerateX86Rsqrtps);
            Add(Instruction.X86Rsqrtss,              GenerateX86Rsqrtss);
            Add(Instruction.X86Shufpd,               GenerateX86Shufpd);
            Add(Instruction.X86Shufps,               GenerateX86Shufps);
            Add(Instruction.X86Sqrtpd,               GenerateX86Sqrtpd);
            Add(Instruction.X86Sqrtps,               GenerateX86Sqrtps);
            Add(Instruction.X86Sqrtsd,               GenerateX86Sqrtsd);
            Add(Instruction.X86Sqrtss,               GenerateX86Sqrtss);
            Add(Instruction.X86Subpd,                GenerateX86Subpd);
            Add(Instruction.X86Subps,                GenerateX86Subps);
            Add(Instruction.X86Subsd,                GenerateX86Subsd);
            Add(Instruction.X86Subss,                GenerateX86Subss);
            Add(Instruction.X86Unpckhpd,             GenerateX86Unpckhpd);
            Add(Instruction.X86Unpckhps,             GenerateX86Unpckhps);
            Add(Instruction.X86Unpcklpd,             GenerateX86Unpcklpd);
            Add(Instruction.X86Unpcklps,             GenerateX86Unpcklps);
            Add(Instruction.X86Xorpd,                GenerateX86Xorpd);
            Add(Instruction.X86Xorps,                GenerateX86Xorps);
        }

        private static void Add(Instruction inst, Action<CodeGenContext, Operation> func)
        {
            _instTable[(int)inst] = func;
        }

        public static byte[] Generate(ControlFlowGraph cfg, MemoryManager memory)
        {
            Logger.StartPass(PassName.Optimization);

            Optimizer.RunPass(cfg);

            Logger.EndPass(PassName.Optimization);

            Logger.StartPass(PassName.PreAllocation);

            PreAllocator.RunPass(cfg, memory, out int maxCallArgs);

            Logger.EndPass(PassName.PreAllocation, cfg);

            Logger.StartPass(PassName.RegisterAllocation);

            LinearScan regAlloc = new LinearScan();

            RegisterMasks regMasks = new RegisterMasks(
                CallingConvention.GetIntAvailableRegisters(),
                CallingConvention.GetVecAvailableRegisters(),
                CallingConvention.GetIntCallerSavedRegisters(),
                CallingConvention.GetVecCallerSavedRegisters(),
                CallingConvention.GetIntCalleeSavedRegisters(),
                CallingConvention.GetVecCalleeSavedRegisters());

            AllocationResult allocResult = regAlloc.RunPass(cfg, regMasks);

            Logger.EndPass(PassName.RegisterAllocation, cfg);

            using (MemoryStream stream = new MemoryStream())
            {
                CodeGenContext context = new CodeGenContext(stream, allocResult, maxCallArgs, cfg.Blocks.Count);

                WritePrologue(context);

                context.Assembler.Mov(Register(X86Register.Rbp), Register(X86Register.Rcx));

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

                return context.GetCode();
            }
        }

        private static void GenerateOperation(CodeGenContext context, Operation operation)
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

            context.Assembler.Xor(operation.Dest, operation.GetSource(1));
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
            else if (dest.Type == OperandType.I64 && source.Type == OperandType.I32)
            {
                //I32 -> I64 zero-extension.
                if (dest.Kind == OperandKind.Register && source.Kind == OperandKind.Register)
                {
                    dest = Get32BitsRegister(dest.GetRegister());
                }
                else if (source.Kind == OperandKind.Constant)
                {
                    source = new Operand(source.Value);
                }

                context.Assembler.Mov(dest, source);
            }
            else if (dest.GetRegister().Type == RegisterType.Vector)
            {
                if (source.GetRegister().Type == RegisterType.Integer)
                {
                    //FIXME.
                    context.Assembler.Movd(dest, source);
                }
                else
                {
                    context.Assembler.Movdqu(dest, source);
                }
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

            if (value.GetRegister().Type == RegisterType.Integer)
            {
                context.Assembler.Mov(value, address);
            }
            else
            {
                context.Assembler.Movdqu(value, address);
            }
        }

        private static void GenerateLoadFromContext(CodeGenContext context, Operation operation)
        {
            Operand dest   = operation.Dest;
            Operand offset = operation.GetSource(0);

            if (offset.Kind != OperandKind.Constant)
            {
                throw new InvalidOperationException("LoadFromContext has non-constant context offset.");
            }

            Operand rbp = Register(X86Register.Rbp);

            X86MemoryOperand memOp = new X86MemoryOperand(dest.Type, rbp, null, Scale.x1, offset.AsInt32());

            if (dest.GetRegister().Type == RegisterType.Vector)
            {
                context.Assembler.Movdqu(dest, memOp);
            }
            else
            {
                context.Assembler.Mov(dest, memOp);
            }
        }

        private static void GenerateLoadSx16(CodeGenContext context, Operation operation)
        {
            Operand value   = operation.Dest;
            Operand address = GetMemoryOperand(operation.GetSource(0), value.Type);

            context.Assembler.Movsx16(operation.Dest, address);
        }

        private static void GenerateLoadSx32(CodeGenContext context, Operation operation)
        {
            Operand value   = operation.Dest;
            Operand address = GetMemoryOperand(operation.GetSource(0), value.Type);

            context.Assembler.Movsx32(operation.Dest, address);
        }

        private static void GenerateLoadSx8(CodeGenContext context, Operation operation)
        {
            Operand value   = operation.Dest;
            Operand address = GetMemoryOperand(operation.GetSource(0), value.Type);

            context.Assembler.Movsx8(operation.Dest, address);
        }

        private static void GenerateLoadZx16(CodeGenContext context, Operation operation)
        {
            Operand value   = operation.Dest;
            Operand address = GetMemoryOperand(operation.GetSource(0), value.Type);

            context.Assembler.Movzx16(operation.Dest, address);
        }

        private static void GenerateLoadZx8(CodeGenContext context, Operation operation)
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
            if (operation.SourcesCount != 0)
            {
                Operand returnReg = Register(CallingConvention.GetIntReturnRegister());

                Operand sourceReg = operation.GetSource(0);

                if (returnReg.GetRegister() != sourceReg.GetRegister())
                {
                    context.Assembler.Mov(returnReg, sourceReg);
                }
            }

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

            if (value.GetRegister().Type == RegisterType.Integer)
            {
                context.Assembler.Mov(address, value);
            }
            else
            {
                context.Assembler.Movdqu(address, value);
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

        private static void GenerateStoreToContext(CodeGenContext context, Operation operation)
        {
            Operand offset = operation.GetSource(0);
            Operand source = operation.GetSource(1);

            if (offset.Kind != OperandKind.Constant)
            {
                throw new InvalidOperationException("StoreToContext has non-constant context offset.");
            }

            Operand rbp = Register(X86Register.Rbp);

            X86MemoryOperand memOp = new X86MemoryOperand(source.Type, rbp, null, Scale.x1, offset.AsInt32());

            if (source.GetRegister().Type == RegisterType.Vector)
            {
                context.Assembler.Movdqu(memOp, source);
            }
            else
            {
                context.Assembler.Mov(memOp, source);
            }
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

        private static void GenerateX86Addpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Addpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Addps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Addps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Addsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Addsd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Addss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Addss(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Andnpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Andnpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Andnps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Andnps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Cmppd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cmppd(
                operation.Dest,
                operation.GetSource(1),
                operation.GetSource(0),
                operation.GetSource(2).AsByte());
        }

        private static void GenerateX86Cmpps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cmpps(
                operation.Dest,
                operation.GetSource(1),
                operation.GetSource(0),
                operation.GetSource(2).AsByte());
        }

        private static void GenerateX86Cmpsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cmpsd(
                operation.Dest,
                operation.GetSource(1),
                operation.GetSource(0),
                operation.GetSource(2).AsByte());
        }

        private static void GenerateX86Cmpss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cmpss(
                operation.Dest,
                operation.GetSource(1),
                operation.GetSource(0),
                operation.GetSource(2).AsByte());
        }

        private static void GenerateX86Comisdeq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Comisd(operation.GetSource(0), operation.GetSource(1));
            context.Assembler.Setcc(operation.Dest, X86Condition.Equal);
        }

        private static void GenerateX86Comisdge(CodeGenContext context, Operation operation)
        {
            context.Assembler.Comisd(operation.GetSource(0), operation.GetSource(1));
            context.Assembler.Setcc(operation.Dest, X86Condition.AboveOrEqual);
        }

        private static void GenerateX86Comisdlt(CodeGenContext context, Operation operation)
        {
            context.Assembler.Comisd(operation.GetSource(0), operation.GetSource(1));
            context.Assembler.Setcc(operation.Dest, X86Condition.Below);
        }

        private static void GenerateX86Comisseq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Comiss(operation.GetSource(0), operation.GetSource(1));
            context.Assembler.Setcc(operation.Dest, X86Condition.Equal);
        }

        private static void GenerateX86Comissge(CodeGenContext context, Operation operation)
        {
            context.Assembler.Comiss(operation.GetSource(0), operation.GetSource(1));
            context.Assembler.Setcc(operation.Dest, X86Condition.AboveOrEqual);
        }

        private static void GenerateX86Comisslt(CodeGenContext context, Operation operation)
        {
            context.Assembler.Comiss(operation.GetSource(0), operation.GetSource(1));
            context.Assembler.Setcc(operation.Dest, X86Condition.Below);
        }

        private static void GenerateX86Cvtdq2pd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cvtdq2pd(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Cvtdq2ps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cvtdq2ps(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Cvtpd2dq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cvtpd2dq(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Cvtpd2ps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cvtpd2ps(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Cvtps2dq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cvtps2dq(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Cvtps2pd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cvtps2pd(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Cvtsd2si(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cvtsd2si(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Cvtsd2ss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cvtsd2ss(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Cvtss2sd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Cvtss2sd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Divpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Divpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Divps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Divps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Divsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Divsd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Divss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Divss(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Haddpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Addpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Haddps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Addps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Maxpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Maxpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Maxps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Maxps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Maxsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Maxsd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Maxss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Maxss(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Minpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Minpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Minps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Minps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Minsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Minsd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Minss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Minss(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Movhlps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movhlps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Movlhps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movlhps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Mulpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Mulpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Mulps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Mulps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Mulsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Mulsd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Mulss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Mulss(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Paddb(CodeGenContext context, Operation operation)
        {
            context.Assembler.Paddb(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Paddd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Paddd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Paddq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Paddq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Paddw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Paddw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pand(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pand(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pandn(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pandn(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pavgb(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pavgb(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pavgw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pavgw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pblendvb(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pblendvb(
                operation.Dest,
                operation.GetSource(0),
                operation.GetSource(1),
                operation.GetSource(2));
        }

        private static void GenerateX86Pcmpeqb(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pcmpeqb(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pcmpeqd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pcmpeqd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pcmpeqq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pcmpeqq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pcmpeqw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pcmpeqw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pcmpgtb(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pcmpgtb(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pcmpgtd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pcmpgtd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pcmpgtq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pcmpgtq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pcmpgtw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pcmpgtw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pmaxsb(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmaxsb(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pmaxsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmaxsd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pmaxsw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmaxsw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pmaxub(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmaxub(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pmaxud(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmaxud(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pmaxuw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmaxuw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pminsb(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pminsb(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pminsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pminsd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pminsw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pminsw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pminub(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pminub(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pminud(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pminud(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pminuw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pminuw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pmovsxbw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmovsxbw(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Pmovsxdq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmovsxdq(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Pmovsxwd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmovsxwd(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Pmovzxbw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmovzxbw(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Pmovzxdq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmovzxdq(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Pmovzxwd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmovzxwd(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Pmulld(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmulld(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pmullw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pmullw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Popcnt(CodeGenContext context, Operation operation)
        {
            context.Assembler.Popcnt(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Por(CodeGenContext context, Operation operation)
        {
            context.Assembler.Por(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pshufb(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pshufb(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pslld(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pslld(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pslldq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pslldq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psllq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psllq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psllw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psllw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psrad(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psrad(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psraw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psraw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psrld(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psrld(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psrlq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psrlq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psrldq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psrldq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psrlw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psrlw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psubb(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psubb(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psubd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psubd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psubq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psubq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Psubw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Psubw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Punpckhbw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Punpckhbw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Punpckhdq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Punpckhdq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Punpckhqdq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Punpckhqdq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Punpckhwd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Punpckhwd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Punpcklbw(CodeGenContext context, Operation operation)
        {
            context.Assembler.Punpcklbw(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Punpckldq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Punpckldq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Punpcklqdq(CodeGenContext context, Operation operation)
        {
            context.Assembler.Punpcklqdq(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Punpcklwd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Punpcklwd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Pxor(CodeGenContext context, Operation operation)
        {
            context.Assembler.Pxor(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Rcpps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Rcpps(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Rcpss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Rcpss(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Roundpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Roundpd(operation.Dest, operation.GetSource(0), operation.GetSource(1).AsByte());
        }

        private static void GenerateX86Roundps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Roundps(operation.Dest, operation.GetSource(0), operation.GetSource(1).AsByte());
        }

        private static void GenerateX86Roundsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Roundsd(operation.Dest, operation.GetSource(0), operation.GetSource(1).AsByte());
        }

        private static void GenerateX86Roundss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Roundss(operation.Dest, operation.GetSource(0), operation.GetSource(1).AsByte());
        }

        private static void GenerateX86Rsqrtps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Rsqrtps(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Rsqrtss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Rsqrtss(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Shufpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Shufpd(
                operation.Dest,
                operation.GetSource(1),
                operation.GetSource(2).AsByte(),
                operation.GetSource(0));
        }

        private static void GenerateX86Shufps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Shufps(
                operation.Dest,
                operation.GetSource(1),
                operation.GetSource(2).AsByte(),
                operation.GetSource(0));
        }

        private static void GenerateX86Sqrtpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Sqrtpd(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Sqrtps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Sqrtps(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Sqrtsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Sqrtsd(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Sqrtss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Sqrtss(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateX86Subpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Subpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Subps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Subps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Subsd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Subsd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Subss(CodeGenContext context, Operation operation)
        {
            context.Assembler.Subss(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Unpckhpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Unpckhpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Unpckhps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Unpckhps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Unpcklpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Unpcklpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Unpcklps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Unpcklps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Xorpd(CodeGenContext context, Operation operation)
        {
            context.Assembler.Xorpd(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateX86Xorps(CodeGenContext context, Operation operation)
        {
            context.Assembler.Xorps(operation.Dest, operation.GetSource(1), operation.GetSource(0));
        }

        private static void GenerateCompare(CodeGenContext context, Operation operation, X86Condition condition)
        {
            context.Assembler.Cmp(operation.GetSource(0), operation.GetSource(1));
            context.Assembler.Setcc(operation.Dest, condition);
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

        private static void WritePrologue(CodeGenContext context)
        {
            int mask = CallingConvention.GetIntCalleeSavedRegisters() & context.AllocResult.IntUsedRegisters;

            mask |= 1 << (int)X86Register.Rbp;

            while (mask != 0)
            {
                int bit = BitUtils.LowestBitSet(mask);

                context.Assembler.Push(Register((X86Register)bit));

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

                mask &= ~(1 << bit);
            }

            int reservedStackSize = context.CallArgsRegionSize + context.AllocResult.SpillRegionSize;

            reservedStackSize += context.VecCalleeSaveSize;

            if (reservedStackSize != 0)
            {
                context.Assembler.Sub(Register(X86Register.Rsp), new Operand(reservedStackSize));
            }
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

            mask |= 1 << (int)X86Register.Rbp;

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