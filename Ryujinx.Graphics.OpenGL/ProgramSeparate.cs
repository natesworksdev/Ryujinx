using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.CodeGen.Glsl;
using System;
using System.Buffers.Binary;

namespace Ryujinx.Graphics.OpenGL
{
    class ProgramSeparate : IProgram
    {
        public int Handle { get; private set; }

        private string _code;

        public bool IsLinked
        {
            get
            {
                if (_status == ProgramLinkStatus.Incomplete)
                {
                    CheckProgramLink(true);
                }

                return _status == ProgramLinkStatus.Success;
            }
        }

        private ProgramLinkStatus _status = ProgramLinkStatus.Incomplete;

        public ProgramSeparate(ShaderStage stage, string code)
        {
            _code = code;
            Handle = GL.CreateShaderProgram(stage.Convert(), 1, new string[] { code });
        }

        public void Bind()
        {
            GL.UseProgram(Handle);
        }

        public ProgramLinkStatus CheckProgramLink(bool blocking)
        {
            if (!blocking && HwCapabilities.SupportsParallelShaderCompile)
            {
                GL.GetProgram(Handle, (GetProgramParameterName)ArbParallelShaderCompile.CompletionStatusArb, out int completed);

                if (completed == 0)
                {
                    return ProgramLinkStatus.Incomplete;
                }
            }

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int status);

            if (status == 0)
            {
                Console.WriteLine(_code);
                // throw new Exception(GL.GetProgramInfoLog(Handle));
                // Use GL.GetProgramInfoLog(Handle), it may be too long to print on the log.
                _status = ProgramLinkStatus.Failure;
                Logger.Debug?.Print(LogClass.Gpu, "Shader linking failed.");
            }
            else
            {
                _status = ProgramLinkStatus.Success;
            }

            return _status;
        }

        public byte[] GetBinary()
        {
            GL.GetProgram(Handle, (GetProgramParameterName)All.ProgramBinaryLength, out int size);

            byte[] data = new byte[size + 4];

            GL.GetProgramBinary(Handle, size, out _, out BinaryFormat binFormat, data);

            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan().Slice(size, 4), (int)binFormat);

            return data;
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                GL.DeleteProgram(Handle);

                Handle = 0;
            }
        }
    }
}
