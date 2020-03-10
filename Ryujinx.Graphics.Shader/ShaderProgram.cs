using System;

namespace Ryujinx.Graphics.Shader
{
    public class ShaderProgram
    {
        public ShaderProgramInfo Info { get; }

        public string Code { get; private set; }

        internal ShaderProgram(ShaderProgramInfo info, string code)
        {
            Info = info;
            Code = code;
        }

        public void Prepend(string line)
        {
            Code = line + Environment.NewLine + Code;
        }

        public void Replace(string name, string value)
        {
            Code = Code.Replace(name, value);
        }
    }
}