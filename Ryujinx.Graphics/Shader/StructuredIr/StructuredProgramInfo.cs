using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class StructuredProgramInfo
    {
        public AstBlock MainBlock { get; }

        public HashSet<AstOperand> Locals { get; }

        public HashSet<int> ConstantBuffers { get; }

        public HashSet<int> IAttributes { get; }
        public HashSet<int> OAttributes { get; }

        private HashSet<int> _textureHandles;

        public HashSet<AstTextureOperation> Samplers { get; }

        public StructuredProgramInfo(AstBlock mainBlock)
        {
            MainBlock = mainBlock;

            Locals = new HashSet<AstOperand>();

            ConstantBuffers = new HashSet<int>();

            IAttributes = new HashSet<int>();
            OAttributes = new HashSet<int>();

            Samplers = new HashSet<AstTextureOperation>();
        }
    }
}