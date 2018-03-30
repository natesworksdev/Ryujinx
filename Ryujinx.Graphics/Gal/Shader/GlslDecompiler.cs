using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Ryujinx.Graphics.Gal.Shader
{
    class GlslDecompiler
    {
        private delegate string GetInstExpr(ShaderIrOp Op);

        private Dictionary<ShaderIrInst, GetInstExpr> InstsExpr;

        private const string IdentationStr = "    ";

        private static string[] ElemTypes = new string[] { "float", "vec2", "vec3", "vec4" };

        private Dictionary<int, GlslDeclInfo> Textures;

        private Dictionary<(int, int), GlslDeclInfo> Uniforms;

        private Dictionary<int, GlslDeclInfo> InputAttributes;
        private Dictionary<int, GlslDeclInfo> OutputAttributes;

        private Dictionary<int, GlslDeclInfo> Gprs;
        private Dictionary<int, GlslDeclInfo> Preds;

        private const int AttrStartIndex = 8;
        private const int TexStartIndex = 8;

        private const string InputAttrName = "in_attr";
        private const string OutputName    = "out_attr";
        private const string UniformName   = "c";

        private const string GprName     = "gpr";
        private const string PredName    = "pred";
        private const string TextureName = "tex";

        private StringBuilder SB;

        public GlslDecompiler()
        {
            InstsExpr = new Dictionary<ShaderIrInst, GetInstExpr>()
            {
                { ShaderIrInst.Band, GetBandExpr },
                { ShaderIrInst.Bnot, GetBnotExpr },
                { ShaderIrInst.Clt,  GetCltExpr  },
                { ShaderIrInst.Ceq,  GetCeqExpr  },
                { ShaderIrInst.Cle,  GetCleExpr  },
                { ShaderIrInst.Cgt,  GetCgtExpr  },
                { ShaderIrInst.Cne,  GetCneExpr  },
                { ShaderIrInst.Cge,  GetCgeExpr  },
                { ShaderIrInst.Exit, GetExitExpr },
                { ShaderIrInst.Fabs, GetFabsExpr },
                { ShaderIrInst.Fadd, GetFaddExpr },
                { ShaderIrInst.Fcos, GetFcosExpr },
                { ShaderIrInst.Fex2, GetFex2Expr },
                { ShaderIrInst.Ffma, GetFfmaExpr },
                { ShaderIrInst.Flg2, GetFlg2Expr },
                { ShaderIrInst.Fmul, GetFmulExpr },
                { ShaderIrInst.Fneg, GetFnegExpr },
                { ShaderIrInst.Frcp, GetFrcpExpr },
                { ShaderIrInst.Frsq, GetFrsqExpr },
                { ShaderIrInst.Fsin, GetFsinExpr },
                { ShaderIrInst.Ipa,  GetIpaExpr  },
                { ShaderIrInst.Kil,  GetKilExpr  },
                { ShaderIrInst.Texr, GetTexrExpr },
                { ShaderIrInst.Texg, GetTexgExpr },
                { ShaderIrInst.Texb, GetTexbExpr },
                { ShaderIrInst.Texa, GetTexaExpr }
            };
        }

        public GlslProgram Decompile(int[] Code, GalShaderType Type)
        {
            Uniforms = new Dictionary<(int, int), GlslDeclInfo>();

            Textures = new Dictionary<int, GlslDeclInfo>();

            InputAttributes  = new Dictionary<int, GlslDeclInfo>();
            OutputAttributes = new Dictionary<int, GlslDeclInfo>();

            Gprs  = new Dictionary<int, GlslDeclInfo>();
            Preds = new Dictionary<int, GlslDeclInfo>();

            SB = new StringBuilder();

            //FIXME: Only valid for vertex shaders.
            if (Type == GalShaderType.Fragment)
            {
                Gprs.Add(0, new GlslDeclInfo("FragColor", 0, 0, 4));
            }
            else
            {
                OutputAttributes.Add(7, new GlslDeclInfo("gl_Position", -1, 0, 4));
            }

            ShaderIrBlock Block = ShaderDecoder.DecodeBasicBlock(Code, 0, Type);

            ShaderIrNode[] Nodes = Block.GetNodes();

            foreach (ShaderIrNode Node in Nodes)
            {
                Traverse(null, Node);
            }

            SB.AppendLine("#version 430");

            PrintDeclTextures();
            PrintDeclUniforms();
            PrintDeclInAttributes();
            PrintDeclOutAttributes();
            PrintDeclGprs();
            PrintDeclPreds();

            PrintBlockScope("void main()", 1, Nodes);

            GlslProgram Program = new GlslProgram();

            Program.Code = SB.ToString();

            Program.Attributes = InputAttributes.Values.ToArray();

            SB.Clear();

            return Program;
        }

        private void Traverse(ShaderIrNode Parent, ShaderIrNode Node)
        {
            switch (Node)
            {
                case ShaderIrAsg Asg:
                {
                    Traverse(Asg, Asg.Dst);
                    Traverse(Asg, Asg.Src);

                    break;
                }

                case ShaderIrCond Cond:
                {
                    Traverse(Cond, Cond.Pred);
                    Traverse(Cond, Cond.Child);

                    break;
                }

                case ShaderIrOp Op:
                {
                    Traverse(Op, Op.OperandA);
                    Traverse(Op, Op.OperandB);
                    Traverse(Op, Op.OperandC);

                    if (Op.Inst == ShaderIrInst.Texr ||
                        Op.Inst == ShaderIrInst.Texg ||
                        Op.Inst == ShaderIrInst.Texb ||
                        Op.Inst == ShaderIrInst.Texa)
                    {
                        int Handle = ((ShaderIrOperImm)Op.OperandC).Imm;

                        int Index = Handle - TexStartIndex;

                        string Name = $"{TextureName}{Index}";

                        Textures.TryAdd(Handle, new GlslDeclInfo(Name, Index));
                    }
                    break;
                }

                case ShaderIrOperCbuf Cbuf:
                {
                    string Name = $"{UniformName}{Cbuf.Index}_{Cbuf.Offs}";

                    GlslDeclInfo DeclInfo = new GlslDeclInfo(Name, Cbuf.Offs, Cbuf.Index);

                    Uniforms.TryAdd((Cbuf.Index, Cbuf.Offs), DeclInfo);

                    break;
                }

                case ShaderIrOperAbuf Abuf:
                {
                    int Index =  Abuf.Offs >> 4;
                    int Elem  = (Abuf.Offs >> 2) & 3;

                    int GlslIndex = Index - AttrStartIndex;

                    GlslDeclInfo DeclInfo;

                    if (Parent is ShaderIrAsg Asg && Asg.Dst == Node)
                    {
                        if (!OutputAttributes.TryGetValue(Index, out DeclInfo))
                        {
                            DeclInfo = new GlslDeclInfo(OutputName + GlslIndex, GlslIndex);

                            OutputAttributes.Add(Index, DeclInfo);
                        }
                    }
                    else
                    {
                        if (!InputAttributes.TryGetValue(Index, out DeclInfo))
                        {
                            DeclInfo = new GlslDeclInfo(InputAttrName + GlslIndex, GlslIndex);

                            InputAttributes.Add(Index, DeclInfo);
                        }
                    }

                    DeclInfo.Enlarge(Elem + 1);

                    break;
                }

                case ShaderIrOperGpr Gpr:
                {
                    if (!Gpr.IsConst && GetNameWithSwizzle(Gprs, Gpr.Index) == null)
                    {
                        string Name = $"{GprName}{Gpr.Index}";

                        Gprs.TryAdd(Gpr.Index, new GlslDeclInfo(Name, Gpr.Index));
                    }
                    break;
                }

                case ShaderIrOperPred Pred:
                {
                    if (!Pred.IsConst && GetNameWithSwizzle(Preds, Pred.Index) == null)
                    {
                        string Name = $"{PredName}{Pred.Index}";

                        Preds.TryAdd(Pred.Index, new GlslDeclInfo(Name, Pred.Index));
                    }
                    break;
                }
            }
        }

        private void PrintDeclTextures()
        {
            PrintDecls(Textures.Values, "uniform sampler2D");
        }

        private void PrintDeclUniforms()
        {
            foreach (GlslDeclInfo DeclInfo in Uniforms.Values.OrderBy(DeclKeySelector))
            {
                SB.AppendLine($"uniform {GetDecl(DeclInfo)};");
            }

            if (Uniforms.Values.Count > 0)
            {
                SB.AppendLine();
            }
        }

        private void PrintDeclInAttributes()
        {
            PrintDeclAttributes(InputAttributes.Values, "in");
        }

        private void PrintDeclOutAttributes()
        {
            PrintDeclAttributes(OutputAttributes.Values, "out");
        }

        private void PrintDeclAttributes(ICollection<GlslDeclInfo> Decls, string InOut)
        {
            bool PrintNl = false;

            foreach (GlslDeclInfo DeclInfo in Decls.OrderBy(DeclKeySelector))
            {
                if (DeclInfo.Index >= 0)
                {
                    SB.AppendLine($"layout (location = {DeclInfo.Index}) {InOut} {GetDecl(DeclInfo)};");

                    PrintNl = true;
                }
            }

            if (PrintNl)
            {
                SB.AppendLine();
            }
        }

        private void PrintDeclGprs()
        {
            PrintDecls(Gprs.Values);
        }

        private void PrintDeclPreds()
        {
            PrintDecls(Preds.Values, "bool");
        }

        private void PrintDecls(ICollection<GlslDeclInfo> Decls, string CustomType = null)
        {
            foreach (GlslDeclInfo DeclInfo in Decls.OrderBy(DeclKeySelector))
            {
                string Name;

                if (CustomType != null)
                {
                    Name = $"{CustomType} {DeclInfo.Name};";
                }
                else
                {
                    Name = $"{GetDecl(DeclInfo)};";
                }

                SB.AppendLine(Name);
            }

            if (Decls.Count > 0)
            {
                SB.AppendLine();
            }
        }

        private int DeclKeySelector(GlslDeclInfo DeclInfo)
        {
            return DeclInfo.Cbuf << 24 | DeclInfo.Index;
        }

        private string GetDecl(GlslDeclInfo DeclInfo)
        {
            return $"{ElemTypes[DeclInfo.Size - 1]} {DeclInfo.Name}";
        }

        private void PrintBlockScope(string ScopeName, int IdentationLevel, params ShaderIrNode[] Nodes)
        {
            string Identation = string.Empty;

            for (int Index = 0; Index < IdentationLevel - 1; Index++)
            {
                Identation += IdentationStr;
            }

            if (ScopeName != string.Empty)
            {
                ScopeName += " ";
            }

            SB.AppendLine(Identation + ScopeName + "{");

            string LastLine = Identation + "}";

            if (IdentationLevel > 0)
            {
                Identation += IdentationStr;
            }

            for (int Index = 0; Index < Nodes.Length; Index++)
            {
                ShaderIrNode Node = Nodes[Index];

                if (Node is ShaderIrCond Cond)
                {
                    string SubScopeName = $"if ({GetInOperName(Cond.Pred, true)})";

                    PrintBlockScope(SubScopeName, IdentationLevel + 1, Cond.Child);
                }
                else if (Node is ShaderIrAsg Asg)
                {
                    if (IsValidOutOper(Asg.Dst))
                    {
                        SB.AppendLine(Identation +
                            $"{GetOutOperName(Asg.Dst)} = " +
                            $"{GetInOperName (Asg.Src, true)};");
                    }
                }
                else if (Node is ShaderIrOp Op)
                {
                    SB.AppendLine($"{Identation}{GetInOperName(Op, true)};");
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            SB.AppendLine(LastLine);
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

        private string GetOutOperName(ShaderIrNode Node)
        {
            if (Node is ShaderIrOperAbuf Abuf)
            {
                return GetOutAbufName(Abuf);
            }
            else if (Node is ShaderIrOperGpr Gpr)
            {
                return GetName(Gpr);
            }
            else if (Node is ShaderIrOperPred Pred)
            {
                return GetName(Pred);
            }

            throw new ArgumentException(nameof(Node));
        }

        private string GetInOperName(ShaderIrNode Node, bool Entry = false)
        {
            switch (Node)
            {
                case ShaderIrOperAbuf Abuf: return GetName(Abuf);
                case ShaderIrOperCbuf Cbuf: return GetName(Cbuf);
                case ShaderIrOperGpr  Gpr:  return GetName(Gpr);
                case ShaderIrOperImm  Imm:  return GetName(Imm);
                case ShaderIrOperPred Pred: return GetName(Pred);

                case ShaderIrOp Op:
                    string Expr;

                    if (InstsExpr.TryGetValue(Op.Inst, out GetInstExpr GetExpr))
                    {
                        Expr = GetExpr(Op);
                    }
                    else
                    {
                        throw new NotImplementedException(Op.Inst.ToString());
                    }

                    if (!(Entry || IsUnary(Op.Inst)))
                    {
                        Expr = $"({Expr})";
                    }

                    return Expr;

                default: throw new ArgumentException(nameof(Node));
            }
        }

        private bool IsUnary(ShaderIrInst Inst)
        {
            return Inst == ShaderIrInst.Bnot ||
                   Inst == ShaderIrInst.Fabs ||
                   Inst == ShaderIrInst.Fcos ||
                   Inst == ShaderIrInst.Fex2 ||
                   Inst == ShaderIrInst.Flg2 ||
                   Inst == ShaderIrInst.Fneg ||
                   Inst == ShaderIrInst.Frcp ||
                   Inst == ShaderIrInst.Frsq ||
                   Inst == ShaderIrInst.Fsin ||
                   Inst == ShaderIrInst.Ipa  ||
                   Inst == ShaderIrInst.Texr ||
                   Inst == ShaderIrInst.Texg ||
                   Inst == ShaderIrInst.Texb ||
                   Inst == ShaderIrInst.Texa;
        }

        private string GetName(ShaderIrOperCbuf Cbuf)
        {
            if (!Uniforms.TryGetValue((Cbuf.Index, Cbuf.Offs), out GlslDeclInfo DeclInfo))
            {
                throw new InvalidOperationException();
            }

            return DeclInfo.Name;
        }

        private string GetOutAbufName(ShaderIrOperAbuf Abuf)
        {
            return GetName(OutputAttributes, Abuf, Swizzle: true);
        }

        private string GetName(ShaderIrOperAbuf Abuf, bool Swizzle = true)
        {
            return GetName(InputAttributes, Abuf, Swizzle);
        }

        private string GetName(Dictionary<int, GlslDeclInfo> Decls, ShaderIrOperAbuf Abuf, bool Swizzle)
        {
            int Index =  Abuf.Offs >> 4;
            int Elem  = (Abuf.Offs >> 2) & 3;

            if (!Decls.TryGetValue(Index, out GlslDeclInfo DeclInfo))
            {
                throw new InvalidOperationException();
            }

            Swizzle &= DeclInfo.Size > 1;

            return Swizzle ? $"{DeclInfo.Name}.{GetAttrSwizzle(Elem)}" : DeclInfo.Name;
        }

        private string GetName(ShaderIrOperGpr Gpr)
        {
            return Gpr.IsConst ? "0" : GetNameWithSwizzle(Gprs, Gpr.Index);
        }

        private string GetName(ShaderIrOperImm Imm)
        {
            return Imm.Imm.ToString(CultureInfo.InvariantCulture);
        }

        private string GetName(ShaderIrOperPred Pred)
        {
            return Pred.IsConst ? "true" : GetNameWithSwizzle(Preds, Pred.Index);
        }

        private string GetNameWithSwizzle(Dictionary<int, GlslDeclInfo> Decls, int Index)
        {
            int VecIndex = Index >> 2;

            if (Decls.TryGetValue(VecIndex, out GlslDeclInfo DeclInfo))
            {
                if (DeclInfo.Size > 1 && Index < VecIndex + DeclInfo.Size)
                {
                    return $"{DeclInfo.Name}.{GetAttrSwizzle(Index & 3)}";
                }
            }

            if (!Decls.TryGetValue(Index, out DeclInfo))
            {
                return null;
            }

            return DeclInfo.Name;
        }

        private string GetBandExpr(ShaderIrOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} && " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private string GetBnotExpr(ShaderIrOp Op)
        {
            return $"!{GetInOperName(Op.OperandA)}";
        }

        private string GetCltExpr(ShaderIrOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} < " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private string GetCeqExpr(ShaderIrOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} == " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private string GetCleExpr(ShaderIrOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} <= " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private string GetCgtExpr(ShaderIrOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} > " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private string GetCneExpr(ShaderIrOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} != " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private string GetCgeExpr(ShaderIrOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} >= " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private string GetExitExpr(ShaderIrOp Op)
        {
            return "return";
        }

        private string GetFabsExpr(ShaderIrOp Op)
        {
            return $"abs({GetInOperName(Op.OperandA)})";
        }

        private string GetFaddExpr(ShaderIrOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} + " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private string GetFcosExpr(ShaderIrOp Op)
        {
            return $"cos({GetInOperName(Op.OperandA)})";
        }

        private string GetFex2Expr(ShaderIrOp Op)
        {
            return $"exp2({GetInOperName(Op.OperandA)})";
        }

        private string GetFfmaExpr(ShaderIrOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} * " +
                   $"{GetInOperName(Op.OperandB)} + " +
                   $"{GetInOperName(Op.OperandC)}";
        }

        private string GetFlg2Expr(ShaderIrOp Op)
        {
            return $"log2({GetInOperName(Op.OperandA)})";
        }

        private  string GetFmulExpr(ShaderIrOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} * " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private string GetFnegExpr(ShaderIrOp Op)
        {
            return $"-{GetInOperName(Op.OperandA)}";
        }

        private string GetFrcpExpr(ShaderIrOp Op)
        {
            return $"1 / {GetInOperName(Op.OperandA)}";
        }

        private string GetFrsqExpr(ShaderIrOp Op)
        {
            return $"inversesqrt({GetInOperName(Op.OperandA)})";
        }

        private string GetFsinExpr(ShaderIrOp Op)
        {
            return $"sin({GetInOperName(Op.OperandA)})";
        }

        private string GetIpaExpr(ShaderIrOp Op)
        {
            return GetInOperName(Op.OperandA);
        }

        private string GetKilExpr(ShaderIrOp Op)
        {
            return "discard";
        }

        private string GetTexrExpr(ShaderIrOp Op) => GetTexExpr(Op, 'r');
        private string GetTexgExpr(ShaderIrOp Op) => GetTexExpr(Op, 'g');
        private string GetTexbExpr(ShaderIrOp Op) => GetTexExpr(Op, 'b');
        private string GetTexaExpr(ShaderIrOp Op) => GetTexExpr(Op, 'a');

        private string GetTexExpr(ShaderIrOp Op, char Ch)
        {
            return $"texture({GetTexSamplerName(Op)}, {GetTexSamplerCoords(Op)}).{Ch}";
        }

        private string GetTexSamplerName(ShaderIrOp Op)
        {
            ShaderIrOperImm Node = (ShaderIrOperImm)Op.OperandC;

            int Handle = ((ShaderIrOperImm)Op.OperandC).Imm;

            if (!Textures.TryGetValue(Handle, out GlslDeclInfo DeclInfo))
            {
                throw new InvalidOperationException();
            }

            return DeclInfo.Name;
        }

        private string GetTexSamplerCoords(ShaderIrOp Op)
        {
            if (GetInnerNode(Op.OperandA) is ShaderIrOperAbuf AAbuf &&
                GetInnerNode(Op.OperandB) is ShaderIrOperAbuf BAbuf)
            {
                if (AAbuf.GprIndex == ShaderIrOperGpr.ZRIndex &&
                    BAbuf.GprIndex == ShaderIrOperGpr.ZRIndex &&
                    (AAbuf.Offs >> 4) == (BAbuf.Offs >> 4))
                {
                    //Needs to call this to ensure it registers all elements used.
                    GetName(BAbuf);

                    return $"{GetName(AAbuf, Swizzle: false)}." +
                        $"{GetAttrSwizzle((AAbuf.Offs >> 2) & 3)}" +
                        $"{GetAttrSwizzle((BAbuf.Offs >> 2) & 3)}";
                }
            }

            return "vec2(" +
                $"{GetInOperName(Op.OperandA)}, " +
                $"{GetInOperName(Op.OperandB)})";
        }

        private ShaderIrNode GetInnerNode(ShaderIrNode Node)
        {
            if (Node is ShaderIrOp Op && Op.Inst == ShaderIrInst.Ipa)
            {
                return Op.OperandA;
            }

            return Node;
        }

        private string GetAttrSwizzle(int Elem)
        {
            return "xyzw".Substring(Elem, 1);
        }
    }
}