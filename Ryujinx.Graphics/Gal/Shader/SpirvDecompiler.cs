using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Ryujinx.Graphics.Gal.Shader.SPIRV;

namespace Ryujinx.Graphics.Gal.Shader
{
    public class SpirvDecompiler : ShaderDecompiler
    {
        #region "Declarations"

        private class SpirvVariable
        {
            public Instruction Id;
            public StorageClass Storage;
            public string Name;
            public int Location = -1;
        }

        private const int ShaderStageSafespaceStride = 1024 / 5;

        private delegate Instruction GetInstExpr(ShaderIrOp Op);

        private delegate Instruction OpCompareBuilder(Instruction A, Instruction B);

        private Dictionary<ShaderIrInst, GetInstExpr> InstsExpr;

        private GlslDecl Decl;

        private ShaderIrBlock[] Blocks;

        private Assembler Assembler;

        private Instruction GlslExtension;
        private GLSLstd450Builder Glsl450;

        private int UniformLocation = 0;

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

        private Dictionary<string, int> Locations;

        private Instruction Main;

        private Instruction PerVertex = null;
        private Instruction VertexID = null;
        private Instruction InstanceID = null;

        private Instruction TypeVoid;

        private Instruction  TypeBool, TypeBool_Private;

        private Instruction TypeInt, TypeUInt, TypeInt2;

        private Instruction TypeFloat;

        private Instruction[] TypeFloats, TypeFloats_In, TypeFloats_Out,
            TypeFloats_Uniform, TypeFloats_UniformConst, TypeFloats_Private;

        private Instruction TypeImage2D, TypeSampler2D, TypeSampler2D_Uniform;

        private Instruction TrueConstant, FalseConstant;

        #endregion

