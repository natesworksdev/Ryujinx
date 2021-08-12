using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections.Generic;
using static Spv.Specification;

namespace Ryujinx.Graphics.Shader.CodeGen.Spirv
{
    using SpvInstruction = Spv.Generator.Instruction;
    using SpvLiteralInteger = Spv.Generator.LiteralInteger;

    static class SpirvGenerator
    {
        private const HelperFunctionsMask NeedsInvocationIdMask =
            HelperFunctionsMask.Shuffle |
            HelperFunctionsMask.ShuffleDown |
            HelperFunctionsMask.ShuffleUp |
            HelperFunctionsMask.ShuffleXor |
            HelperFunctionsMask.SwizzleAdd;

        public static byte[] Generate(StructuredProgramInfo info, ShaderConfig config)
        {
            CodeGenContext context = new CodeGenContext(config);

            context.AddCapability(Capability.GroupNonUniformBallot);
            context.AddCapability(Capability.ImageBuffer);
            context.AddCapability(Capability.SampledBuffer);
            context.AddCapability(Capability.SubgroupBallotKHR);
            context.AddCapability(Capability.SubgroupVoteKHR);
            context.AddExtension("SPV_KHR_shader_ballot");
            context.AddExtension("SPV_KHR_subgroup_vote");

            Declarations.DeclareAll(context, info);

            if ((info.HelperFunctionsMask & NeedsInvocationIdMask) != 0)
            {
                Declarations.DeclareInvocationId(context);
            }

            for (int funcIndex = 0; funcIndex < info.Functions.Count; funcIndex++)
            {
                var function = info.Functions[funcIndex];
                var retType = context.GetType(function.ReturnType.Convert());

                var funcArgs = new SpvInstruction[function.InArguments.Length + function.OutArguments.Length];

                for (int argIndex = 0; argIndex < funcArgs.Length; argIndex++)
                {
                    var argType = context.GetType(function.GetArgumentType(argIndex).Convert());
                    var argPointerType = context.TypePointer(StorageClass.Function, argType);
                    funcArgs[argIndex] = argPointerType;
                }

                var funcType = context.TypeFunction(retType, false, funcArgs);
                var spvFunc = context.Function(retType, FunctionControlMask.MaskNone, funcType);

                context.DeclareFunction(funcIndex, function, spvFunc);
            }

            for (int funcIndex = 0; funcIndex < info.Functions.Count; funcIndex++)
            {
                Generate(context, info, funcIndex);
            }

            return context.Generate();
        }

        private static void Generate(CodeGenContext context, StructuredProgramInfo info, int funcIndex)
        {
            var function = info.Functions[funcIndex];

            (_, var spvFunc) = context.GetFunction(funcIndex);

            context.AddFunction(spvFunc);
            context.StartFunction();

            Declarations.DeclareParameters(context, function);

            context.EnterBlock(function.MainBlock);

            Declarations.DeclareLocals(context, function);
            Declarations.DeclareLocalForArgs(context, info.Functions);

            if (funcIndex == 0)
            {
                var v4Type = context.TypeVector(context.TypeFP32(), 4);
                var zero = context.Constant(context.TypeFP32(), 0f);
                var one = context.Constant(context.TypeFP32(), 1f);

                // Some games will leave some elements of gl_Position uninitialized,
                // in those cases, the elements will contain undefined values according
                // to the spec, but on NVIDIA they seems to be always initialized to (0, 0, 0, 1),
                // so we do explicit initialization to avoid UB on non-NVIDIA gpus.
                if (context.Config.Stage == ShaderStage.Vertex)
                {
                    var elemPointer = context.GetAttributeVectorPointer(new AstOperand(OperandType.Attribute, AttributeConsts.PositionX), true);
                    context.Store(elemPointer, context.CompositeConstruct(v4Type, zero, zero, zero, one));
                }

                // Ensure that unused attributes are set, otherwise the downstream
                // compiler may eliminate them.
                // (Not needed for fragment shader as it is the last stage).
                if (context.Config.Stage != ShaderStage.Compute &&
                    context.Config.Stage != ShaderStage.Fragment &&
                    !context.Config.GpPassthrough)
                {
                    for (int attr = 0; attr < Declarations.MaxAttributes; attr++)
                    {
                        if (context.Config.Options.Flags.HasFlag(TranslationFlags.Feedback))
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            int currAttr = AttributeConsts.UserAttributeBase + attr * 16;
                            var elemPointer = context.GetAttributeVectorPointer(new AstOperand(OperandType.Attribute, currAttr), true);
                            context.Store(elemPointer, context.CompositeConstruct(v4Type, zero, zero, zero, one));
                        }
                    }
                }
            }

            Generate(context, function.MainBlock);

            context.FunctionEnd();

