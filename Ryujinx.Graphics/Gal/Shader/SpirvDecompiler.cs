using System;
using System.IO;
using System.Collections.Generic;
using Ryujinx.Graphics.Gal.Shader.SPIRV;

namespace Ryujinx.Graphics.Gal.Shader
{
    public class SpirvDecompiler
    {
        private class SpirvVariable
        {
            public Instruction Id;
            public StorageClass Storage;
            public string Name;
            public int Location = -1;
        }

        private enum OperType
        {
            Bool,
            F32,
            I32
        }

        private delegate int LocationAllocator();

        private delegate Instruction GetInstExpr(ShaderIrOp Op);

        private Dictionary<ShaderIrInst, GetInstExpr> InstsExpr;

        private GlslDecl Decl;

        private ShaderIrBlock[] Blocks;

        private Assembler Assembler;

        private int UniformCount = 0;
        private int InAttributeCount = 0;
        private int OutAttributeCount = 0;

        private Instruction TypeVoid,
            TypeBool, TypeBool_Private,
            TypeInt, TypeUInt,
            TypeFloat,
            TypeImage2D, TypeSampler2D, TypeSampler2D_Uniform;

        private Instruction[] TypeFloats, TypeFloats_In, TypeFloats_Out, TypeFloats_Uniform, TypeFloats_Private;

        private Instruction TrueConstant, FalseConstant;

        //Holds debug info, they are not needed but do not hurt to add
        private List<Instruction> Names;

        //Types and constants have to be defined sequentially
        //because some types require constants and most constants require types
        private List<Instruction> TypesConstants;
        
        private List<Instruction> Decorates;
        
        //Variables declarations. They are different to "Variables" declared below
        //These holds instructions
        private List<Instruction> VarsDeclaration;

        private List<Instruction> Code;

        private List<SpirvVariable> Variables;

        private Instruction Main;

        private Instruction PerVertexVar = null;

        private Instruction GlslExtension;

        private GLSLstd450Builder Glsl450;

        //

        private Instruction GetOperExpr(ShaderIrOp Op, ShaderIrNode Oper)
        {
            return GetExprWithCast(Op, Oper, GetSrcExpr(Oper));
        }

        private Instruction GetFcltExpr(ShaderIrOp Op)
        {
            return new OpFOrdLessThan(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));
        }

        private Instruction GetSrcNodeTypeId(ShaderIrOp Op)
        {
            switch (GetSrcNodeType(Op))
            {
                case OperType.Bool: return TypeBool;
                case OperType.F32:  return TypeFloat;
                case OperType.I32:  return TypeInt;
                default:
                    throw new InvalidOperationException();
            }
        }

