using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    struct TransformFeedbackOutput
    {
        public bool Valid { get; }
        public int Buffer { get; }
        public int Offset { get; }
        public int Stride { get; }

        public TransformFeedbackOutput(int buffer, int offset, int stride)
        {
            Valid = true;
            Buffer = buffer;
            Offset = offset;
            Stride = stride;
        }
    }

    class StructuredProgramInfo
    {
        public List<StructuredFunction> Functions { get; }

        public HashSet<int> Inputs { get; }
        public HashSet<int> Outputs { get; }

        public HelperFunctionsMask HelperFunctionsMask { get; set; }

        public TransformFeedbackOutput[] TransformFeedbackOutputs { get; }

        public StructuredProgramInfo()
        {
            Functions = new List<StructuredFunction>();

            Inputs = new HashSet<int>();
            Outputs = new HashSet<int>();

            TransformFeedbackOutputs = new TransformFeedbackOutput[0x80];
        }
    }
}