        public SpirvDecompiler()
        {
            InstsExpr = new Dictionary<ShaderIrInst, GetInstExpr>()
            {
                { ShaderIrInst.Abs,    GetAbsExpr    },
                { ShaderIrInst.Add,    GetAddExpr    },
                { ShaderIrInst.And,    GetAndExpr    },
                { ShaderIrInst.Asr,    GetAsrExpr    },
                { ShaderIrInst.Band,   GetBandExpr   },
                { ShaderIrInst.Bnot,   GetBnotExpr   },
                { ShaderIrInst.Bor,    GetBorExpr    },
                { ShaderIrInst.Bxor,   GetBxorExpr   },
                { ShaderIrInst.Ceil,   GetCeilExpr   },
                { ShaderIrInst.Ceq,    GetCeqExpr    },
                { ShaderIrInst.Cge,    GetCgeExpr    },
                { ShaderIrInst.Cgt,    GetCgtExpr    },
                { ShaderIrInst.Clamps, GetClampsExpr },
                { ShaderIrInst.Clampu, GetClampuExpr },
                { ShaderIrInst.Cle,    GetCleExpr    },
                { ShaderIrInst.Clt,    GetCltExpr    },
                { ShaderIrInst.Cne,    GetCneExpr    },
                { ShaderIrInst.Fabs,   GetFabsExpr   },
                { ShaderIrInst.Fadd,   GetFaddExpr   },
                { ShaderIrInst.Fceq,   GetFceqExpr   },
                { ShaderIrInst.Fcequ,  GetFcequExpr  },
                { ShaderIrInst.Fcge,   GetFcgeExpr   },
                { ShaderIrInst.Fcgeu,  GetFcgeuExpr  },
                { ShaderIrInst.Fcgt,   GetFcgtExpr   },
                { ShaderIrInst.Fcgtu,  GetFcgtuExpr  },
                { ShaderIrInst.Fclamp, GetFclampExpr },
                { ShaderIrInst.Fcle,   GetFcleExpr   },
                { ShaderIrInst.Fcleu,  GetFcleuExpr  },
                { ShaderIrInst.Fclt,   GetFcltExpr   },
                { ShaderIrInst.Fcltu,  GetFcltuExpr  },
                { ShaderIrInst.Fcnan,  GetFcnanExpr  },
                { ShaderIrInst.Fcne,   GetFcneExpr   },
                { ShaderIrInst.Fcneu,  GetFcneuExpr  },
                { ShaderIrInst.Fcnum,  GetFcnumExpr  },
                { ShaderIrInst.Fcos,   GetFcosExpr   },
                { ShaderIrInst.Fex2,   GetFex2Expr   },
                { ShaderIrInst.Ffma,   GetFfmaExpr   },
                { ShaderIrInst.Flg2,   GetFlg2Expr   },
                { ShaderIrInst.Floor,  GetFloorExpr  },
                { ShaderIrInst.Fmax,   GetFmaxExpr   },
                { ShaderIrInst.Fmin,   GetFminExpr   },
                { ShaderIrInst.Fmul,   GetFmulExpr   },
                { ShaderIrInst.Fneg,   GetFnegExpr   },
                { ShaderIrInst.Frcp,   GetFrcpExpr   },
                { ShaderIrInst.Frsq,   GetFrsqExpr   },
                { ShaderIrInst.Fsin,   GetFsinExpr   },
                { ShaderIrInst.Ftos,   GetFtosExpr   },
                { ShaderIrInst.Ftou,   GetFtouExpr   },
                { ShaderIrInst.Kil,    GetKilExpr    },
                { ShaderIrInst.Lsl,    GetLslExpr    },
                { ShaderIrInst.Lsr,    GetLsrExpr    },
                { ShaderIrInst.Max,    GetMaxExpr    },
                { ShaderIrInst.Min,    GetMinExpr    },
                { ShaderIrInst.Mul,    GetMulExpr    },
                { ShaderIrInst.Neg,    GetNegExpr    },
                { ShaderIrInst.Not,    GetNotExpr    },
                { ShaderIrInst.Or,     GetOrExpr     },
                { ShaderIrInst.Stof,   GetStofExpr   },
                { ShaderIrInst.Sub,    GetSubExpr    },
                { ShaderIrInst.Texq,   GetTexqExpr   },
                { ShaderIrInst.Texs,   GetTexsExpr   },
                { ShaderIrInst.Trunc,  GetTruncExpr  },
                { ShaderIrInst.Txlf,   GetTxlfExpr   },
                { ShaderIrInst.Utof,   GetUtofExpr   },
                { ShaderIrInst.Xor,    GetXorExpr    }
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

            Locations = new Dictionary<string, int>();
        }

        public SpirvProgram Decompile(IGalMemory Memory, long Position, GalShaderType ShaderType)
        {
            Blocks = ShaderDecoder.Decode(Memory, Position);

            Decl = new GlslDecl(Blocks, ShaderType);

            BuildCommonTypes();
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

            using (MemoryStream MS = new MemoryStream())
            {
                Assembler.Write(MS);
                byte[] SpirvBytecode = MS.ToArray();

                return new SpirvProgram(
                    SpirvBytecode,
                    Locations,
                    Decl.Textures.Values,
                    Decl.Uniforms.Values);
            }
        }

        #region "Builders"

        private void BuildCommonTypes()
        {
            Instruction Add(Instruction TypeOrConstant, string Name = "")
            {
                TypesConstants.Add(TypeOrConstant);

                if (Name != "")
                {
                    Names.Add(new OpName(TypeOrConstant, Name));
                }

                return TypeOrConstant;
            }

            TypeVoid = Add(new OpTypeVoid(), "void");

            TypeBool = Add(new OpTypeBool(), "bool");
            TypeBool_Private = Add(new OpTypePointer(StorageClass.Private, TypeBool), "ptrBoolPrivate");

            TrueConstant = Add(new OpConstantTrue(TypeBool), "true");
            FalseConstant = Add(new OpConstantFalse(TypeBool), "false");

            TypeInt = Add(new OpTypeInt(32, true), "int");
            TypeUInt = Add(new OpTypeInt(32, false), "uint");
            TypeInt2 = Add(new OpTypeVector(TypeInt, 2), "i2");

            TypeFloats = new Instruction[4];
            TypeFloats_In = new Instruction[4];
            TypeFloats_Out = new Instruction[4];
            TypeFloats_Uniform = new Instruction[4];
            TypeFloats_UniformConst = new Instruction[4];
            TypeFloats_Private = new Instruction[4];

            TypeFloat = Add(new OpTypeFloat(32), "float");
            TypeFloats[0] = TypeFloat;

            for (int i = 0; i < 4; i++)
            {
                string e = i == 0 ? "" : (i + 1).ToString();

                if (i > 0)
                {
                    TypeFloats[i] = Add(new OpTypeVector(TypeFloat, i+1), "float" + e);
                }

                TypeFloats_In[i] = Add(new OpTypePointer(StorageClass.Input, TypeFloats[i]), "ptrFloatIn" + e);

                TypeFloats_Out[i] = Add(new OpTypePointer(StorageClass.Output, TypeFloats[i]), "ptrFloatOut" + e);

                TypeFloats_Uniform[i] = Add(new OpTypePointer(StorageClass.Uniform, TypeFloats[i]), "ptrFloatUniform" + e);

                TypeFloats_UniformConst[i] =
                    Add(new OpTypePointer(StorageClass.UniformConstant, TypeFloats[i]), "ptrFloatUniformConst" + e);

                TypeFloats_Private[i] = Add(new OpTypePointer(StorageClass.Private, TypeFloats[i]), "ptrFloatPrivate" + e);
            }

            TypeImage2D = Add(new OpTypeImage(TypeFloat, Dim.Dim2D, 0, 0, 0, 0, ImageFormat.Unknown), "image2D");
            TypeSampler2D = Add(new OpTypeSampledImage(TypeImage2D), "sampler2D");
            TypeSampler2D_Uniform = Add(new OpTypePointer(StorageClass.UniformConstant, TypeSampler2D), "pSampler2D");
        }

        private void BuildBuiltIns()
        {
            switch (Decl.ShaderType)
            {
                case GalShaderType.Vertex:

                    //Build gl_PerVertex type and variable
                    Instruction One = AllocConstant(TypeUInt, new LiteralNumber((int)1));
                    Instruction ArrFloatUInt1 = new OpTypeArray(TypeFloat, One);

                    Instruction TypePerVertex = new OpTypeStruct(TypeFloats[3], TypeFloat, ArrFloatUInt1, ArrFloatUInt1);

                    Instruction OutputPerVertex = new OpTypePointer(StorageClass.Output, TypePerVertex);

                    Decorates.Add(new OpMemberDecorate(TypePerVertex, 0, BuiltIn.Position));
                    Decorates.Add(new OpMemberDecorate(TypePerVertex, 1, BuiltIn.PointSize));
                    Decorates.Add(new OpMemberDecorate(TypePerVertex, 2, BuiltIn.ClipDistance));
                    Decorates.Add(new OpMemberDecorate(TypePerVertex, 3, BuiltIn.CullDistance));

                    Decorates.Add(new OpDecorate(TypePerVertex, Decoration.Block));

                    TypesConstants.Add(ArrFloatUInt1);
                    TypesConstants.Add(TypePerVertex);
                    TypesConstants.Add(OutputPerVertex);

                    PerVertex = new OpVariable(OutputPerVertex, StorageClass.Output);
                    VarsDeclaration.Add(PerVertex);

                    //Build gl_VertexID and gl_InstanceID
                    Instruction TypeIntInput = AllocType(new OpTypePointer(StorageClass.Input, TypeInt));

                    VertexID = new OpVariable(TypeIntInput, StorageClass.Input);
                    InstanceID = new OpVariable(TypeIntInput, StorageClass.Input);

                    VarsDeclaration.Add(VertexID);
                    VarsDeclaration.Add(InstanceID);

                    Decorates.Add(new OpDecorate(VertexID, BuiltIn.VertexId));
                    Decorates.Add(new OpDecorate(InstanceID, BuiltIn.InstanceId));

                    Names.Add(new OpName(TypePerVertex, "struct_PerVertex"));
                    Names.Add(new OpName(PerVertex, "gl_PerVertex"));
                    Names.Add(new OpName(VertexID, "gl_VertexID"));
                    Names.Add(new OpName(InstanceID, "gl_InstanceID"));

                    break;
            }
        }

        private void BuildTextures()
        {
            foreach (ShaderDeclInfo DeclInfo in Decl.Textures.Values)
            {
                Instruction Variable = AllocVariable(
                    TypeSampler2D_Uniform,
                    StorageClass.UniformConstant,
                    DeclInfo.Name,
                    AllocUniformLocation(1));
                
                Decorates.Add(new OpDecorate(Variable, Decoration.DescriptorSet, new LiteralNumber(0)));
            }
        }

        private void BuildUniforms()
        {
            if (Decl.ShaderType == GalShaderType.Vertex)
            {
                Instruction Float2 = TypeFloats_UniformConst[1];
                AllocVariable(
                    Float2,
                    StorageClass.UniformConstant,
                    GalConsts.FlipUniformName,
                    AllocUniformLocation(1));
            }
            
            foreach (ShaderDeclInfo DeclInfo in Decl.Uniforms.Values.OrderBy(DeclKeySelector))
            {
                //Create const buffer type
                Operand ArraySize = new LiteralNumber(4096); //16 KiB of floats

                Instruction Array = new OpTypeArray(TypeFloat, AllocConstant(TypeInt, ArraySize));

                Instruction Struct = new OpTypeStruct(Array);

                Instruction StructPointer = new OpTypePointer(StorageClass.Uniform, Struct);

                Instruction Variable = AllocVariable(
                    StructPointer,
                    StorageClass.Uniform,
                    DeclInfo.Name);

                int Binding = UniformBinding.Get(Decl.ShaderType, DeclInfo.Cbuf);

                Decorates.Add(new OpDecorate(Array, Decoration.ArrayStride, new LiteralNumber(4)));
                Decorates.Add(new OpDecorate(Struct, Decoration.Block));
                Decorates.Add(new OpMemberDecorate(Struct, 0, Decoration.Offset, new LiteralNumber(0)));

                Decorates.Add(new OpDecorate(Variable, Decoration.DescriptorSet, new LiteralNumber(0)));
                Decorates.Add(new OpDecorate(Variable, Decoration.Binding, new LiteralNumber(Binding)));

                TypesConstants.Add(Array);
                TypesConstants.Add(Struct);
                TypesConstants.Add(StructPointer);

                //IMPORTANT! nVidia's (propietary) OpenGL driver requires this to be declared
                Names.Add(new OpName(Struct, "BLOCK_" + DeclInfo.Name));
            }
        }

        private void BuildInAttributes()
        {
            if (Decl.ShaderType == GalShaderType.Fragment)
            {
                AllocVariable(
                    TypeFloats_In[3],
                    StorageClass.Input,
                    GlslDecl.PositionOutAttrName,
                    0);
            }

            BuildAttributes(
                Decl.InAttributes.Values,
                TypeFloats_In,
                StorageClass.Input);
        }

        private void BuildOutAttributes()
        {
            if (Decl.ShaderType == GalShaderType.Vertex)
            {
                AllocVariable(
                    TypeFloats_Out[3],
                    StorageClass.Output,
                    GlslDecl.PositionOutAttrName,
                    0);
            }

            BuildAttributes(
                Decl.OutAttributes.Values,
                TypeFloats_Out,
                StorageClass.Output);
        }

        private void BuildGprs()
        {
            foreach (ShaderDeclInfo DeclInfo in Decl.Gprs.Values.OrderBy(DeclKeySelector))
            {
                if (GlslDecl.FragmentOutputName == DeclInfo.Name)
                {
                    Instruction Type = TypeFloats_Out[DeclInfo.Size - 1];

                    AllocVariable(Type, StorageClass.Output, DeclInfo.Name);
                }
                else
                {
                    Instruction Type = TypeFloats_Private[DeclInfo.Size - 1];

                    AllocVariable(Type, StorageClass.Private, DeclInfo.Name);
                }
            }
        }

        private void BuildDeclPreds()
        {
            foreach (ShaderDeclInfo DeclInfo in Decl.Preds.Values.OrderBy(DeclKeySelector))
            {
                AllocVariable(TypeBool_Private, StorageClass.Private, DeclInfo.Name);
            }
        }

        private void BuildAttributes(
            IEnumerable<ShaderDeclInfo> Decls,
            Instruction[] TypeFloats_InOut,
            StorageClass StorageClass)
        {
            foreach (ShaderDeclInfo DeclInfo in Decls.OrderBy(DeclKeySelector))
            {
                if (DeclInfo.Index >= 0)
                {
                    Instruction Type = TypeFloats_InOut[DeclInfo.Size - 1];

                    int Location = DeclInfo.Index + 1;

                    AllocVariable(Type, StorageClass, DeclInfo.Name, Location);
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

            //First label is implicit when building first block

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

            if (Decl.ShaderType == GalShaderType.Vertex)
            {
                BuildVertexFinish();
            }

            Code.Add(new OpReturn());

            Code.Add(new OpFunctionEnd());
        }

        public void BuildVertexFinish()
        {
            //The following code is written from a glslangValidator output for the following GLSL code
            //gl_Position.xy *= flip;
            //position = gl_Position;
            //position.w = 1;

            Instruction Varying = FindVariable(GlslDecl.PositionOutAttrName).Id;

            Instruction FlipXY = GetVariable(TypeFloats[1], GalConsts.FlipUniformName, false);

            Instruction Zero = AllocConstant(TypeInt, new LiteralNumber(0));
            Instruction BuiltInChain = AC(new OpAccessChain(TypeFloats_Out[3], PerVertex, Zero));

            Instruction BuiltInXYZW = AC(new OpLoad(TypeFloats[3], BuiltInChain));

            Instruction BuiltInXY = AC(new OpVectorShuffle(TypeFloats[1], BuiltInXYZW, BuiltInXYZW,
                new LiteralNumber(0), new LiteralNumber(1)));

            Instruction FlippedXY = AC(new OpFMul(TypeFloats[1], BuiltInXY, FlipXY));

            BuiltInChain = AC(new OpAccessChain(TypeFloats_Out[3], PerVertex, Zero));

            BuiltInXYZW = AC(new OpLoad(TypeFloats[3], BuiltInChain));

            Instruction FinalPosition = AC(new OpVectorShuffle(TypeFloats[3], BuiltInXYZW, FlippedXY,
                new LiteralNumber(4), new LiteralNumber(5), new LiteralNumber(2), new LiteralNumber(3)));

            Code.Add(new OpStore(BuiltInChain, FinalPosition));

            BuiltInChain = AC(new OpAccessChain(TypeFloats_Out[3], PerVertex, Zero));

            BuiltInXYZW = AC(new OpLoad(TypeFloats[3], BuiltInChain));

            Code.Add(new OpStore(Varying, BuiltInXYZW));

            Instruction VaryingW = AC(new OpAccessChain(TypeFloats_Out[0], Varying,
                AllocConstant(TypeUInt, new LiteralNumber(3))));

            Code.Add(new OpStore(VaryingW, AllocConstant(TypeFloat, new LiteralNumber(1f))));
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
                        CondExpr = AC(new OpLogicalNot(TypeBool, CondExpr));
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

                        Instruction Target = GetDstOperValue(Asg.Dst);

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

                        if (Operation is OpKill)
                        {
                            return true;
                        }
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

        #endregion

        #region "Printers"

        private void PrintHeader()
        {
            Assembler.Add(new OpCapability(Capability.Shader));

            Assembler.Add(GlslExtension);

            Assembler.Add(new OpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450));
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

            switch (Decl.ShaderType)
            {
                case GalShaderType.Vertex:
                    Interface.Add(PerVertex);
                    Interface.Add(VertexID);
                    Interface.Add(InstanceID);
                    break;

                case GalShaderType.Fragment:
                    Interface.Add(FindVariable(GlslDecl.FragmentOutputName).Id);
                    break;
            }

            Assembler.Add(new OpEntryPoint(
                GetExecutionModel(),
                Main,
                "main",
                Interface.ToArray()));
        }

        #endregion

        #region "Getter instructions"

        private Instruction GetExprWithCast(ShaderIrNode Dst, ShaderIrNode Src, Instruction Expr)
        {
            OperType DstType = GetSrcNodeType(Dst);
            OperType SrcType = GetDstNodeType(Src);

            if (DstType != SrcType)
            {
                if (SrcType != OperType.F32 &&
                    SrcType != OperType.I32)
                {
                    throw new InvalidOperationException();
                }

                switch (Src)
                {
                    case ShaderIrOperGpr Gpr:
                    {
                        if (Gpr.IsConst)
                        {
                            if (DstType == OperType.I32)
                            {
                                return AllocConstant(TypeInt, new LiteralNumber(0));
                            }
                            else
                            {
                                return AllocConstant(TypeFloat, new LiteralNumber(0f));
                            }
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
                        return AC(new OpBitcast(TypeFloat, Expr));
                    
                    case OperType.I32:
                        return AC(new OpBitcast(TypeInt, Expr));
                }
            }

            return Expr;
        }

        private Instruction GetDstOperValue(ShaderIrNode Node)
        {
            if (Node is ShaderIrOperAbuf Abuf)
            {
                return GetOutAbufValue(Abuf);
            }
            else if (Node is ShaderIrOperGpr Gpr)
            {
                return GetValue(Gpr, true);
            }
            else if (Node is ShaderIrOperPred Pred)
            {
                return GetValue(Pred, true);
            }

            throw new ArgumentException(nameof(Node));
        }

        private Instruction GetOutAbufValue(ShaderIrOperAbuf Abuf)
        {
            return GetValue(Decl.OutAttributes, Abuf, true);
        }

        private Instruction GetSrcExpr(ShaderIrNode Node, bool Entry = false)
        {
            switch (Node)
            {
                case ShaderIrOperAbuf Abuf: return GetValue (Abuf, false);
                case ShaderIrOperCbuf Cbuf: return GetValue (Cbuf, false);
                case ShaderIrOperGpr  Gpr:  return GetValue (Gpr, false);
                case ShaderIrOperImm  Imm:  return GetValue(Imm);
                case ShaderIrOperImmf Immf: return GetValue(Immf);
                case ShaderIrOperPred Pred: return GetValue (Pred, false);

                case ShaderIrOp Op:
                    if (Op.Inst == ShaderIrInst.Ipa)
                    {
                        return GetOperExpr(Op, Op.OperandA);
                    }
                    else if (InstsExpr.TryGetValue(Op.Inst, out GetInstExpr GetExpr))
                    {
                        return AC(GetExpr(Op));
                    }
                    else
                    {
                        throw new NotImplementedException(Op.Inst.ToString());
                    }
                
                default: throw new ArgumentException(nameof(Node));
            }
        }

        private Instruction GetValue(ShaderIrOperAbuf Abuf, bool Pointer)
        {
            if (Decl.ShaderType == GalShaderType.Vertex)
            {
                switch (Abuf.Offs)
                {
                    case GlslDecl.VertexIdAttr:   return GetVariable(TypeInt, "gl_VertexID", Pointer);
                    case GlslDecl.InstanceIdAttr: return GetVariable(TypeInt, "gl_InstanceID", Pointer);
                }
            }
            else if (Decl.ShaderType == GalShaderType.TessEvaluation)
            {
                switch (Abuf.Offs)
                {
                    case GlslDecl.TessCoordAttrX: return GetVariable(TypeFloat, "gl_TessCoord", 0, Pointer);
                    case GlslDecl.TessCoordAttrY: return GetVariable(TypeFloat, "gl_TessCoord", 1, Pointer);
                    case GlslDecl.TessCoordAttrZ: return GetVariable(TypeFloat, "gl_TessCoord", 2, Pointer);
                }
            }

            return GetValue(Decl.InAttributes, Abuf, Pointer);
        }

        private Instruction GetValue(IReadOnlyDictionary<int, ShaderDeclInfo> Dict, ShaderIrOperAbuf Abuf, bool Pointer)
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
                return GetVariable(TypeFloat, DeclInfo.Name, Elem, Pointer);
            }
            else
            {
                return GetVariable(TypeFloat, DeclInfo.Name, Pointer);
            }
        }

        private Instruction GetValue(ShaderIrOperGpr Gpr, bool Pointer)
        {
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
                return GetValueWithSwizzle(TypeFloat, Decl.Gprs, Gpr.Index, Pointer);
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

        private Instruction GetValue(ShaderIrOperPred Pred, bool Pointer)
        {
            if (Pred.IsConst)
            {
                return TrueConstant;
            }
            else
            {
                return GetValueWithSwizzle(TypeBool, Decl.Preds, Pred.Index, Pointer);
            }
        }

        private Instruction GetValue(ShaderIrOperCbuf Cbuf, bool Pointer)
        {
            if (!Decl.Uniforms.TryGetValue(Cbuf.Index, out ShaderDeclInfo DeclInfo))
            {
                throw new InvalidOperationException();
            }

            Instruction PosConstant = AllocConstant(TypeInt, new LiteralNumber(Cbuf.Pos));

            Instruction Index;

            if (Cbuf.Offs != null)
            {
                //Note: We assume that the register value is always a multiple of 4.
                //This may not be always the case.

                Instruction ShiftConstant = AllocConstant(TypeInt, new LiteralNumber(2));

                Instruction Source = GetSrcExpr(Cbuf.Offs);

                Instruction Casted = AC(new OpBitcast(TypeInt, Source));

                Instruction Offset = AC(new OpShiftRightLogical(TypeInt, Casted, ShiftConstant));

                Index = AC(new OpIAdd(TypeInt, PosConstant, Offset));
            }
            else
            {
                Index = PosConstant;
            }

            SpirvVariable Variable = FindVariable(DeclInfo.Name);

            Instruction MemberIndex = AllocConstant(TypeInt, new LiteralNumber(0));

            Instruction Access = AC(new OpAccessChain(TypeFloats_Uniform[0], Variable.Id, MemberIndex, Index));

            if (Pointer)
            {
                return Access;
            }
            else
            {
                return AC(new OpLoad(TypeFloat, Access));
            }
        }

        private Instruction GetValueWithSwizzle(
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
                    return GetVariable(Type, DeclInfo.Name, Index & 3, Pointer);
                }
            }

            if (!Dict.TryGetValue(Index, out DeclInfo))
            {
                throw new InvalidOperationException();
            }

            return GetVariable(Type, DeclInfo.Name, Pointer);
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

        //Get scalar / vector
        private Instruction GetVariable(Instruction ResultType, string Name, bool Pointer)
        {
            Instruction Variable;
            switch (Name)
            {
                case "gl_VertexID":
                    Variable = VertexID;
                    break;

                case "gl_InstanceID":
                    Variable = InstanceID;
                    break;

                default:
                    Variable = FindVariable(Name).Id;
                    break;
            }

            if (Pointer)
            {
                return Variable;
            }

            return AC(new OpLoad(ResultType, Variable));
        }

        private Instruction GetVariable(Instruction ResultType, string Name, int Index, bool Pointer)
        {
            Instruction InstIndex = AllocConstant(TypeInt, new LiteralNumber(Index));

            return GetVariable(ResultType, Name, InstIndex, Pointer);
        }

        //Get scalar from a compounds
        private Instruction GetVariable(Instruction ResultType, string Name, Instruction Index, bool Pointer)
        {
            Instruction AccessTypePointer;
            Instruction Component;

            switch (Name)
            {
                case "gl_Position":
                {
                    Instruction MemberIndex = AllocConstant(TypeInt, new LiteralNumber((int)0));

                    AccessTypePointer = AllocType(new OpTypePointer(StorageClass.Output, ResultType));
                    Component = new OpAccessChain(AccessTypePointer, PerVertex, MemberIndex, Index);
                    break;
                }

                default:
                {
                    SpirvVariable Base = FindVariable(Name);

                    AccessTypePointer = AllocType(new OpTypePointer(Base.Storage, ResultType));
                    Component = new OpAccessChain(AccessTypePointer, Base.Id, Index);
                    break;
                }
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

        #endregion

        #region "Allocators"

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

                //Add it to locations dictionary (used for query)
                Locations.Add(Name, Location);
            }

            SpirvVariable Variable = new SpirvVariable();
            Variable.Id = InstVariable;
            Variable.Storage = StorageClass;
            Variable.Name = Name;
            Variable.Location = Location;

            Variables.Add(Variable);

            return InstVariable;
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

        private int AllocUniformLocation(int Count)
        {
            if (UniformLocation + Count >= ShaderStageSafespaceStride)
            {
                throw new NotSupportedException("Too many uniform components");
            }

            int Base = ShaderStageSafespaceStride * GetShaderUniformOffset();
            int Location = Base + UniformLocation;
 
            UniformLocation += Count;

            return Location;
        }

        #endregion

        #region "Helpers"

        private SpirvVariable FindVariable(string Name)
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

        private int GetShaderUniformOffset()
        {
            switch (Decl.ShaderType)
            {
                case GalShaderType.Vertex:         return 0;
                case GalShaderType.TessControl:    return 1;
                case GalShaderType.TessEvaluation: return 2;
                case GalShaderType.Geometry:       return 3;
                case GalShaderType.Fragment:       return 4;
                default:
                    throw new ArgumentException();
            }
        }

        //AC stands for Add Code
        private Instruction AC(Instruction Instruction)
        {
            Code.Add(Instruction);

            return Instruction;
        }

        #endregion

        #region "IrOp Builders"

        private Instruction GetSrcNodeTypeId(ShaderIrNode Op)
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

        private Instruction GetOperExpr(ShaderIrOp Op, ShaderIrNode Oper)
        {
            return GetExprWithCast(Op, Oper, GetSrcExpr(Oper));
        }

        private Instruction GetBinaryExprWithNaN(ShaderIrOp Op, OpCompareBuilder Builder)
        {
            Instruction A = GetOperExpr(Op, Op.OperandA);
            Instruction B = GetOperExpr(Op, Op.OperandB);

            Instruction NanA = AC(new OpIsNan(TypeBool, A));
            Instruction NanB = AC(new OpIsNan(TypeBool, B));

            Instruction IsNan = AC(new OpLogicalOr(TypeBool, NanA, NanB));
            Instruction IsCompared = AC(Builder(A, B));
            
            return new OpLogicalOr(TypeBool, IsNan, IsCompared);
        }

        private Instruction GetTexSamplerVariable(ShaderIrOp Op)
        {
            ShaderIrOperImm Node = (ShaderIrOperImm)Op.OperandC;

            int Handle = ((ShaderIrOperImm)Op.OperandC).Value;

            if (!Decl.Textures.TryGetValue(Handle, out ShaderDeclInfo DeclInfo))
            {
                throw new InvalidOperationException();
            }

            return FindVariable(DeclInfo.Name).Id;
        }

        private Instruction GetTexSamplerImage(ShaderIrOp Op)
        {
            return AC(new OpImage(TypeImage2D, GetTexSamplerVariable(Op)));
        }

        private Instruction GetTexSamplerCoords(ShaderIrOp Op)
        {
            return AC(new OpCompositeConstruct(
                TypeFloats[1],
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB)));
        }

        private Instruction GetITexSamplerCoords(ShaderIrOp Op)
        {
            return AC(new OpCompositeConstruct(
                TypeInt2,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB)));
        }

        private Instruction GetFnegExpr(ShaderIrOp Op)
            => new OpFNegate(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetFaddExpr(ShaderIrOp Op)
            => new OpFAdd(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFmulExpr(ShaderIrOp Op)
            => new OpFMul(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFfmaExpr(ShaderIrOp Op)
            => Glsl450.Fma(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB),
                GetOperExpr(Op, Op.OperandC));

        private Instruction GetAndExpr(ShaderIrOp Op)
            => new OpBitwiseAnd(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetCneExpr(ShaderIrOp Op)
            => new OpINotEqual(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFtouExpr(ShaderIrOp Op)
        {
            Instruction Unsigned = AC(new OpConvertFToU(TypeUInt, GetOperExpr(Op, Op.OperandA)));

            //Cast to int because all variables are handled as signed integers
            return new OpBitcast(TypeInt, Unsigned);
        }

        private Instruction GetFtosExpr(ShaderIrOp Op)
            => new OpConvertFToS(
                TypeInt,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetTruncExpr(ShaderIrOp Op)
            => Glsl450.Trunc(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetAddExpr(ShaderIrOp Op)
            => new OpIAdd(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetLslExpr(ShaderIrOp Op)
            => new OpShiftLeftLogical(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetLsrExpr(ShaderIrOp Op)
            => new OpShiftRightLogical(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));
            
        private Instruction GetCeqExpr(ShaderIrOp Op)
            => new OpIEqual(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetAbsExpr(ShaderIrOp Op)
            => Glsl450.SAbs(
                TypeInt,
                GetOperExpr(Op, Op.OperandA));
        
        private Instruction GetUtofExpr(ShaderIrOp Op)
        {
            Instruction Unsigned = AC(new OpBitcast(TypeUInt, GetOperExpr(Op, Op.OperandA)));

            return new OpConvertUToF(TypeFloat, Unsigned);
        }

        private Instruction GetStofExpr(ShaderIrOp Op)
            => new OpConvertSToF(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetXorExpr(ShaderIrOp Op)
            => new OpBitwiseXor(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetNegExpr(ShaderIrOp Op)
            => new OpSNegate(
                TypeInt,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetAsrExpr(ShaderIrOp Op)
            => new OpShiftRightArithmetic(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetBandExpr(ShaderIrOp Op)
            => new OpLogicalAnd(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));
        
        private Instruction GetBnotExpr(ShaderIrOp Op)
            => new OpLogicalNot(
                TypeBool,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetBorExpr(ShaderIrOp Op)
            => new OpLogicalOr(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetBxorExpr(ShaderIrOp Op)
            => new OpLogicalNotEqual(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetCeilExpr(ShaderIrOp Op)
            => Glsl450.Ceil(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetCgeExpr(ShaderIrOp Op)
            => new OpSGreaterThan(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetCgtExpr(ShaderIrOp Op)
            => new OpSGreaterThanEqual(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetClampsExpr(ShaderIrOp Op)
            => Glsl450.SClamp(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB),
                GetOperExpr(Op, Op.OperandC));

        private Instruction GetClampuExpr(ShaderIrOp Op)
        {
            Instruction X = AC(new OpBitcast(TypeUInt, GetOperExpr(Op, Op.OperandA)));
            Instruction MinVal = AC(new OpBitcast(TypeUInt, GetOperExpr(Op, Op.OperandB)));
            Instruction MaxVal = AC(new OpBitcast(TypeUInt, GetOperExpr(Op, Op.OperandC)));
            Instruction Result = AC(Glsl450.UClamp(TypeUInt, X, MinVal, MaxVal));

            //There are no variables uint, so cast it to int
            return new OpBitcast(TypeInt, Result);
        }

        private Instruction GetCleExpr(ShaderIrOp Op)
            => new OpSLessThanEqual(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetCltExpr(ShaderIrOp Op)
            => new OpSLessThan(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFabsExpr(ShaderIrOp Op)
            => Glsl450.FAbs(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetFceqExpr(ShaderIrOp Op)
            => new OpFOrdEqual(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFcequExpr(ShaderIrOp Op)
        {
            Instruction Compare(Instruction A, Instruction B)
                => new OpFOrdEqual(TypeBool, A, B);

            return GetBinaryExprWithNaN(Op, Compare);
        }

        private Instruction GetFcgeExpr(ShaderIrOp Op)
            => new OpFOrdGreaterThanEqual(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFcgeuExpr(ShaderIrOp Op)
        {
            Instruction Compare(Instruction A, Instruction B)
                => new OpFOrdGreaterThanEqual(TypeBool, A, B);

            return GetBinaryExprWithNaN(Op, Compare);
        }

        private Instruction GetFcgtExpr(ShaderIrOp Op)
            => new OpFOrdGreaterThan(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFcgtuExpr(ShaderIrOp Op)
        {
            Instruction Compare(Instruction A, Instruction B)
                => new OpFOrdGreaterThan(TypeBool, A, B);

            return GetBinaryExprWithNaN(Op, Compare);
        }

        private Instruction GetFclampExpr(ShaderIrOp Op)
            => Glsl450.FClamp(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB),
                GetOperExpr(Op, Op.OperandC));

        private Instruction GetFcleExpr(ShaderIrOp Op)
            => new OpFOrdLessThanEqual(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));
        
        private Instruction GetFcleuExpr(ShaderIrOp Op)
        {
            Instruction Compare(Instruction A, Instruction B)
                => new OpFOrdLessThanEqual(TypeBool, A, B);

            return GetBinaryExprWithNaN(Op, Compare);
        }

        private Instruction GetFcltExpr(ShaderIrOp Op)
            => new OpFOrdLessThan(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFcltuExpr(ShaderIrOp Op)
        {
            Instruction Compare(Instruction A, Instruction B)
                => new OpFOrdLessThan(TypeBool, A, B);

            return GetBinaryExprWithNaN(Op, Compare);
        }

        private Instruction GetFcnanExpr(ShaderIrOp Op)
            => new OpIsNan(
                TypeBool,
                GetOperExpr(Op, Op.OperandA));
        
        private Instruction GetFcneExpr(ShaderIrOp Op)
            => new OpFOrdNotEqual(
                TypeBool,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFcneuExpr(ShaderIrOp Op)
        {
            Instruction Compare(Instruction A, Instruction B)
                => new OpFOrdNotEqual(TypeBool, A, B);

            return GetBinaryExprWithNaN(Op, Compare);
        }

        private Instruction GetFcnumExpr(ShaderIrOp Op)
            => new OpIsNormal(
                TypeBool,
                GetOperExpr(Op, Op.OperandA));
        
        private Instruction GetFcosExpr(ShaderIrOp Op)
            => Glsl450.Cos(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetFex2Expr(ShaderIrOp Op)
            => Glsl450.Exp2(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetFlg2Expr(ShaderIrOp Op)
            => Glsl450.Log2(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetFloorExpr(ShaderIrOp Op)
            => Glsl450.Floor(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));
        
        private Instruction GetFmaxExpr(ShaderIrOp Op)
            => Glsl450.FMax(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFminExpr(ShaderIrOp Op)
            => Glsl450.FMin(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetFrcpExpr(ShaderIrOp Op)
            => new OpFDiv(
                TypeFloat,
                AllocConstant(TypeFloat, new LiteralNumber(1f)),
                GetOperExpr(Op, Op.OperandA));
        
        private Instruction GetFrsqExpr(ShaderIrOp Op)
            => Glsl450.InverseSqrt(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetFsinExpr(ShaderIrOp Op)
            => Glsl450.Sin(
                TypeFloat,
                GetOperExpr(Op, Op.OperandA));
        
        private Instruction GetKilExpr(ShaderIrOp Op)
            => new OpKill();

        private Instruction GetMaxExpr(ShaderIrOp Op)
            => Glsl450.SMax(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));
        
        private Instruction GetMinExpr(ShaderIrOp Op)
            => Glsl450.SMin(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetMulExpr(ShaderIrOp Op)
            => new OpIMul(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetNotExpr(ShaderIrOp Op)
            => new OpNot(
                TypeInt,
                GetOperExpr(Op, Op.OperandA));

        private Instruction GetOrExpr(ShaderIrOp Op)
            => new OpBitwiseOr(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));

        private Instruction GetSubExpr(ShaderIrOp Op)
            => new OpISub(
                TypeInt,
                GetOperExpr(Op, Op.OperandA),
                GetOperExpr(Op, Op.OperandB));
        
        private Instruction GetTexqExpr(ShaderIrOp Op)
        {
            ShaderIrMetaTexq Meta = (ShaderIrMetaTexq)Op.MetaData;

            int Elem = Meta.Elem;

            if (Meta.Info == ShaderTexqInfo.Dimension)
            {
                Instruction Image = GetTexSamplerImage(Op);

                Instruction Lod = GetOperExpr(Op, Op.OperandA);

                Instruction Size = AC(new OpImageQuerySizeLod(TypeInt2, Image, Lod));

                return new OpCompositeExtract(TypeInt, Size, Meta.Elem);
            }
            else
            {
                throw new NotImplementedException(Meta.Info.ToString());
            }
        }

        private Instruction GetTexsExpr(ShaderIrOp Op)
        {
            ShaderIrMetaTex Meta = (ShaderIrMetaTex)Op.MetaData;

            Instruction SamplerUniform = GetTexSamplerVariable(Op);

            Instruction Sampler = AC(new OpLoad(TypeSampler2D, SamplerUniform));

            Instruction Coords = GetTexSamplerCoords(Op);

            Instruction Color = AC(new OpImageSampleImplicitLod(TypeFloats[3], Sampler, Coords));

            return new OpCompositeExtract(TypeFloat, Color, Meta.Elem);
        }

        private Instruction GetTxlfExpr(ShaderIrOp Op)
        {
            ShaderIrMetaTex Meta = (ShaderIrMetaTex)Op.MetaData;

            Instruction Image = GetTexSamplerImage(Op);

            Instruction Coords = GetITexSamplerCoords(Op);

            Instruction Fetch = AC(new OpImageFetch(TypeFloats[3], Image, Coords));

            return new OpCompositeExtract(TypeFloat, Fetch, Meta.Elem);
        }

        #endregion
    }
}