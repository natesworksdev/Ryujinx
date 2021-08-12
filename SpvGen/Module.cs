using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static Spv.Specification;

namespace Spv.Generator
{
    public partial class Module
    {
        // TODO: register to SPIR-V registry
        private const int GeneratorId = 0;

        private readonly uint _version;

        private uint _bound;

        // Follow spec order here why keeping it as dumb as possible.
        private List<Capability> _capabilities;
        private List<string> _extensions;
        private List<Instruction> _extInstImports;
        private AddressingModel _addressingModel;
        private MemoryModel _memoryModel;

        private List<Instruction> _entrypoints;
        private List<Instruction> _executionModes;
        private List<Instruction> _debug;
        private List<Instruction> _annotations;

        // In the declaration block.
        private List<Instruction> _typeDeclarations;
        // In the declaration block.
        private List<Instruction> _globals;
        // In the declaration block, for function that aren't defined in the module.
        private List<Instruction> _functionsDeclarations;

        private List<Instruction> _functionsDefinitions;

        public Module(uint version)
        {
            _version = version;
            _bound = 1;
            _capabilities = new List<Capability>();
            _extensions = new List<string>();
            _extInstImports = new List<Instruction>();
            _addressingModel = AddressingModel.Logical;
            _memoryModel = MemoryModel.Simple;
            _entrypoints = new List<Instruction>();
            _executionModes = new List<Instruction>();
            _debug = new List<Instruction>();
            _annotations = new List<Instruction>();
            _typeDeclarations = new List<Instruction>();
            _globals = new List<Instruction>();
            _functionsDeclarations = new List<Instruction>();
            _functionsDefinitions = new List<Instruction>();
        }

        private uint GetNewId()
        {
            return _bound++;
        }

        public void AddCapability(Capability capability)
        {
            _capabilities.Add(capability);
        }

        public void AddExtension(string extension)
        {
            _extensions.Add(extension);
        }

        public Instruction AddExtInstImport(string import)
        {
            Instruction instruction = new Instruction(Op.OpExtInstImport);
            instruction.AddOperand(import);

            foreach (Instruction extInstImport in _extInstImports)
            {
                if (extInstImport.Opcode == Op.OpExtInstImport && extInstImport.EqualsContent(instruction))
                {
                    // update the duplicate instance to use the good id so it ends up being encoded right.
                    return extInstImport;
                }
            }

            instruction.SetId(GetNewId());

            _extInstImports.Add(instruction);

            return instruction;
        }

        private void AddTypeDeclaration(Instruction instruction, bool forceIdAllocation)
        {
            if (!forceIdAllocation)
            {
                foreach (Instruction typeDeclaration in _typeDeclarations)
                {
                    if (typeDeclaration.Opcode == instruction.Opcode && typeDeclaration.EqualsContent(instruction))
                    {
                        // update the duplicate instance to use the good id so it ends up being encoded right.
                        instruction.SetId(typeDeclaration.Id);

                        return;
                    }
                }
            }

            instruction.SetId(GetNewId());

            _typeDeclarations.Add(instruction);
        }

        public void AddEntryPoint(ExecutionModel executionModel, Instruction function, string name, params Instruction[] interfaces)
        {
            Debug.Assert(function.Opcode == Op.OpFunction);

            Instruction entryPoint = new Instruction(Op.OpEntryPoint);

            entryPoint.AddOperand(executionModel);
            entryPoint.AddOperand(function);
            entryPoint.AddOperand(name);
            entryPoint.AddOperand(interfaces);

            _entrypoints.Add(entryPoint);
        }

        public void AddExecutionMode(Instruction function, ExecutionMode mode, params Operand[] parameters)
        {
            Debug.Assert(function.Opcode == Op.OpFunction);

            Instruction executionModeInstruction = new Instruction(Op.OpExecutionMode);

            executionModeInstruction.AddOperand(function);
            executionModeInstruction.AddOperand(mode);
            executionModeInstruction.AddOperand(parameters);

            _executionModes.Add(executionModeInstruction);
        }

        private void AddToFunctionDefinitions(Instruction instruction)
        {
            Debug.Assert(instruction.Opcode != Op.OpTypeInt);
            _functionsDefinitions.Add(instruction);
        }

        private void AddAnnotation(Instruction annotation)
        {
            _annotations.Add(annotation);
        }

