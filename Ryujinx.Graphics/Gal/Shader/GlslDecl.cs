using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.Shader
{
    class GlslDecl
    {
        public const int LayerAttr       = 0x064;
        public const int PointSizeAttr   = 0x06c;
        public const int PointCoordAttrX = 0x2e0;
        public const int PointCoordAttrY = 0x2e4;
        public const int TessCoordAttrX  = 0x2f0;
        public const int TessCoordAttrY  = 0x2f4;
        public const int TessCoordAttrZ  = 0x2f8;
        public const int InstanceIdAttr  = 0x2f8;
        public const int VertexIdAttr    = 0x2fc;
        public const int FaceAttr        = 0x3fc;

        public const int MaxUboSize = 1024;

        public const int GlPositionVec4Index = 7;

        public const int PositionOutAttrLocation = 15;

        private const int AttrStartIndex = 8;
        private const int TexStartIndex  = 8;

        public const string PositionOutAttrName = "position";

        private const string TextureName = "tex";
        private const string UniformName = "c";

        private const string AttrName    = "attr";
        private const string InAttrName  = "in_"  + AttrName;
        private const string OutAttrName = "out_" + AttrName;

        private const string GprName  = "gpr";
        private const string PredName = "pred";

        public const string FragmentOutputName = "FragColor";

        public const string ExtraUniformBlockName = "Extra";
        public const string FlipUniformName = "flip";
        public const string InstanceUniformName = "instance";

        public const string BasicBlockName  = "bb";
        public const string BasicBlockAName = BasicBlockName + "_a";
        public const string BasicBlockBName = BasicBlockName + "_b";

        public const int SsyStackSize = 16;
        public const string SsyStackName = "ssy_stack";
        public const string SsyCursorName = "ssy_cursor";

        private string[] _stagePrefixes = new string[] { "vp", "tcp", "tep", "gp", "fp" };

        private string _stagePrefix;

        private Dictionary<ShaderIrOp, ShaderDeclInfo> _cbTextures;

        private Dictionary<int, ShaderDeclInfo> _Textures;
        private Dictionary<int, ShaderDeclInfo> _uniforms;

        private Dictionary<int, ShaderDeclInfo> _attributes;
        private Dictionary<int, ShaderDeclInfo> _inAttributes;
        private Dictionary<int, ShaderDeclInfo> _outAttributes;

        private Dictionary<int, ShaderDeclInfo> _gprs;
        private Dictionary<int, ShaderDeclInfo> _preds;

        public IReadOnlyDictionary<ShaderIrOp, ShaderDeclInfo> CbTextures => _cbTextures;

        public IReadOnlyDictionary<int, ShaderDeclInfo> Textures => _Textures;
        public IReadOnlyDictionary<int, ShaderDeclInfo> Uniforms => _uniforms;

        public IReadOnlyDictionary<int, ShaderDeclInfo> Attributes    => _attributes;
        public IReadOnlyDictionary<int, ShaderDeclInfo> InAttributes  => _inAttributes;
        public IReadOnlyDictionary<int, ShaderDeclInfo> OutAttributes => _outAttributes;

        public IReadOnlyDictionary<int, ShaderDeclInfo> Gprs  => _gprs;
        public IReadOnlyDictionary<int, ShaderDeclInfo> Preds => _preds;

        public GalShaderType ShaderType { get; private set; }

        private GlslDecl(GalShaderType shaderType)
        {
            ShaderType = shaderType;

            _cbTextures = new Dictionary<ShaderIrOp, ShaderDeclInfo>();

            _Textures = new Dictionary<int, ShaderDeclInfo>();
            _uniforms = new Dictionary<int, ShaderDeclInfo>();

            _attributes    = new Dictionary<int, ShaderDeclInfo>();
            _inAttributes  = new Dictionary<int, ShaderDeclInfo>();
            _outAttributes = new Dictionary<int, ShaderDeclInfo>();

            _gprs  = new Dictionary<int, ShaderDeclInfo>();
            _preds = new Dictionary<int, ShaderDeclInfo>();
        }

        public GlslDecl(ShaderIrBlock[] blocks, GalShaderType shaderType, ShaderHeader header)
            : this(shaderType)
        {
            _stagePrefix = _stagePrefixes[(int)shaderType] + "_";

            if (shaderType == GalShaderType.Fragment)
            {
                int index = 0;

                for (int attachment = 0; attachment < 8; attachment++)
                {
                    for (int component = 0; component < 4; component++)
                    {
                        if (header.OmapTargets[attachment].ComponentEnabled(component))
                        {
                            _gprs.TryAdd(index, new ShaderDeclInfo(GetGprName(index), index));

                            index++;
                        }
                    }
                }

                if (header.OmapDepth)
                {
                    index = header.DepthRegister;

                    _gprs.TryAdd(index, new ShaderDeclInfo(GetGprName(index), index));
                }
            }

            foreach (ShaderIrBlock block in blocks)
            {
                ShaderIrNode[] nodes = block.GetNodes();

                foreach (ShaderIrNode node in nodes)
                {
                    Traverse(nodes, null, node);
                }
            }
        }

        public static GlslDecl Merge(GlslDecl vpA, GlslDecl vpB)
        {
            GlslDecl combined = new GlslDecl(GalShaderType.Vertex);

            Merge(combined._Textures, vpA._Textures, vpB._Textures);
            Merge(combined._uniforms, vpA._uniforms, vpB._uniforms);

            Merge(combined._attributes,    vpA._attributes,    vpB._attributes);
            Merge(combined._outAttributes, vpA._outAttributes, vpB._outAttributes);

            Merge(combined._gprs,  vpA._gprs,  vpB._gprs);
            Merge(combined._preds, vpA._preds, vpB._preds);

            //Merge input attributes.
            foreach (KeyValuePair<int, ShaderDeclInfo> kv in vpA._inAttributes)
            {
                combined._inAttributes.TryAdd(kv.Key, kv.Value);
            }

            foreach (KeyValuePair<int, ShaderDeclInfo> kv in vpB._inAttributes)
            {
                //If Vertex Program A already writes to this attribute,
                //then we don't need to add it as an input attribute since
                //Vertex Program A will already have written to it anyway,
                //and there's no guarantee that there is an input attribute
                //for this slot.
                if (!vpA._outAttributes.ContainsKey(kv.Key))
                {
                    combined._inAttributes.TryAdd(kv.Key, kv.Value);
                }
            }

            return combined;
        }

        public static string GetGprName(int index)
        {
            return GprName + index;
        }

        private static void Merge(
            Dictionary<int, ShaderDeclInfo> c,
            Dictionary<int, ShaderDeclInfo> a,
            Dictionary<int, ShaderDeclInfo> b)
        {
            foreach (KeyValuePair<int, ShaderDeclInfo> kv in a)
            {
                c.TryAdd(kv.Key, kv.Value);
            }

            foreach (KeyValuePair<int, ShaderDeclInfo> kv in b)
            {
                c.TryAdd(kv.Key, kv.Value);
            }
        }

        private void Traverse(ShaderIrNode[] nodes, ShaderIrNode parent, ShaderIrNode node)
        {
            switch (node)
            {
                case ShaderIrAsg asg:
                {
                    Traverse(nodes, asg, asg.Dst);
                    Traverse(nodes, asg, asg.Src);

                    break;
                }

                case ShaderIrCond cond:
                {
                    Traverse(nodes, cond, cond.Pred);
                    Traverse(nodes, cond, cond.Child);

                    break;
                }

                case ShaderIrOp op:
                {
                    Traverse(nodes, op, op.OperandA);
                    Traverse(nodes, op, op.OperandB);
                    Traverse(nodes, op, op.OperandC);

                    if (op.Inst == ShaderIrInst.Texq ||
                        op.Inst == ShaderIrInst.Texs ||
                        op.Inst == ShaderIrInst.Txlf)
                    {
                        int handle = ((ShaderIrOperImm)op.OperandC).Value;

                        int index = handle - TexStartIndex;

                        string name = _stagePrefix + TextureName + index;

                        _Textures.TryAdd(handle, new ShaderDeclInfo(name, handle));
                    }
                    else if (op.Inst == ShaderIrInst.Texb)
                    {
                        ShaderIrNode handleSrc = null;

                        int index = Array.IndexOf(nodes, parent) - 1;

                        for (; index >= 0; index--)
                        {
                            ShaderIrNode curr = nodes[index];

                            if (curr is ShaderIrAsg asg && asg.Dst is ShaderIrOperGpr gpr)
                            {
                                if (gpr.Index == ((ShaderIrOperGpr)op.OperandC).Index)
                                {
                                    handleSrc = asg.Src;

                                    break;
                                }
                            }
                        }

                        if (handleSrc != null && handleSrc is ShaderIrOperCbuf cbuf)
                        {
                            string name = _stagePrefix + TextureName + "_cb" + cbuf.Index + "_" + cbuf.Pos;

                            _cbTextures.Add(op, new ShaderDeclInfo(name, cbuf.Pos, true, cbuf.Index));
                        }
                        else
                        {
                            throw new NotImplementedException("Shader TEX.B instruction is not fully supported!");
                        }
                    }
                    break;
                }

                case ShaderIrOperCbuf cbuf:
                {
                    if (!_uniforms.ContainsKey(cbuf.Index))
                    {
                        string name = _stagePrefix + UniformName + cbuf.Index;

                        ShaderDeclInfo declInfo = new ShaderDeclInfo(name, cbuf.Pos, true, cbuf.Index);

                        _uniforms.Add(cbuf.Index, declInfo);
                    }
                    break;
                }

                case ShaderIrOperAbuf abuf:
                {
                    //This is a built-in variable.
                    if (abuf.Offs == LayerAttr       ||
                        abuf.Offs == PointSizeAttr   ||
                        abuf.Offs == PointCoordAttrX ||
                        abuf.Offs == PointCoordAttrY ||
                        abuf.Offs == VertexIdAttr    ||
                        abuf.Offs == InstanceIdAttr  ||
                        abuf.Offs == FaceAttr)
                    {
                        break;
                    }

                    int index =  abuf.Offs >> 4;
                    int elem  = (abuf.Offs >> 2) & 3;

                    int glslIndex = index - AttrStartIndex;

                    if (glslIndex < 0)
                    {
                        return;
                    }

                    ShaderDeclInfo declInfo;

                    if (parent is ShaderIrAsg asg && asg.Dst == node)
                    {
                        if (!_outAttributes.TryGetValue(index, out declInfo))
                        {
                            declInfo = new ShaderDeclInfo(OutAttrName + glslIndex, glslIndex);

                            _outAttributes.Add(index, declInfo);
                        }
                    }
                    else
                    {
                        if (!_inAttributes.TryGetValue(index, out declInfo))
                        {
                            declInfo = new ShaderDeclInfo(InAttrName + glslIndex, glslIndex);

                            _inAttributes.Add(index, declInfo);
                        }
                    }

                    declInfo.Enlarge(elem + 1);

                    if (!_attributes.ContainsKey(index))
                    {
                        declInfo = new ShaderDeclInfo(AttrName + glslIndex, glslIndex, false, 0, 4);

                        _attributes.Add(index, declInfo);
                    }

                    Traverse(nodes, abuf, abuf.Vertex);

                    break;
                }

                case ShaderIrOperGpr gpr:
                {
                    if (!gpr.IsConst)
                    {
                        string name = GetGprName(gpr.Index);

                        _gprs.TryAdd(gpr.Index, new ShaderDeclInfo(name, gpr.Index));
                    }
                    break;
                }

                case ShaderIrOperPred pred:
                {
                    if (!pred.IsConst && !HasName(_preds, pred.Index))
                    {
                        string name = PredName + pred.Index;

                        _preds.TryAdd(pred.Index, new ShaderDeclInfo(name, pred.Index));
                    }
                    break;
                }
            }
        }

        private bool HasName(Dictionary<int, ShaderDeclInfo> decls, int index)
        {
            //This is used to check if the dictionary already contains
            //a entry for a vector at a given index position.
            //Used to enable turning gprs into vectors.
            int vecIndex = index & ~3;

            if (decls.TryGetValue(vecIndex, out ShaderDeclInfo declInfo))
            {
                if (declInfo.Size > 1 && index < vecIndex + declInfo.Size)
                {
                    return true;
                }
            }

            return decls.ContainsKey(index);
        }
    }
}
