using Ryujinx.Graphics.Shader.StructuredIr;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Graphics.Shader.CodeGen.Glsl
{
    class CodeGenContext
    {
        private const string Tab = "    ";

        public ShaderConfig Config { get; }

        public List<CBufferDescriptor> CBufferDescriptors { get; }
        public List<TextureDescriptor> TextureDescriptors { get; }

        private StringBuilder _sb;

        private Dictionary<AstOperand, string> _locals;

        private int _level;

        private string _identation;

        public CodeGenContext(ShaderConfig config)
        {
            Config = config;

            CBufferDescriptors = new List<CBufferDescriptor>();
            TextureDescriptors = new List<TextureDescriptor>();

            _sb = new StringBuilder();

            _locals = new Dictionary<AstOperand, string>();
        }

        public void AppendLine()
        {
            _sb.AppendLine();
        }

        public void AppendLine(string str)
        {
            _sb.AppendLine(_identation + str);
        }

        public string GetCode()
        {
            return _sb.ToString();
        }

        public void EnterScope()
        {
            AppendLine("{");

            _level++;

            UpdateIdentation();
        }

        public void LeaveScope(string suffix = "")
        {
            if (_level == 0)
            {
                return;
            }

            _level--;

            UpdateIdentation();

            AppendLine("}" + suffix);
        }

        private void UpdateIdentation()
        {
            _identation = GetIdentation(_level);
        }

        private static string GetIdentation(int level)
        {
            string identation = string.Empty;

            for (int index = 0; index < level; index++)
            {
                identation += Tab;
            }

            return identation;
        }

        public string DeclareLocal(AstOperand operand)
        {
            string name = $"temp_{_locals.Count}";

            _locals.Add(operand, name);

            return name;
        }

        public string GetLocalName(AstOperand operand)
        {
            return _locals[operand];
        }
    }
}