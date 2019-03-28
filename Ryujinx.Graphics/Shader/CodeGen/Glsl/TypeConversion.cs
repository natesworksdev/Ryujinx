using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System;

namespace Ryujinx.Graphics.Shader.CodeGen.Glsl
{
    static class TypeConversion
    {
        public static string ReinterpretCast(string expr, VariableType srcType, VariableType dstType)
        {
            if (srcType == dstType)
            {
                return expr;
            }

            if (srcType == VariableType.F32)
            {
                switch (dstType)
                {
                    case VariableType.S32: return $"floatBitsToInt({expr})";
                    case VariableType.U32: return $"floatBitsToUint({expr})";
                }
            }
            else if (dstType == VariableType.F32)
            {
                switch (srcType)
                {
                    case VariableType.S32: return $"intBitsToFloat({expr})";
                    case VariableType.U32: return $"uintBitsToFloat({expr})";
                }
            }
            else if (srcType == VariableType.Bool)
            {
                return $"(({expr}) ? {IrConsts.True} : {IrConsts.False})";
            }
            else if (dstType == VariableType.Bool)
            {
                return $"(({expr}) != 0)";
            }
            else if (dstType == VariableType.S32)
            {
                return $"int({expr})";
            }
            else if (dstType == VariableType.U32)
            {
                return $"uint({expr})";
            }

            throw new ArgumentException($"Invalid reinterpret cast from \"{srcType}\" to \"{dstType}\".");
        }
    }
}