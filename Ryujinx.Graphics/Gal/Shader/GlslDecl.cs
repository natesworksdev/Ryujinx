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

        private Dictionary<ShaderIrOp, ShaderDeclInfo> _mCbTextures;

        private Dictionary<int, ShaderDeclInfo> _mTextures;
        private Dictionary<int, ShaderDeclInfo> _mUniforms;

        private Dictionary<int, ShaderDeclInfo> _mAttributes;
        private Dictionary<int, ShaderDeclInfo> _mInAttributes;
        private Dictionary<int, ShaderDeclInfo> _mOutAttributes;

        private Dictionary<int, ShaderDeclInfo> _mGprs;
        private Dictionary<int, ShaderDeclInfo> _mPreds;

        public IReadOnlyDictionary<ShaderIrOp, ShaderDeclInfo> CbTextures => _mCbTextures;

        public IReadOnlyDictionary<int, ShaderDeclInfo> Textures => _mTextures;
        public IReadOnlyDictionary<int, ShaderDeclInfo> Uniforms => _mUniforms;

        public IReadOnlyDictionary<int, ShaderDeclInfo> Attributes    => _mAttributes;
        public IReadOnlyDictionary<int, ShaderDeclInfo> InAttributes  => _mInAttributes;
        public IReadOnlyDictionary<int, ShaderDeclInfo> OutAttributes => _mOutAttributes;

        public IReadOnlyDictionary<int, ShaderDeclInfo> Gprs  => _mGprs;
        public IReadOnlyDictionary<int, ShaderDeclInfo> Preds => _mPreds;

        public GalShaderType ShaderType { get; private set; }

        private GlslDecl(GalShaderType shaderType)
        {
            ShaderType = shaderType;

            _mCbTextures = new Dictionary<ShaderIrOp, ShaderDeclInfo>();

            _mTextures = new Dictionary<int, ShaderDeclInfo>();
            _mUniforms = new Dictionary<int, ShaderDeclInfo>();

            _mAttributes    = new Dictionary<int, ShaderDeclInfo>();
            _mInAttributes  = new Dictionary<int, ShaderDeclInfo>();
            _mOutAttributes = new Dictionary<int, ShaderDeclInfo>();

            _mGprs  = new Dictionary<int, ShaderDeclInfo>();
            _mPreds = new Dictionary<int, ShaderDeclInfo>();
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
                            _mGprs.TryAdd(index, new ShaderDeclInfo(GetGprName(index), index));

                            index++;
                        }
                    }
                }

                if (header.OmapDepth)
                {
                    index = header.DepthRegister;

                    _mGprs.TryAdd(index, new ShaderDeclInfo(GetGprName(index), index));
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

            Merge(combined._mTextures, vpA._mTextures, vpB._mTextures);
            Merge(combined._mUniforms, vpA._mUniforms, vpB._mUniforms);

            Merge(combined._mAttributes,    vpA._mAttributes,    vpB._mAttributes);
            Merge(combined._mOutAttributes, vpA._mOutAttributes, vpB._mOutAttributes);

            Merge(combined._mGprs,  vpA._mGprs,  vpB._mGprs);
            Merge(combined._mPreds, vpA._mPreds, vpB._mPreds);

            //Merge input attributes.
            foreach (KeyValuePair<int, ShaderDeclInfo> kv in vpA._mInAttributes)
            {
                combined._mInAttributes.TryAdd(kv.Key, kv.Value);
            }

            foreach (KeyValuePair<int, ShaderDeclInfo> kv in vpB._mInAttributes)
            {
                //If Vertex Program A already writes to this attribute,
                //then we don't need to add it as an input attribute since
                //Vertex Program A will already have written to it anyway,
                //and there's no guarantee that there is an input attribute
                //for this slot.
                if (!vpA._mOutAttributes.ContainsKey(kv.Key))
                {
                    combined._mInAttributes.TryAdd(kv.Key, kv.Value);
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

                        _mTextures.TryAdd(handle, new ShaderDeclInfo(name, handle));
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

                            _mCbTextures.Add(op, new ShaderDeclInfo(name, cbuf.Pos, true, cbuf.Index));
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
                    if (!_mUniforms.ContainsKey(cbuf.Index))
                    {
                        string name = _stagePrefix + UniformName + cbuf.Index;

                        ShaderDeclInfo declInfo = new ShaderDeclInfo(name, cbuf.Pos, true, cbuf.Index);

                        _mUniforms.Add(cbuf.Index, declInfo);
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
                        if (!_mOutAttributes.TryGetValue(index, out declInfo))
                        {
                            declInfo = new ShaderDeclInfo(OutAttrName + glslIndex, glslIndex);

                            _mOutAttributes.Add(index, declInfo);
                        }
                    }
                    else
                    {
                        if (!_mInAttributes.TryGetValue(index, out declInfo))
                        {
                            declInfo = new ShaderDeclInfo(InAttrName + glslIndex, glslIndex);

                            _mInAttributes.Add(index, declInfo);
                        }
                    }

                    declInfo.Enlarge(elem + 1);

                    if (!_mAttributes.ContainsKey(index))
                    {
                        declInfo = new ShaderDeclInfo(AttrName + glslIndex, glslIndex, false, 0, 4);

                        _mAttributes.Add(index, declInfo);
                    }

                    Traverse(nodes, abuf, abuf.Vertex);

                    break;
                }

                case ShaderIrOperGpr gpr:
                {
                    if (!gpr.IsConst)
                    {
                        string name = GetGprName(gpr.Index);

                        _mGprs.TryAdd(gpr.Index, new ShaderDeclInfo(name, gpr.Index));
                    }
                    break;
                }

                case ShaderIrOperPred pred:
                {
                    if (!pred.IsConst && !HasName(_mPreds, pred.Index))
                    {
                        string name = PredName + pred.Index;

                        _mPreds.TryAdd(pred.Index, new ShaderDeclInfo(name, pred.Index));
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
