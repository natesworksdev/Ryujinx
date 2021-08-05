using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Spv.Specification;

namespace Ryujinx.Graphics.Shader.CodeGen.Spirv
{
    using SpvInstruction = Spv.Generator.Instruction;
    using SpvLiteralInteger = Spv.Generator.LiteralInteger;

    static class Instructions
    {
        private static readonly Func<CodeGenContext, AstOperation, OperationResult>[] InstTable;

        static Instructions()
        {
            InstTable = new Func<CodeGenContext, AstOperation, OperationResult>[(int)Instruction.Count];

            Add(Instruction.Absolute,                 GenerateAbsolute);
            Add(Instruction.Add,                      GenerateAdd);
            Add(Instruction.AtomicAdd,                GenerateAtomicAdd);
            Add(Instruction.AtomicAnd,                GenerateAtomicAnd);
            Add(Instruction.AtomicCompareAndSwap,     GenerateAtomicCompareAndSwap);
            Add(Instruction.AtomicMinS32,             GenerateAtomicMinS32);
            Add(Instruction.AtomicMinU32,             GenerateAtomicMinU32);
            Add(Instruction.AtomicMaxS32,             GenerateAtomicMaxS32);
            Add(Instruction.AtomicMaxU32,             GenerateAtomicMaxU32);
            Add(Instruction.AtomicOr,                 GenerateAtomicOr);
            Add(Instruction.AtomicSwap,               GenerateAtomicSwap);
            Add(Instruction.AtomicXor,                GenerateAtomicXor);
            Add(Instruction.Ballot,                   GenerateBallot);
            Add(Instruction.Barrier,                  GenerateBarrier);
            Add(Instruction.BitCount,                 GenerateBitCount);
            Add(Instruction.BitfieldExtractS32,       GenerateBitfieldExtractS32);
            Add(Instruction.BitfieldExtractU32,       GenerateBitfieldExtractU32);
            Add(Instruction.BitfieldInsert,           GenerateBitfieldInsert);
            Add(Instruction.BitfieldReverse,          GenerateBitfieldReverse);
            Add(Instruction.BitwiseAnd,               GenerateBitwiseAnd);
            Add(Instruction.BitwiseExclusiveOr,       GenerateBitwiseExclusiveOr);
            Add(Instruction.BitwiseNot,               GenerateBitwiseNot);
            Add(Instruction.BitwiseOr,                GenerateBitwiseOr);
            Add(Instruction.Call,                     GenerateCall);
            Add(Instruction.Ceiling,                  GenerateCeiling);
            Add(Instruction.Clamp,                    GenerateClamp);
            Add(Instruction.ClampU32,                 GenerateClampU32);
            Add(Instruction.Comment,                  GenerateComment);
            Add(Instruction.CompareEqual,             GenerateCompareEqual);
            Add(Instruction.CompareGreater,           GenerateCompareGreater);
            Add(Instruction.CompareGreaterOrEqual,    GenerateCompareGreaterOrEqual);
            Add(Instruction.CompareGreaterOrEqualU32, GenerateCompareGreaterOrEqualU32);
            Add(Instruction.CompareGreaterU32,        GenerateCompareGreaterU32);
            Add(Instruction.CompareLess,              GenerateCompareLess);
            Add(Instruction.CompareLessOrEqual,       GenerateCompareLessOrEqual);
            Add(Instruction.CompareLessOrEqualU32,    GenerateCompareLessOrEqualU32);
            Add(Instruction.CompareLessU32,           GenerateCompareLessU32);
            Add(Instruction.CompareNotEqual,          GenerateCompareNotEqual);
            Add(Instruction.ConditionalSelect,        GenerateConditionalSelect);
            Add(Instruction.ConvertFP32ToFP64,        GenerateConvertFP32ToFP64);
            Add(Instruction.ConvertFP64ToFP32,        GenerateConvertFP64ToFP32);
            Add(Instruction.ConvertFPToS32,           GenerateConvertFPToS32);
            Add(Instruction.ConvertFPToU32,           GenerateConvertFPToU32);
            Add(Instruction.ConvertS32ToFP,           GenerateConvertS32ToFP);
            Add(Instruction.ConvertU32ToFP,           GenerateConvertU32ToFP);
            Add(Instruction.Cosine,                   GenerateCosine);
            Add(Instruction.Ddx,                      GenerateDdx);
            Add(Instruction.Ddy,                      GenerateDdy);
            Add(Instruction.Discard,                  GenerateDiscard);
            Add(Instruction.Divide,                   GenerateDivide);
            Add(Instruction.ExponentB2,               GenerateExponentB2);
            Add(Instruction.FindFirstSetS32,          GenerateFindFirstSetS32);
            Add(Instruction.FindFirstSetU32,          GenerateFindFirstSetU32);
            Add(Instruction.Floor,                    GenerateFloor);
            Add(Instruction.FusedMultiplyAdd,         GenerateFusedMultiplyAdd);
            Add(Instruction.GroupMemoryBarrier,       GenerateGroupMemoryBarrier);
            Add(Instruction.ImageLoad,                GenerateImageLoad);
            Add(Instruction.ImageStore,               GenerateImageStore);
            Add(Instruction.IsNan,                    GenerateIsNan);
            Add(Instruction.LoadAttribute,            GenerateLoadAttribute);
            Add(Instruction.LoadConstant,             GenerateLoadConstant);
            Add(Instruction.LoadLocal,                GenerateLoadLocal);
            Add(Instruction.LoadShared,               GenerateLoadShared);
            Add(Instruction.LoadStorage,              GenerateLoadStorage);
            Add(Instruction.Lod,                      GenerateLod);
            Add(Instruction.LogarithmB2,              GenerateLogarithmB2);
            Add(Instruction.LogicalAnd,               GenerateLogicalAnd);
            Add(Instruction.LogicalNot,               GenerateLogicalNot);
            Add(Instruction.LogicalOr,                GenerateLogicalOr);
            Add(Instruction.LoopBreak,                GenerateLoopBreak);
            Add(Instruction.Maximum,                  GenerateMaximum);
            Add(Instruction.MaximumU32,               GenerateMaximumU32);
            Add(Instruction.MemoryBarrier,            GenerateMemoryBarrier);
            Add(Instruction.Minimum,                  GenerateMinimum);
            Add(Instruction.MinimumU32,               GenerateMinimumU32);
            Add(Instruction.Multiply,                 GenerateMultiply);
            Add(Instruction.Negate,                   GenerateNegate);
            Add(Instruction.PackHalf2x16,             GeneratePackHalf2x16);
            Add(Instruction.ReciprocalSquareRoot,     GenerateReciprocalSquareRoot);
            Add(Instruction.Return,                   GenerateReturn);
            Add(Instruction.Round,                    GenerateRound);
            Add(Instruction.ShiftLeft,                GenerateShiftLeft);
            Add(Instruction.ShiftRightS32,            GenerateShiftRightS32);
            Add(Instruction.ShiftRightU32,            GenerateShiftRightU32);
            Add(Instruction.Shuffle,                  GenerateShuffle);
            Add(Instruction.ShuffleDown,              GenerateShuffleDown);
            Add(Instruction.ShuffleUp,                GenerateShuffleUp);
            Add(Instruction.ShuffleXor,               GenerateShuffleXor);
            Add(Instruction.Sine,                     GenerateSine);
            Add(Instruction.SquareRoot,               GenerateSquareRoot);
            Add(Instruction.StoreLocal,               GenerateStoreLocal);
            Add(Instruction.StoreShared,              GenerateStoreShared);
            Add(Instruction.StoreStorage,             GenerateStoreStorage);
            Add(Instruction.Subtract,                 GenerateSubtract);
            Add(Instruction.TextureSample,            GenerateTextureSample);
            Add(Instruction.TextureSize,              GenerateTextureSize);
            Add(Instruction.Truncate,                 GenerateTruncate);
            Add(Instruction.UnpackHalf2x16,           GenerateUnpackHalf2x16);
            Add(Instruction.VoteAll,                  GenerateVoteAll);
            Add(Instruction.VoteAllEqual,             GenerateVoteAllEqual);
            Add(Instruction.VoteAny,                  GenerateVoteAny);
        }

