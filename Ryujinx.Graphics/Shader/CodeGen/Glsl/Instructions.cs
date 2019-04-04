using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System;

using static Ryujinx.Graphics.Shader.CodeGen.Glsl.TypeConversion;
using static Ryujinx.Graphics.Shader.StructuredIr.InstructionInfo;

namespace Ryujinx.Graphics.Shader.CodeGen.Glsl
{
    static class Instructions
    {
        [Flags]
        private enum InstFlags
        {
            OpNullary = 0,
            OpUnary   = 1,
            OpBinary  = 2,
            OpTernary = 3,

            CallNullary    = Call | 0,
            CallUnary      = Call | 1,
            CallBinary     = Call | 2,
            CallTernary    = Call | 3,
            CallQuaternary = Call | 4,

            Call = 1 << 8,

            ArityMask = 0xff
        }

        private struct InstInfo
        {
            public InstFlags Flags { get; }

            public string OpName { get; }

            public int Precedence { get; }

            public InstInfo(InstFlags flags, string opName, int precedence)
            {
                Flags      = flags;
                OpName     = opName;
                Precedence = precedence;
            }
        }

        private static InstInfo[] _infoTbl;

        static Instructions()
        {
            _infoTbl = new InstInfo[(int)Instruction.Count];

            Add(Instruction.Absolute,                 InstFlags.CallUnary,      "abs");
            Add(Instruction.Add,                      InstFlags.OpBinary,       "+",               2);
            Add(Instruction.BitfieldExtractS32,       InstFlags.CallTernary,    "bitfieldExtract");
            Add(Instruction.BitfieldExtractU32,       InstFlags.CallTernary,    "bitfieldExtract");
            Add(Instruction.BitfieldInsert,           InstFlags.CallQuaternary, "bitfieldInsert");
            Add(Instruction.BitfieldReverse,          InstFlags.CallUnary,      "bitfieldReverse");
            Add(Instruction.BitwiseAnd,               InstFlags.OpBinary,       "&",               6);
            Add(Instruction.BitwiseExclusiveOr,       InstFlags.OpBinary,       "^",               7);
            Add(Instruction.BitwiseNot,               InstFlags.OpUnary,        "~",               0);
            Add(Instruction.BitwiseOr,                InstFlags.OpBinary,       "|",               8);
            Add(Instruction.Ceiling,                  InstFlags.CallUnary,      "ceil");
            Add(Instruction.Clamp,                    InstFlags.CallTernary,    "clamp");
            Add(Instruction.ClampU32,                 InstFlags.CallTernary,    "clamp");
            Add(Instruction.CompareEqual,             InstFlags.OpBinary,       "==",              5);
            Add(Instruction.CompareGreater,           InstFlags.OpBinary,       ">",               4);
            Add(Instruction.CompareGreaterOrEqual,    InstFlags.OpBinary,       ">=",              4);
            Add(Instruction.CompareGreaterOrEqualU32, InstFlags.OpBinary,       ">=",              4);
            Add(Instruction.CompareGreaterU32,        InstFlags.OpBinary,       ">",               4);
            Add(Instruction.CompareLess,              InstFlags.OpBinary,       "<",               4);
            Add(Instruction.CompareLessOrEqual,       InstFlags.OpBinary,       "<=",              4);
            Add(Instruction.CompareLessOrEqualU32,    InstFlags.OpBinary,       "<=",              4);
            Add(Instruction.CompareLessU32,           InstFlags.OpBinary,       "<",               4);
            Add(Instruction.CompareNotEqual,          InstFlags.OpBinary,       "!=",              5);
            Add(Instruction.ConditionalSelect,        InstFlags.OpTernary,      "?:",              12);
            Add(Instruction.ConvertFPToS32,           InstFlags.CallUnary,      "int");
            Add(Instruction.ConvertS32ToFP,           InstFlags.CallUnary,      "float");
            Add(Instruction.ConvertU32ToFP,           InstFlags.CallUnary,      "float");
            Add(Instruction.Cosine,                   InstFlags.CallUnary,      "cos");
            Add(Instruction.Discard,                  InstFlags.OpNullary,      "discard");
            Add(Instruction.Divide,                   InstFlags.OpBinary,       "/",               1);
            Add(Instruction.EmitVertex,               InstFlags.CallNullary,    "EmitVertex");
            Add(Instruction.EndPrimitive,             InstFlags.CallNullary,    "EndPrimitive");
            Add(Instruction.ExponentB2,               InstFlags.CallUnary,      "exp2");
            Add(Instruction.Floor,                    InstFlags.CallUnary,      "floor");
            Add(Instruction.FusedMultiplyAdd,         InstFlags.CallTernary,    "fma");
            Add(Instruction.IsNan,                    InstFlags.CallUnary,      "isnan");
            Add(Instruction.LoadConstant,             InstFlags.Call);
            Add(Instruction.LogarithmB2,              InstFlags.CallUnary,      "log2");
            Add(Instruction.LogicalAnd,               InstFlags.OpBinary,       "&&",              9);
            Add(Instruction.LogicalExclusiveOr,       InstFlags.OpBinary,       "^^",              10);
            Add(Instruction.LogicalNot,               InstFlags.OpUnary,        "!",               0);
            Add(Instruction.LogicalOr,                InstFlags.OpBinary,       "||",              11);
            Add(Instruction.ShiftLeft,                InstFlags.OpBinary,       "<<",              3);
            Add(Instruction.ShiftRightS32,            InstFlags.OpBinary,       ">>",              3);
            Add(Instruction.ShiftRightU32,            InstFlags.OpBinary,       ">>",              3);
            Add(Instruction.Maximum,                  InstFlags.CallBinary,     "max");
            Add(Instruction.MaximumU32,               InstFlags.CallBinary,     "max");
            Add(Instruction.Minimum,                  InstFlags.CallBinary,     "min");
            Add(Instruction.MinimumU32,               InstFlags.CallBinary,     "min");
            Add(Instruction.Multiply,                 InstFlags.OpBinary,       "*",               1);
            Add(Instruction.Negate,                   InstFlags.OpUnary,        "-",               0);
            Add(Instruction.ReciprocalSquareRoot,     InstFlags.CallUnary,      "inversesqrt");
            Add(Instruction.Return,                   InstFlags.OpNullary,      "return");
            Add(Instruction.Sine,                     InstFlags.CallUnary,      "sin");
            Add(Instruction.SquareRoot,               InstFlags.CallUnary,      "sqrt");
            Add(Instruction.Subtract,                 InstFlags.OpBinary,       "-",               2);
            Add(Instruction.TextureSample,            InstFlags.Call);
            Add(Instruction.Truncate,                 InstFlags.CallUnary,      "trunc");
        }

