using System.IO;

namespace Ryujinx.Horizon.Kernel.Diagnostics.Demangler.Ast
{
    public class CtorDtorNameType : ParentNode
    {
        private bool _isDestructor;

        public CtorDtorNameType(BaseNode name, bool isDestructor) : base(NodeType.CtorDtorNameType, name)
        {
            _isDestructor = isDestructor;
        }

        public override void PrintLeft(TextWriter writer)
        {
            if (_isDestructor)
            {
                writer.Write("~");
            }

            writer.Write(Child.GetName());
        }
    }
}