            if (funcIndex == 0)
            {
                context.AddEntryPoint(context.Config.Stage.Convert(), spvFunc, "main", context.GetMainInterface());

                if (context.Config.Stage == ShaderStage.Fragment)
                {
                    context.AddExecutionMode(spvFunc, context.Config.Options.TargetApi == TargetApi.Vulkan
                        ? ExecutionMode.OriginUpperLeft
                        : ExecutionMode.OriginLowerLeft);
                }
                else if (context.Config.Stage == ShaderStage.Compute)
                {
                    var localSizeX = (SpvLiteralInteger)context.Config.GpuAccessor.QueryComputeLocalSizeX();
                    var localSizeY = (SpvLiteralInteger)context.Config.GpuAccessor.QueryComputeLocalSizeY();
                    var localSizeZ = (SpvLiteralInteger)context.Config.GpuAccessor.QueryComputeLocalSizeZ();

                    context.AddExecutionMode(
                        spvFunc,
                        ExecutionMode.LocalSize,
                        localSizeX,
                        localSizeY,
                        localSizeZ);
                }
            }
        }

        private static void Generate(CodeGenContext context, AstBlock block)
        {
            AstBlockVisitor visitor = new AstBlockVisitor(block);

            var loopTargets = new Dictionary<AstBlock, (SpvInstruction, SpvInstruction)>();

            visitor.BlockEntered += (sender, e) =>
            {
                AstBlock mergeBlock = e.Block.Parent;

                if (e.Block.Type == AstBlockType.If)
                {
                    AstBlock ifTrueBlock = e.Block;
                    AstBlock ifFalseBlock;

                    if (AstHelper.Next(e.Block) is AstBlock nextBlock && nextBlock.Type == AstBlockType.Else)
                    {
                        ifFalseBlock = nextBlock;
                    }
                    else
                    {
                        ifFalseBlock = mergeBlock;
                    }

                    var condition = context.Get(AggregateType.Bool, e.Block.Condition);

                    context.SelectionMerge(context.GetNextLabel(mergeBlock), SelectionControlMask.MaskNone);
                    context.BranchConditional(condition, context.GetNextLabel(ifTrueBlock), context.GetNextLabel(ifFalseBlock));
                }
                else if (e.Block.Type == AstBlockType.DoWhile)
                {
                    var continueTarget = context.Label();

                    loopTargets.Add(e.Block, (context.NewBlock(), continueTarget));

                    context.LoopMerge(context.GetNextLabel(mergeBlock), continueTarget, LoopControlMask.MaskNone);
                    context.Branch(context.GetFirstLabel(e.Block));
                }

                context.EnterBlock(e.Block);
            };

            visitor.BlockLeft += (sender, e) =>
            {
                if (e.Block.Parent != null)
                {
                    if (e.Block.Type == AstBlockType.DoWhile)
                    {
                        // This is a loop, we need to jump back to the loop header
                        // if the condition is true.
                        AstBlock mergeBlock = e.Block.Parent;

                        (var loopTarget, var continueTarget) = loopTargets[e.Block];

                        context.Branch(continueTarget);
                        context.AddLabel(continueTarget);

                        var condition = context.Get(AggregateType.Bool, e.Block.Condition);

                        context.BranchConditional(condition, loopTarget, context.GetNextLabel(mergeBlock));
                    }
                    else
                    {
                        // We only need a branch if the last instruction didn't
                        // already cause the program to exit or jump elsewhere.
                        bool lastIsCf = e.Block.Last is AstOperation lastOp &&
                            (lastOp.Inst == Instruction.Discard ||
                             lastOp.Inst == Instruction.LoopBreak ||
                             lastOp.Inst == Instruction.Return);

                        if (!lastIsCf)
                        {
                            context.Branch(context.GetNextLabel(e.Block.Parent));
                        }
                    }

                    bool hasElse = AstHelper.Next(e.Block) is AstBlock nextBlock &&
                        (nextBlock.Type == AstBlockType.Else ||
                         nextBlock.Type == AstBlockType.ElseIf);

                    // Re-enter the parent block.
                    if (e.Block.Parent != null && !hasElse)
                    {
                        context.EnterBlock(e.Block.Parent);
                    }
                }
            };

            foreach (IAstNode node in visitor.Visit())
            {
                if (node is AstAssignment assignment)
                {
                    var dest = (AstOperand)assignment.Destination;

                    if (dest.Type == OperandType.LocalVariable)
                    {
                        var source = context.Get(dest.VarType.Convert(), assignment.Source);
                        context.Store(context.GetLocalPointer(dest), source);
                    }
                    else if (dest.Type == OperandType.Attribute)
                    {
                        var elemPointer = context.GetAttributeElemPointer(dest, true, out var elemType);
                        context.Store(elemPointer, context.Get(elemType, assignment.Source));
                    }
                    else if (dest.Type == OperandType.Argument)
                    {
                        var source = context.Get(dest.VarType.Convert(), assignment.Source);
                        context.Store(context.GetArgumentPointer(dest), source);
                    }
                    else
                    {
                        throw new NotImplementedException(dest.Type.ToString());
                    }
                }
                else if (node is AstOperation operation)
                {
                    Instructions.Generate(context, operation);
                }
            }
        }
    }
}
