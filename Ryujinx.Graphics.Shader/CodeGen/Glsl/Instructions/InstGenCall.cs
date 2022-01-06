using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System.Diagnostics;

using static Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions.InstGenHelper;

namespace Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions
{
    static class InstGenCall
    {
        public static string Call(CodeGenContext context, AstOperation operation)
        {
            AstOperand funcId = (AstOperand)operation.GetSource(0);

            Debug.Assert(funcId.Type == OperandType.Constant);

            var function = context.GetFunction(funcId.Value);

            string[] args = new string[operation.SourcesCount - 1];

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = GetSoureExpr(context, operation.GetSource(i + 1), function.GetArgumentType(i));
            }

            return $"{function.Name}({string.Join(", ", args)})";
        }

        public static string Return(CodeGenContext context, AstOperation operation)
        {
            if (context.Config.Stage == ShaderStage.Vertex && context.CurrentFunction.Name == "fun0")
            {
                context.AppendLine($"if ({DefaultNames.SupportBlockViewportInverse}.w == 1.0)");
                context.EnterScope();
                context.AppendLine($"gl_Position.xy = (gl_Position.xy * {DefaultNames.SupportBlockViewportInverse}.xy) - vec2(1.0);");
                context.LeaveScope();
            }

            return "return";
        }
    }
}