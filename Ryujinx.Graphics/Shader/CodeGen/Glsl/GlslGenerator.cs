using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System;

using static Ryujinx.Graphics.Shader.CodeGen.Glsl.TypeConversion;

namespace Ryujinx.Graphics.Shader.CodeGen.Glsl
{
    class GlslGenerator
    {
        public string Generate(StructuredProgramInfo prgInfo, GalShaderType shaderType)
        {
            CodeGenContext cgContext = new CodeGenContext(prgInfo, shaderType);

            Declarations.Declare(cgContext, prgInfo);

            PrintBlock(cgContext, prgInfo.MainBlock);

            return cgContext.GetCode();
        }

        private void PrintBlock(CodeGenContext context, AstBlock block)
        {
            switch (block.Type)
            {
                case AstBlockType.DoWhile:
                    context.AppendLine("do");
                    break;

                case AstBlockType.Else:
                    context.AppendLine("else");
                    break;

                case AstBlockType.If:
                    context.AppendLine($"if ({GetCondExpr(context, block.Condition)})");
                    break;

                case AstBlockType.Main:
                    context.AppendLine("void main()");
                    break;
            }

            context.EnterScope();

            foreach (IAstNode node in block.Nodes)
            {
                if (node is AstBlock subBlock)
                {
                    PrintBlock(context, subBlock);
                }
                else if (node is AstOperation operation)
                {
                    if (operation.Inst == Instruction.Return)
                    {
                        PrepareForReturn(context);
                    }

                    context.AppendLine(Instructions.GetExpression(context, operation) + ";");
                }
                else if (node is AstAssignment asg)
                {
                    VariableType srcType = OperandManager.GetNodeDestType(asg.Source);
                    VariableType dstType = OperandManager.GetNodeDestType(asg.Destination);

                    string dest;

                    if (asg.Destination is AstOperand operand && operand.Type == OperandType.Attribute)
                    {
                        dest = OperandManager.GetOutAttributeName(context, operand);
                    }
                    else
                    {
                        dest = Instructions.GetExpression(context, asg.Destination);
                    }

                    string src = ReinterpretCast(Instructions.GetExpression(context, asg.Source), srcType, dstType);

                    context.AppendLine(dest + " = " + src + ";");
                }
                else if (node is AstDeclaration decl && decl.Operand.Type != OperandType.Undefined)
                {
                    string name = context.DeclareLocal(decl.Operand);

                    context.AppendLine(GetVarTypeName(decl.Operand.VarType) + " " + name + ";");
                }
            }

            context.LeaveScope();

            if (block.Type == AstBlockType.DoWhile)
            {
                context.AppendLine($"while ({GetCondExpr(context, block.Condition)});");
            }
        }

        private static string GetCondExpr(CodeGenContext context, IAstNode cond)
        {
            VariableType srcType = OperandManager.GetNodeDestType(cond);

            return ReinterpretCast(Instructions.GetExpression(context, cond), srcType, VariableType.Bool);
        }

        private string GetVarTypeName(VariableType type)
        {
            switch (type)
            {
                case VariableType.Bool: return "bool";
                case VariableType.F32:  return "float";
                case VariableType.S32:  return "int";
                case VariableType.U32:  return "uint";
            }

            throw new ArgumentException($"Invalid variable type \"{type}\".");
        }

        private static void PrepareForReturn(CodeGenContext context)
        {
            if (context.ShaderType == GalShaderType.Vertex)
            {
                context.AppendLine("gl_Position.xy *= flip;");
            }
        }
    }
}