        private Instruction GetFcgtExpr(ShaderIrOp Op)
        {
            return new OpFOrdGreaterThan(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));
        }

        private Instruction GetFnegExpr(ShaderIrOp Op)
        {
            return new OpFNegate(TypeFloat, GetOperExpr(Op, Op.OperandA));
        }

        private Instruction GetFaddExpr(ShaderIrOp Op)
        {
            return new OpFAdd(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));
        }

        private Instruction GetFmulExpr(ShaderIrOp Op)
        {
            return new OpFMul(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));
        }

        private Instruction GetFfmaExpr(ShaderIrOp Op)
        {
            return Glsl450.Fma(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB),
                GetOperExpr(Op, Op.OperandC));
        }

        //


        public SpirvDecompiler()
        {
            InstsExpr = new Dictionary<ShaderIrInst, GetInstExpr>()
            {
                { ShaderIrInst.Fadd, GetFaddExpr },
                { ShaderIrInst.Fmul, GetFmulExpr },
                { ShaderIrInst.Fclt, GetFcltExpr },
                { ShaderIrInst.Fcgt, GetFcgtExpr },
                { ShaderIrInst.Fneg, GetFnegExpr },
                { ShaderIrInst.Ffma, GetFfmaExpr }
            };

            Assembler = new Assembler();

            GlslExtension = new OpExtInstImport("GLSL.std.450");
            Glsl450 = new GLSLstd450Builder(GlslExtension);

            TypesConstants = new List<Instruction>();
            Names = new List<Instruction>();
            Decorates = new List<Instruction>();
            VarsDeclaration = new List<Instruction>();
            Code = new List<Instruction>();

            Variables = new List<SpirvVariable>();

            //Declare types types
            TypeVoid = new OpTypeVoid();

            TypeBool = new OpTypeBool();
            TypeBool_Private = new OpTypePointer(StorageClass.Private, TypeBool);

            TrueConstant = new OpConstantTrue(TypeBool);
            FalseConstant = new OpConstantFalse(TypeBool);

            TypeInt = new OpTypeInt(32, true);
            TypeUInt = new OpTypeInt(32, false);

            TypeFloats = new Instruction[4];
            TypeFloats_In = new Instruction[4];
            TypeFloats_Out = new Instruction[4];
            TypeFloats_Uniform = new Instruction[4];
            TypeFloats_Private = new Instruction[4];

            TypeFloat = new OpTypeFloat(32);
            TypeFloats[0] = TypeFloat;

            for (int i = 0; i < 4; i++)
            {
                if (i > 0)
                {
                    TypeFloats[i] = new OpTypeVector(TypeFloat, i+1);
                }

                TypeFloats_In[i] = new OpTypePointer(StorageClass.Input, TypeFloats[i]);

                TypeFloats_Out[i] = new OpTypePointer(StorageClass.Output, TypeFloats[i]);

                TypeFloats_Uniform[i] = new OpTypePointer(StorageClass.UniformConstant, TypeFloats[i]);

                TypeFloats_Private[i] = new OpTypePointer(StorageClass.Private, TypeFloats[i]);
            }

            TypeImage2D = new OpTypeImage(TypeFloat, Dim.Dim2D, 0, 0, 0, 0, ImageFormat.Unknown);
            TypeSampler2D = new OpTypeSampledImage(TypeImage2D);
            TypeSampler2D_Uniform = new OpTypePointer(StorageClass.UniformConstant, TypeSampler2D);

            //Add them (these do not need to be added safely)
            TypesConstants.Add(TypeVoid);

            TypesConstants.Add(TypeBool);
            TypesConstants.Add(TypeBool_Private);
            TypesConstants.Add(TrueConstant);
            TypesConstants.Add(FalseConstant);

            TypesConstants.Add(TypeInt);
            TypesConstants.Add(TypeUInt);

            for (int i = 0; i < 4; i++)
            {
                TypesConstants.Add(TypeFloats[i]);
                TypesConstants.Add(TypeFloats_In[i]);
                TypesConstants.Add(TypeFloats_Out[i]);
                TypesConstants.Add(TypeFloats_Uniform[i]);
                TypesConstants.Add(TypeFloats_Private[i]);
            }

            TypesConstants.Add(TypeImage2D);
            TypesConstants.Add(TypeSampler2D);
            TypesConstants.Add(TypeSampler2D_Uniform);
        }

        public byte[] Decompile(IGalMemory Memory, long Position, GalShaderType ShaderType)
        {
            Blocks = ShaderDecoder.Decode(Memory, Position);

            Decl = new GlslDecl(Blocks, ShaderType);

            BuildBuiltIns();
            BuildTextures();
            BuildUniforms();
            BuildInAttributes();
            BuildOutAttributes();
            BuildGprs();
            BuildDeclPreds();
            BuildMain();
            BuildCode();

            PrintHeader();
            PrintEntryPoint();
            
            Assembler.Add(Names);
            Assembler.Add(Decorates);
            Assembler.Add(TypesConstants);
            Assembler.Add(VarsDeclaration);
            Assembler.Add(Code);

            //Temp code ahead
            var Stream = new MemoryStream();
            Assembler.Write(Stream);
            byte[] Bytecode = Stream.ToArray();
            Stream.Close();
            return Bytecode;
        }

        private void BuildBuiltIns()
        {
            switch (Decl.ShaderType)
            {
                case GalShaderType.Vertex:

                    Instruction One = AllocConstant(TypeUInt, new LiteralNumber((int)1));
                    Instruction ArrFloatUInt1 = new OpTypeArray(TypeFloat, One);

                    Instruction PerVertex = new OpTypeStruct(TypeFloats[3], TypeFloat, ArrFloatUInt1, ArrFloatUInt1);

                    Instruction OutputPerVertex = new OpTypePointer(StorageClass.Output, PerVertex);

                    Decorates.Add(new OpMemberDecorate(PerVertex, 0, Decoration.BuiltIn, BuiltIn.Position));
                    Decorates.Add(new OpMemberDecorate(PerVertex, 1, Decoration.BuiltIn, BuiltIn.PointSize));
                    Decorates.Add(new OpMemberDecorate(PerVertex, 2, Decoration.BuiltIn, BuiltIn.ClipDistance));
                    Decorates.Add(new OpMemberDecorate(PerVertex, 3, Decoration.BuiltIn, BuiltIn.CullDistance));

                    Decorates.Add(new OpDecorate(PerVertex, Decoration.Block));

                    TypesConstants.Add(ArrFloatUInt1);
                    TypesConstants.Add(PerVertex);
                    TypesConstants.Add(OutputPerVertex);

                    PerVertexVar = new OpVariable(OutputPerVertex, StorageClass.Output);
                    VarsDeclaration.Add(PerVertexVar);

                    Names.Add(new OpName(PerVertex, "gl_PerVertex"));

                    break;
            }
        }

        private void BuildTextures()
        {
            foreach (ShaderDeclInfo DeclInfo in Decl.Textures.Values)
            {
                Instruction Variable = AllocLocatedVariable(
                    TypeSampler2D_Uniform,
                    StorageClass.UniformConstant,
                    DeclInfo.Name,
                    AllocUniformLocation);
                
                //TODO What is a "DescriptorSet"? It sounds like something from Vulkan
                Decorates.Add(new OpDecorate(Variable, Decoration.DescriptorSet, new LiteralNumber(0)));
            }
        }

        private void BuildUniforms()
        {
            if (Decl.ShaderType == GalShaderType.Vertex)
            {
                Instruction Float2 = TypeFloats_Uniform[1];
                AllocLocatedVariable(
                    Float2,
                    StorageClass.UniformConstant,
                    GalConsts.FlipUniformName,
                    AllocUniformLocation);
            }

            foreach (ShaderDeclInfo DeclInfo in Decl.Uniforms.Values)
            {
                Instruction ElemType = TypeFloats[DeclInfo.Size - 1];

                Operand Size = new LiteralNumber(DeclInfo.Index + 1);
                Instruction Constant = AllocConstant(TypeInt, Size);

                Instruction ArrayType = AllocType(new OpTypeArray(ElemType, Constant));
                Instruction ArrayTypeUniform = AllocType(new OpTypePointer(StorageClass.UniformConstant, ArrayType));

                AllocLocatedVariable(
                    ArrayTypeUniform,
                    StorageClass.UniformConstant,
                    DeclInfo.Name,
                    AllocUniformLocation);
            }
        }

        private void BuildInAttributes()
        {
            if (Decl.ShaderType == GalShaderType.Fragment)
            {
                AllocLocatedVariable(
                    TypeFloats_In[3],
                    StorageClass.Input,
                    GlslDecl.PositionOutAttrName,
                    AllocInAttributeLocation);
            }

            BuildAttributes(
                Decl.InAttributes.Values,
                TypeFloats_In,
                StorageClass.Input,
                AllocInAttributeLocation);
        }

        private void BuildOutAttributes()
        {
            if (Decl.ShaderType == GalShaderType.Vertex)
            {
                AllocLocatedVariable(
                    TypeFloats_In[3],
                    StorageClass.Output,
                    GlslDecl.PositionOutAttrName,
                    AllocOutAttributeLocation);
            }

            BuildAttributes(
                Decl.OutAttributes.Values,
                TypeFloats_Out,
                StorageClass.Output,
                AllocOutAttributeLocation);
        }

        private void BuildGprs()
        {
            foreach (ShaderDeclInfo DeclInfo in Decl.Gprs.Values)
            {
                Instruction Type = TypeFloats_Private[DeclInfo.Size - 1];

                AllocVariable(Type, StorageClass.Private, DeclInfo.Name);
            }
        }

        private void BuildDeclPreds()
        {
            foreach (ShaderDeclInfo DeclInfo in Decl.Preds.Values)
            {
                AllocVariable(TypeBool_Private, StorageClass.Private, DeclInfo.Name);
            }
        }

        private void BuildAttributes(
            IEnumerable<ShaderDeclInfo> Decls,
            Instruction[] TypeFloats_InOut,
            StorageClass StorageClass,
            LocationAllocator Allocator)
        {
            foreach (ShaderDeclInfo DeclInfo in Decls)
            {
                if (DeclInfo.Index >= 0)
                {
                    Instruction Type = TypeFloats_InOut[DeclInfo.Size - 1];

                    AllocLocatedVariable(Type, StorageClass, DeclInfo.Name, Allocator);
                }
            }
        }

        private void BuildMain()
        {
            Instruction TypeFunction = AllocType(new OpTypeFunction(TypeVoid));

            Main = new OpFunction(TypeVoid, FunctionControl.None, TypeFunction);
        }

        private void BuildCode()
        {
            Code.Add(Main);

            // First label is implicit when building first block

            Dictionary<ShaderIrBlock, Instruction> Labels = new Dictionary<ShaderIrBlock, Instruction>();

            foreach (ShaderIrBlock Block in Blocks)
            {
                Labels[Block] = new OpLabel();
            }

            Instruction EndLabel = new OpLabel();

            for (int BlockIndex = 0; BlockIndex < Blocks.Length; BlockIndex++)
            {
                BuildBlock(BlockIndex, Labels, EndLabel);
            }

            Code.Add(EndLabel);

            //TODO
            //SB.AppendLine(Identation + "gl_Position.xy *= flip;");

            //SB.AppendLine(Identation + GlslDecl.PositionOutAttrName + " = gl_Position;");
            //SB.AppendLine(Identation + GlslDecl.PositionOutAttrName + ".w = 1;");

            Code.Add(new OpReturn());

            Code.Add(new OpFunctionEnd());
        }

        private void BuildBlock(
            int BlockIndex,
            Dictionary<ShaderIrBlock, Instruction> Labels,
            Instruction EndLabel)
        {
            ShaderIrBlock Block = Blocks[BlockIndex];

            Code.Add(Labels[Block]);

            bool HasBranchTail = BuildNodes(Block, Block.Nodes, Labels, EndLabel);

            // No unconditional branch instruction found. Branch to next block
            if (!HasBranchTail)
            {
                Instruction Label;

                if (Block.Next != null)
                {
                    Label = Labels[Block.Next];
                }
                else if (BlockIndex + 1 < Blocks.Length)
                {
                    ShaderIrBlock NextBlock = Blocks[BlockIndex + 1];

                    Label = Labels[NextBlock];
                }
                else
                {
                    Label = EndLabel;
                }

                Code.Add(new OpBranch(Label));
            }
        }

        private bool BuildNodes(
            ShaderIrBlock Block,
            List<ShaderIrNode> Nodes,
            Dictionary<ShaderIrBlock, Instruction> Labels,
            Instruction EndLabel)
        {
            foreach (ShaderIrNode Node in Nodes)
            {
                if (Node is ShaderIrCond Cond)
                {
                    Instruction CondExpr = GetSrcExpr(Cond.Pred, true);

                    if (Cond.Not)
                    {
                        CondExpr = new OpLogicalNot(TypeBool, CondExpr);
                        Code.Add(CondExpr);
                    }
                    
                    if (Cond.Child is ShaderIrOp Op && Op.Inst == ShaderIrInst.Bra)
                    {
                        Instruction BranchLabel = Labels[Block.Branch];

                        Instruction SkipLabel = new OpLabel();

                        Instruction Branch = new OpBranchConditional(CondExpr, BranchLabel, SkipLabel);

                        Code.Add(Branch);

                        Code.Add(SkipLabel);
                    }
                    else
                    {
                        Instruction ExecuteLabel = new OpLabel();

                        Instruction SkipLabel = new OpLabel();

                        Instruction Branch = new OpBranchConditional(CondExpr, ExecuteLabel, SkipLabel);

                        Code.Add(Branch);

                        Code.Add(ExecuteLabel);

                        List<ShaderIrNode> ChildList = new List<ShaderIrNode>();
                        ChildList.Add(Cond.Child);

                        bool HasBranchTail = BuildNodes(Block, ChildList, Labels, EndLabel);

                        if (!HasBranchTail)
                        {
                            Code.Add(new OpBranch(SkipLabel));
                        }

                        Code.Add(SkipLabel);
                    }
                }
                else if (Node is ShaderIrAsg Asg)
                {
                    if (IsValidOutOper(Asg.Dst))
                    {
                        Instruction SrcExpr = GetSrcExpr(Asg.Src, true);

                        Instruction Expr = GetExprWithCast(Asg.Dst, Asg.Src, SrcExpr);

                        Instruction Target = GetDstOperName(Asg.Dst);

                        Code.Add(new OpStore(Target, Expr));
                    }
                }
                else if (Node is ShaderIrOp Op)
                {
                    if (Op.Inst == ShaderIrInst.Bra)
                    {
                        Instruction BranchLabel = Labels[Block.Branch];

                        Code.Add(new OpBranch(BranchLabel));

                        //Unconditional branch found, ignore following nodes in this hierarchy
                        return true;
                    }
                    else if (Op.Inst == ShaderIrInst.Exit)
                    {
                        Code.Add(new OpBranch(EndLabel));

                        //Ignore following nodes, same as ^
                        return true;
                    }
                    else
                    {
                        Instruction Operation = GetSrcExpr(Op, true);
                    }
                }
                else if (Node is ShaderIrCmnt Comment)
                {
                    // Couldn't find a commentary OpCode in Spirv, for now just ignore it
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return false;
        }

        private Instruction GetExprWithCast(ShaderIrNode Dst, ShaderIrNode Src, Instruction Expr)
        {
            //Note: The "DstType" (of the cast) is the type that the operation
            //uses on the source operands, while the "SrcType" is the destination
            //type of the operand result (if it is a operation) or just the type
            //of the variable for registers/uniforms/attributes.
            OperType DstType = GetSrcNodeType(Dst);
            OperType SrcType = GetDstNodeType(Src);

            if (DstType != SrcType)
            {
                //Check for invalid casts
                //(like bool to int/float and others).
                if (SrcType != OperType.F32 &&
                    SrcType != OperType.I32)
                {
                    throw new InvalidOperationException();
                }

                switch (Src)
                {
                    case ShaderIrOperGpr Gpr:
                    {
                        //When the Gpr is ZR, just return the 0 value directly,
                        //since the float encoding for 0 is 0.
                        if (Gpr.IsConst)
                        {
                            return AllocConstant(TypeFloat, new LiteralNumber(0f));
                        }
                        break;
                    }

                    case ShaderIrOperImm Imm:
                    {
                        if (DstType == OperType.F32)
                        {
                            float Value = BitConverter.Int32BitsToSingle(Imm.Value);

                            if (!float.IsNaN(Value) && !float.IsInfinity(Value))
                            {
                                return AllocConstant(TypeFloat, new LiteralNumber(Value));
                            }
                        }
                        break;
                    }
                }

                switch (DstType)
                {
                    case OperType.F32:
                        return new OpBitcast(TypeFloat, Expr);
                    
                    case OperType.I32:
                        return new OpBitcast(TypeInt, Expr);
                }
            }

            return Expr;
        }

        private static OperType GetDstNodeType(ShaderIrNode Node)
        {
            //Special case instructions with the result type different
            //from the input types (like integer <-> float conversion) here.
            if (Node is ShaderIrOp Op)
            {
                switch (Op.Inst)
                {
                    case ShaderIrInst.Stof:
                    case ShaderIrInst.Txlf:
                    case ShaderIrInst.Utof:
                        return OperType.F32;

                    case ShaderIrInst.Ftos:
                    case ShaderIrInst.Ftou:
                        return OperType.I32;
                }
            }

            return GetSrcNodeType(Node);
        }

        private static OperType GetSrcNodeType(ShaderIrNode Node)
        {
            switch (Node)
            {
                case ShaderIrOperAbuf Abuf:
                    return Abuf.Offs == GlslDecl.VertexIdAttr
                        ? OperType.I32
                        : OperType.F32;

                case ShaderIrOperCbuf Cbuf: return OperType.F32;
                case ShaderIrOperGpr  Gpr:  return OperType.F32;
                case ShaderIrOperImm  Imm:  return OperType.I32;
                case ShaderIrOperImmf Immf: return OperType.F32;
                case ShaderIrOperPred Pred: return OperType.Bool;

                case ShaderIrOp Op:
                    if (Op.Inst > ShaderIrInst.B_Start &&
                        Op.Inst < ShaderIrInst.B_End)
                    {
                        return OperType.Bool;
                    }
                    else if (Op.Inst > ShaderIrInst.F_Start &&
                             Op.Inst < ShaderIrInst.F_End)
                    {
                        return OperType.F32;
                    }
                    else if (Op.Inst > ShaderIrInst.I_Start &&
                             Op.Inst < ShaderIrInst.I_End)
                    {
                        return OperType.I32;
                    }
                    break;
            }

            throw new ArgumentException(nameof(Node));
        }

        private bool IsValidOutOper(ShaderIrNode Node)
        {
            if (Node is ShaderIrOperGpr Gpr && Gpr.IsConst)
            {
                return false;
            }
            else if (Node is ShaderIrOperPred Pred && Pred.IsConst)
            {
                return false;
            }

            return true;
        }

        private Instruction GetDstOperName(ShaderIrNode Node)
        {
            if (Node is ShaderIrOperAbuf Abuf)
            {
                return GetOutAbufName(Abuf);
            }
            else if (Node is ShaderIrOperGpr Gpr)
            {
                return GetName(Gpr, true);
            }
            else if (Node is ShaderIrOperPred Pred)
            {
                return GetName(Pred, true);
            }

            throw new ArgumentException(nameof(Node));
        }

        private Instruction GetOutAbufName(ShaderIrOperAbuf Abuf)
        {
            return GetName(Decl.OutAttributes, Abuf, true);
        }

        private Instruction GetSrcExpr(ShaderIrNode Node, bool Entry = false)
        {
            switch (Node)
            {
                case ShaderIrOperAbuf Abuf: return GetName (Abuf, false);
                case ShaderIrOperCbuf Cbuf: return GetName (Cbuf, false);
                case ShaderIrOperGpr  Gpr:  return GetName (Gpr, false);
                case ShaderIrOperImm  Imm:  return GetValue(Imm);
                case ShaderIrOperImmf Immf: return GetValue(Immf);
                case ShaderIrOperPred Pred: return GetName (Pred, false);

                case ShaderIrOp Op:
                    Instruction Expr;

                    if (InstsExpr.TryGetValue(Op.Inst, out GetInstExpr GetExpr))
                    {
                        Expr = GetExpr(Op);
                    }
                    else
                    {
                        throw new NotImplementedException(Op.Inst.ToString());
                    }

                    //GetExpr does not add it to Code
                    Code.Add(Expr);

                    return Expr;
                
                default: throw new ArgumentException(nameof(Node));
            }
        }

        private Instruction GetName(ShaderIrOperAbuf Abuf, bool Pointer)
        {
            if (Decl.ShaderType == GalShaderType.Vertex)
            {
                switch (Abuf.Offs)
                {
                    case GlslDecl.VertexIdAttr:   return GetVariableValue(TypeInt, "gl_VertexID", Pointer);
                    case GlslDecl.InstanceIdAttr: return GetVariableValue(TypeInt, "gl_InstanceID", Pointer);
                }
            }
            else if (Decl.ShaderType == GalShaderType.TessEvaluation)
            {
                switch (Abuf.Offs)
                {
                    case GlslDecl.TessCoordAttrX: return GetVariableValue(TypeFloat, "gl_TessCoord", 0, Pointer);
                    case GlslDecl.TessCoordAttrY: return GetVariableValue(TypeFloat, "gl_TessCoord", 1, Pointer);
                    case GlslDecl.TessCoordAttrZ: return GetVariableValue(TypeFloat, "gl_TessCoord", 2, Pointer);
                }
            }

            return GetName(Decl.InAttributes, Abuf, Pointer);
        }

        private Instruction GetName(IReadOnlyDictionary<int, ShaderDeclInfo> Dict, ShaderIrOperAbuf Abuf, bool Pointer)
        {
            int Index =  Abuf.Offs >> 4;
            int Elem  = (Abuf.Offs >> 2) & 3;

            if (!Dict.TryGetValue(Index, out ShaderDeclInfo DeclInfo))
            {
                throw new InvalidOperationException();
            }

            //Guess types are float-based
            if (DeclInfo.Size > 1)
            {
                return GetVariableValue(TypeFloat, DeclInfo.Name, Elem, Pointer);
            }
            else
            {
                return GetVariableValue(TypeFloat, DeclInfo.Name, Pointer);
            }
        }

        private Instruction GetName(ShaderIrOperGpr Gpr, bool Pointer)
        {
            //Gprs are always float, right?

            if (Gpr.IsConst)
            {
                if (Pointer)
                {
                    throw new InvalidOperationException("Can't return pointer to a constant");
                }

                return AllocConstant(TypeFloat, new LiteralNumber(0f));
            }
            else
            {
                return GetNameWithSwizzle(TypeFloat, Decl.Gprs, Gpr.Index, Pointer);
            }
        }

        private Instruction GetValue(ShaderIrOperImm Imm)
        {
            return AllocConstant(TypeInt, new LiteralNumber(Imm.Value));
        }

        private Instruction GetValue(ShaderIrOperImmf Immf)
        {
            return AllocConstant(TypeFloat, new LiteralNumber(Immf.Value));
        }

        private Instruction GetName(ShaderIrOperPred Pred, bool Pointer)
        {
            if (Pred.IsConst)
            {
                return TrueConstant;
            }
            else
            {
                return GetNameWithSwizzle(TypeBool, Decl.Preds, Pred.Index, Pointer);
            }
        }

        private Instruction GetName(ShaderIrOperCbuf Cbuf, bool Pointer)
        {
            if (!Decl.Uniforms.TryGetValue(Cbuf.Index, out ShaderDeclInfo DeclInfo))
            {
                throw new InvalidOperationException();
            }

            Instruction PosConstant = AllocConstant(TypeInt, new LiteralNumber(Cbuf.Pos));

            if (Cbuf.Offs != null)
            {
                //Note: We assume that the register value is always a multiple of 4.
                //This may not be always the case.

                Instruction ShiftConstant = AllocConstant(TypeInt, new LiteralNumber(2));

                Instruction Source = GetSrcExpr(Cbuf.Offs);

                Instruction Casted = new OpBitcast(TypeInt, Source);

                Instruction Offset = new OpShiftRightLogical(TypeInt, Casted, ShiftConstant);

                Instruction Index = new OpIAdd(TypeInt, PosConstant, Offset);

                Code.Add(Source);
                Code.Add(Casted);
                Code.Add(Offset);
                Code.Add(Index);

                return GetVariableValue(TypeFloat, DeclInfo.Name, Index, Pointer);
            }
            else
            {
                return GetVariableValue(TypeFloat, DeclInfo.Name, PosConstant, Pointer);
            }
        }

        private Instruction GetNameWithSwizzle(
            Instruction Type,
            IReadOnlyDictionary<int, ShaderDeclInfo> Dict,
            int Index,
            bool Pointer)
        {
            int VecIndex = Index >> 2;

            if (Dict.TryGetValue(VecIndex, out ShaderDeclInfo DeclInfo))
            {
                if (DeclInfo.Size > 1 && Index < VecIndex + DeclInfo.Size)
                {
                    return GetVariableValue(Type, DeclInfo.Name, Index & 3, Pointer);
                }
            }

            if (!Dict.TryGetValue(Index, out DeclInfo))
            {
                throw new InvalidOperationException();
            }

            return GetVariableValue(Type, DeclInfo.Name, Pointer);
        }

        private void PrintHeader()
        {
            Assembler.Add(new OpCapability(Capability.Shader));

            Assembler.Add(GlslExtension);

            Assembler.Add(new OpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450));
        }

        private void PrintTypes()
        {
            Assembler.Add(TypesConstants);
        }

        private void PrintEntryPoint()
        {
            List<Instruction> Interface = new List<Instruction>();

            foreach (SpirvVariable Variable in Variables)
            {
                if (Variable.Storage == StorageClass.Input
                    || Variable.Storage == StorageClass.Output)
                {
                    Interface.Add(Variable.Id);
                }
            }

            Assembler.Add(new OpEntryPoint(
                GetExecutionModel(),
                Main,
                "main",
                Interface.ToArray()));
        }

        private ExecutionModel GetExecutionModel()
        {
            switch (Decl.ShaderType)
            {
                case GalShaderType.Vertex:         return ExecutionModel.Vertex;
                case GalShaderType.TessControl:    return ExecutionModel.TessellationControl;
                case GalShaderType.TessEvaluation: return ExecutionModel.TessellationEvaluation;
                case GalShaderType.Geometry:       return ExecutionModel.Geometry;
                case GalShaderType.Fragment:       return ExecutionModel.Fragment;

                default:
                    throw new InvalidOperationException();
            }
        }

        private Instruction AllocConstant(Instruction Type, Operand Value)
        {
            Instruction NewConstant = new OpConstant(Type, Value);

            foreach (Instruction Constant in TypesConstants)
            {
                if (Constant.Equals(NewConstant))
                {
                    return Constant;
                }
            }

            TypesConstants.Add(NewConstant);

            return NewConstant;
        }

        private Instruction GetVariableValue(Instruction ResultType, string Name, bool Pointer)
        {
            SpirvVariable Variable = GetVariable(Name);

            if (Pointer)
            {
                return Variable.Id;
            }

            Instruction Value = new OpLoad(ResultType, Variable.Id);

            Code.Add(Value);

            return Value;
        }

        private Instruction GetVariableValue(Instruction ResultType, string Name, int Index, bool Pointer)
        {
            Instruction InstIndex = AllocConstant(TypeInt, new LiteralNumber(Index));

            return GetVariableValue(ResultType, Name, InstIndex, Pointer);
        }

        private Instruction GetVariableValue(Instruction ResultType, string Name, Instruction Index, bool Pointer)
        {
            OpTypePointer AccessTypePointer;
            OpAccessChain Component;

            switch (Name)
            {
                case "gl_Position":

                    Instruction PositionIndex = AllocConstant(TypeInt, new LiteralNumber((int)0));

                    AccessTypePointer = (OpTypePointer)AllocType(new OpTypePointer(StorageClass.Output, ResultType));

                    Component = new OpAccessChain(AccessTypePointer, PerVertexVar, PositionIndex, Index);

                    break;

                default:

                    SpirvVariable Base = GetVariable(Name);

                    AccessTypePointer = (OpTypePointer)AllocType(new OpTypePointer(Base.Storage, ResultType));

                    Component = new OpAccessChain(AccessTypePointer, Base.Id, Index);

                    break;
            }

            Code.Add(Component);

            if (Pointer)
            {
                return Component;
            }
            else
            {
                Instruction Value = new OpLoad(ResultType, Component);

                Code.Add(Value);

                return Value;
            }
        }

        private SpirvVariable GetVariable(string Name)
        {
            foreach (SpirvVariable Variable in Variables)
            {
                if (Variable.Name == Name)
                {
                    return Variable;
                }
            }

            throw new InvalidOperationException($"Variable {Name} not declared");
        }

        private Instruction AllocVariable(
            Instruction Type,
            StorageClass StorageClass,
            string Name,
            int Location = -1)
        {
            Instruction InstVariable = new OpVariable(Type, StorageClass);
            VarsDeclaration.Add(InstVariable);

            Names.Add(new OpName(InstVariable, Name));

            if (Location >= 0)
            {
                Operand Literal = new LiteralNumber(Location);
                Decorates.Add(new OpDecorate(InstVariable, Decoration.Location, Literal));
            }

            SpirvVariable Variable = new SpirvVariable();
            Variable.Id = InstVariable;
            Variable.Storage = StorageClass;
            Variable.Name = Name;
            Variable.Location = Location;

            Variables.Add(Variable);

            return InstVariable;
        }

        private Instruction AllocLocatedVariable(
            Instruction Type,
            StorageClass StorageClass,
            string Name,
            LocationAllocator Allocator)
        {
            return AllocVariable(Type, StorageClass, Name, Allocator());
        }

        private Instruction AllocType(Instruction NewType)
        {
            foreach (Instruction StoredType in TypesConstants)
            {
                if (StoredType.Equals(NewType))
                {
                    return StoredType;
                }
            }

            TypesConstants.Add(NewType);

            return NewType;
        }

        private int AllocInAttributeLocation()
        {
            InAttributeCount += 1;
            return InAttributeCount - 1;
        }

        private int AllocOutAttributeLocation()
        {
            OutAttributeCount += 1;
            return OutAttributeCount - 1;
        }

        private int AllocUniformLocation()
        {
            UniformCount += 1;
            return UniformCount - 1;
        }
    }
}