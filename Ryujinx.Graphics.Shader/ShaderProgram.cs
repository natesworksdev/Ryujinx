using System;

namespace Ryujinx.Graphics.Shader
{
    public class ShaderProgram
    {
        public ShaderProgramInfo Info { get; }

        public string Code { get; private set; }
        public byte[] BinaryCode { get; }

        private ShaderProgram(ShaderProgramInfo info)
        {
            Info = info;
        }

        public ShaderProgram(ShaderProgramInfo info, string code) : this(info)
        {
            Code = code;
        }

        public ShaderProgram(ShaderProgramInfo info, byte[] binaryCode) : this(info)
        {
            BinaryCode = binaryCode;
        }

        public void Prepend(string line)
        {
            Code = line + Environment.NewLine + Code;
        }
    }
}