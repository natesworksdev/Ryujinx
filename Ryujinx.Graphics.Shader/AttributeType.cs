using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation;
using System;

namespace Ryujinx.Graphics.Shader
{
    public enum AttributeType : byte
    {
        // Generic types.
        Float,
        Sint,
        Uint,

        // Specific formats for conversion.
        Rgb16f,
        Rgb16Si,
        Rgb16Ui
    }

    static class AttributeTypeExtensions
    {
        public static bool HasFormatForConversion(this AttributeType type)
        {
            return type >= AttributeType.Rgb16f;
        }

        public static string ToVec4Type(this AttributeType type)
        {
            return type switch
            {
                AttributeType.Float => "vec4",
                AttributeType.Sint => "ivec4",
                AttributeType.Uint => "uvec4",
                AttributeType.Rgb16f or
                AttributeType.Rgb16Si or
                AttributeType.Rgb16Ui => "vec4",
                _ => throw new ArgumentException($"Invalid attribute type \"{type}\".")
            };
        }

        public static VariableType ToVariableType(this AttributeType type)
        {
            return type switch
            {
                AttributeType.Float => VariableType.F32,
                AttributeType.Sint => VariableType.S32,
                AttributeType.Uint => VariableType.U32,
                AttributeType.Rgb16f or
                AttributeType.Rgb16Si or
                AttributeType.Rgb16Ui => VariableType.F32,
                _ => throw new ArgumentException($"Invalid attribute type \"{type}\".")
            };
        }

        public static AggregateType ToAggregateType(this AttributeType type)
        {
            return type switch
            {
                AttributeType.Float => AggregateType.FP32,
                AttributeType.Sint => AggregateType.S32,
                AttributeType.Uint => AggregateType.U32,
                AttributeType.Rgb16f or
                AttributeType.Rgb16Si or
                AttributeType.Rgb16Ui => AggregateType.FP32,
                _ => throw new ArgumentException($"Invalid attribute type \"{type}\".")
            };
        }
    }
}