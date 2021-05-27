using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class StructuredProgramInfo
    {
        public List<StructuredFunction> Functions { get; }

        public HashSet<int> Inputs { get; }
        public HashSet<int> Outputs { get; }

        public HashSet<int> IAttributes { get; }
        public HashSet<int> OAttributes { get; }

        public HelperFunctionsMask HelperFunctionsMask { get; set; }

        public StructuredProgramInfo()
        {
            Functions = new List<StructuredFunction>();

            Inputs = new HashSet<int>();
            Outputs = new HashSet<int>();

            IAttributes = new HashSet<int>();
            OAttributes = new HashSet<int>();
        }
    }
}