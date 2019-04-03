using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Shader.CodeGen.Glsl
{
    static class Declarations
    {
        public static void Declare(CodeGenContext context, StructuredProgramInfo prgInfo)
        {
            context.AppendLine("#version 410 core");

            context.AppendLine();

            context.AppendLine($"const int {DefaultNames.UndefinedName} = 0;");

            context.AppendLine();

            if (context.ShaderType == GalShaderType.Geometry)
            {
                context.AppendLine("layout (points) in;");
                context.AppendLine("layout (triangle_strip, max_vertices = 4) out;");

                context.AppendLine();
            }

            context.AppendLine("layout (std140) uniform Extra");

            context.EnterScope();

            context.AppendLine("vec2 flip;");
            context.AppendLine("int instance;");

            context.LeaveScope(";");

            context.AppendLine();

            if (prgInfo.ConstantBuffers.Count != 0)
            {
                DeclareUniforms(context, prgInfo);

                context.AppendLine();
            }

            if (prgInfo.Samplers.Count != 0)
            {
                DeclareSamplers(context, prgInfo);

                context.AppendLine();
            }

            if (prgInfo.IAttributes.Count != 0)
            {
                DeclareInputAttributes(context, prgInfo);

                context.AppendLine();
            }

            if (prgInfo.OAttributes.Count != 0)
            {
                DeclareOutputAttributes(context, prgInfo);

                context.AppendLine();
            }
        }

        private static void DeclareUniforms(CodeGenContext context, StructuredProgramInfo prgInfo)
        {
            foreach (int cbufSlot in prgInfo.ConstantBuffers.OrderBy(x => x))
            {
                string ubName = OperandManager.GetShaderStagePrefix(context.ShaderType);

                ubName += "_" + DefaultNames.UniformNamePrefix + cbufSlot;

                context.AppendLine("layout (std140) uniform " + ubName);

                context.EnterScope();

                context.AppendLine("vec4 " + OperandManager.GetUbName(context.ShaderType, cbufSlot) + "[4096];");

                context.LeaveScope(";");
            }
        }

        private static void DeclareSamplers(CodeGenContext context, StructuredProgramInfo prgInfo)
        {
            foreach (KeyValuePair<int, TextureType> kv in prgInfo.Samplers.OrderBy(x => x.Key))
            {
                int textureHandle = kv.Key;

                string samplerTypeName = GetSamplerTypeName(kv.Value);

                string samplerName = OperandManager.GetSamplerName(context.ShaderType, textureHandle);

                context.AppendLine("uniform " + samplerTypeName + " " + samplerName + ";");
            }
        }

        private static void DeclareInputAttributes(CodeGenContext context, StructuredProgramInfo prgInfo)
        {
            string suffix = context.ShaderType == GalShaderType.Geometry ? "[]" : string.Empty;

            foreach (int attr in prgInfo.IAttributes.OrderBy(x => x))
            {
                context.AppendLine($"layout (location = {attr}) in vec4 {DefaultNames.IAttributePrefix}{attr}{suffix};");
            }
        }

        private static void DeclareOutputAttributes(CodeGenContext context, StructuredProgramInfo prgInfo)
        {
            foreach (int attr in prgInfo.OAttributes.OrderBy(x => x))
            {
                context.AppendLine($"layout (location = {attr}) out vec4 {DefaultNames.OAttributePrefix}{attr};");
            }
        }

        private static string GetSamplerTypeName(TextureType type)
        {
            string typeName;

            switch (type & TextureType.Mask)
            {
                case TextureType.Texture1D:   typeName = "sampler1D";   break;
                case TextureType.Texture2D:   typeName = "sampler2D";   break;
                case TextureType.Texture3D:   typeName = "sampler3D";   break;
                case TextureType.TextureCube: typeName = "samplerCube"; break;

                default: throw new ArgumentException($"Invalid sampler type \"{type}\".");
            }

            if ((type & TextureType.Array) != 0)
            {
                typeName += "Array";
            }

            if ((type & TextureType.Shadow) != 0)
            {
                typeName += "Shadow";
            }

            return typeName;
        }
    }
}