using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System;

using static Ryujinx.Graphics.Shader.CodeGen.Glsl.TypeConversion;
using static Ryujinx.Graphics.Shader.StructuredIr.InstructionInfo;

namespace Ryujinx.Graphics.Shader.CodeGen.Glsl
{
    static class Instructions
    {
        public static string GetExpression(CodeGenContext context, IAstNode node)
        {
            if (node is AstOperation operation)
            {
                return GetExpression(context, operation);
            }
            else if (node is AstOperand operand)
            {
                switch (operand.Type)
                {
                    case OperandType.Attribute:
                        return OperandManager.GetAttributeName(context, operand);

                    case OperandType.Constant:
                        return NumberFormatter.FormatInt(operand.Value);

                    case OperandType.ConstantBuffer:
                        return OperandManager.GetConstantBufferName(context.ShaderType, operand);

                    case OperandType.LocalVariable:
                        return context.GetLocalName(operand);

                    case OperandType.Undefined:
                        return DefaultNames.UndefinedName;
                }

                return DefaultNames.UndefinedName;
            }

            throw new ArgumentException($"Invalid node type \"{node?.GetType().Name ?? "null"}\".");
        }

        private static string GetExpression(CodeGenContext context, AstOperation operation)
        {
            switch (operation.Inst & Instruction.Mask)
            {
                case Instruction.Absolute:
                    return GetUnaryCallExpr(context, operation, "abs");

                case Instruction.Add:
                    return GetBinaryExpr(context, operation, "+");

                case Instruction.BitfieldExtractS32:
                case Instruction.BitfieldExtractU32:
                    return GetTernaryCallExpr(context, operation, "bitfieldExtract");

                case Instruction.BitfieldInsert:
                    return GetQuaternaryCallExpr(context, operation, "bitfieldInsert");

                case Instruction.BitfieldReverse:
                    return GetUnaryCallExpr(context, operation, "bitfieldReverse");

                case Instruction.BitwiseAnd:
                    return GetBinaryExpr(context, operation, "&");

                case Instruction.BitwiseExclusiveOr:
                    return GetBinaryExpr(context, operation, "^");

                case Instruction.BitwiseNot:
                    return GetUnaryExpr(context, operation, "~");

                case Instruction.BitwiseOr:
                    return GetBinaryExpr(context, operation, "|");

                case Instruction.Ceiling:
                    return GetUnaryCallExpr(context, operation, "ceil");

                case Instruction.CompareEqual:
                    return GetBinaryExpr(context, operation, "==");

                case Instruction.CompareGreater:
                case Instruction.CompareGreaterU32:
                    return GetBinaryExpr(context, operation, ">");

                case Instruction.CompareGreaterOrEqual:
                case Instruction.CompareGreaterOrEqualU32:
                    return GetBinaryExpr(context, operation, ">=");

                case Instruction.CompareLess:
                case Instruction.CompareLessU32:
                    return GetBinaryExpr(context, operation, "<");

                case Instruction.CompareLessOrEqual:
                case Instruction.CompareLessOrEqualU32:
                    return GetBinaryExpr(context, operation, "<=");

                case Instruction.CompareNotEqual:
                    return GetBinaryExpr(context, operation, "!=");

                case Instruction.ConditionalSelect:
                    return GetConditionalSelectExpr(context, operation);

                case Instruction.Cosine:
                    return GetUnaryCallExpr(context, operation, "cos");

                case Instruction.Clamp:
                case Instruction.ClampU32:
                    return GetTernaryCallExpr(context, operation, "clamp");

                case Instruction.ConvertFPToS32:
                    return GetUnaryCallExpr(context, operation, "int");

                case Instruction.ConvertS32ToFP:
                case Instruction.ConvertU32ToFP:
                    return GetUnaryCallExpr(context, operation, "float");

                case Instruction.Discard:
                    return "discard";

                case Instruction.Divide:
                    return GetBinaryExpr(context, operation, "/");

                case Instruction.EmitVertex:
                    return "EmitVertex()";

                case Instruction.EndPrimitive:
                    return "EndPrimitive()";

                case Instruction.ExponentB2:
                    return GetUnaryCallExpr(context, operation, "exp2");

                case Instruction.Floor:
                    return GetUnaryCallExpr(context, operation, "floor");

                case Instruction.FusedMultiplyAdd:
                    return GetTernaryCallExpr(context, operation, "fma");

                case Instruction.IsNan:
                    return GetUnaryCallExpr(context, operation, "isnan");

                case Instruction.LoadConstant:
                    return GetLoadConstantExpr(context, operation);

                case Instruction.LogarithmB2:
                    return GetUnaryCallExpr(context, operation, "log2");

                case Instruction.LogicalAnd:
                    return GetBinaryExpr(context, operation, "&&");

                case Instruction.LogicalExclusiveOr:
                    return GetBinaryExpr(context, operation, "^^");

                case Instruction.LogicalNot:
                    return GetUnaryExpr(context, operation, "!");

                case Instruction.LogicalOr:
                    return GetBinaryExpr(context, operation, "||");

                case Instruction.LoopBreak:
                    return "break";

                case Instruction.LoopContinue:
                    return "continue";

                case Instruction.Maximum:
                case Instruction.MaximumU32:
                    return GetBinaryCallExpr(context, operation, "max");

                case Instruction.Minimum:
                case Instruction.MinimumU32:
                    return GetBinaryCallExpr(context, operation, "min");

                case Instruction.Multiply:
                    return GetBinaryExpr(context, operation, "*");

                case Instruction.Negate:
                    return GetUnaryExpr(context, operation, "-");

                case Instruction.ReciprocalSquareRoot:
                    return GetUnaryCallExpr(context, operation, "inversesqrt");

                case Instruction.Return:
                    return "return";

                case Instruction.ShiftLeft:
                    return GetBinaryExpr(context, operation, "<<");

                case Instruction.ShiftRightS32:
                case Instruction.ShiftRightU32:
                    return GetBinaryExpr(context, operation, ">>");

                case Instruction.Sine:
                    return GetUnaryCallExpr(context, operation, "sin");

                case Instruction.SquareRoot:
                    return GetUnaryCallExpr(context, operation, "sqrt");

                case Instruction.Subtract:
                    return GetBinaryExpr(context, operation, "-");

                case Instruction.TextureSample:
                    return GetTextureSampleExpr(context, operation);

                case Instruction.Truncate:
                    return GetUnaryCallExpr(context, operation, "trunc");
            }

            throw new ArgumentException($"Operation has invalid instruction \"{operation.Inst}\".");
        }

