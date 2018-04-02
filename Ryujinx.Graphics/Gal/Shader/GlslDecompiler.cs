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

        private GlslDecl Decl;

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

        public GlslProgram Decompile(int[] Code, GalShaderType ShaderType)
        {
            ShaderIrBlock Block = ShaderDecoder.DecodeBasicBlock(Code, 0, ShaderType);

            ShaderIrNode[] Nodes = Block.GetNodes();

            Decl = new GlslDecl(Nodes, ShaderType);

            SB = new StringBuilder();

            SB.AppendLine("#version 430");

            PrintDeclTextures();
            PrintDeclUniforms();
            PrintDeclInAttributes();
            PrintDeclOutAttributes();
            PrintDeclGprs();
            PrintDeclPreds();

            PrintBlockScope("void main()", 1, Nodes);

            string GlslCode = SB.ToString();

            return new GlslProgram(
                GlslCode,
                Decl.Textures.Values,
                Decl.Uniforms.Values);
        }

        private void PrintDeclTextures()
        {
            PrintDecls(Decl.Textures, "uniform sampler2D");
        }

        private void PrintDeclUniforms()
        {
            foreach (ShaderDeclInfo DeclInfo in Decl.Uniforms.Values.OrderBy(DeclKeySelector))
            {
                SB.AppendLine($"uniform {GetDecl(DeclInfo)};");
            }

            if (Decl.Uniforms.Count > 0)
            {
                SB.AppendLine();
            }
        }

        private void PrintDeclInAttributes()
        {
            PrintDeclAttributes(Decl.InAttributes.Values, "in");
        }

        private void PrintDeclOutAttributes()
        {
            PrintDeclAttributes(Decl.OutAttributes.Values, "out");
        }

        private void PrintDeclAttributes(IEnumerable<ShaderDeclInfo> Decls, string InOut)
        {
            int Count = 0;

            foreach (ShaderDeclInfo DeclInfo in Decls.OrderBy(DeclKeySelector))
            {
                if (DeclInfo.Index >= 0)
                {
                    SB.AppendLine($"layout (location = {DeclInfo.Index}) {InOut} {GetDecl(DeclInfo)};");

                    Count++;
                }
            }

            if (Count > 0)
            {
                SB.AppendLine();
            }
        }

        private void PrintDeclGprs()
        {
            PrintDecls(Decl.Gprs);
        }

        private void PrintDeclPreds()
        {
            PrintDecls(Decl.Preds, "bool");
        }

        private void PrintDecls(IReadOnlyDictionary<int, ShaderDeclInfo> Dict, string CustomType = null)
        {
            foreach (ShaderDeclInfo DeclInfo in Dict.Values.OrderBy(DeclKeySelector))
            {
                string Name;

                if (CustomType != null)
                {
                    Name = CustomType + " " + DeclInfo.Name + ";";
                }
                else if (DeclInfo.Name == GlslDecl.FragmentOutputName)
                {
                    Name = "out " + GetDecl(DeclInfo) + ";";
                }
                else
                {
                    Name = GetDecl(DeclInfo) + ";";
                }

                SB.AppendLine(Name);
            }

            if (Dict.Count > 0)
            {
                SB.AppendLine();
            }
        }

        private int DeclKeySelector(ShaderDeclInfo DeclInfo)
        {
            return DeclInfo.Cbuf << 24 | DeclInfo.Index;
        }

        private string GetDecl(ShaderDeclInfo DeclInfo)
        {
            return ElemTypes[DeclInfo.Size - 1] + " " + DeclInfo.Name;
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
                    string SubScopeName = "if (" + GetInOperName(Cond.Pred, true) + ")";

                    PrintBlockScope(SubScopeName, IdentationLevel + 1, Cond.Child);
                }
                else if (Node is ShaderIrAsg Asg && IsValidOutOper(Asg.Dst))
                {
                    SB.AppendLine(Identation + GetOutOperName(Asg.Dst) + " = " + GetInOperName(Asg.Src, true) + ";");
                }
                else if (Node is ShaderIrOp Op)
                {
                    SB.AppendLine(Identation + GetInOperName(Op, true) + ";");
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

                    if (!Entry && (Op.OperandB != null ||
                                   Op.OperandC != null))
                    {
                        Expr = "(" + Expr + ")";
                    }

                    return Expr;

                default: throw new ArgumentException(nameof(Node));
            }
        }

        private string GetName(ShaderIrOperCbuf Cbuf)
        {
            if (!Decl.Uniforms.TryGetValue((Cbuf.Index, Cbuf.Offs), out ShaderDeclInfo DeclInfo))
            {
                throw new InvalidOperationException();
            }

            return DeclInfo.Name;
        }

        private string GetOutAbufName(ShaderIrOperAbuf Abuf)
        {
            return GetName(Decl.OutAttributes, Abuf);
        }

        private string GetName(ShaderIrOperAbuf Abuf)
        {
            return GetName(Decl.InAttributes, Abuf);
        }

        private string GetName(IReadOnlyDictionary<int, ShaderDeclInfo> Dict, ShaderIrOperAbuf Abuf)
        {
            int Index =  Abuf.Offs >> 4;
            int Elem  = (Abuf.Offs >> 2) & 3;

            if (!Dict.TryGetValue(Index, out ShaderDeclInfo DeclInfo))
            {
                throw new InvalidOperationException();
            }

            return DeclInfo.Size > 1 ? DeclInfo.Name + "." + GetAttrSwizzle(Elem) : DeclInfo.Name;
        }

        private string GetName(ShaderIrOperGpr Gpr)
        {
            return Gpr.IsConst ? "0" : GetNameWithSwizzle(Decl.Gprs, Gpr.Index);
        }

        private string GetName(ShaderIrOperImm Imm)
        {
            return Imm.Imm.ToString(CultureInfo.InvariantCulture);
        }

        private string GetName(ShaderIrOperPred Pred)
        {
            return Pred.IsConst ? "true" : GetNameWithSwizzle(Decl.Preds, Pred.Index);
        }

        private string GetNameWithSwizzle(IReadOnlyDictionary<int, ShaderDeclInfo> Dict, int Index)
        {
            int VecIndex = Index >> 2;

            if (Dict.TryGetValue(VecIndex, out ShaderDeclInfo DeclInfo))
            {
                if (DeclInfo.Size > 1 && Index < VecIndex + DeclInfo.Size)
                {
                    return DeclInfo.Name + "." + GetAttrSwizzle(Index & 3);
                }
            }

            if (!Dict.TryGetValue(Index, out DeclInfo))
            {
                throw new InvalidOperationException();
            }

            return DeclInfo.Name;
        }

        private string GetAttrSwizzle(int Elem)
        {
            return "xyzw".Substring(Elem, 1);
        }

        private string GetBandExpr(ShaderIrOp Op) => GetBinaryExpr(Op, "&&");

        private string GetBnotExpr(ShaderIrOp Op) => GetUnaryExpr(Op, "!");

        private string GetCltExpr(ShaderIrOp Op) => GetBinaryExpr(Op, "<");
        private string GetCeqExpr(ShaderIrOp Op) => GetBinaryExpr(Op, "==");
        private string GetCleExpr(ShaderIrOp Op) => GetBinaryExpr(Op, "<=");
        private string GetCgtExpr(ShaderIrOp Op) => GetBinaryExpr(Op, ">");
        private string GetCneExpr(ShaderIrOp Op) => GetBinaryExpr(Op, "!=");
        private string GetCgeExpr(ShaderIrOp Op) => GetBinaryExpr(Op, ">=");

        private string GetExitExpr(ShaderIrOp Op) => "return";

        private string GetFabsExpr(ShaderIrOp Op) => GetUnaryCall(Op, "abs");

        private string GetFaddExpr(ShaderIrOp Op) => GetBinaryExpr(Op, "+");

        private string GetFcosExpr(ShaderIrOp Op) => GetUnaryCall(Op, "cos");

        private string GetFex2Expr(ShaderIrOp Op) => GetUnaryCall(Op, "exp2");

        private string GetFfmaExpr(ShaderIrOp Op) => GetTernaryExpr(Op, "*", "+");

        private string GetFlg2Expr(ShaderIrOp Op) => GetUnaryCall(Op, "log2");

        private string GetFmulExpr(ShaderIrOp Op) => GetBinaryExpr(Op, "*");

        private string GetFnegExpr(ShaderIrOp Op) => GetUnaryExpr(Op, "-");

        private string GetFrcpExpr(ShaderIrOp Op) => GetUnaryExpr(Op, "1 / ");

        private string GetFrsqExpr(ShaderIrOp Op) => GetUnaryCall(Op, "inversesqrt");

        private string GetFsinExpr(ShaderIrOp Op) => GetUnaryCall(Op, "sin");

        private string GetIpaExpr(ShaderIrOp Op) => GetInOperName(Op.OperandA);

        private string GetKilExpr(ShaderIrOp Op) => "discard";

        private string GetUnaryCall(ShaderIrOp Op, string FuncName)
        {
            return FuncName + "(" + GetInOperName(Op.OperandA) + ")";
        }

        private string GetUnaryExpr(ShaderIrOp Op, string Opr)
        {
            return Opr + GetInOperName(Op.OperandA);
        }

        private string GetBinaryExpr(ShaderIrOp Op, string Opr)
        {
            return GetInOperName(Op.OperandA) + " " + Opr + " " +
                   GetInOperName(Op.OperandB);
        }

        private string GetTernaryExpr(ShaderIrOp Op, string Opr1, string Opr2)
        {
            return GetInOperName(Op.OperandA) + " " + Opr1 + " " +
                   GetInOperName(Op.OperandB) + " " + Opr2 + " " +
                   GetInOperName(Op.OperandC);
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

            if (!Decl.Textures.TryGetValue(Handle, out ShaderDeclInfo DeclInfo))
            {
                throw new InvalidOperationException();
            }

            return DeclInfo.Name;
        }

        private string GetTexSamplerCoords(ShaderIrOp Op)
        {
            return "vec2(" + GetInOperName(Op.OperandA, Entry: true) + ", " +
                             GetInOperName(Op.OperandB, Entry: true) + ")";
        }
    }
}