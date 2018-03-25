using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Graphics.Gal.Shader
{
    class GlslDecompiler
    {
        private delegate string GetInstExpr(ShaderIrOp Op);

        private Dictionary<ShaderIrInst, GetInstExpr> InstsExpr;

        private const string IdentationStr = "    ";

        private static string[] ElemTypes = new string[] { "float", "vec2", "vec3", "vec4" };

        private class Attrib
        {
            public string Name;
            public int    Elems;

            public Attrib(string Name, int Elems)
            {
                this.Name  = Name;
                this.Elems = Elems;
            }
        }       

        private SortedDictionary<int, Attrib> InputAttributes;
        private SortedDictionary<int, Attrib> OutputAttributes;

        private HashSet<int> UsedCbufs;

        private const int AttrStartIndex = 8;
        private const int TexStartIndex = 8;

        private const string InputAttrPrefix  = "in_attr";
        private const string OutputAttrPrefix = "out_attr";

        private const string CbufBuffPrefix = "c";
        private const string CbufDataName   = "buf";

        private const string GprName  = "gpr";
        private const string PredName = "pred";
        private const string SampName = "samp";

        private int GprsCount;
        private int PredsCount;
        private int SampsCount;

        private StringBuilder BodySB;

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

        public string Decompile(int[] Code, ShaderType Type)
        {
            InputAttributes  = new SortedDictionary<int, Attrib>();
            OutputAttributes = new SortedDictionary<int, Attrib>();

            UsedCbufs = new HashSet<int>();

            BodySB = new StringBuilder();

            //FIXME: Only valid for vertex shaders.
            if (Type == ShaderType.Fragment)
            {
                OutputAttributes.Add(7, new Attrib("FragColor", 4));
            }
            else
            {
                OutputAttributes.Add(7, new Attrib("gl_Position", 4));
            }

            ShaderIrBlock Block = ShaderDecoder.DecodeBasicBlock(Code, 0, Type);

            ShaderIrNode[] Nodes = Block.GetNodes();

            PrintBlockScope("void main()", 1, Nodes);

            StringBuilder SB = new StringBuilder();

            SB.AppendLine("#version 430");

            PrintDeclSSBOs(SB);
            PrintDeclInAttributes(SB);
            PrintDeclOutAttributes(SB);

            if (Type == ShaderType.Fragment)
            {
                SB.AppendLine($"out vec4 {OutputAttributes[7].Name};");
                SB.AppendLine();
            }

            PrintDeclSamplers(SB);
            PrintDeclGprs(SB);
            PrintDeclPreds(SB);

            SB.Append(BodySB.ToString());

            BodySB.Clear();

            return SB.ToString();
        }

        private void PrintDeclSSBOs(StringBuilder SB)
        {
            foreach (int Cbuf in UsedCbufs)
            {
                SB.AppendLine($"layout(std430, binding = {Cbuf}) buffer {CbufBuffPrefix}{Cbuf} {{");
                SB.AppendLine($"{IdentationStr}float {CbufDataName}[];");
                SB.AppendLine("};");
                SB.AppendLine();
            }
        }

        private void PrintDeclInAttributes(StringBuilder SB)
        {
            bool PrintNl = false;

            foreach (KeyValuePair<int, Attrib> KV in InputAttributes)
            {
                int Index = KV.Key - AttrStartIndex;

                if (Index >= 0)
                {
                    string Type = ElemTypes[KV.Value.Elems];

                    SB.AppendLine($"layout(location = {Index}) in {Type} {KV.Value.Name};");

                    PrintNl = true;
                }
            }

            if (PrintNl)
            {
                SB.AppendLine();
            }
        }

        private void PrintDeclOutAttributes(StringBuilder SB)
        {
            bool PrintNl = false;

            foreach (KeyValuePair<int, Attrib> KV in OutputAttributes)
            {
                int Index = KV.Key - AttrStartIndex;

                if (Index >= 0)
                {
                    string Type = ElemTypes[KV.Value.Elems];

                    SB.AppendLine($"layout(location = {Index}) out {Type} {KV.Value.Name};");

                    PrintNl = true;
                }
            }

            if (PrintNl)
            {
                SB.AppendLine();
            }
        }

        private void PrintDeclSamplers(StringBuilder SB)
        {
            if (SampsCount > 0)
            {
                SB.AppendLine($"uniform sampler2D {SampName}[{SampsCount}];");
                SB.AppendLine();
            }
        }

        private void PrintDeclGprs(StringBuilder SB)
        {
            if (GprsCount > 0)
            {
                SB.AppendLine($"float {GprName}[{GprsCount}];");
                SB.AppendLine();
            }
        }

        private void PrintDeclPreds(StringBuilder SB)
        {
            if (PredsCount > 0)
            {
                SB.AppendLine($"bool {PredName}[{PredsCount}];");
                SB.AppendLine();
            }
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

            BodySB.AppendLine(Identation + ScopeName + "{");

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
                        BodySB.AppendLine(Identation +
                            $"{GetOutOperName(Asg.Dst)} = " + 
                            $"{GetInOperName (Asg.Src, true)};");
                    }
                }
                else if (Node is ShaderIrOp Op)
                {
                    BodySB.AppendLine($"{Identation}{GetInOperName(Op, true)};");
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            BodySB.AppendLine(LastLine);
        }

        private bool IsValidOutOper(ShaderIrNode Node)
        {
            if (Node is ShaderIrOperGpr Gpr && Gpr.Index == ShaderIrOperGpr.ZRIndex)
            {
                return false;
            }
            else if (Node is ShaderIrOperPred Pred && Pred.Index == ShaderIrOperPred.UnusedIndex)
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

        private string GetOutAbufName(ShaderIrOperAbuf Abuf)
        {
            int AttrIndex = Abuf.Offs >> 4;

            int Elem = (Abuf.Offs >> 2) & 3;

            if (!OutputAttributes.TryGetValue(AttrIndex, out Attrib Attr))
            {
                Attr = new Attrib(OutputAttrPrefix + (AttrIndex - AttrStartIndex), Elem);

                OutputAttributes.Add(AttrIndex, Attr);
            }

            if (Attr.Elems < Elem)
            {
                Attr.Elems = Elem;
            }

            return $"{Attr.Name}.{GetAttrSwizzle(Elem)}";
        }

        private string GetName(ShaderIrOperAbuf Abuf, bool Swizzle = true)
        {
            int AttrIndex = Abuf.Offs >> 4;

            int Elem = (Abuf.Offs >> 2) & 3;

            if (!InputAttributes.TryGetValue(AttrIndex, out Attrib Attr))
            {
                Attr = new Attrib(InputAttrPrefix + (AttrIndex - AttrStartIndex), Elem);

                InputAttributes.Add(AttrIndex, Attr);
            }

            if (Attr.Elems < Elem)
            {
                Attr.Elems = Elem;
            }

            return Attr.Name + (Swizzle ? $".{GetAttrSwizzle(Elem)}" : string.Empty);
        }

        private string GetName(ShaderIrOperCbuf Cbuf)
        {
            UsedCbufs.Add(Cbuf.Index);

            return $"{CbufBuffPrefix}{Cbuf.Index}.{CbufDataName}[{Cbuf.Offs}]";
        }

        private string GetName(ShaderIrOperGpr Gpr)
        {
            if (GprsCount < Gpr.Index + 1)
            {
                GprsCount = Gpr.Index + 1;
            }

            return GetRegName(Gpr.Index);
        }

        private string GetName(ShaderIrOperPred Pred)
        {
            if (PredsCount < Pred.Index + 1)
            {
                PredsCount = Pred.Index + 1;
            }

            return GetPredName(Pred.Index);
        }

        private string GetRegName(int GprIndex)
        {
            return GprIndex == ShaderIrOperGpr.ZRIndex ? "0" : $"{GprName}[{GprIndex}]";
        }

        private string GetPredName(int PredIndex)
        {
            return PredIndex == ShaderIrOperPred.UnusedIndex ? "true" : $"{PredName}[{PredIndex}]";
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

            int Handle = Node.Imm - TexStartIndex;

            if (SampsCount < Handle + 1)
            {
                SampsCount = Handle + 1;
            }

            return $"{SampName}[{Handle}]";
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
                    string AttrName = GetName(AAbuf, Swizzle: false);

                    //Needs to call this to ensure it registers all elements used.
                    GetName(BAbuf);

                    return $"{AttrName}." +
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