        private static string GetUnaryCallExpr(CodeGenContext context, AstOperation operation, string funcName)
        {
            return funcName + "(" + GetSoureExpr(context, operation.Sources[0], GetSrcVarType(operation.Inst, 0)) + ")";
        }

        private static string GetBinaryCallExpr(CodeGenContext context, AstOperation operation, string funcName)
        {
            return funcName + "(" +
                GetSoureExpr(context, operation.Sources[0], GetSrcVarType(operation.Inst, 0)) + ", " +
                GetSoureExpr(context, operation.Sources[1], GetSrcVarType(operation.Inst, 1)) + ")";
        }

        private static string GetTernaryCallExpr(CodeGenContext context, AstOperation operation, string funcName)
        {
            return funcName + "(" +
                GetSoureExpr(context, operation.Sources[0], GetSrcVarType(operation.Inst, 0)) + ", " +
                GetSoureExpr(context, operation.Sources[1], GetSrcVarType(operation.Inst, 1)) + ", " +
                GetSoureExpr(context, operation.Sources[2], GetSrcVarType(operation.Inst, 2)) + ")";
        }

        private static string GetQuaternaryCallExpr(CodeGenContext context, AstOperation operation, string funcName)
        {
            return funcName + "(" +
                GetSoureExpr(context, operation.Sources[0], GetSrcVarType(operation.Inst, 0)) + ", " +
                GetSoureExpr(context, operation.Sources[1], GetSrcVarType(operation.Inst, 1)) + ", " +
                GetSoureExpr(context, operation.Sources[2], GetSrcVarType(operation.Inst, 2)) + ", " +
                GetSoureExpr(context, operation.Sources[3], GetSrcVarType(operation.Inst, 3)) + ")";
        }

        private static string GetUnaryExpr(CodeGenContext context, AstOperation operation, string op)
        {
            return op + GetSoureExpr(context, operation.Sources[0], GetSrcVarType(operation.Inst, 0));
        }

        private static string GetBinaryExpr(CodeGenContext context, AstOperation operation, string op)
        {
            return GetSoureExpr(context, operation.Sources[0], GetSrcVarType(operation.Inst, 0)) + " " + op + " " +
                   GetSoureExpr(context, operation.Sources[1], GetSrcVarType(operation.Inst, 1));
        }

        private static string GetConditionalSelectExpr(CodeGenContext context, AstOperation operation)
        {
            return "((" +
                GetSoureExpr(context, operation.Sources[0], GetSrcVarType(operation.Inst, 0)) + ") ? (" +
                GetSoureExpr(context, operation.Sources[1], GetSrcVarType(operation.Inst, 1)) + ") : (" +
                GetSoureExpr(context, operation.Sources[2], GetSrcVarType(operation.Inst, 2)) + "))";
        }

