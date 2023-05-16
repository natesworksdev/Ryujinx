using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ryujinx.Horizon.Generators.Kernel
{
    [Generator]
    class SyscallGenerator : ISourceGenerator
    {
        private const string ClassNamespace = "Ryujinx.HLE.HOS.Kernel.SupervisorCall";
        private const string ClassName = "SyscallDispatch";
        private const string A32Suffix = "32";
        private const string A64Suffix = "64";
        private const string ResultVariableName = "result";
        private const string ArgVariablePrefix = "arg";
        private const string ResultCheckHelperName = "LogResultAsTrace";

        private const string TypeSystemBoolean = "System.Boolean";
        private const string TypeSystemInt32 = "System.Int32";
        private const string TypeSystemInt64 = "System.Int64";
        private const string TypeSystemUInt32 = "System.UInt32";
        private const string TypeSystemUInt64 = "System.UInt64";

        private const string NamespaceKernel = "Ryujinx.HLE.HOS.Kernel";
        private const string NamespaceHorizonCommon = "Ryujinx.Horizon.Common";
        private const string TypeSvcAttribute = NamespaceKernel + ".SupervisorCall.SvcAttribute";
        private const string TypePointerSizedAttribute = NamespaceKernel + ".SupervisorCall.PointerSizedAttribute";
        private const string TypeResultName = "Result";
        private const string TypeKernelResultName = "KernelResult";
        private const string TypeResult = NamespaceHorizonCommon + "." + TypeResultName;
        private const string TypeExecutionContext = "IExecutionContext";
        private const string TypeTask = "System.Threading.Tasks.Task";

        private static readonly string[] _expectedResults = new string[]
        {
            $"{TypeResultName}.Success",
            $"{TypeKernelResultName}.TimedOut",
            $"{TypeKernelResultName}.Cancelled",
            $"{TypeKernelResultName}.PortRemoteClosed",
            $"{TypeKernelResultName}.InvalidState",
        };

        private readonly struct OutParameter
        {
            public readonly string Identifier;
            public readonly bool NeedsSplit;

            public OutParameter(string identifier, bool needsSplit = false)
            {
                Identifier = identifier;
                NeedsSplit = needsSplit;
            }
        }

        private struct RegisterAllocatorA32
        {
            private uint _useSet;
            private int _linearIndex;

            public int AllocateSingle()
            {
                return Allocate();
            }

            public (int, int) AllocatePair()
            {
                _linearIndex += _linearIndex & 1;

                return (Allocate(), Allocate());
            }

            private int Allocate()
            {
                int regIndex;

                if (_linearIndex < 4)
                {
                    regIndex = _linearIndex++;
                }
                else
                {
                    regIndex = -1;

                    for (int i = 0; i < 32; i++)
                    {
                        if ((_useSet & (1 << i)) == 0)
                        {
                            regIndex = i;
                            break;
                        }
                    }

                    Debug.Assert(regIndex != -1);
                }

                _useSet |= 1u << regIndex;

                return regIndex;
            }

            public void AdvanceLinearIndex()
            {
                _linearIndex++;
            }
        }

        private readonly struct SyscallIdAndName : IComparable<SyscallIdAndName>
        {
            public readonly int Id;
            public readonly string Name;
            public readonly bool IsAsync;

            public SyscallIdAndName(int id, string name, bool isAsync)
            {
                Id = id;
                Name = name;
                IsAsync = isAsync;
            }

            public int CompareTo(SyscallIdAndName other)
            {
                return Id.CompareTo(other.Id);
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            SyscallSyntaxReceiver syntaxReceiver = (SyscallSyntaxReceiver)context.SyntaxReceiver;

            CodeGenerator generator = new CodeGenerator();

            generator.AppendLine("using Ryujinx.Common.Logging;");
            generator.AppendLine("using Ryujinx.Cpu;");
            generator.AppendLine($"using {NamespaceKernel}.Common;");
            generator.AppendLine($"using {NamespaceKernel}.Memory;");
            generator.AppendLine($"using {NamespaceKernel}.Process;");
            generator.AppendLine($"using {NamespaceKernel}.Threading;");
            generator.AppendLine($"using {NamespaceHorizonCommon};");
            generator.AppendLine("using System;");
            generator.AppendLine("using System.Threading.Tasks;");
            generator.AppendLine();
            generator.EnterScope($"namespace {ClassNamespace}");
            generator.EnterScope($"static class {ClassName}");

            GenerateResultCheckHelper(generator);
            generator.AppendLine();

            List<SyscallIdAndName> syscalls = new List<SyscallIdAndName>();

            foreach (var method in syntaxReceiver.SvcImplementations)
            {
                GenerateMethod32(generator, context.Compilation, method);
                GenerateMethod64(generator, context.Compilation, method);

                foreach (AttributeSyntax attribute in method.AttributeLists.SelectMany(attributeList =>
                             attributeList.Attributes.Where(attribute =>
                                 GetCanonicalTypeName(context.Compilation, attribute) == TypeSvcAttribute)))
                {
                    syscalls.AddRange(from attributeArg in attribute.ArgumentList.Arguments
                        where attributeArg.Expression.Kind() == SyntaxKind.NumericLiteralExpression
                        select (LiteralExpressionSyntax)attributeArg.Expression
                        into numericLiteral
                        select new SyscallIdAndName((int)numericLiteral.Token.Value, method.Identifier.Text, IsAsyncMethod(method)));
                }
            }

            syscalls.Sort();

            GenerateDispatch(generator, syscalls, A32Suffix);
            generator.AppendLine();
            GenerateDispatch(generator, syscalls, A64Suffix);

            generator.LeaveScope();
            generator.LeaveScope();

            context.AddSource($"{ClassName}.g.cs", generator.ToString());
        }

        private static void GenerateResultCheckHelper(CodeGenerator generator)
        {
            generator.EnterScope($"private static bool {ResultCheckHelperName}({TypeResultName} {ResultVariableName})");

            string[] expectedChecks = new string[_expectedResults.Length];

            for (int i = 0; i < expectedChecks.Length; i++)
            {
                expectedChecks[i] = $"{ResultVariableName} == {_expectedResults[i]}";
            }

            string checks = string.Join(" || ", expectedChecks);

            generator.AppendLine($"return {checks};");
            generator.LeaveScope();
        }

        private static void GenerateMethod32(CodeGenerator generator, Compilation compilation, MethodDeclarationSyntax method)
        {
            var retType = IsAsyncMethod(method) ? "async Task" : "void";
            generator.EnterScope($"private static {retType} {method.Identifier.Text}{A32Suffix}(Syscall syscall, {TypeExecutionContext} context)");

            string[] args = new string[method.ParameterList.Parameters.Count];
            int index = 0;

            RegisterAllocatorA32 regAlloc = new RegisterAllocatorA32();

            List<OutParameter> outParameters = new List<OutParameter>();
            List<string> logInArgs = new List<string>();
            List<string> logOutArgs = new List<string>();

            foreach (var methodParameter in method.ParameterList.Parameters)
            {
                string name = methodParameter.Identifier.Text;
                string argName = GetPrefixedArgName(name);
                string typeName = methodParameter.Type.ToString();
                string canonicalTypeName = GetCanonicalTypeName(compilation, methodParameter.Type);

                if (methodParameter.Modifiers.Any(SyntaxKind.OutKeyword))
                {
                    bool needsSplit = Is64BitInteger(canonicalTypeName) && !IsPointerSized(compilation, methodParameter);
                    outParameters.Add(new OutParameter(argName, needsSplit));
                    logOutArgs.Add($"{name}: {GetFormattedLogValue(argName, canonicalTypeName)}");

                    argName = $"out {typeName} {argName}";

                    regAlloc.AdvanceLinearIndex();
                }
                else
                {
                    if (Is64BitInteger(canonicalTypeName))
                    {
                        if (IsPointerSized(compilation, methodParameter))
                        {
                            int registerIndex = regAlloc.AllocateSingle();

                            generator.AppendLine($"var {argName} = (uint)context.GetX({registerIndex});");
                        }
                        else
                        {
                            (int registerIndex, int registerIndex2) = regAlloc.AllocatePair();

                            string valueLow = $"(ulong)(uint)context.GetX({registerIndex})";
                            string valueHigh = $"(ulong)(uint)context.GetX({registerIndex2})";
                            string value = $"{valueLow} | ({valueHigh} << 32)";

                            generator.AppendLine($"var {argName} = ({typeName})({value});");
                        }
                    }
                    else
                    {
                        int registerIndex = regAlloc.AllocateSingle();

                        string value = GenerateCastFromUInt64($"context.GetX({registerIndex})", canonicalTypeName, typeName);

                        generator.AppendLine($"var {argName} = {value};");
                    }

                    logInArgs.Add($"{name}: {GetFormattedLogValue(argName, canonicalTypeName)}");
                }

                args[index++] = argName;
            }

            GenerateLogPrintBeforeCall(generator, $"{method.Identifier.Text}", logInArgs);

            string argsList = string.Join(", ", args);
            int returnRegisterIndex = 0;
            string result = null;
            string canonicalReturnTypeName = null;
            
            // TODO: clean this up, make it more generic ?
            if (TaskArity(compilation, method) == 0) // Task
            {
                generator.AppendLine($"await syscall.{method.Identifier.Text}({argsList});");                
            }
            else if (TaskArity(compilation, method) == 1) // Task<Result>
            {
                generator.AppendLine($"var {ResultVariableName} = await syscall.{method.Identifier.Text}({argsList});");
                generator.AppendLine($"context.SetX({returnRegisterIndex++}, (uint)({ResultVariableName}.ErrorCode));");
                canonicalReturnTypeName = TypeResult;
                result = GetFormattedLogValue($"{ResultVariableName}", canonicalReturnTypeName);
            }
            else if (TaskArity(compilation, method) == 2) // Task<(Result, int)>
            {
                generator.AppendLine($"var ({ResultVariableName}, {ResultVariableName}2) = await syscall.{method.Identifier.Text}({argsList});");
                generator.AppendLine($"context.SetX({returnRegisterIndex++}, (uint)({ResultVariableName}.ErrorCode));");
                generator.AppendLine($"context.SetX({returnRegisterIndex++}, (uint)({ResultVariableName}2));");
                canonicalReturnTypeName = TypeResult;
                result = GetFormattedLogValue($"{ResultVariableName}", canonicalReturnTypeName);
            }
            else if (method.ReturnType.ToString() != "void")
            {
                generator.AppendLine($"var {ResultVariableName} = syscall.{method.Identifier.Text}({argsList});");
                canonicalReturnTypeName = GetCanonicalTypeName(compilation, method.ReturnType);

                if (canonicalReturnTypeName == TypeResult)
                {
                    generator.AppendLine($"context.SetX({returnRegisterIndex++}, (uint){ResultVariableName}.ErrorCode);");
                }
                else
                {
                    generator.AppendLine($"context.SetX({returnRegisterIndex++}, (uint){ResultVariableName});");
                }

                if (Is64BitInteger(canonicalReturnTypeName))
                {
                    generator.AppendLine($"context.SetX({returnRegisterIndex++}, (uint)({ResultVariableName} >> 32));");
                }

                result = GetFormattedLogValue(ResultVariableName, canonicalReturnTypeName);
            }
            else
            {
                generator.AppendLine($"syscall.{method.Identifier.Text}({argsList});");
            }

            foreach (OutParameter outParameter in outParameters)
            {
                generator.AppendLine($"context.SetX({returnRegisterIndex++}, (uint){outParameter.Identifier});");

                if (outParameter.NeedsSplit)
                {
                    generator.AppendLine($"context.SetX({returnRegisterIndex++}, (uint)({outParameter.Identifier} >> 32));");
                }
            }

            while (returnRegisterIndex < 4)
            {
                generator.AppendLine($"context.SetX({returnRegisterIndex++}, 0);");
            }

            GenerateLogPrintAfterCall(generator, method.Identifier.Text, logOutArgs, result, canonicalReturnTypeName);

            generator.LeaveScope();
            generator.AppendLine();
        }

        private static void GenerateMethod64(CodeGenerator generator, Compilation compilation, MethodDeclarationSyntax method)
        {
            var retType = IsAsyncMethod(method) ? "async Task" : "void";
            generator.EnterScope($"private static {retType} {method.Identifier.Text}{A64Suffix}(Syscall syscall, {TypeExecutionContext} context)");

            string[] args = new string[method.ParameterList.Parameters.Count];
            int registerIndex = 0;
            int index = 0;

            List<OutParameter> outParameters = new List<OutParameter>();
            List<string> logInArgs = new List<string>();
            List<string> logOutArgs = new List<string>();

            foreach (var methodParameter in method.ParameterList.Parameters)
            {
                string name = methodParameter.Identifier.Text;
                string argName = GetPrefixedArgName(name);
                string typeName = methodParameter.Type.ToString();
                string canonicalTypeName = GetCanonicalTypeName(compilation, methodParameter.Type);

                if (methodParameter.Modifiers.Any(SyntaxKind.OutKeyword))
                {
                    outParameters.Add(new OutParameter(argName));
                    logOutArgs.Add($"{name}: {GetFormattedLogValue(argName, canonicalTypeName)}");
                    argName = $"out {typeName} {argName}";
                    registerIndex++;
                }
                else
                {
                    string value = GenerateCastFromUInt64($"context.GetX({registerIndex++})", canonicalTypeName, typeName);
                    generator.AppendLine($"var {argName} = {value};");
                    logInArgs.Add($"{name}: {GetFormattedLogValue(argName, canonicalTypeName)}");
                }

                args[index++] = argName;
            }

            GenerateLogPrintBeforeCall(generator, method.Identifier.Text, logInArgs);

            string argsList = string.Join(", ", args);
            int returnRegisterIndex = 0;
            string result = null;
            string canonicalReturnTypeName = null;

            // TODO: clean this up, make it more generic ?
            if (TaskArity(compilation, method) == 0) // Task
            {
                generator.AppendLine($"await syscall.{method.Identifier.Text}({argsList});");                
            }
            else if (TaskArity(compilation, method) == 1) // Task<Result>
            {
                generator.AppendLine($"var {ResultVariableName} = await syscall.{method.Identifier.Text}({argsList});");
                generator.AppendLine($"context.SetX({returnRegisterIndex++}, (ulong)({ResultVariableName}.ErrorCode));");
                canonicalReturnTypeName = TypeResult;
                result = GetFormattedLogValue($"{ResultVariableName}", canonicalReturnTypeName);
            }
            else if (TaskArity(compilation, method) == 2) // Task<(Result, int)>
            {
                generator.AppendLine($"var ({ResultVariableName}, {ResultVariableName}2) = await syscall.{method.Identifier.Text}({argsList});");
                generator.AppendLine($"context.SetX({returnRegisterIndex++}, (uint)({ResultVariableName}.ErrorCode));");
                generator.AppendLine($"context.SetX({returnRegisterIndex++}, (uint)({ResultVariableName}2));");
                canonicalReturnTypeName = TypeResult;
                result = GetFormattedLogValue($"{ResultVariableName}", canonicalReturnTypeName);
            }
            else if (method.ReturnType.ToString() != "void")
            {
                generator.AppendLine($"var {ResultVariableName} = syscall.{method.Identifier.Text}({argsList});");
                canonicalReturnTypeName = GetCanonicalTypeName(compilation, method.ReturnType);

                if (canonicalReturnTypeName == TypeResult)
                {
                    generator.AppendLine($"context.SetX({returnRegisterIndex++}, (ulong){ResultVariableName}.ErrorCode);");
                }
                else
                {
                    generator.AppendLine($"context.SetX({returnRegisterIndex++}, (ulong){ResultVariableName});");
                }
  
                result = GetFormattedLogValue(ResultVariableName, canonicalReturnTypeName);
            }
            else
            {
                generator.AppendLine($"syscall.{method.Identifier.Text}({argsList});");
            }

            foreach (OutParameter outParameter in outParameters)
            {
                generator.AppendLine($"context.SetX({returnRegisterIndex++}, (ulong){outParameter.Identifier});");
            }

            while (returnRegisterIndex < 8)
            {
                generator.AppendLine($"context.SetX({returnRegisterIndex++}, 0);");
            }

            GenerateLogPrintAfterCall(generator, method.Identifier.Text, logOutArgs, result, canonicalReturnTypeName);

            generator.LeaveScope();
            generator.AppendLine();
        }

        private static string GetFormattedLogValue(string value, string canonicalTypeName)
        {
            if (Is32BitInteger(canonicalTypeName))
            {
                return $"0x{{{value}:X8}}";
            }
            else if (Is64BitInteger(canonicalTypeName))
            {
                return $"0x{{{value}:X16}}";
            }

            return $"{{{value}}}";
        }

        private static string GetPrefixedArgName(string name)
        {
            return ArgVariablePrefix + char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        private static string GetCanonicalTypeName(Compilation compilation, SyntaxNode syntaxNode)
        {
            TypeInfo typeInfo = compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode);
            if (typeInfo.Type.ContainingNamespace == null)
            {
                return typeInfo.Type.Name;
            }

            return $"{typeInfo.Type.ContainingNamespace.ToDisplayString()}.{typeInfo.Type.Name}";
        }

        private static void GenerateLogPrintBeforeCall(CodeGenerator generator, string methodName, List<string> argList)
        {
            string log = $"[{{KernelStatic.GetCurrentThread().ThreadUid}}] {methodName}({string.Join(", ", argList)})";
            GenerateLogPrint(generator, "Trace", "KernelSvc", log);
        }

        private static void GenerateLogPrintAfterCall(
            CodeGenerator generator,
            string methodName,
            List<string> argList,
            string result,
            string canonicalResultTypeName)
        {
            string log = $"[{{KernelStatic.GetCurrentThread().ThreadUid}}] {methodName}({string.Join(", ", argList)})";

            if (result != null)
            {
                log += $" = {result}";
            }

            if (canonicalResultTypeName == TypeResult)
            {
                generator.EnterScope($"if ({ResultCheckHelperName}({ResultVariableName}))");
                GenerateLogPrint(generator, "Trace", "KernelSvc", log);
                generator.LeaveScope();
                generator.EnterScope("else");
                GenerateLogPrint(generator, "Warning", "KernelSvc", log);
                generator.LeaveScope();
            }
            else
            {
                GenerateLogPrint(generator, "Trace", "KernelSvc", log);
            }
        }

        private static void GenerateLogPrint(CodeGenerator generator, string logLevel, string logClass, string log)
        {
            generator.AppendLine($"Logger.{logLevel}?.PrintMsg(LogClass.{logClass}, $\"{log}\");");
        }

        private static void GenerateDispatch(CodeGenerator generator, List<SyscallIdAndName> syscalls, string suffix)
        {
            generator.EnterScope($"public static async Task Dispatch{suffix}(Syscall syscall, {TypeExecutionContext} context, int id)");
            generator.EnterScope("switch (id)");

            foreach (var syscall in syscalls)
            {
                var maybeAwait = syscall.IsAsync ? "await" : "";
                generator.AppendLine($"case {syscall.Id}:");
                generator.IncreaseIndentation();

                // generator.AppendLine($"System.Console.WriteLine(\"syscall: {syscall.Name}{suffix}\");");
                generator.AppendLine($"{maybeAwait} {syscall.Name}{suffix}(syscall, context);");
                generator.AppendLine("break;");

                generator.DecreaseIndentation();
            }

            generator.AppendLine($"default:");
            generator.IncreaseIndentation();

            generator.AppendLine("throw new NotImplementedException($\"SVC 0x{id:X4} is not implemented.\");");

            generator.DecreaseIndentation();

            generator.LeaveScope();
            generator.LeaveScope();
        }

        private static bool Is32BitInteger(string canonicalTypeName)
        {
            return canonicalTypeName == TypeSystemInt32 || canonicalTypeName == TypeSystemUInt32;
        }

        private static bool Is64BitInteger(string canonicalTypeName)
        {
            return canonicalTypeName == TypeSystemInt64 || canonicalTypeName == TypeSystemUInt64;
        }

        private static string GenerateCastFromUInt64(string value, string canonicalTargetTypeName, string targetTypeName)
        {
            return canonicalTargetTypeName == TypeSystemBoolean ? $"({value} & 1) != 0" : $"({targetTypeName}){value}";
        }

        private static bool IsPointerSized(Compilation compilation, ParameterSyntax parameterSyntax)
        {
            return parameterSyntax.AttributeLists.Any(attributeList =>
                attributeList.Attributes.Any(attribute =>
                    GetCanonicalTypeName(compilation, attribute) == TypePointerSizedAttribute));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyscallSyntaxReceiver());
        }

        private static bool IsAsyncMethod(MethodDeclarationSyntax method)
        {
            return method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
        }
        
        // TODO: cleanup and make it robust
        // NOTE: naively assuming sigs are unique for a given arity (true for now)
        private static int _TaskArity(Compilation compilation, MethodDeclarationSyntax method)
        {
            var tname = method.ReturnType.ToFullString();
    
    
            if (tname == "Task")
            {
                return 0;
            }
            else if (tname == "Task<Result>")
            {
                return 1;
            }
            else if (
                tname == "Task<(Result, int)>" ||
                tname == "Task<(Result, long)>" ||
                tname == "Task<(Result, uint)>" ||
                tname == "Task<(Result, ulong)>"
            )
            {
                return 2;
            }
            else
            {
                return -1;                
            }

            // if (tname == $"{TypeTask}")
            // {
            //     return 0;
            // }
            // else if (tname == $"{TypeTask}<{TypeResult}>")
            // {
            //     return 1;
            // }
            // else if (
            //     tname == $"{TypeTask}<{TypeResult}, {TypeSystemInt32}>" ||
            //     tname == $"{TypeTask}<{TypeResult}, {TypeSystemInt64}>" ||
            //     tname == $"{TypeTask}<{TypeResult}, {TypeSystemUInt32}>" ||
            //     tname == $"{TypeTask}<{TypeResult}, {TypeSystemUInt64}>"
            // )
            // {
            //     return 2;
            // }
            // else
            // {
            //     return -1;                
            // }
        }
        
        private static int TaskArity(Compilation _compilation, MethodDeclarationSyntax method)
        {
            var returnType = method.ReturnType;

            if (returnType is IdentifierNameSyntax identifier)
            {
                if (identifier.Identifier.Text == "Task")
                    return 0;
            }
            else if (returnType is GenericNameSyntax generic)
            {
                if (generic.Identifier.Text == "Task" && generic.TypeArgumentList.Arguments.Count == 1)
                {
                    var argument = generic.TypeArgumentList.Arguments[0];
                    if (argument is TupleTypeSyntax)
                        return 2;
                    return 1;
                }
            }

            return -1;
        }
    }
}