        private void AddDebug(Instruction debug)
        {
            _debug.Add(debug);
        }

        public void AddLabel(Instruction label)
        {
            Debug.Assert(label.Opcode == Op.OpLabel);

            label.SetId(GetNewId());

            AddToFunctionDefinitions(label);
        }

        public void AddLocalVariable(Instruction variable)
        {
            // TODO: ensure it has the local modifier
            Debug.Assert(variable.Opcode == Op.OpVariable);

            variable.SetId(GetNewId());

            AddToFunctionDefinitions(variable);
        }

        public void AddGlobalVariable(Instruction variable)
        {
            // TODO: ensure it has the global modifier
            // TODO: all constants opcodes (OpSpecXXX and the rest of the OpConstantXXX)
            Debug.Assert(variable.Opcode == Op.OpVariable);

            variable.SetId(GetNewId());

            _globals.Add(variable);
        }

        private void AddConstant(Instruction constant)
        {
            Debug.Assert(constant.Opcode == Op.OpConstant ||
                         constant.Opcode == Op.OpConstantFalse ||
                         constant.Opcode == Op.OpConstantTrue ||
                         constant.Opcode == Op.OpConstantNull ||
                         constant.Opcode == Op.OpConstantComposite);

            foreach (Instruction global in _globals)
            {
                if (global.Opcode == constant.Opcode && global.EqualsResultType(constant) && global.EqualsContent(constant))
                {
                    // update the duplicate instance to use the good id so it ends up being encoded right.
                    constant.SetId(global.Id);

                    return;
                }
            }

            constant.SetId(GetNewId());

            _globals.Add(constant);
        }

        public void SetMemoryModel(AddressingModel addressingModel, MemoryModel memoryModel)
        {
            _addressingModel = addressingModel;
            _memoryModel = memoryModel;
        }

        protected virtual void Construct()
        {
            throw new NotSupportedException("Construct should be overriden.");
        }

        public byte[] Generate()
        {
            Construct();

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);

                // Header
                writer.Write(MagicNumber);
                writer.Write(_version);
                writer.Write(GeneratorId);
                writer.Write(_bound);
                writer.Write(0u);

                // 1.
                foreach (Capability capability in _capabilities)
                {
                    Instruction capabilityInstruction = new Instruction(Op.OpCapability);

                    capabilityInstruction.AddOperand(capability);
                    capabilityInstruction.Write(stream);
                }

                // 2.
                foreach (string extension in _extensions)
                {
                    Instruction extensionInstruction = new Instruction(Op.OpExtension);

                    extensionInstruction.AddOperand(extension);
                    extensionInstruction.Write(stream);
                }

                // 3.
                foreach (Instruction extInstImport in _extInstImports)
                {
                    extInstImport.Write(stream);
                }

                // 4.
                Instruction memoryModelInstruction = new Instruction(Op.OpMemoryModel);
                memoryModelInstruction.AddOperand(_addressingModel);
                memoryModelInstruction.AddOperand(_memoryModel);
                memoryModelInstruction.Write(stream);

                // 5.
                foreach (Instruction entrypoint in _entrypoints)
                {
                    entrypoint.Write(stream);
                }

                // 6.
                foreach (Instruction executionMode in _executionModes)
                {
                    executionMode.Write(stream);
                }

                // 7.
                // TODO: order debug information correclty.
                foreach (Instruction debug in _debug)
                {
                    debug.Write(stream);
                }

                // 8.
                foreach (Instruction annotation in _annotations)
                {
                    annotation.Write(stream);
                }

                // Ensure that everything is in the right order in the declarations section
                List<Instruction> declarations = new List<Instruction>();
                declarations.AddRange(_typeDeclarations);
                declarations.AddRange(_globals);
                declarations.Sort((Instruction x, Instruction y) => x.Id.CompareTo(y.Id));

                // 9.
                foreach (Instruction declaration in declarations)
                {
                    declaration.Write(stream);
                }

                // 10.
                foreach (Instruction functionDeclaration in _functionsDeclarations)
                {
                    functionDeclaration.Write(stream);
                }

                // 11.
                foreach (Instruction functionDefinition in _functionsDefinitions)
                {
                    functionDefinition.Write(stream);
                }

                return stream.ToArray();
            }
        }
    }
}
