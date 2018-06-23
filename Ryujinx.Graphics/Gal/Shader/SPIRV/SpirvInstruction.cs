using System;
using System.IO;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.Shader.SPIRV
{
    public class Instruction
    {
        public bool HoldsResultId { get; }

        private OpCode Opcode;
        private List<Operand> Operands;
        private Instruction ResultType;
        
        public Instruction(
            OpCode Opcode,
            bool HoldsResultId,
            Instruction ResultType = null)
        {
            this.Opcode = Opcode;
            this.HoldsResultId = HoldsResultId;
            this.ResultType = ResultType;

            Operands = new List<Operand>();
        }

        public void Write(BinaryWriter BinaryWriter)
        {
            BinaryWriter.Write((ushort)Opcode);
            BinaryWriter.Write(GetWordCount());

            if (ResultType != null)
            {
                BinaryWriter.Write((uint)ResultType.ResultId);
            }

            if (HoldsResultId)
            {
                BinaryWriter.Write((uint)ResultId);
            }

            foreach (Operand Operand in Operands)
            {
                Operand.Write(BinaryWriter);
            }
        }

        public ushort GetWordCount()
        {
            int WordCount = 1; // Opcode and WordCount word

            if (ResultType != null)
            {
                WordCount++;
            }

            if (HoldsResultId)
            {
                WordCount++;
            }

            foreach (Operand Operand in Operands)
            {
                WordCount += Operand.GetWordCount();
            }

            return (ushort)WordCount;
        }

        protected void AddOperand(Operand Operand)
        {
            Operands.Add(Operand);
        }

        protected void AddLiteralInteger(int Value)
        {
            AddOperand(new LiteralNumber(Value));
        }

        protected void AddEnum(int Value)
        {
            AddLiteralInteger(Value);
        }

        protected void AddString(string Value)
        {
            AddOperand(new LiteralString(Value));
        }
        
        protected void AddId(Instruction Instruction)
        {
            AddOperand(new Id(Instruction));
        }

        protected void AddOperands(Operand[] Operands)
        {
            foreach (var Operand in Operands)
            {
                AddOperand(Operand);
            }
        }

        protected void AddIds(Instruction[] Instructions)
        {
            foreach (var Instruction in Instructions)
            {
                AddId(Instruction);
            }
        }

        private uint _ResultId;
        public uint ResultId
        {
            get
            {
                if (!HoldsResultId)
                {
                    string Message = "Instruction does not hold a Result ID";
                    throw new InvalidOperationException(Message);
                }
                else if (_ResultId == 0)
                {
                    //You forgot to add this instruction to the Assembler
                    //and it was referenced from other instruction
                    string Message = "Instruction does not have a Result ID setted";
                    throw new InvalidOperationException(Message);
                }

                return _ResultId;
            }
            set
            {
                if (!HoldsResultId)
                {
                    throw new InvalidOperationException("Instruction does not take Result ID");
                }
                else if (_ResultId != 0)
                {
                    throw new InvalidOperationException("Instruction is used twice");
                }

                _ResultId = value;
            }
        }
    }

    public class UnaryInstruction : Instruction
    {
        public UnaryInstruction(
            OpCode Opcode,
            Instruction ResultType,
            Instruction Operand)
            : base(Opcode, true, ResultType)
        {
            AddId(Operand);
        }
    }

    public class BinaryInstruction : Instruction
    {
        public BinaryInstruction(
            OpCode Opcode,
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(Opcode, true, ResultType)
        {
            AddId(Operand1);
            AddId(Operand2);
        }
    }

    public class OpExtInst : Instruction
    {
        public OpExtInst(
            Instruction ResultType,
            Instruction ExtensionSet,
            int InstructionOpcode,
            params Instruction[] Ids)
            : base(OpCode.ExtInst, true, ResultType)
        {
            AddId(ExtensionSet);
            AddEnum(InstructionOpcode);
            AddIds(Ids);
        }
    }

    public class OpCapability : Instruction
    {
        public OpCapability(Capability Capability)
            : base(OpCode.Capability, false)
        {
            AddEnum((int)Capability);
        }
    }

    public class OpExtInstImport : Instruction
    {
        public OpExtInstImport(string Name)
            : base(OpCode.ExtInstImport, true)
        {
            AddString(Name);
        }
    }

    public class OpMemoryModel : Instruction
    {
        public OpMemoryModel(AddressingModel Addressing, MemoryModel Memory)
            : base(OpCode.MemoryModel, false)
        {
            AddEnum((int)Addressing);
            AddEnum((int)Memory);
        }
    }

    public class OpEntryPoint : Instruction
    {
        public OpEntryPoint(
            ExecutionModel Execution,
            Instruction EntryPoint,
            string Name,
            Instruction[] Interface)
            : base(OpCode.EntryPoint, false)
        {
            AddEnum((int)Execution);
            AddId(EntryPoint);
            AddString(Name);
            AddIds(Interface);
        }
    }

    public class OpExecutionMode : Instruction
    {
        public OpExecutionMode(
            Instruction EntryPoint,
            ExecutionMode Execution,
            params Operand[] OptionalLiterals)
            : base(OpCode.ExecutionMode, false)
        {
            AddId(EntryPoint);
            AddEnum((int)Execution);
            AddOperands(OptionalLiterals);
        }
    }

    public class OpDecorate : Instruction
    {
        public OpDecorate(
            Instruction Target,
            Decoration Decoration,
            params Operand[] Literals)
            : base(OpCode.Decorate, false)
        {
            AddId(Target);
            AddEnum((int)Decoration);
            AddOperands(Literals);
        }

        public OpDecorate(
            Instruction Target,
            BuiltIn BuiltIn)
            : this(Target, Decoration.BuiltIn, new LiteralNumber((int)BuiltIn))
        {
        }
    }

    public class OpTypeVoid : Instruction
    {
        public OpTypeVoid()
            : base(OpCode.TypeVoid, true)
        {
        }
    }

    public class OpTypeFunction : Instruction
    {
        public OpTypeFunction(
            Instruction ReturnType,
            params Instruction[] Parameters)
            : base(OpCode.TypeFunction, true)
        {
            AddId(ReturnType);
            AddIds(Parameters);
        }
    }

    public class OpTypeFloat : Instruction
    {
        public OpTypeFloat(int Width)
            : base(OpCode.TypeFloat, true)
        {
            if (Width != 32 && Width != 64)
            {
                throw new ArgumentException("Float type size has to be 32 or 64");
            }

            AddLiteralInteger(Width);
        }
    }

    public class OpTypeVector : Instruction
    {
        public OpTypeVector(Instruction ComponentType, int ComponentCount)
            : base(OpCode.TypeVector, true)
        {
            AddId(ComponentType);
            AddLiteralInteger(ComponentCount);
        }
    }

    public class OpTypePointer : Instruction
    {
        public StorageClass Storage;

        public Instruction PointedType;

        public OpTypePointer(StorageClass Storage, Instruction Type)
            : base(OpCode.TypePointer, true)
        {
            AddEnum((int)Storage);
            AddId(Type);

            this.Storage = Storage;
            this.PointedType = Type;
        }

        public override bool Equals(object Object)
        {
            if (Object is OpTypePointer Other)
            {
                return this.Storage == Other.Storage
                    && this.PointedType.Equals(Other.PointedType);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Storage.GetHashCode() + PointedType.GetHashCode();
        }
    }

    public class OpVariable : Instruction
    {
        public StorageClass Storage;

        public OpVariable(
            Instruction ResultType,
            StorageClass Storage,
            Instruction Initializer = null)
            : base(OpCode.Variable, true, ResultType)
        {
            AddEnum((int)Storage);

            if (Initializer != null)
            {
                AddId(Initializer);
            }

            this.Storage = Storage;
        }
    }

    public class OpConstant : Instruction
    {
        public Instruction ResultType;

        public Operand[] Literals;

        public OpConstant(
            Instruction ResultType,
            params Operand[] Literals)
            : base(OpCode.Constant, true, ResultType)
        {
            AddOperands(Literals);

            this.ResultType = ResultType;
            this.Literals = Literals;
        }

        public override bool Equals(object Object)
        {
            if (Object is OpConstant Other
                && this.ResultType.Equals(Other.ResultType)
                && this.Literals.Length == Other.Literals.Length)
            {
                for (int i = 0; i < Literals.Length; i++)
                {
                    if (!(this.Literals[i].Equals(Other.Literals[i])))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return ResultType.GetHashCode() + Literals.GetHashCode();
        }
    }

    public class OpConstantComposite : Instruction
    {
        public OpConstantComposite(
            Instruction ResultType,
            params Instruction[] Constituents)
            : base(OpCode.ConstantComposite, true, ResultType)
        {
            AddIds(Constituents);
        }
    }

    public class OpFunction : Instruction
    {
        public OpFunction(
            Instruction ResultType,
            FunctionControl Control,
            Instruction FunctionType)
            : base(OpCode.Function, true, ResultType)
        {
            AddEnum((int)Control);
            AddId(FunctionType);
        }
    }

    public class OpStore : Instruction
    {
        public OpStore(
            Instruction Pointer,
            Instruction Object,
            MemoryAccess MemoryAccess = MemoryAccess.None)
            : base(OpCode.Store, false)
        {
            AddId(Pointer);
            AddId(Object);

            if (MemoryAccess != MemoryAccess.None)
            {
                AddLiteralInteger((int)MemoryAccess);
            }
        }
    }

    public class OpReturn : Instruction
    {
        public OpReturn()
            : base(OpCode.Return, false)
        {
        }
    }

    public class OpFunctionEnd : Instruction
    {
        public OpFunctionEnd()
            : base(OpCode.FunctionEnd, false)
        {
        }
    }

    public class OpLabel : Instruction
    {
        public OpLabel()
            : base(OpCode.Label, true)
        {
        }
    }

    public class OpMemberDecorate : Instruction
    {
        public OpMemberDecorate(
            Instruction StructureType,
            int Member,
            Decoration Decoration,
            params Operand[] Literals)
            : base(OpCode.MemberDecorate, false)
        {
            AddId(StructureType);
            AddLiteralInteger(Member);
            AddEnum((int)Decoration);
            AddOperands(Literals);
        }

        public OpMemberDecorate(
            Instruction StructureType,
            int Member,
            BuiltIn BuiltIn)
            : this(StructureType, Member, Decoration.BuiltIn, new LiteralNumber((int)BuiltIn))
        {
        }
    }

    public class OpTypeStruct : Instruction
    {
        public OpTypeStruct(params Instruction[] MemberIds)
            : base(OpCode.TypeStruct, true)
        {
            AddIds(MemberIds);
        }
    }

    public class OpTypeInt : Instruction
    {
        public OpTypeInt(int Width, bool IsSigned)
            : base(OpCode.TypeInt, true)
        {
            // Width shouldn't be checked here because the specification 1.0 does not define it
            // but for safety it's locked to 32 and 64 bits
            if (Width != 32 && Width != 64)
            {
                throw new ArgumentException("Integer type size is locked 32 and 64");
            }

            AddLiteralInteger(Width);
            AddLiteralInteger(IsSigned ? 1 : 0);
        }
    }

    public class OpCompositeExtract : Instruction
    {
        public OpCompositeExtract(
            Instruction ResultType,
            Instruction Composite,
            int[] Indexes)
            : base(OpCode.CompositeExtract, true, ResultType)
        {
            AddId(Composite);
            foreach (int Index in Indexes)
            {
                AddLiteralInteger(Index);
            }
        }

        public OpCompositeExtract(
            Instruction ResultType,
            Instruction Composite,
            int Index)
            : this(ResultType, Composite, new int[]{ Index })
        {
        }
    }

    public class OpCompositeConstruct : Instruction
    {
        public OpCompositeConstruct(
            Instruction ResultType,
            params Instruction[] Constituents)
            : base(OpCode.CompositeConstruct, true, ResultType)
        {
            AddIds(Constituents);
        }
    }

    public class OpLoad : Instruction
    {
        public OpLoad(
            Instruction ResultType,
            Instruction Pointer,
            MemoryAccess MemoryAccess = MemoryAccess.None)
            : base(OpCode.Load, true, ResultType)
        {
            AddId(Pointer);

            if (MemoryAccess != MemoryAccess.None)
            {
                AddLiteralInteger((int)MemoryAccess.None);
            }
        }
    }

    public class OpAccessChain : Instruction
    {
        public OpAccessChain(
            Instruction ResultType,
            Instruction Base,
            params Instruction[] Indexes)
            : base(OpCode.AccessChain, true, ResultType)
        {
            if (Base is OpVariable Variable)
            {
                if (ResultType is OpTypePointer Pointer)
                {
                    if (Variable.Storage != Pointer.Storage)
                    {
                        throw new ArgumentException("Result type and base have to share the same storage");
                    }
                }
                else
                {
                    throw new ArgumentException("Result type has to be a pointer");
                }
            }
            else
            {
                throw new ArgumentException("Base has to be a variable");
            }

            AddId(Base);
            AddIds(Indexes);
        }
    }

    public class OpTypeArray : Instruction
    {
        public OpTypeArray(
            Instruction ElementType,
            Instruction Length)
            : base(OpCode.TypeArray, true)
        {
            AddId(ElementType);
            AddId(Length);
        }
    }

    public class OpIAdd : BinaryInstruction
    {
        public OpIAdd(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.IAdd, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFAdd : BinaryInstruction
    {
        public OpFAdd(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FAdd, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpISub : BinaryInstruction
    {
        public OpISub(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.ISub, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFSub : BinaryInstruction
    {
        public OpFSub(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FSub, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpIMul : BinaryInstruction
    {
        public OpIMul(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.IMul, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFMul : BinaryInstruction
    {
        public OpFMul(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FMul, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpUDiv : BinaryInstruction
    {
        public OpUDiv(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.UDiv, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpSDiv : BinaryInstruction
    {
        public OpSDiv(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.SDiv, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFDiv : BinaryInstruction
    {
        public OpFDiv(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FDiv, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpUMod : BinaryInstruction
    {
        public OpUMod(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.UMod, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpSRem : BinaryInstruction
    {
        public OpSRem(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.SRem, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpSMod : BinaryInstruction
    {
        public OpSMod(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.SMod, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFRem : BinaryInstruction
    {
        public OpFRem(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FRem, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFMod : BinaryInstruction
    {
        public OpFMod(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FMod, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpTypeImage : Instruction
    {
        public OpTypeImage(
            Instruction SampledType,
            Dim Dim,
            int Depth,
            int Arrayed,
            int Multisampled,
            int Sampled,
            ImageFormat ImageFormat,
            AccessQualifier AccessQualifier = AccessQualifier.Max)
            : base(OpCode.TypeImage, true)
        {
            AddId(SampledType);
            AddEnum((int)Dim);
            AddLiteralInteger(Depth);
            AddLiteralInteger(Arrayed);
            AddLiteralInteger(Multisampled);
            AddLiteralInteger(Sampled);
            AddEnum((int)ImageFormat);

            if (AccessQualifier != AccessQualifier.Max)
            {
                AddEnum((int)AccessQualifier);
            }
        }
    }

    public class OpTypeSampledImage : Instruction
    {
        public OpTypeSampledImage(Instruction ImageType)
            : base(OpCode.TypeSampledImage, true)
        {
            AddId(ImageType);
        }
    }

    public class OpName : Instruction
    {
        public OpName(
            Instruction Target,
            string Name)
            : base(OpCode.Name, false)
        {
            AddId(Target);
            AddString(Name);
        }
    }

    public class OpTypeBool : Instruction
    {
        public OpTypeBool()
            : base(OpCode.TypeBool, true)
        {
        }
    }

    public class OpConstantTrue : Instruction
    {
        public OpConstantTrue(Instruction ResultType)
            : base(OpCode.ConstantTrue, true, ResultType)
        {
        }
    }

    public class OpConstantFalse : Instruction
    {
        public OpConstantFalse(Instruction ResultType)
            : base(OpCode.ConstantFalse, true, ResultType)
        {
        }
    }

    public class OpBitcast : Instruction
    {
        public OpBitcast(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.Bitcast, true, ResultType)
        {
            AddId(Operand);
        }
    }

    public class OpShiftRightLogical : Instruction
    {
        public OpShiftRightLogical(
            Instruction ResultType,
            Instruction Base,
            Instruction Shift)
            : base(OpCode.ShiftRightLogical, true, ResultType)
        {
            AddId(Base);
            AddId(Shift);
        }
    }

    public class OpShiftLeftLogical : Instruction
    {
        public OpShiftLeftLogical(
            Instruction ResultType,
            Instruction Base,
            Instruction Shift)
            : base(OpCode.ShiftLeftLogical, true, ResultType)
        {
            AddId(Base);
            AddId(Shift);
        }
    }

    public class OpShiftRightArithmetic : Instruction
    {
        public OpShiftRightArithmetic(
            Instruction ResultType,
            Instruction Base,
            Instruction Shift)
            : base(OpCode.ShiftRightArithmetic, true, ResultType)
        {
            AddId(Base);
            AddId(Shift);
        }
    }

    public class OpLogicalNot : Instruction
    {
        public OpLogicalNot(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.LogicalNot, true, ResultType)
        {
            AddId(Operand);
        }
    }

    public class OpBranch : Instruction
    {
        public OpBranch(Instruction TargetLabel)
            : base(OpCode.Branch, false)
        {
            AddId(TargetLabel);
        }
    }

    public class OpBranchConditional : Instruction
    {
        public OpBranchConditional(
            Instruction Condition,
            Instruction TrueLabel,
            Instruction FalseLabel,
            Operand TrueWeight = null,
            Operand FalseWeight = null)
            : base(OpCode.BranchConditional, false)
        {
            AddId(Condition);
            AddId(TrueLabel);
            AddId(FalseLabel);

            if (TrueWeight != null || FalseWeight != null)
            {
                if (TrueWeight == null || FalseWeight == null)
                {
                    throw new InvalidOperationException("There must be either no Weights or two");
                }

                AddOperand(TrueWeight);
                AddOperand(FalseWeight);
            }

        }
    }

    public class OpNop : Instruction
    {
        public OpNop()
            : base(OpCode.Nop, false)
        {
        }
    }

    public class OpFOrdLessThan : BinaryInstruction
    {
        public OpFOrdLessThan(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FOrdLessThan, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFOrdGreaterThan : BinaryInstruction
    {
        public OpFOrdGreaterThan(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FOrdGreaterThan, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFOrdGreaterThanEqual : BinaryInstruction
    {
        public OpFOrdGreaterThanEqual(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FOrdGreaterThanEqual, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFOrdLessThanEqual : BinaryInstruction
    {
        public OpFOrdLessThanEqual(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FOrdLessThanEqual, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpSLessThan : BinaryInstruction
    {
        public OpSLessThan(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.SLessThan, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpSLessThanEqual : BinaryInstruction
    {
        public OpSLessThanEqual(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.SLessThanEqual, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpSGreaterThan : BinaryInstruction
    {
        public OpSGreaterThan(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.SGreaterThan, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpSGreaterThanEqual : BinaryInstruction
    {
        public OpSGreaterThanEqual(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.SGreaterThanEqual, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpIsNan : UnaryInstruction
    {
        public OpIsNan(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.IsNan, ResultType, Operand)
        {
        }
    }

    public class OpIsNormal : UnaryInstruction
    {
        public OpIsNormal(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.IsNormal, ResultType, Operand) 
        {
        }
    }

    public class OpKill: Instruction
    {
        public OpKill()
            : base(OpCode.Kill, false)
        {
        }
    }

    public class OpNot : UnaryInstruction
    {
        public OpNot(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.Not, ResultType, Operand)
        {
        }
    }

    public class OpFOrdEqual : BinaryInstruction
    {
        public OpFOrdEqual(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FOrdEqual, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFOrdNotEqual : BinaryInstruction
    {
        public OpFOrdNotEqual(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.FOrdNotEqual, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpFNegate : UnaryInstruction
    {
        public OpFNegate(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.FNegate, ResultType, Operand)
        {
        }
    }

    public class OpBitwiseAnd : BinaryInstruction
    {
        public OpBitwiseAnd(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.BitwiseAnd, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpBitwiseOr : BinaryInstruction
    {
        public OpBitwiseOr(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.BitwiseOr, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpImage : Instruction
    {
        public OpImage(
            Instruction ResultType,
            Instruction SampledImage)
            : base(OpCode.Image, true, ResultType)
        {
            AddId(SampledImage);
        }
    }

    public class OpImageQuerySizeLod : Instruction
    {
        public OpImageQuerySizeLod(
            Instruction ResultType,
            Instruction Image,
            Instruction LevelOfDetail)
            : base(OpCode.ImageQuerySizeLod, true, ResultType)
        {
            AddId(Image);
            AddId(LevelOfDetail);
        }
    }

    public class OpImageSampleImplicitLod : Instruction
    {
        public OpImageSampleImplicitLod(
            Instruction ResultType,
            Instruction SampledImage,
            Instruction Coordinate,
            ImageOperands ImageOperands = ImageOperands.None,
            params Instruction[] Params)
            : base(OpCode.ImageSampleImplicitLod, true, ResultType)
        {
            AddId(SampledImage);
            AddId(Coordinate);

            if (ImageOperands != ImageOperands.None)
            {
                AddEnum((int)ImageOperands);
                AddIds(Params);
            }
        }
    }

    public class OpImageFetch : Instruction
    {
        public OpImageFetch(
            Instruction ResultType,
            Instruction Image,
            Instruction Coordinate,
            ImageOperands ImageOperands = ImageOperands.None,
            params Instruction[] Params)
            : base(OpCode.ImageFetch, true, ResultType)
        {
            AddId(Image);
            AddId(Coordinate);

            if (ImageOperands != ImageOperands.None)
            {
                AddEnum((int)ImageOperands);
                AddIds(Params);
            }
        }
    }

    public class OpINotEqual : BinaryInstruction
    {
        public OpINotEqual(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.INotEqual, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpIEqual : BinaryInstruction
    {
        public OpIEqual(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.IEqual, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpConvertFToU : UnaryInstruction
    {
        public OpConvertFToU(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.ConvertFToU, ResultType, Operand)
        {
        }
    }

    public class OpConvertFToS : UnaryInstruction
    {
        public OpConvertFToS(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.ConvertFToS, ResultType, Operand)
        {
        }
    }

    public class OpConvertUToF : UnaryInstruction
    {
        public OpConvertUToF(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.ConvertUToF, ResultType, Operand)
        {
        }
    }

    public class OpConvertSToF : UnaryInstruction
    {
        public OpConvertSToF(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.ConvertSToF, ResultType, Operand)
        {
        }
    }

    public class OpBitwiseXor : BinaryInstruction
    {
        public OpBitwiseXor(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.BitwiseXor, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpSNegate : UnaryInstruction
    {
        public OpSNegate(
            Instruction ResultType,
            Instruction Operand)
            : base(OpCode.SNegate, ResultType, Operand)
        {
        }
    }

    public class OpLogicalAnd : BinaryInstruction
    {
        public OpLogicalAnd(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.LogicalAnd, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpLogicalOr : BinaryInstruction
    {
        public OpLogicalOr(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.LogicalOr, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpLogicalNotEqual : BinaryInstruction
    {
        public OpLogicalNotEqual(
            Instruction ResultType,
            Instruction Operand1,
            Instruction Operand2)
            : base(OpCode.LogicalNotEqual, ResultType, Operand1, Operand2)
        {
        }
    }

    public class OpVectorShuffle : Instruction
    {
        public OpVectorShuffle(
            Instruction ResultType,
            Instruction Vector1,
            Instruction Vector2,
            params Operand[] Components)
            : base(OpCode.VectorShuffle, true, ResultType)
        {
            AddId(Vector1);
            AddId(Vector2);
            AddOperands(Components);
        }
    }

    public class GLSLstd450Builder
    {
        private Instruction ExtensionSet;

        public GLSLstd450Builder(Instruction ExtensionSet)
        {
            this.ExtensionSet = ExtensionSet;
        }

        public OpExtInst Round(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Round, X);
        }

        public OpExtInst RoundEven(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.RoundEven, X);
        }

        public OpExtInst Trunc(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Trunc, X);
        }

        public OpExtInst FAbs(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FAbs, X);
        }

        public OpExtInst SAbs(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.SAbs, X);
        }

        public OpExtInst FSign(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FSign, X);
        }

        public OpExtInst SSign(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.SSign, X);
        }

        public OpExtInst Floor(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Floor, X);
        }

        public OpExtInst Ceil(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Ceil, X);
        }

        public OpExtInst Fract(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Fract, X);
        }

        public OpExtInst Radians(Instruction ResultType, Instruction Degrees)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Radians, Degrees);
        }

        public OpExtInst Degrees(Instruction ResultType, Instruction Radians)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Degrees, Radians);
        }

        public OpExtInst Sin(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Sin, X);
        }

        public OpExtInst Cos(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Cos, X);
        }

        public OpExtInst Tan(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Tan, X);
        }

        public OpExtInst Asin(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Asin, X);
        }

        public OpExtInst Acos(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Acos, X);
        }

        public OpExtInst Atan(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Atan, X);
        }

        public OpExtInst Sinh(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Sinh, X);
        }

        public OpExtInst Cosh(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Cosh, X);
        }

        public OpExtInst Tanh(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Tanh, X);
        }

        public OpExtInst Asinh(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Asinh, X);
        }

        public OpExtInst Acosh(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Acosh, X);
        }

        public OpExtInst Atanh(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Atanh, X);
        }

        public OpExtInst Atan2(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Atan2, X, Y);
        }

        public OpExtInst Pow(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Pow, X, Y);
        }

        public OpExtInst Exp(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Exp, X);
        }

        public OpExtInst Log(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Log, X);
        }

        public OpExtInst Exp2(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Exp2, X);
        }

        public OpExtInst Log2(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Log2, X);
        }

        public OpExtInst Sqrt(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Sqrt, X);
        }

        public OpExtInst InverseSqrt(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.InverseSqrt, X);
        }

        public OpExtInst Determinant(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Determinant, X);
        }

        public OpExtInst MatrixInverse(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.MatrixInverse, X);
        }

        public OpExtInst Modf(Instruction ResultType, Instruction X, Instruction I)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Modf, X, I);
        }

        public OpExtInst ModfStruct(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.ModfStruct, X);
        }

        public OpExtInst FMin(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FMin, X, Y);
        }

        public OpExtInst UMin(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.UMin, X, Y);
        }

        public OpExtInst SMin(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.SMin, X, Y);
        }

        public OpExtInst FMax(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FMax, X, Y);
        }

        public OpExtInst UMax(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.UMax, X, Y);
        }

        public OpExtInst SMax(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.SMax, X, Y);
        }

        public OpExtInst FClamp(Instruction ResultType, Instruction X, Instruction MinVal, Instruction MaxVal)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FClamp, X, MinVal, MaxVal);
        }

        public OpExtInst UClamp(Instruction ResultType, Instruction X, Instruction MinVal, Instruction MaxVal)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.UClamp, X, MinVal, MaxVal);
        }

        public OpExtInst SClamp(Instruction ResultType, Instruction X, Instruction MinVal, Instruction MaxVal)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.SClamp, X, MinVal, MaxVal);
        }

        public OpExtInst FMix(Instruction ResultType, Instruction X, Instruction Y, Instruction A)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FMix, X, Y, A);
        }

        public OpExtInst Step(Instruction ResultType, Instruction Edge, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Step, Edge, X);
        }

        public OpExtInst SmoothStep(Instruction ResultType, Instruction Edge1, Instruction Edge2, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.SmoothStep, Edge1, Edge2, X);
        }

        public OpExtInst Fma(Instruction ResultType, Instruction A, Instruction B, Instruction C)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Fma, A, B, C);
        }

        public OpExtInst Frexp(Instruction ResultType, Instruction X, Instruction Exp)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Frexp, X, Exp);
        }

        public OpExtInst FrexpStruct(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FrexpStruct, X);
        }

        public OpExtInst Ldexp(Instruction ResultType, Instruction X, Instruction Exp)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Ldexp, X, Exp);
        }

        public OpExtInst PackSnorm4x8(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.PackSnorm4x8, V);
        }

        public OpExtInst PackUnorm4x8(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.PackUnorm4x8, V);
        }

        public OpExtInst PackSnorm2x16(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.PackSnorm2x16, V);
        }

        public OpExtInst PackUnorm2x16(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.PackUnorm2x16, V);
        }

        public OpExtInst PackHalf2x16(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.PackHalf2x16, V);
        }

        public OpExtInst PackDouble2x32(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.PackDouble2x32, V);
        }

        public OpExtInst UnpackSnorm4x8(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.UnpackSnorm4x8, V);
        }

        public OpExtInst UnpackUnorm4x8(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.UnpackUnorm4x8, V);
        }

        public OpExtInst UnpackSnorm2x16(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.UnpackSnorm2x16, V);
        }

        public OpExtInst UnpackUnorm2x16(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.UnpackUnorm2x16, V);
        }

        public OpExtInst UnpackHalf2x16(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.UnpackHalf2x16, V);
        }

        public OpExtInst UnpackDouble2x32(Instruction ResultType, Instruction V)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.UnpackDouble2x32, V);
        }

        public OpExtInst Length(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Length, X);
        }

        public OpExtInst Distance(Instruction ResultType, Instruction Point1, Instruction Point2)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Distance, Point1, Point2);
        }

        public OpExtInst Cross(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Cross, X, Y);
        }

        public OpExtInst Normalize(Instruction ResultType, Instruction X)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Normalize, X);
        }

        public OpExtInst FaceForward(Instruction ResultType, Instruction N, Instruction I, Instruction Nref)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FaceForward, N, I, Nref);
        }

        public OpExtInst Reflect(Instruction ResultType, Instruction I, Instruction N)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Reflect, I, N);
        }

        public OpExtInst Refract(Instruction ResultType, Instruction I, Instruction N, Instruction Eta)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.Refract, I, N, Eta);
        }

        public OpExtInst FindILsb(Instruction ResultType, Instruction Value)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FindILsb, Value);
        }

        public OpExtInst FindSMsb(Instruction ResultType, Instruction Value)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FindSMsb, Value);
        }

        public OpExtInst FindUMsb(Instruction ResultType, Instruction Value)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.FindUMsb, Value);
        }

        public OpExtInst InterpolateAtCentroid(Instruction ResultType, Instruction Interpolant)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.InterpolateAtCentroid, Interpolant);
        }

        public OpExtInst InterpolateAtSample(Instruction ResultType, Instruction Interpolant, Instruction Sample)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.InterpolateAtSample, Interpolant, Sample);
        }

        public OpExtInst InterpolateAtOffset(Instruction ResultType, Instruction Interpolant, Instruction Offset)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.InterpolateAtOffset, Interpolant, Offset);
        }

        public OpExtInst NMin(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.NMin, X, Y);
        }

        public OpExtInst NMax(Instruction ResultType, Instruction X, Instruction Y)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.NMax, X, Y);
        }

        public OpExtInst NClamp(Instruction ResultType, Instruction X, Instruction MinVal, Instruction MaxVal)
        {
            return new OpExtInst(ResultType, ExtensionSet, (int)GLSLstd450.NClamp, X, MinVal, MaxVal);
        }
    }
}