        private static void Add(Instruction inst, InstFlags flags, string opName = null, int precedence = 0)
        {
            _infoTbl[(int)inst] = new InstInfo(flags, opName, precedence);
        }

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
            Instruction inst = operation.Inst & Instruction.Mask;

            switch (inst)
            {
                case Instruction.LoadConstant:
                    return GetLoadConstantExpr(context, operation);

                case Instruction.TextureSample:
                    return GetTextureSampleExpr(context, operation);
            }

            InstInfo info = _infoTbl[(int)inst];

            if ((info.Flags & InstFlags.Call) != 0)
            {
                int arity = (int)(info.Flags & InstFlags.ArityMask);

                string args = string.Empty;

                for (int argIndex = 0; argIndex < arity; argIndex++)
                {
                    if (argIndex != 0)
                    {
                        args += ", ";
                    }

                    VariableType dstType = GetSrcVarType(operation.Inst, argIndex);

                    args += GetSoureExpr(context, operation.GetSource(argIndex), dstType);
                }

                return info.OpName + "(" + args + ")";
            }
            else
            {
                if (info.Flags == InstFlags.OpNullary)
                {
                    return info.OpName;
                }
                else if (info.Flags == InstFlags.OpUnary)
                {
                    IAstNode src = operation.GetSource(0);

                    string expr = GetSoureExpr(context, src, GetSrcVarType(operation.Inst, 0));

                    return info.OpName + Enclose(expr, src, info);
                }
                else if (info.Flags == InstFlags.OpBinary)
                {
                    IAstNode src0 = operation.GetSource(0);
                    IAstNode src1 = operation.GetSource(1);

                    string expr0 = GetSoureExpr(context, src0, GetSrcVarType(operation.Inst, 0));
                    string expr1 = GetSoureExpr(context, src1, GetSrcVarType(operation.Inst, 1));

                    expr0 = Enclose(expr0, src0, info, isLhs: true);
                    expr1 = Enclose(expr1, src1, info, isLhs: false);

                    return expr0 + " " + info.OpName + " " + expr1;
                }
                else if (info.Flags == InstFlags.OpTernary)
                {
                    IAstNode src0 = operation.GetSource(0);
                    IAstNode src1 = operation.GetSource(1);
                    IAstNode src2 = operation.GetSource(2);

                    string expr0 = GetSoureExpr(context, src0, GetSrcVarType(operation.Inst, 0));
                    string expr1 = GetSoureExpr(context, src1, GetSrcVarType(operation.Inst, 1));
                    string expr2 = GetSoureExpr(context, src2, GetSrcVarType(operation.Inst, 2));

                    expr0 = Enclose(expr0, src0, info);
                    expr1 = Enclose(expr1, src1, info);
                    expr2 = Enclose(expr2, src2, info);

                    char op0 = info.OpName[0];
                    char op1 = info.OpName[1];

                    return expr0 + " " + op0 + " " + expr1 + " " + op1 + " " + expr2;
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected instruction flags \"{info.Flags}\".");
                }
            }
        }

        private static string GetLoadConstantExpr(CodeGenContext context, AstOperation operation)
        {
            IAstNode src1 = operation.GetSource(1);

            string offsetExpr = GetSoureExpr(context, src1, GetSrcVarType(operation.Inst, 1));

            offsetExpr = Enclose(offsetExpr, src1, Instruction.ShiftRightS32, isLhs: true);

            return OperandManager.GetConstantBufferName(context, operation.GetSource(0), offsetExpr);
        }

        private static string GetTextureSampleExpr(CodeGenContext context, AstOperation operation)
        {
            AstTextureOperation texOp = (AstTextureOperation)operation;

            bool isGather    = (texOp.Flags & TextureFlags.Gather)   != 0;
            bool hasLodBias  = (texOp.Flags & TextureFlags.LodBias)  != 0;
            bool hasLodLevel = (texOp.Flags & TextureFlags.LodLevel) != 0;
            bool hasOffset   = (texOp.Flags & TextureFlags.Offset)   != 0;
            bool hasOffsets  = (texOp.Flags & TextureFlags.Offsets)  != 0;
            bool isArray     = (texOp.Type  & TextureType.Array)     != 0;
            bool isShadow    = (texOp.Type  & TextureType.Shadow)    != 0;

            string samplerName = OperandManager.GetSamplerName(context.ShaderType, texOp.TextureHandle);

            string texCall = "texture";

            if (isGather)
            {
                texCall += "Gather";
            }

            if (hasLodLevel)
            {
                texCall += "Lod";
            }

            if (hasOffset)
            {
                texCall += "Offset";
            }
            else if (hasOffsets)
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

            if (isArray)
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
                return GetSoureExpr(context, texOp.GetSource(srcIndex++), type);
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

            if (hasLodLevel)
            {
                texCall += ", " + Src(VariableType.F32);
            }

            if (hasOffset)
            {
                texCall += ", " + AssembleVector(elemsCount, VariableType.S32);
            }
            else if (hasOffsets)
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

            if (hasLodBias && !hasLodLevel)
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

        public static string Enclose(string expr, IAstNode node, Instruction inst, bool isLhs)
        {
            InstInfo info = _infoTbl[(int)(inst & Instruction.Mask)];

            return Enclose(expr, node, info, isLhs);
        }

        private static string Enclose(string expr, IAstNode node, InstInfo pInfo, bool isLhs = false)
        {
            if (NeedsParenthesis(node, pInfo, isLhs))
            {
                expr = "(" + expr + ")";
            }

            return expr;
        }

        private static bool NeedsParenthesis(IAstNode node, InstInfo pInfo, bool isLhs)
        {
            //If the node isn't a operation, then it can only be a operand,
            //and those never needs to be surrounded in parenthesis.
            if (!(node is AstOperation operation))
            {
                return false;
            }

            if ((pInfo.Flags & InstFlags.Call) != 0)
            {
                return false;
            }

            InstInfo info = _infoTbl[(int)(operation.Inst & Instruction.Mask)];

            if ((info.Flags & InstFlags.Call) != 0)
            {
                return false;
            }

            if (info.Precedence < pInfo.Precedence)
            {
                return false;
            }

            if (info.Precedence == pInfo.Precedence && isLhs)
            {
                return false;
            }

            return true;
        }
    }
}