        private static void Add(Instruction inst, Func<CodeGenContext, AstOperation, OperationResult> handler)
        {
            InstTable[(int)(inst & Instruction.Mask)] = handler;
        }

        public static OperationResult Generate(CodeGenContext context, AstOperation operation)
        {
            var handler = InstTable[(int)(operation.Inst & Instruction.Mask)];
            if (handler != null)
            {
                return handler(context, operation);
            }
            else
            {
                throw new NotImplementedException(operation.Inst.ToString());
            }
        }

        private static OperationResult GenerateAbsolute(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 4, 5);
        }

        private static OperationResult GenerateAdd(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinary(context, operation, context.FAdd, context.IAdd);
        }

        private static OperationResult GenerateAtomicAdd(CodeGenContext context, AstOperation operation)
        {
            return GenerateAtomicMemoryBinary(context, operation, context.AtomicIAdd);
        }

        private static OperationResult GenerateAtomicAnd(CodeGenContext context, AstOperation operation)
        {
            return GenerateAtomicMemoryBinary(context, operation, context.AtomicAnd);
        }

        private static OperationResult GenerateAtomicCompareAndSwap(CodeGenContext context, AstOperation operation)
        {
            return GenerateAtomicMemoryCas(context, operation);
        }

        private static OperationResult GenerateAtomicMinS32(CodeGenContext context, AstOperation operation)
        {
            return GenerateAtomicMemoryBinary(context, operation, context.AtomicSMin);
        }