        private static string GetLoadConstantExpr(CodeGenContext context, AstOperation operation)
        {
            string offsetExpr = GetSoureExpr(context, operation.Sources[1], GetSrcVarType(operation.Inst, 1));

            return OperandManager.GetConstantBufferName(context, operation.Sources[0], offsetExpr);
        }

        private static string GetTextureSampleExpr(CodeGenContext context, AstOperation operation)
        {
            AstTextureOperation texOp = (AstTextureOperation)operation;

            bool isGather = (texOp.Flags & TextureFlags.Gather) != 0;
            bool isShadow = (texOp.Type  & TextureType.Shadow)  != 0;

            string samplerName = OperandManager.GetSamplerName(context.ShaderType, texOp.TextureHandle);

            string texCall = "texture";

            if (isGather)
            {
                texCall += "Gather";
            }

            if ((texOp.Flags & TextureFlags.LodLevel) != 0)
            {
                texCall += "Lod";
            }

            if ((texOp.Flags & TextureFlags.Offset) != 0)
            {
                texCall += "Offset";
            }
            else if ((texOp.Flags & TextureFlags.Offsets) != 0)
            {
                texCall += "Offsets";
            }

            texCall += "(" + samplerName;

            TextureType baseType = texOp.Type & TextureType.Mask;

            int elemsCount;

            switch (baseType)
            {
                case TextureType.Texture1D:   elemsCount = 1; break;
                case TextureType.Texture2D:   elemsCount = 2; break;
                case TextureType.Texture3D:   elemsCount = 3; break;
                case TextureType.TextureCube: elemsCount = 3; break;

                default: throw new InvalidOperationException($"Invalid texture type \"{baseType}\".");
            }

            int pCount = elemsCount;

            int arrayIndexElem = -1;

            if ((texOp.Type & TextureType.Array) != 0)
            {
                arrayIndexElem = pCount++;
            }

            if (isShadow && !isGather)
            {
                pCount++;
            }

            //On textureGather*, the comparison value is always specified as an extra argument.
            bool hasExtraCompareArg = isShadow && isGather;

            if (pCount == 5)
            {
                pCount = 4;

                hasExtraCompareArg = true;
            }

            int srcIndex = 0;

            string Src(VariableType type)
            {
                return GetSoureExpr(context, texOp.Sources[srcIndex++], type);
            }

            string AssembleVector(int count, VariableType type, bool isP = false)
            {
                if (count > 1)
                {
                    string[] vecElems = new string[count];

                    for (int index = 0; index < count; index++)
                    {
                        if (isP && index == arrayIndexElem)
                        {
                            vecElems[index] = "float(" + Src(VariableType.S32) + ")";
                        }
                        else
                        {
                            vecElems[index] = Src(type);
                        }
                    }

                    string prefix = type == VariableType.F32 ? string.Empty : "i";

                    return prefix + "vec" + count + "(" + string.Join(", ", vecElems) + ")";
                }
                else
                {
                    return Src(type);
                }
            }

            texCall += ", " + AssembleVector(pCount, VariableType.F32, isP: true);

            if (hasExtraCompareArg)
            {
                texCall += ", " + Src(VariableType.F32);
            }

            if ((texOp.Flags & TextureFlags.LodLevel) != 0)
            {
                texCall += ", " + Src(VariableType.F32);
            }

            if ((texOp.Flags & TextureFlags.Offset) != 0)
            {
                texCall += ", " + AssembleVector(elemsCount, VariableType.S32);
            }
            else if ((texOp.Flags & TextureFlags.Offsets) != 0)
            {
                const int gatherTexelsCount = 4;

                texCall += $", ivec{elemsCount}[{gatherTexelsCount}](";

                for (int index = 0; index < gatherTexelsCount; index++)
                {
                    texCall += AssembleVector(elemsCount, VariableType.S32);

                    if (index < gatherTexelsCount - 1)
                    {
                        texCall += ", ";
                    }
                }

                texCall += ")";
            }

            if ((texOp.Flags & TextureFlags.LodBias) != 0)
            {
                texCall += ", " + Src(VariableType.F32);
            }

            //textureGather* optional extra component index, not needed for shadow samplers.
            if (isGather && !isShadow)
            {
                texCall += ", " + Src(VariableType.S32);
            }

            texCall += ")";

            if (isGather || !isShadow)
            {
                texCall += ".";

                for (int compIndex = 0; compIndex < texOp.Components.Length; compIndex++)
                {
                    texCall += "rgba".Substring(texOp.Components[compIndex], 1);
                }
            }

            return texCall;
        }

        private static string GetSoureExpr(CodeGenContext context, IAstNode node, VariableType dstType)
        {
            return ReinterpretCast(context, node, OperandManager.GetNodeDestType(node), dstType);
        }
    }
}