        private static OperationResult GenerateAtomicMinU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateAtomicMemoryBinary(context, operation, context.AtomicUMin);
        }

        private static OperationResult GenerateAtomicMaxS32(CodeGenContext context, AstOperation operation)
        {
            return GenerateAtomicMemoryBinary(context, operation, context.AtomicSMax);
        }

        private static OperationResult GenerateAtomicMaxU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateAtomicMemoryBinary(context, operation, context.AtomicUMax);
        }

        private static OperationResult GenerateAtomicOr(CodeGenContext context, AstOperation operation)
        {
            return GenerateAtomicMemoryBinary(context, operation, context.AtomicOr);
        }

        private static OperationResult GenerateAtomicSwap(CodeGenContext context, AstOperation operation)
        {
            return GenerateAtomicMemoryBinary(context, operation, context.AtomicExchange);
        }

        private static OperationResult GenerateAtomicXor(CodeGenContext context, AstOperation operation)
        {
            return GenerateAtomicMemoryBinary(context, operation, context.AtomicXor);
        }

        private static OperationResult GenerateBallot(CodeGenContext context, AstOperation operation)
        {
            var source = operation.GetSource(0);

            var uvec4Type = context.TypeVector(context.TypeU32(), 4);
            var execution = context.Constant(context.TypeU32(), 3); // Subgroup

            var maskVector = context.GroupNonUniformBallot(uvec4Type, execution, context.Get(AggregateType.Bool, source));
            var mask = context.CompositeExtract(context.TypeU32(), maskVector, (SpvLiteralInteger)0);

            return new OperationResult(AggregateType.U32, mask);
        }

        private static OperationResult GenerateBarrier(CodeGenContext context, AstOperation operation)
        {
            context.ControlBarrier(
                context.Constant(context.TypeU32(), 2),
                context.Constant(context.TypeU32(), 2),
                context.Constant(context.TypeU32(), 264));

            return OperationResult.Invalid;
        }

        private static OperationResult GenerateBitCount(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryS32(context, operation, context.BitCount);
        }

        private static OperationResult GenerateBitfieldExtractS32(CodeGenContext context, AstOperation operation)
        {
            return GenerateTernaryS32(context, operation, context.BitFieldSExtract);
        }

        private static OperationResult GenerateBitfieldExtractU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateTernaryS32(context, operation, context.BitFieldUExtract);
        }

        private static OperationResult GenerateBitfieldInsert(CodeGenContext context, AstOperation operation)
        {
            return GenerateQuaternaryS32(context, operation, context.BitFieldInsert);
        }

        private static OperationResult GenerateBitfieldReverse(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryS32(context, operation, context.BitReverse);
        }

        private static OperationResult GenerateBitwiseAnd(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryS32(context, operation, context.BitwiseAnd);
        }

        private static OperationResult GenerateBitwiseExclusiveOr(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryS32(context, operation, context.BitwiseXor);
        }

        private static OperationResult GenerateBitwiseNot(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryS32(context, operation, context.Not);
        }

        private static OperationResult GenerateBitwiseOr(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryS32(context, operation, context.BitwiseOr);
        }

        private static OperationResult GenerateCall(CodeGenContext context, AstOperation operation)
        {
            AstOperand funcId = (AstOperand)operation.GetSource(0);

            Debug.Assert(funcId.Type == OperandType.Constant);

            (var function, var spvFunc) = context.GetFunction(funcId.Value);

            var args = new SpvInstruction[operation.SourcesCount - 1];
            var spvLocals = context.GetLocalForArgsPointers(funcId.Value);

            for (int i = 0; i < args.Length; i++)
            {
                var operand = (AstOperand)operation.GetSource(i + 1);
                if (i >= function.InArguments.Length)
                {
                    args[i] = context.GetLocalPointer(operand);
                }
                else
                {
                    var type = function.GetArgumentType(i).Convert();
                    var value = context.Get(type, operand);
                    var spvLocal = spvLocals[i];

                    context.Store(spvLocal, value);

                    args[i] = spvLocal;
                }
            }

            var retType = function.ReturnType.Convert();
            var result = context.FunctionCall(context.GetType(retType), spvFunc, args);
            return new OperationResult(retType, result);
        }

        private static OperationResult GenerateCeiling(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 9);
        }

        private static OperationResult GenerateClamp(CodeGenContext context, AstOperation operation)
        {
            return GenerateTernaryExtInst(context, operation, 43, 45);
        }

        private static OperationResult GenerateClampU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateTernaryExtInstU32(context, operation, 44);
        }

        private static OperationResult GenerateComment(CodeGenContext context, AstOperation operation)
        {
            return OperationResult.Invalid;
        }

        private static OperationResult GenerateCompareEqual(CodeGenContext context, AstOperation operation)
        {
            return GenerateCompare(context, operation, context.FOrdEqual, context.IEqual);
        }

        private static OperationResult GenerateCompareGreater(CodeGenContext context, AstOperation operation)
        {
            return GenerateCompare(context, operation, context.FOrdGreaterThan, context.SGreaterThan);
        }

        private static OperationResult GenerateCompareGreaterOrEqual(CodeGenContext context, AstOperation operation)
        {
            return GenerateCompare(context, operation, context.FOrdGreaterThanEqual, context.SGreaterThanEqual);
        }

        private static OperationResult GenerateCompareGreaterOrEqualU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateCompareU32(context, operation, context.UGreaterThanEqual);
        }

        private static OperationResult GenerateCompareGreaterU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateCompareU32(context, operation, context.UGreaterThan);
        }

        private static OperationResult GenerateCompareLess(CodeGenContext context, AstOperation operation)
        {
            return GenerateCompare(context, operation, context.FOrdLessThan, context.SLessThan);
        }

        private static OperationResult GenerateCompareLessOrEqual(CodeGenContext context, AstOperation operation)
        {
            return GenerateCompare(context, operation, context.FOrdLessThanEqual, context.SLessThanEqual);
        }

        private static OperationResult GenerateCompareLessOrEqualU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateCompareU32(context, operation, context.ULessThanEqual);
        }

        private static OperationResult GenerateCompareLessU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateCompareU32(context, operation, context.ULessThan);
        }

        private static OperationResult GenerateCompareNotEqual(CodeGenContext context, AstOperation operation)
        {
            return GenerateCompare(context, operation, context.FOrdNotEqual, context.INotEqual);
        }

        private static OperationResult GenerateConditionalSelect(CodeGenContext context, AstOperation operation)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);
            var src3 = operation.GetSource(2);

            var cond = context.Get(AggregateType.Bool, src1);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.FP64, context.Select(context.TypeFP64(), cond, context.GetFP64(src2), context.GetFP64(src3)));
            }
            else if (operation.Inst.HasFlag(Instruction.FP32))
            {
                return new OperationResult(AggregateType.FP32, context.Select(context.TypeFP32(), cond, context.GetFP32(src2), context.GetFP32(src3)));
            }
            else
            {
                return new OperationResult(AggregateType.S32, context.Select(context.TypeS32(), cond, context.GetS32(src2), context.GetS32(src3)));
            }
        }

        private static OperationResult GenerateConvertFP32ToFP64(CodeGenContext context, AstOperation operation)
        {
            var source = operation.GetSource(0);

            return new OperationResult(AggregateType.FP64, context.FConvert(context.TypeFP64(), context.GetFP32(source)));
        }

        private static OperationResult GenerateConvertFP64ToFP32(CodeGenContext context, AstOperation operation)
        {
            var source = operation.GetSource(0);

            return new OperationResult(AggregateType.FP32, context.FConvert(context.TypeFP32(), context.GetFP64(source)));
        }

        private static OperationResult GenerateConvertFPToS32(CodeGenContext context, AstOperation operation)
        {
            var source = operation.GetSource(0);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.S32, context.ConvertFToS(context.TypeS32(), context.GetFP64(source)));
            }
            else
            {
                return new OperationResult(AggregateType.S32, context.ConvertFToS(context.TypeS32(), context.GetFP32(source)));
            }
        }

        private static OperationResult GenerateConvertFPToU32(CodeGenContext context, AstOperation operation)
        {
            var source = operation.GetSource(0);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.U32, context.ConvertFToU(context.TypeU32(), context.GetFP64(source)));
            }
            else
            {
                return new OperationResult(AggregateType.U32, context.ConvertFToU(context.TypeU32(), context.GetFP32(source)));
            }
        }

        private static OperationResult GenerateConvertS32ToFP(CodeGenContext context, AstOperation operation)
        {
            var source = operation.GetSource(0);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.FP64, context.ConvertSToF(context.TypeFP64(), context.GetS32(source)));
            }
            else
            {
                return new OperationResult(AggregateType.FP32, context.ConvertSToF(context.TypeFP32(), context.GetS32(source)));
            }
        }

        private static OperationResult GenerateConvertU32ToFP(CodeGenContext context, AstOperation operation)
        {
            var source = operation.GetSource(0);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.FP64, context.ConvertUToF(context.TypeFP64(), context.GetU32(source)));
            }
            else
            {
                return new OperationResult(AggregateType.FP32, context.ConvertUToF(context.TypeFP32(), context.GetU32(source)));
            }
        }

        private static OperationResult GenerateCosine(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 14);
        }

        private static OperationResult GenerateDdx(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryFP32(context, operation, context.DPdx);
        }

        private static OperationResult GenerateDdy(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryFP32(context, operation, context.DPdy);
        }

        private static OperationResult GenerateDiscard(CodeGenContext context, AstOperation operation)
        {
            context.Kill();
            return OperationResult.Invalid;
        }

        private static OperationResult GenerateDivide(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinary(context, operation, context.FDiv, context.SDiv);
        }

        private static OperationResult GenerateExponentB2(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 29);
        }

        private static OperationResult GenerateFindFirstSetS32(CodeGenContext context, AstOperation operation)
        {
            var source = context.GetS32(operation.GetSource(0));
            return new OperationResult(AggregateType.U32, context.ExtInst(context.TypeU32(), context.ExtSet, 74, source));
        }

        private static OperationResult GenerateFindFirstSetU32(CodeGenContext context, AstOperation operation)
        {
            var source = context.GetU32(operation.GetSource(0));
            return new OperationResult(AggregateType.U32, context.ExtInst(context.TypeU32(), context.ExtSet, 75, source));
        }

        private static OperationResult GenerateFloor(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 8);
        }

        private static OperationResult GenerateFusedMultiplyAdd(CodeGenContext context, AstOperation operation)
        {
            return GenerateTernaryExtInst(context, operation, 50);
        }

        private static OperationResult GenerateGroupMemoryBarrier(CodeGenContext context, AstOperation operation)
        {
            context.MemoryBarrier(context.Constant(context.TypeU32(), 2), context.Constant(context.TypeU32(), 3400));
            return OperationResult.Invalid;
        }

        private static OperationResult GenerateImageLoad(CodeGenContext context, AstOperation operation)
        {
            AstTextureOperation texOp = (AstTextureOperation)operation;

            bool isBindless = (texOp.Flags & TextureFlags.Bindless) != 0;

            var componentType = texOp.Format.GetComponentType();

            // TODO: Bindless texture support. For now we just return 0/do nothing.
            if (isBindless)
            {
                var zero = componentType switch
                {
                    VariableType.S32 => context.Constant(context.TypeS32(), 0),
                    VariableType.U32 => context.Constant(context.TypeU32(), 0u),
                    _ => context.Constant(context.TypeFP32(), 0f),
                };

                return new OperationResult(componentType.Convert(), zero);
            }

            bool isArray   = (texOp.Type & SamplerType.Array) != 0;
            bool isIndexed = (texOp.Type & SamplerType.Indexed) != 0;

            int srcIndex = isBindless ? 1 : 0;

            SpvInstruction Src(AggregateType type)
            {
                return context.Get(type, texOp.GetSource(srcIndex++));
            }

            SpvInstruction index = null;

            if (isIndexed)
            {
                index = Src(AggregateType.S32);
            }

            int coordsCount = texOp.Type.GetDimensions();

            int pCount = coordsCount + (isArray ? 1 : 0);

            SpvInstruction pCoords;

            if (pCount > 1)
            {
                SpvInstruction[] elems = new SpvInstruction[pCount];

                for (int i = 0; i < pCount; i++)
                {
                    elems[i] = Src(AggregateType.S32);
                }

                var vectorType = context.TypeVector(context.TypeS32(), pCount);
                pCoords = context.CompositeConstruct(vectorType, elems);
            }
            else
            {
                pCoords = Src(AggregateType.S32);
            }

            (var imageType, var imageVariable) = context.Images[new TextureMeta(texOp.CbufSlot, texOp.Handle, texOp.Format, texOp.Type)];

            var image = context.Load(imageType, imageVariable);

            var texel = context.ImageRead(context.TypeVector(context.GetType(componentType.Convert()), 4), image, pCoords);
            var result = context.CompositeExtract(context.TypeFP32(), texel, (SpvLiteralInteger)texOp.Index);

            return new OperationResult(componentType.Convert(), result);
        }

        private static OperationResult GenerateImageStore(CodeGenContext context, AstOperation operation)
        {
            AstTextureOperation texOp = (AstTextureOperation)operation;

            bool isBindless = (texOp.Flags & TextureFlags.Bindless) != 0;

            // TODO: Bindless texture support. For now we just return 0/do nothing.
            if (isBindless)
            {
                return OperationResult.Invalid;
            }

            bool isArray   = (texOp.Type & SamplerType.Array)   != 0;
            bool isIndexed = (texOp.Type & SamplerType.Indexed) != 0;

            int srcIndex = isBindless ? 1 : 0;

            SpvInstruction Src(AggregateType type)
            {
                return context.Get(type, texOp.GetSource(srcIndex++));
            }

            SpvInstruction index = null;

            if (isIndexed)
            {
                index = Src(AggregateType.S32);
            }

            int coordsCount = texOp.Type.GetDimensions();

            int pCount = coordsCount + (isArray ? 1 : 0);

            SpvInstruction pCoords;

            if (pCount > 1)
            {
                SpvInstruction[] elems = new SpvInstruction[pCount];

                for (int i = 0; i < pCount; i++)
                {
                    elems[i] = Src(AggregateType.S32);
                }

                var vectorType = context.TypeVector(context.TypeS32(), pCount);
                pCoords = context.CompositeConstruct(vectorType, elems);
            }
            else
            {
                pCoords = Src(AggregateType.S32);
            }

            var componentType = texOp.Format.GetComponentType();

            const int ComponentsCount = 4;

            SpvInstruction[] cElems = new SpvInstruction[ComponentsCount];

            for (int i = 0; i < ComponentsCount; i++)
            {
                if (srcIndex < texOp.SourcesCount)
                {
                    cElems[i] = Src(componentType.Convert());
                }
                else
                {
                    cElems[i] = componentType switch
                    {
                        VariableType.S32 => context.Constant(context.TypeS32(), 0),
                        VariableType.U32 => context.Constant(context.TypeU32(), 0u),
                        _ => context.Constant(context.TypeFP32(), 0f),
                    };
                }
            }

            var texel = context.CompositeConstruct(context.TypeVector(context.GetType(componentType.Convert()), ComponentsCount), cElems);

            (var imageType, var imageVariable) = context.Images[new TextureMeta(texOp.CbufSlot, texOp.Handle, texOp.Format, texOp.Type)];

            var image = context.Load(imageType, imageVariable);

            context.ImageWrite(image, pCoords, texel);

            return OperationResult.Invalid;
        }

        private static OperationResult GenerateIsNan(CodeGenContext context, AstOperation operation)
        {
            var source = operation.GetSource(0);

            SpvInstruction result;

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                result = context.IsNan(context.TypeBool(), context.GetFP64(source));
            }
            else
            {
                result = context.IsNan(context.TypeBool(), context.GetFP32(source));
            }

            return new OperationResult(AggregateType.Bool, result);
        }

        private static OperationResult GenerateLoadAttribute(CodeGenContext context, AstOperation operation)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);

            if (src1 is not AstOperand oper || oper.Type != OperandType.Attribute)
            {
                throw new InvalidOperationException("First source of LoadAttribute must be a attribute.");
            }

            return new OperationResult(AggregateType.FP32, context.GetAttribute(AggregateType.FP32, oper, false));
        }

        private static OperationResult GenerateLoadConstant(CodeGenContext context, AstOperation operation)
        {
            var src1 = operation.GetSource(0);
            var src2 = context.Get(AggregateType.S32, operation.GetSource(1));

            var ubVariable = context.UniformBuffers[((AstOperand)src1).Value];
            var i0 = context.Constant(context.TypeS32(), 0);
            var i1 = context.ShiftRightArithmetic(context.TypeS32(), src2, context.Constant(context.TypeS32(), 2));
            var i2 = context.BitwiseAnd(context.TypeS32(), src2, context.Constant(context.TypeS32(), 3));

            var elemPointer = context.AccessChain(context.TypePointer(StorageClass.Uniform, context.TypeFP32()), ubVariable, i0, i1, i2);
            var value = context.Load(context.TypeFP32(), elemPointer);

            return new OperationResult(AggregateType.FP32, value);
        }

        private static OperationResult GenerateLoadLocal(CodeGenContext context, AstOperation operation)
        {
            return GenerateLoadLocalOrShared(context, operation, StorageClass.Private, context.LocalMemory);
        }

        private static OperationResult GenerateLoadShared(CodeGenContext context, AstOperation operation)
        {
            return GenerateLoadLocalOrShared(context, operation, StorageClass.Workgroup, context.SharedMemory);
        }

        private static OperationResult GenerateLoadLocalOrShared(
            CodeGenContext context,
            AstOperation operation,
            StorageClass storageClass,
            SpvInstruction memory)
        {
            var offset = context.Get(AggregateType.S32, operation.GetSource(0));

            var elemPointer = context.AccessChain(context.TypePointer(storageClass, context.TypeU32()), memory, offset);
            var value = context.Load(context.TypeU32(), elemPointer);

            return new OperationResult(AggregateType.U32, value);
        }

        private static OperationResult GenerateLoadStorage(CodeGenContext context, AstOperation operation)
        {
            var elemPointer = GetStorageElemPointer(context, operation);
            var value = context.Load(context.TypeU32(), elemPointer);

            return new OperationResult(AggregateType.U32, value);
        }

        private static OperationResult GenerateLod(CodeGenContext context, AstOperation operation)
        {
            AstTextureOperation texOp = (AstTextureOperation)operation;

            bool isBindless = (texOp.Flags & TextureFlags.Bindless) != 0;

            bool isIndexed = (texOp.Type & SamplerType.Indexed) != 0;

            // TODO: Bindless texture support. For now we just return 0.
            if (isBindless)
            {
                return new OperationResult(AggregateType.S32, context.Constant(context.TypeS32(), 0));
            }

            int srcIndex = 0;

            SpvInstruction Src(AggregateType type)
            {
                return context.Get(type, texOp.GetSource(srcIndex++));
            }

            SpvInstruction index = null;

            if (isIndexed)
            {
                index = Src(AggregateType.S32);
            }

            int pCount = texOp.Type.GetDimensions();

            SpvInstruction pCoords;

            if (pCount > 1)
            {
                SpvInstruction[] elems = new SpvInstruction[pCount];

                for (int i = 0; i < pCount; i++)
                {
                    elems[i] = Src(AggregateType.FP32);
                }

                var vectorType = context.TypeVector(context.TypeFP32(), pCount);
                pCoords = context.CompositeConstruct(vectorType, elems);
            }
            else
            {
                pCoords = Src(AggregateType.FP32);
            }

            var meta = new TextureMeta(texOp.CbufSlot, texOp.Handle, texOp.Format, texOp.Type);

            (_, var sampledImageType, var sampledImageVariable) = context.Samplers[meta];

            var image = context.Load(sampledImageType, sampledImageVariable);

            var resultType = context.TypeVector(context.TypeFP32(), 2);
            var packed = context.ImageQueryLod(resultType, image, pCoords);
            var result = context.CompositeExtract(context.TypeFP32(), packed, (SpvLiteralInteger)texOp.Index);

            return new OperationResult(AggregateType.FP32, result);
        }

        private static OperationResult GenerateLogarithmB2(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 30);
        }

        private static OperationResult GenerateLogicalAnd(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryBool(context, operation, context.LogicalAnd);
        }

        private static OperationResult GenerateLogicalNot(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryBool(context, operation, context.LogicalNot);
        }

        private static OperationResult GenerateLogicalOr(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryBool(context, operation, context.LogicalOr);
        }

        private static OperationResult GenerateLoopBreak(CodeGenContext context, AstOperation operation)
        {
            AstBlock loopBlock = context.CurrentBlock;
            while (loopBlock.Type != AstBlockType.DoWhile)
            {
                loopBlock = loopBlock.Parent;
            }

            context.Branch(context.GetNextLabel(loopBlock.Parent));

            return OperationResult.Invalid;
        }

        private static OperationResult GenerateMaximum(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryExtInst(context, operation, 40, 42);
        }

        private static OperationResult GenerateMaximumU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryExtInstU32(context, operation, 41);
        }

        private static OperationResult GenerateMemoryBarrier(CodeGenContext context, AstOperation operation)
        {
            context.MemoryBarrier(context.Constant(context.TypeU32(), 1), context.Constant(context.TypeU32(), 3400));
            return OperationResult.Invalid;
        }

        private static OperationResult GenerateMinimum(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryExtInst(context, operation, 37, 39);
        }

        private static OperationResult GenerateMinimumU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryExtInstU32(context, operation, 38);
        }

        private static OperationResult GenerateMultiply(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinary(context, operation, context.FMul, context.IMul);
        }

        private static OperationResult GenerateNegate(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnary(context, operation, context.FNegate, context.SNegate);
        }

        private static OperationResult GeneratePackHalf2x16(CodeGenContext context, AstOperation operation)
        {
            var value0 = context.GetFP32(operation.GetSource(0));
            var value1 = context.GetFP32(operation.GetSource(1));
            var vector = context.CompositeConstruct(context.TypeVector(context.TypeFP32(), 2), value0, value1);
            var result = context.ExtInst(context.TypeU32(), context.ExtSet, 58, vector);

            return new OperationResult(AggregateType.U32, result);
        }

        private static OperationResult GenerateReciprocalSquareRoot(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 32);
        }

        private static OperationResult GenerateReturn(CodeGenContext context, AstOperation operation)
        {
            context.Return();
            return OperationResult.Invalid;
        }

        private static OperationResult GenerateRound(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 2);
        }

        private static OperationResult GenerateShiftLeft(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryS32(context, operation, context.ShiftLeftLogical);
        }

        private static OperationResult GenerateShiftRightS32(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryS32(context, operation, context.ShiftRightArithmetic);
        }

        private static OperationResult GenerateShiftRightU32(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinaryS32(context, operation, context.ShiftRightLogical);
        }

        private static OperationResult GenerateShuffle(CodeGenContext context, AstOperation operation)
        {
            var x = context.GetFP32(operation.GetSource(0));
            var index = context.GetU32(operation.GetSource(1));
            var mask = context.GetU32(operation.GetSource(2));

            var const31 = context.Constant(context.TypeU32(), 31);
            var const8 = context.Constant(context.TypeU32(), 8);

            var clamp = context.BitwiseAnd(context.TypeU32(), mask, const31);
            var segMask = context.BitwiseAnd(context.TypeU32(), context.ShiftRightLogical(context.TypeU32(), mask, const8), const31);
            var notSegMask = context.Not(context.TypeU32(), segMask);
            var clampNotSegMask = context.BitwiseAnd(context.TypeU32(), clamp, notSegMask);
            var indexNotSegMask = context.BitwiseAnd(context.TypeU32(), index, notSegMask);

            var threadId = context.GetAttribute(AggregateType.U32, new AstOperand(OperandType.Attribute, AttributeConsts.LaneId), false);

            var minThreadId = context.BitwiseAnd(context.TypeU32(), threadId, segMask);
            var maxThreadId = context.BitwiseOr(context.TypeU32(), minThreadId, clampNotSegMask);
            var srcThreadId = context.BitwiseOr(context.TypeU32(), indexNotSegMask, minThreadId);
            var valid = context.ULessThanEqual(context.TypeBool(), srcThreadId, maxThreadId);
            var value = context.SubgroupReadInvocationKHR(context.TypeFP32(), x, srcThreadId);
            var result = context.Select(context.TypeFP32(), valid, value, x);

            var validLocal = (AstOperand)operation.GetSource(3);

            context.Store(context.GetLocalPointer(validLocal), context.BitcastIfNeeded(validLocal.VarType.Convert(), AggregateType.Bool, valid));

            return new OperationResult(AggregateType.FP32, result);
        }

        private static OperationResult GenerateShuffleDown(CodeGenContext context, AstOperation operation)
        {
            var x = context.GetFP32(operation.GetSource(0));
            var index = context.GetU32(operation.GetSource(1));
            var mask = context.GetU32(operation.GetSource(2));

            var const31 = context.Constant(context.TypeU32(), 31);
            var const8 = context.Constant(context.TypeU32(), 8);

            var clamp = context.BitwiseAnd(context.TypeU32(), mask, const31);
            var segMask = context.BitwiseAnd(context.TypeU32(), context.ShiftRightLogical(context.TypeU32(), mask, const8), const31);
            var notSegMask = context.Not(context.TypeU32(), segMask);
            var clampNotSegMask = context.BitwiseAnd(context.TypeU32(), clamp, notSegMask);
            var indexNotSegMask = context.BitwiseAnd(context.TypeU32(), index, notSegMask);

            var threadId = context.GetAttribute(AggregateType.U32, new AstOperand(OperandType.Attribute, AttributeConsts.LaneId), false);

            var minThreadId = context.BitwiseAnd(context.TypeU32(), threadId, segMask);
            var maxThreadId = context.BitwiseOr(context.TypeU32(), minThreadId, clampNotSegMask);
            var srcThreadId = context.IAdd(context.TypeU32(), threadId, index);
            var valid = context.ULessThanEqual(context.TypeBool(), srcThreadId, maxThreadId);
            var value = context.SubgroupReadInvocationKHR(context.TypeFP32(), x, srcThreadId);
            var result = context.Select(context.TypeFP32(), valid, value, x);

            var validLocal = (AstOperand)operation.GetSource(3);

            context.Store(context.GetLocalPointer(validLocal), context.BitcastIfNeeded(validLocal.VarType.Convert(), AggregateType.Bool, valid));

            return new OperationResult(AggregateType.FP32, result);
        }

        private static OperationResult GenerateShuffleUp(CodeGenContext context, AstOperation operation)
        {
            var x = context.GetFP32(operation.GetSource(0));
            var index = context.GetU32(operation.GetSource(1));
            var mask = context.GetU32(operation.GetSource(2));

            var const31 = context.Constant(context.TypeU32(), 31);
            var const8 = context.Constant(context.TypeU32(), 8);

            var clamp = context.BitwiseAnd(context.TypeU32(), mask, const31);
            var segMask = context.BitwiseAnd(context.TypeU32(), context.ShiftRightLogical(context.TypeU32(), mask, const8), const31);
            var notSegMask = context.Not(context.TypeU32(), segMask);
            var clampNotSegMask = context.BitwiseAnd(context.TypeU32(), clamp, notSegMask);
            var indexNotSegMask = context.BitwiseAnd(context.TypeU32(), index, notSegMask);

            var threadId = context.GetAttribute(AggregateType.U32, new AstOperand(OperandType.Attribute, AttributeConsts.LaneId), false);

            var minThreadId = context.BitwiseAnd(context.TypeU32(), threadId, segMask);
            var srcThreadId = context.ISub(context.TypeU32(), threadId, index);
            var valid = context.UGreaterThanEqual(context.TypeBool(), srcThreadId, minThreadId);
            var value = context.SubgroupReadInvocationKHR(context.TypeFP32(), x, srcThreadId);
            var result = context.Select(context.TypeFP32(), valid, value, x);

            var validLocal = (AstOperand)operation.GetSource(3);

            context.Store(context.GetLocalPointer(validLocal), context.BitcastIfNeeded(validLocal.VarType.Convert(), AggregateType.Bool, valid));

            return new OperationResult(AggregateType.FP32, result);
        }

        private static OperationResult GenerateShuffleXor(CodeGenContext context, AstOperation operation)
        {
            var x = context.GetFP32(operation.GetSource(0));
            var index = context.GetU32(operation.GetSource(1));
            var mask = context.GetU32(operation.GetSource(2));

            var const31 = context.Constant(context.TypeU32(), 31);
            var const8 = context.Constant(context.TypeU32(), 8);

            var clamp = context.BitwiseAnd(context.TypeU32(), mask, const31);
            var segMask = context.BitwiseAnd(context.TypeU32(), context.ShiftRightLogical(context.TypeU32(), mask, const8), const31);
            var notSegMask = context.Not(context.TypeU32(), segMask);
            var clampNotSegMask = context.BitwiseAnd(context.TypeU32(), clamp, notSegMask);
            var indexNotSegMask = context.BitwiseAnd(context.TypeU32(), index, notSegMask);

            var threadId = context.GetAttribute(AggregateType.U32, new AstOperand(OperandType.Attribute, AttributeConsts.LaneId), false);

            var minThreadId = context.BitwiseAnd(context.TypeU32(), threadId, segMask);
            var maxThreadId = context.BitwiseOr(context.TypeU32(), minThreadId, clampNotSegMask);
            var srcThreadId = context.BitwiseXor(context.TypeU32(), threadId, index);
            var valid = context.ULessThanEqual(context.TypeBool(), srcThreadId, maxThreadId);
            var value = context.SubgroupReadInvocationKHR(context.TypeFP32(), x, srcThreadId);
            var result = context.Select(context.TypeFP32(), valid, value, x);

            var validLocal = (AstOperand)operation.GetSource(3);

            context.Store(context.GetLocalPointer(validLocal), context.BitcastIfNeeded(validLocal.VarType.Convert(), AggregateType.Bool, valid));

            return new OperationResult(AggregateType.FP32, result);
        }

        private static OperationResult GenerateSine(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 13);
        }

        private static OperationResult GenerateSquareRoot(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 31);
        }

        private static OperationResult GenerateStoreLocal(CodeGenContext context, AstOperation operation)
        {
            return GenerateStoreLocalOrShared(context, operation, StorageClass.Private, context.LocalMemory);
        }

        private static OperationResult GenerateStoreShared(CodeGenContext context, AstOperation operation)
        {
            return GenerateStoreLocalOrShared(context, operation, StorageClass.Workgroup, context.SharedMemory);
        }

        private static OperationResult GenerateStoreLocalOrShared(
            CodeGenContext context,
            AstOperation operation,
            StorageClass storageClass,
            SpvInstruction memory)
        {
            var offset = context.Get(AggregateType.S32, operation.GetSource(0));
            var value = context.Get(AggregateType.U32, operation.GetSource(1));

            var elemPointer = context.AccessChain(context.TypePointer(storageClass, context.TypeU32()), memory, offset);
            context.Store(elemPointer, value);

            return OperationResult.Invalid;
        }

        private static OperationResult GenerateStoreStorage(CodeGenContext context, AstOperation operation)
        {
            var elemPointer = GetStorageElemPointer(context, operation);
            context.Store(elemPointer, context.Get(AggregateType.U32, operation.GetSource(2)));

            return OperationResult.Invalid;
        }

        private static OperationResult GenerateSubtract(CodeGenContext context, AstOperation operation)
        {
            return GenerateBinary(context, operation, context.FSub, context.ISub);
        }

        private static OperationResult GenerateTextureSample(CodeGenContext context, AstOperation operation)
        {
            AstTextureOperation texOp = (AstTextureOperation)operation;

            bool isBindless     = (texOp.Flags & TextureFlags.Bindless)    != 0;
            bool isGather       = (texOp.Flags & TextureFlags.Gather)      != 0;
            bool hasDerivatives = (texOp.Flags & TextureFlags.Derivatives) != 0;
            bool intCoords      = (texOp.Flags & TextureFlags.IntCoords)   != 0;
            bool hasLodBias     = (texOp.Flags & TextureFlags.LodBias)     != 0;
            bool hasLodLevel    = (texOp.Flags & TextureFlags.LodLevel)    != 0;
            bool hasOffset      = (texOp.Flags & TextureFlags.Offset)      != 0;
            bool hasOffsets     = (texOp.Flags & TextureFlags.Offsets)     != 0;

            bool isArray       = (texOp.Type & SamplerType.Array)       != 0;
            bool isIndexed     = (texOp.Type & SamplerType.Indexed)     != 0;
            bool isMultisample = (texOp.Type & SamplerType.Multisample) != 0;
            bool isShadow      = (texOp.Type & SamplerType.Shadow)      != 0;

            // TODO: Bindless texture support. For now we just return 0.
            if (isBindless)
            {
                return new OperationResult(AggregateType.FP32, context.Constant(context.TypeFP32(), 0f));
            }

            // This combination is valid, but not available on GLSL.
            // For now, ignore the LOD level and do a normal sample.
            // TODO: How to implement it properly?
            if (hasLodLevel && isArray && isShadow)
            {
                hasLodLevel = false;
            }

            int srcIndex = isBindless ? 1 : 0;

            SpvInstruction Src(AggregateType type)
            {
                return context.Get(type, texOp.GetSource(srcIndex++));
            }

            SpvInstruction index = null;

            if (isIndexed)
            {
                index = Src(AggregateType.S32);
            }

            int coordsCount = texOp.Type.GetDimensions();

            int pCount = coordsCount;

            int arrayIndexElem = -1;

            if (isArray)
            {
                arrayIndexElem = pCount++;
            }

            AggregateType coordType = intCoords ? AggregateType.S32 : AggregateType.FP32;

            SpvInstruction AssemblePVector(int count)
            {
                if (count > 1)
                {
                    SpvInstruction[] elems = new SpvInstruction[count];

                    for (int index = 0; index < count; index++)
                    {
                        if (arrayIndexElem == index)
                        {
                            elems[index] = Src(AggregateType.S32);

                            if (!intCoords)
                            {
                                elems[index] = context.ConvertSToF(context.TypeFP32(), elems[index]);
                            }
                        }
                        else
                        {
                            elems[index] = Src(coordType);
                        }
                    }

                    var vectorType = context.TypeVector(intCoords ? context.TypeS32() : context.TypeFP32(), count);
                    return context.CompositeConstruct(vectorType, elems);
                }
                else
                {
                    return Src(coordType);
                }
            }

            SpvInstruction pCoords = AssemblePVector(pCount);

            SpvInstruction AssembleDerivativesVector(int count)
            {
                if (count > 1)
                {
                    SpvInstruction[] elems = new SpvInstruction[count];

                    for (int index = 0; index < count; index++)
                    {
                        elems[index] = Src(AggregateType.FP32);
                    }

                    var vectorType = context.TypeVector(context.TypeFP32(), count);
                    return context.CompositeConstruct(vectorType, elems);
                }
                else
                {
                    return Src(AggregateType.FP32);
                }
            }

            SpvInstruction dRef = null;

            if (isShadow)
            {
                dRef = Src(AggregateType.FP32);
            }

            SpvInstruction[] derivatives = null;

            if (hasDerivatives)
            {
                derivatives = new[]
                {
                    AssembleDerivativesVector(coordsCount), // dPdx
                    AssembleDerivativesVector(coordsCount)  // dPdy
                };
            }

            SpvInstruction sample = null;
            SpvInstruction lod = null;

            if (isMultisample)
            {
                sample = Src(AggregateType.S32);
            }
            else if (hasLodLevel)
            {
                lod = Src(coordType);
            }

            SpvInstruction AssembleOffsetVector(int count)
            {
                if (count > 1)
                {
                    SpvInstruction[] elems = new SpvInstruction[count];

                    for (int index = 0; index < count; index++)
                    {
                        elems[index] = Src(AggregateType.S32);
                    }

                    var vectorType = context.TypeVector(context.TypeS32(), count);
                    return context.CompositeConstruct(vectorType, elems);
                }
                else
                {
                    return Src(AggregateType.S32);
                }
            }

            SpvInstruction[] offsets = null;

            if (hasOffset)
            {
                offsets = new[] { AssembleOffsetVector(coordsCount) };
            }
            else if (hasOffsets)
            {
                offsets = new[]
                {
                    AssembleOffsetVector(coordsCount),
                    AssembleOffsetVector(coordsCount),
                    AssembleOffsetVector(coordsCount),
                    AssembleOffsetVector(coordsCount)
                };
            }

            SpvInstruction lodBias = null;

            if (hasLodBias)
            {
               lodBias = Src(AggregateType.FP32);
            }

            SpvInstruction compIdx = null;

            // textureGather* optional extra component index,
            // not needed for shadow samplers.
            if (isGather && !isShadow)
            {
               compIdx = Src(AggregateType.S32);
            }

            var operandsList = new List<SpvInstruction>();
            var operandsMask = ImageOperandsMask.MaskNone;

            if (hasLodBias)
            {
                operandsMask |= ImageOperandsMask.Bias;
                operandsList.Add(lodBias);
            }

            if (hasLodLevel)
            {
                operandsMask |= ImageOperandsMask.Lod;
                operandsList.Add(lod);
            }

            if (hasDerivatives)
            {
                operandsMask |= ImageOperandsMask.Grad;
                operandsList.Add(derivatives[0]);
                operandsList.Add(derivatives[1]);
            }

            if (hasOffset)
            {
                operandsMask |= ImageOperandsMask.ConstOffset;
                operandsList.Add(offsets[0]);
            }
            else if (hasOffsets)
            {
                operandsMask |= ImageOperandsMask.ConstOffsets;
                operandsList.Add(offsets[0]);
                operandsList.Add(offsets[1]);
                operandsList.Add(offsets[2]);
                operandsList.Add(offsets[3]);
            }

            if (isMultisample)
            {
                operandsMask |= ImageOperandsMask.Sample;
                operandsList.Add(sample);
            }

            bool colorIsVector = isGather || !isShadow;
            var resultType = colorIsVector ? context.TypeVector(context.TypeFP32(), 4) : context.TypeFP32();

            var meta = new TextureMeta(texOp.CbufSlot, texOp.Handle, texOp.Format, texOp.Type);

            (var imageType, var sampledImageType, var sampledImageVariable) = context.Samplers[meta];

            var image = context.Load(sampledImageType, sampledImageVariable);

            if (intCoords)
            {
                image = context.Image(imageType, image);
            }

            var operands = operandsList.ToArray();

            SpvInstruction result;

            if (intCoords)
            {
                result = context.ImageFetch(resultType, image, pCoords, operandsMask, operands);
            }
            else if (isGather)
            {
                if (isShadow)
                {
                    result = context.ImageDrefGather(resultType, image, pCoords, dRef, operandsMask, operands);
                }
                else
                {
                    result = context.ImageGather(resultType, image, pCoords, compIdx, operandsMask, operands);
                }
            }
            else if (isShadow)
            {
                if (hasLodLevel)
                {
                    result = context.ImageSampleDrefExplicitLod(resultType, image, pCoords, dRef, operandsMask, operands);
                }
                else
                {
                    result = context.ImageSampleDrefImplicitLod(resultType, image, pCoords, dRef, operandsMask, operands);
                }
            }
            else if (hasDerivatives || hasLodLevel)
            {
                result = context.ImageSampleExplicitLod(resultType, image, pCoords, operandsMask, operands);
            }
            else
            {
                result = context.ImageSampleImplicitLod(resultType, image, pCoords, operandsMask, operands);
            }

            if (colorIsVector)
            {
                result = context.CompositeExtract(context.TypeFP32(), result, (SpvLiteralInteger)texOp.Index);
            }

            return new OperationResult(AggregateType.FP32, result);
        }

        private static OperationResult GenerateTextureSize(CodeGenContext context, AstOperation operation)
        {
            AstTextureOperation texOp = (AstTextureOperation)operation;

            bool isBindless = (texOp.Flags & TextureFlags.Bindless) != 0;

            // TODO: Bindless texture support. For now we just return 0.
            if (isBindless)
            {
                return new OperationResult(AggregateType.S32, context.Constant(context.TypeS32(), 0));
            }

            bool isIndexed = (texOp.Type & SamplerType.Indexed) != 0;

            SpvInstruction index = null;

            if (isIndexed)
            {
                index = context.GetS32(texOp.GetSource(0));
            }

            int lodSrcIndex = isBindless || isIndexed ? 1 : 0;

            var lod = context.GetS32(operation.GetSource(lodSrcIndex));

            var meta = new TextureMeta(texOp.CbufSlot, texOp.Handle, texOp.Format, texOp.Type);

            (_, var sampledImageType, var sampledImageVariable) = context.Samplers[meta];

            var image = context.Load(sampledImageType, sampledImageVariable);

            if (texOp.Index == 3)
            {
                return new OperationResult(AggregateType.S32, context.ImageQueryLevels(context.TypeS32(), image));
            }
            else
            {
                int components = (texOp.Type & SamplerType.Mask) == SamplerType.TextureCube ? 2 : texOp.Type.GetDimensions();
                var resultType = components == 1 ? context.TypeS32() : context.TypeVector(context.TypeS32(), components);
                var result = context.ImageQuerySizeLod(resultType, image, lod);

                if (components != 1)
                {
                    result = context.CompositeExtract(context.TypeS32(), result, (SpvLiteralInteger)texOp.Index);
                }

                return new OperationResult(AggregateType.S32, result);
            }
        }

        private static OperationResult GenerateTruncate(CodeGenContext context, AstOperation operation)
        {
            return GenerateUnaryExtInst(context, operation, 3);
        }

        private static OperationResult GenerateUnpackHalf2x16(CodeGenContext context, AstOperation operation)
        {
            var value = context.GetU32(operation.GetSource(0));
            var vector = context.ExtInst(context.TypeVector(context.TypeFP32(), 2), context.ExtSet, 62, value);
            var result = context.CompositeExtract(context.TypeFP32(), vector, operation.Index);

            return new OperationResult(AggregateType.FP32, result);
        }

        private static OperationResult GenerateVoteAll(CodeGenContext context, AstOperation operation)
        {
            var result = context.SubgroupAllKHR(context.TypeBool(), context.Get(AggregateType.Bool, operation.GetSource(0)));
            return new OperationResult(AggregateType.Bool, result);
        }

        private static OperationResult GenerateVoteAllEqual(CodeGenContext context, AstOperation operation)
        {
            var result = context.SubgroupAllEqualKHR(context.TypeBool(), context.Get(AggregateType.Bool, operation.GetSource(0)));
            return new OperationResult(AggregateType.Bool, result);
        }

        private static OperationResult GenerateVoteAny(CodeGenContext context, AstOperation operation)
        {
            var result = context.SubgroupAnyKHR(context.TypeBool(), context.Get(AggregateType.Bool, operation.GetSource(0)));
            return new OperationResult(AggregateType.Bool, result);
        }

        private static OperationResult GenerateCompare(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction> emitF,
            Func<SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction> emitI)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);

            SpvInstruction result;

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                result = emitF(context.TypeBool(), context.GetFP64(src1), context.GetFP64(src2));
            }
            else if (operation.Inst.HasFlag(Instruction.FP32))
            {
                result = emitF(context.TypeBool(), context.GetFP32(src1), context.GetFP32(src2));
            }
            else
            {
                result = emitI(context.TypeBool(), context.GetS32(src1), context.GetS32(src2));
            }

            return new OperationResult(AggregateType.Bool, result);
        }

        private static OperationResult GenerateCompareU32(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction> emitU)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);

            var result = emitU(context.TypeBool(), context.GetU32(src1), context.GetU32(src2));

            return new OperationResult(AggregateType.Bool, result);
        }

        private static OperationResult GenerateAtomicMemoryBinary(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction> emitU)
        {
            var value = context.GetU32(operation.GetSource(2));

            SpvInstruction elemPointer;
            Instruction mr = operation.Inst & Instruction.MrMask;

            if (mr == Instruction.MrStorage)
            {
                elemPointer = GetStorageElemPointer(context, operation);
            }
            else if (mr == Instruction.MrShared)
            {
                var offset = context.GetU32(operation.GetSource(0));
                elemPointer = context.AccessChain(context.TypePointer(StorageClass.Workgroup, context.TypeU32()), context.SharedMemory, offset);
            }
            else
            {
                throw new InvalidOperationException($"Invalid storage class \"{mr}\".");
            }

            var one = context.Constant(context.TypeU32(), 1);
            var zero = context.Constant(context.TypeU32(), 0);

            return new OperationResult(AggregateType.U32, emitU(context.TypeU32(), elemPointer, one, zero, value));
        }

        private static OperationResult GenerateAtomicMemoryCas(CodeGenContext context, AstOperation operation)
        {
            var value0 = context.GetU32(operation.GetSource(2));
            var value1 = context.GetU32(operation.GetSource(3));

            SpvInstruction elemPointer;
            Instruction mr = operation.Inst & Instruction.MrMask;

            if (mr == Instruction.MrStorage)
            {
                elemPointer = GetStorageElemPointer(context, operation);
            }
            else if (mr == Instruction.MrShared)
            {
                var offset = context.GetU32(operation.GetSource(0));
                elemPointer = context.AccessChain(context.TypePointer(StorageClass.Workgroup, context.TypeU32()), context.SharedMemory, offset);
            }
            else
            {
                throw new InvalidOperationException($"Invalid storage class \"{mr}\".");
            }

            var one = context.Constant(context.TypeU32(), 1);
            var zero = context.Constant(context.TypeU32(), 0);

            return new OperationResult(AggregateType.U32, context.AtomicCompareExchange(context.TypeU32(), elemPointer, one, zero, zero, value1, value0));
        }

        private static SpvInstruction GetStorageElemPointer(CodeGenContext context, AstOperation operation)
        {
            var sbVariable = context.StorageBuffersArray;
            var i0 = context.Get(AggregateType.S32, operation.GetSource(0));
            var i1 = context.Constant(context.TypeS32(), 0);
            var i2 = context.Get(AggregateType.S32, operation.GetSource(1));

            return context.AccessChain(context.TypePointer(StorageClass.Uniform, context.TypeU32()), sbVariable, i0, i1, i2);
        }

        private static OperationResult GenerateUnary(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction> emitF,
            Func<SpvInstruction, SpvInstruction, SpvInstruction> emitI)
        {
            var source = operation.GetSource(0);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.FP64, emitF(context.TypeFP64(), context.GetFP64(source)));
            }
            else if (operation.Inst.HasFlag(Instruction.FP32))
            {
                return new OperationResult(AggregateType.FP32, emitF(context.TypeFP32(), context.GetFP32(source)));
            }
            else
            {
                return new OperationResult(AggregateType.S32, emitI(context.TypeS32(), context.GetS32(source)));
            }
        }

        private static OperationResult GenerateUnaryBool(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction> emitB)
        {
            var source = operation.GetSource(0);
            return new OperationResult(AggregateType.Bool, emitB(context.TypeBool(), context.Get(AggregateType.Bool, source)));
        }

         private static OperationResult GenerateUnaryFP32(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction> emit)
        {
            var source = operation.GetSource(0);
            return new OperationResult(AggregateType.FP32, emit(context.TypeFP32(), context.GetFP32(source)));
        }

        private static OperationResult GenerateUnaryS32(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction> emitS)
        {
            var source = operation.GetSource(0);
            return new OperationResult(AggregateType.S32, emitS(context.TypeS32(), context.GetS32(source)));
        }

        private static OperationResult GenerateUnaryExtInst(CodeGenContext context, AstOperation operation, int instruction)
        {
            var source = operation.GetSource(0);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.FP64, context.ExtInst(context.TypeFP64(), context.ExtSet, instruction, context.GetFP64(source)));
            }
            else
            {
                return new OperationResult(AggregateType.FP32, context.ExtInst(context.TypeFP32(), context.ExtSet, instruction, context.GetFP32(source)));
            }
        }

        private static OperationResult GenerateUnaryExtInst(CodeGenContext context, AstOperation operation, int instF, int instS)
        {
            var source = operation.GetSource(0);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.FP64, context.ExtInst(context.TypeFP64(), context.ExtSet, instF, context.GetFP64(source)));
            }
            else if (operation.Inst.HasFlag(Instruction.FP32))
            {
                return new OperationResult(AggregateType.FP32, context.ExtInst(context.TypeFP32(), context.ExtSet, instF, context.GetFP32(source)));
            }
            else
            {
                return new OperationResult(AggregateType.S32, context.ExtInst(context.TypeS32(), context.ExtSet, instS, context.GetS32(source)));
            }
        }

        private static OperationResult GenerateBinary(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction> emitF,
            Func<SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction> emitI)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                var result = emitF(context.TypeFP64(), context.GetFP64(src1), context.GetFP64(src2));
                context.Decorate(result, Decoration.NoContraction);
                return new OperationResult(AggregateType.FP64, result);
            }
            else if (operation.Inst.HasFlag(Instruction.FP32))
            {
                var result = emitF(context.TypeFP32(), context.GetFP32(src1), context.GetFP32(src2));
                context.Decorate(result, Decoration.NoContraction);
                return new OperationResult(AggregateType.FP32, result);
            }
            else
            {
                return new OperationResult(AggregateType.S32, emitI(context.TypeS32(), context.GetS32(src1), context.GetS32(src2)));
            }
        }

        private static OperationResult GenerateBinaryBool(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction> emitB)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);

            return new OperationResult(AggregateType.Bool, emitB(context.TypeBool(), context.Get(AggregateType.Bool, src1), context.Get(AggregateType.Bool, src2)));
        }

        private static OperationResult GenerateBinaryS32(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction> emitS)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);

            return new OperationResult(AggregateType.S32, emitS(context.TypeS32(), context.GetS32(src1), context.GetS32(src2)));
        }

        private static OperationResult GenerateBinaryExtInst(CodeGenContext context, AstOperation operation, int instF, int instS)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.FP64, context.ExtInst(context.TypeFP64(), context.ExtSet, instF, context.GetFP64(src1), context.GetFP64(src2)));
            }
            else if (operation.Inst.HasFlag(Instruction.FP32))
            {
                return new OperationResult(AggregateType.FP32, context.ExtInst(context.TypeFP32(), context.ExtSet, instF, context.GetFP32(src1), context.GetFP32(src2)));
            }
            else
            {
                return new OperationResult(AggregateType.S32, context.ExtInst(context.TypeS32(), context.ExtSet, instS, context.GetS32(src1), context.GetS32(src2)));
            }
        }

        private static OperationResult GenerateBinaryExtInstU32(CodeGenContext context, AstOperation operation, int instU)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);

            return new OperationResult(AggregateType.U32, context.ExtInst(context.TypeU32(), context.ExtSet, instU, context.GetU32(src1), context.GetU32(src2)));
        }

        private static OperationResult GenerateTernaryS32(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction> emitS)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);
            var src3 = operation.GetSource(2);

            return new OperationResult(AggregateType.S32, emitS(
                context.TypeS32(),
                context.GetS32(src1),
                context.GetS32(src2),
                context.GetS32(src3)));
        }

        private static OperationResult GenerateTernaryExtInst(CodeGenContext context, AstOperation operation, int instF)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);
            var src3 = operation.GetSource(2);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.FP64, context.ExtInst(
                    context.TypeFP64(),
                    context.ExtSet,
                    instF,
                    context.GetFP64(src1),
                    context.GetFP64(src2),
                    context.GetFP64(src3)));
            }
            else
            {
                return new OperationResult(AggregateType.FP32, context.ExtInst(
                    context.TypeFP32(),
                    context.ExtSet,
                    instF,
                    context.GetFP32(src1),
                    context.GetFP32(src2),
                    context.GetFP32(src3)));
            }
        }

        private static OperationResult GenerateTernaryExtInst(CodeGenContext context, AstOperation operation, int instF, int instS)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);
            var src3 = operation.GetSource(2);

            if (operation.Inst.HasFlag(Instruction.FP64))
            {
                return new OperationResult(AggregateType.FP64, context.ExtInst(
                    context.TypeFP64(),
                    context.ExtSet,
                    instF,
                    context.GetFP64(src1),
                    context.GetFP64(src2),
                    context.GetFP64(src3)));
            }
            else if (operation.Inst.HasFlag(Instruction.FP32))
            {
                return new OperationResult(AggregateType.FP32, context.ExtInst(
                    context.TypeFP32(),
                    context.ExtSet,
                    instF,
                    context.GetFP32(src1),
                    context.GetFP32(src2),
                    context.GetFP32(src3)));
            }
            else
            {
                return new OperationResult(AggregateType.S32, context.ExtInst(
                    context.TypeS32(),
                    context.ExtSet,
                    instS,
                    context.GetS32(src1),
                    context.GetS32(src2),
                    context.GetS32(src3)));
            }
        }

        private static OperationResult GenerateTernaryExtInstU32(CodeGenContext context, AstOperation operation, int instU)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);
            var src3 = operation.GetSource(2);

            return new OperationResult(AggregateType.U32, context.ExtInst(
                context.TypeU32(),
                context.ExtSet,
                instU,
                context.GetU32(src1),
                context.GetU32(src2),
                context.GetU32(src3)));
        }

        private static OperationResult GenerateQuaternaryS32(
            CodeGenContext context,
            AstOperation operation,
            Func<SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction, SpvInstruction> emitS)
        {
            var src1 = operation.GetSource(0);
            var src2 = operation.GetSource(1);
            var src3 = operation.GetSource(2);
            var src4 = operation.GetSource(3);

            return new OperationResult(AggregateType.S32, emitS(
                context.TypeS32(),
                context.GetS32(src1),
                context.GetS32(src2),
                context.GetS32(src3),
                context.GetS32(src4)));
        }
    }
}
